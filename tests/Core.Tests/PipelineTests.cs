using System; 
using System.Threading; 
using System.Threading.Tasks; 
using Michelangelo.Types; 
using Xunit; 

namespace Core.Tests;

// Test contexts
class PStart : Context { public string Value { get; set; } = "S"; }
class PMid : Context { public string Acc { get; set; } = string.Empty; }
class PEnd : Context { public string Final { get; set; } = string.Empty; public int Length { get; set; } }

// Jobs
class AppendJob : IContextJob<PStart, PMid>
{ public Task<ContextResult<PMid>> RunAsync(PStart context, CancellationToken cancellationToken)
    { var mid = new PMid { Acc = context.Value + ":A" }; var diag = new DiagnosticsLog(); diag.Info("AppendJob"); return Task.FromResult(ContextResult<PMid>.FromSuccess(mid, diag)); } }

class UpperJob : IContextJob<PMid, PEnd>
{ public Task<ContextResult<PEnd>> RunAsync(PMid context, CancellationToken cancellationToken)
    { var end = new PEnd { Final = context.Acc.ToUpperInvariant(), Length = context.Acc.Length }; var diag = new DiagnosticsLog(); diag.Info("UpperJob"); return Task.FromResult(ContextResult<PEnd>.FromSuccess(end, diag)); } }

class FailingMidJob : IContextJob<PStart, PMid>
{ public Task<ContextResult<PMid>> RunAsync(PStart context, CancellationToken cancellationToken)
    { var ex = new InvalidOperationException("fail mid"); var diag = new DiagnosticsLog(); diag.Error("FailingMidJob triggered"); return Task.FromResult(ContextResult<PMid>.FromError(new PMid{ Acc = "ERR" }, ex, diag)); } }

class SameTypeA : IContextJob<PMid, PMid>
{ public Task<ContextResult<PMid>> RunAsync(PMid context, CancellationToken cancellationToken)
    { context.Acc += ":B"; var d = new DiagnosticsLog(); d.Info("SameTypeA"); return Task.FromResult(ContextResult<PMid>.FromSuccess(context, d)); } }

class SameTypeB : IContextJob<PMid, PMid>
{ public Task<ContextResult<PMid>> RunAsync(PMid context, CancellationToken cancellationToken)
    { context.Acc += ":C"; var d = new DiagnosticsLog(); d.Info("SameTypeB"); return Task.FromResult(ContextResult<PMid>.FromSuccess(context, d)); } }

public class PipelineTests
{
    [Fact]
    public async Task SimplePipeline_Succeeds()
    {
        var pipeline = Pipeline.Start<PStart>()
            .ThenJob<PMid, AppendJob>()
            .ThenJob<PEnd, UpperJob>()
            .Build();

        var start = new PStart { Value = "root" };
        var result = await pipeline.ExecuteAsync(start);
        Assert.True(result.Success);
        Assert.Equal("ROOT:A", result.Context.Final);
        Assert.Equal("root:A".Length, result.Context.Length);
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics!.Entries, e => e.Message.Contains("AppendJob"));
        Assert.Contains(result.Diagnostics!.Entries, e => e.Message.Contains("UpperJob"));
    }

    [Fact]
    public async Task FailurePipeline_StopsEarly()
    {
        var failingPipeline = Pipeline.Start<PStart>()
            .ThenJob<PMid, FailingMidJob>()
            .ThenJob<PEnd, UpperJob>() // should not run
            .Build();

        var res = await failingPipeline.ExecuteAsync(new PStart());
        Assert.False(res.Success);
        Assert.NotNull(res.Error);
        Assert.Contains("fail mid", res.Error!.Message);
        Assert.NotNull(res.Diagnostics);
        Assert.DoesNotContain(res.Diagnostics!.Entries, e => e.Message.Contains("UpperJob"));
    }

    [Fact]
    public async Task SameTypeJobs_Compose()
    {
        var pipeline = Pipeline.Start<PStart>()
            .ThenJob<PMid, AppendJob>()
            .ThenJob<PMid, SameTypeA>()
            .ThenJob<PMid, SameTypeB>()
            .ThenJob<PEnd, UpperJob>()
            .Build();
        var res = await pipeline.ExecuteAsync(new PStart { Value = "seed" });
        Assert.True(res.Success);
        Assert.Equal("SEED:A:B:C", res.Context.Final);
        Assert.Contains(res.Diagnostics!.Entries, e => e.Message.Contains("SameTypeA"));
        Assert.Contains(res.Diagnostics!.Entries, e => e.Message.Contains("SameTypeB"));
    }

    [Fact]
    public async Task NestedPipelines_Work()
    {
        // inner 1: PStart -> PMid
        var inner1 = Pipeline.Start<PStart>()
            .ThenJob<PMid, AppendJob>()
            .Build();
        // inner 2: PMid -> PEnd (with same-type modifications before final)
        var inner2 = Pipeline.Start<PMid>()
            .ThenJob<PMid, SameTypeA>()
            .ThenJob<PMid, SameTypeB>()
            .ThenJob<PEnd, UpperJob>()
            .Build();
        var outer = Pipeline.Start<PStart>()
            .ThenPipeline<PMid, PEnd>(inner1, inner2)
            .Build();

        var result = await outer.ExecuteAsync(new PStart { Value = "x" });
        Assert.True(result.Success);
        Assert.Equal("X:A:B:C", result.Context.Final);
    }
}
