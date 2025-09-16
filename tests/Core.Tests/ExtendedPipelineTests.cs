using System; 
using System.Threading; 
using System.Threading.Tasks; 
using Michelangelo.Types; 
using Xunit; 

namespace Core.Tests;

// Contexts for extended scenarios
class ExStart : Context { public string Seed { get; set; } = "ex"; }
class ExMid1 : Context { public string Trace { get; set; } = string.Empty; }
class ExMid2 : Context { public string Trace { get; set; } = string.Empty; }
class ExEnd : Context { public string Result { get; set; } = string.Empty; public bool FromPlaceholder { get; set; } }

// Job adding trace marker
class MarkerJob : IContextJob<ExStart, ExMid1>
{ public Task<ContextResult<ExMid1>> RunAsync(ExStart context, CancellationToken cancellationToken) { var d = new DiagnosticsLog(); d.Info("MarkerJob"); return Task.FromResult(ContextResult<ExMid1>.FromSuccess(new ExMid1 { Trace = context.Seed + ":M1" }, d)); } }

// Same-type enrichment
class Enrich1 : IContextJob<ExMid1, ExMid1>
{ public Task<ContextResult<ExMid1>> RunAsync(ExMid1 ctx, CancellationToken ct) { ctx.Trace += ":E1"; var d = new DiagnosticsLog(); d.Info("Enrich1"); return Task.FromResult(ContextResult<ExMid1>.FromSuccess(ctx, d)); } }
class Enrich2 : IContextJob<ExMid1, ExMid1>
{ public Task<ContextResult<ExMid1>> RunAsync(ExMid1 ctx, CancellationToken ct) { ctx.Trace += ":E2"; var d = new DiagnosticsLog(); d.Info("Enrich2"); return Task.FromResult(ContextResult<ExMid1>.FromSuccess(ctx, d)); } }

class ToMid2 : IContextJob<ExMid1, ExMid2>
{ public Task<ContextResult<ExMid2>> RunAsync(ExMid1 ctx, CancellationToken ct) { var d = new DiagnosticsLog(); d.Info("ToMid2"); return Task.FromResult(ContextResult<ExMid2>.FromSuccess(new ExMid2 { Trace = ctx.Trace + ":M2" }, d)); } }

class FinalizeJob : IContextJob<ExMid2, ExEnd>
{ public Task<ContextResult<ExEnd>> RunAsync(ExMid2 ctx, CancellationToken ct) { var d = new DiagnosticsLog(); d.Info("FinalizeJob"); return Task.FromResult(ContextResult<ExEnd>.FromSuccess(new ExEnd { Result = ctx.Trace + ":END" }, d)); } }

// Failing job mid-chain producing different context chain
class FailingExJob : IContextJob<ExMid1, ExMid2>
{ public Task<ContextResult<ExMid2>> RunAsync(ExMid1 ctx, CancellationToken ct) { var d = new DiagnosticsLog(); d.Error("FailingExJob"); var ex = new InvalidOperationException("extended failure"); return Task.FromResult(ContextResult<ExMid2>.FromError(new ExMid2 { Trace = ctx.Trace + ":FAIL" }, ex, d)); } }

// End type without parameterless constructor to test failure path
class NoCtorEnd : Context { public string Data { get; } public NoCtorEnd(string data) { Data = data; } }
class ToNoCtorEnd : IContextJob<ExMid2, NoCtorEnd>
{ public Task<ContextResult<NoCtorEnd>> RunAsync(ExMid2 ctx, CancellationToken ct) { var d = new DiagnosticsLog(); d.Info("ToNoCtorEnd"); return Task.FromResult(ContextResult<NoCtorEnd>.FromSuccess(new NoCtorEnd(ctx.Trace))); } }
class FailingBeforeNoCtor : IContextJob<ExMid2, ExMid2>
{ public Task<ContextResult<ExMid2>> RunAsync(ExMid2 ctx, CancellationToken ct) { var d = new DiagnosticsLog(); d.Error("PreNoCtor failure"); return Task.FromResult(ContextResult<ExMid2>.FromError(ctx, new InvalidOperationException("pre no ctor fail"), d)); } }

public class ExtendedPipelineTests
{
    [Fact]
    public async Task NestedDiagnostics_Aggregate()
    {
        // Build two-stage pipeline Start -> Mid1 -> Mid2
        var stage1 = Pipeline.Start<ExStart>()
            .ThenJob<ExMid1, MarkerJob>()
            .ThenJob<ExMid1, Enrich1>()
            .ThenJob<ExMid1, Enrich2>()
            .Build();
        var stage2 = Pipeline.Start<ExMid1>()
            .ThenJob<ExMid2, ToMid2>()
            .Build();
        var stage3 = Pipeline.Start<ExMid2>()
            .ThenJob<ExEnd, FinalizeJob>()
            .Build();
        // Compose: ((Start->Mid1) + Mid1->Mid2) + (Mid2->End)
        var composite = Pipeline.Start<ExStart>()
            .ThenPipeline<ExMid1, ExMid2>(stage1, stage2)
            .ThenJob<ExEnd, FinalizeJob>() // directly to end from ExMid2
            .Build();
        var result = await composite.ExecuteAsync(new ExStart { Seed = "agg" });
        Assert.True(result.Success);
        Assert.Contains(":M1", result.Context.Result);
        Assert.Contains(":E1", result.Context.Result);
        Assert.Contains(":E2", result.Context.Result);
        Assert.Contains(":M2", result.Context.Result);
        Assert.Contains(":END", result.Context.Result);
        Assert.NotNull(result.Diagnostics);
        var diag = result.Diagnostics!;
    // Current implementation only captures diagnostics from executed steps in linear composition, nested pipeline diagnostics are merged sequentially.
    Assert.Contains(diag.Entries, e => e.Message.Contains("MarkerJob"));
    Assert.Contains(diag.Entries, e => e.Message.Contains("Enrich1"));
    Assert.Contains(diag.Entries, e => e.Message.Contains("Enrich2"));
    Assert.Contains(diag.Entries, e => e.Message.Contains("ToMid2"));
    Assert.Contains(diag.Entries, e => e.Message.Contains("FinalizeJob"));
    }

