using System; using System.Threading; using System.Threading.Tasks; using Michelangelo.Types; using Xunit;

namespace Core.Tests;

// Homo step uses Context base to exercise fast path case pattern.
class BaseCtx : Context { public int Value { get; set; } }
class HomoJob : IContextJob<BaseCtx, BaseCtx>
{ public Task<ContextResult<BaseCtx>> RunAsync(BaseCtx context, CancellationToken cancellationToken) { context.Value++; var d = new DiagnosticsLog(); d.Info("HomoJob"); return Task.FromResult(ContextResult<BaseCtx>.FromSuccess(context, d)); } }

public class AdditionalPipelineTests
{
    [Fact]
    public async Task HomoStep_FastPath()
    {
        // Build pipeline manually containing a homo step typed exactly as Context->Context via wrapper
        var pipeline = Pipeline.Start<BaseCtx>()
            .ThenJob<BaseCtx, HomoJob>()
            .Build();
        var res = await pipeline.ExecuteAsync(new BaseCtx { Value = 1 });
        Assert.True(res.Success);
        Assert.Equal(2, res.Context.Value);
        Assert.Contains(res.Diagnostics!.Entries, e => e.Message.Contains("HomoJob"));
    }

    [Fact]
    public void ContextResult_Bind_ExceptionWrappedInResult()
    {
        var ctx = new BaseCtx { Value = 10 };
        var r1 = ContextResult<BaseCtx>.FromSuccess(ctx);
        var r2 = r1.Bind<BaseCtx>(_ => throw new InvalidOperationException("bind throw"));
        Assert.False(r2.Success);
        Assert.NotNull(r2.Error);
        Assert.Contains("bind throw", r2.Error!.Message);
    }

    [Fact]
    public async Task ContextResult_BindAsync_ThrowsCaptured()
    {
        var ctx = new BaseCtx { Value = 20 };
        var r1 = ContextResult<BaseCtx>.FromSuccess(ctx);
        var r2 = await r1.BindAsync<BaseCtx>(async _ => { await Task.Delay(1); throw new InvalidOperationException("bind async throw"); });
        Assert.False(r2.Success);
        Assert.NotNull(r2.Error);
        Assert.Contains("bind async throw", r2.Error!.Message);
    }
}