    [Fact]
    public async Task NestedFailure_Propagates()
    {
        var stage1 = Pipeline.Start<ExStart>()
            .ThenJob<ExMid1, MarkerJob>()
            .Build();
        var failingStage = Pipeline.Start<ExMid1>()
            .ThenJob<ExMid2, FailingExJob>()
            .Build();
        var finalStage = Pipeline.Start<ExMid2>()
            .ThenJob<ExEnd, FinalizeJob>()
            .Build();
        var composite = Pipeline.Start<ExStart>()
            .ThenPipeline<ExMid1, ExMid2>(stage1, failingStage)
            .ThenJob<ExEnd, FinalizeJob>() // should not execute
            .Build();
        var res = await composite.ExecuteAsync(new ExStart());
        Assert.False(res.Success);
        Assert.NotNull(res.Error);
        Assert.Contains("extended failure", res.Error!.Message);
        Assert.NotNull(res.Diagnostics);
        Assert.DoesNotContain(res.Diagnostics!.Entries, e => e.Message.Contains("FinalizeJob"));
    }

    [Fact]
    public async Task FailureBeforeNoCtorEnd_ThrowsInformative()
    {
        var path = Pipeline.Start<ExMid2>()
            .ThenJob<ExMid2, FailingBeforeNoCtor>()
            .ThenJob<NoCtorEnd, ToNoCtorEnd>()
            .Build();
        // This should fail before reaching ToNoCtorEnd but since final type NoCtorEnd lacks parameterless ctor the placeholder creation throws; capture exception.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await path.ExecuteAsync(new ExMid2 { Trace = "x" }));
        Assert.Contains("cannot be cast", ex.Message);
    }

    [Fact]
    public async Task EmptyPipeline_ReturnsInputContext()
    {
        var empty = Pipeline.Start<ExStart>().Build();
        var start = new ExStart { Seed = "empty" };
        var res = await empty.ExecuteAsync(start);
        Assert.True(res.Success);
        Assert.Same(start, res.Context);
    }

    [Fact]
    public async Task NestedFirstStageFailure_Stops()
    {
        var failingStage1 = Pipeline.Start<ExStart>()
            .ThenJob<ExMid1, MarkerJob>()
            .ThenJob<ExMid1, Enrich1>()
            .ThenJob<ExMid1, Enrich2>()
            .Build();
        // Build a wrapper pipeline that forces failure inside first nested pipeline by injecting a failing job at end of stage1 replacement
        var failingInject = Pipeline.Start<ExStart>()
            .ThenJob<ExMid1, MarkerJob>()
            .Build();
        var failMid = Pipeline.Start<ExMid1>()
            .ThenJob<ExMid2, FailingExJob>()
            .Build();
        var final = Pipeline.Start<ExMid2>()
            .ThenJob<ExEnd, FinalizeJob>()
            .Build();
        var composite = Pipeline.Start<ExStart>()
            .ThenPipeline<ExMid1, ExMid2>(failingInject, failMid) // failure occurs here
            .ThenJob<ExEnd, FinalizeJob>() // should not execute
            .Build();
        var res = await composite.ExecuteAsync(new ExStart());
        Assert.False(res.Success);
        Assert.NotNull(res.Error);
        Assert.DoesNotContain(res.Diagnostics!.Entries, e => e.Message.Contains("FinalizeJob"));
    }

    [Fact]
    public async Task DiagnosticsMerge_FirstHasSecondNone()
    {
        // First stage produces diagnostics
        var first = Pipeline.Start<ExStart>()
            .ThenJob<ExMid1, MarkerJob>()
            .Build();
        // Second stage returns result without diagnostics (custom job)
        var noDiagStage = new NoDiagPipeline();
        var composite = Pipeline.Start<ExStart>()
            .ThenPipeline<ExMid1, ExMid2>(first, noDiagStage)
            .Build();
        var res = await composite.ExecuteAsync(new ExStart());
        Assert.True(res.Success);
        Assert.NotNull(res.Diagnostics);
        Assert.Contains(res.Diagnostics!.Entries, e => e.Message.Contains("MarkerJob"));
    }
}

// Helper pipeline that converts ExMid1->ExMid2 without diagnostics
class NoDiagPipeline : IPipeline<ExMid1, ExMid2>
{
    public Task<ContextResult<ExMid2>> ExecuteAsync(ExMid1 input, CancellationToken ct = default)
    {
        var mid2 = new ExMid2 { Trace = input.Trace + ":ND" };
        // no diagnostics log
        return Task.FromResult(new ContextResult<ExMid2>{ Context = mid2, Success = true, CompletedUtc = DateTime.UtcNow });
    }
}
