using System;
using System.Threading.Tasks;
using Michelangelo.Types;
using Xunit;

namespace Core.Tests;

public class DummyContext : Context { }

public class ContextResultTests
{
    [Fact]
    public void BindSuccessChainsDiagnostics()
    {
        var ctx = new DummyContext();
        var log = new DiagnosticsLog();
        log.Info("start");
        var r1 = ContextResult<DummyContext>.FromSuccess(ctx, log);
        var r2 = r1.Then(c => ContextResult<DummyContext>.FromSuccess(c, new DiagnosticsLog()));
        Assert.True(r2.Success);
    }

    [Fact]
    public async Task BindAsyncSuccessChains()
    {
        var ctx = new DummyContext();
        var r1 = ContextResult<DummyContext>.FromSuccess(ctx, new DiagnosticsLog());
        var r2 = await r1.BindAsync(async c =>
        {
            await Task.Delay(10);
            return ContextResult<DummyContext>.FromSuccess(c, new DiagnosticsLog());
        });
        Assert.True(r2.Success);
    }

    [Fact]
    public async Task BindAsyncFailureShortCircuits()
    {
        var ctx = new DummyContext();
        var ex = new InvalidOperationException("fail");
        var r1 = ContextResult<DummyContext>.FromError(ctx, ex, new DiagnosticsLog());
        var r2 = await r1.BindAsync(async c =>
        {
            await Task.Delay(10);
            return ContextResult<DummyContext>.FromSuccess(c, new DiagnosticsLog());
        });
        Assert.False(r2.Success);
        Assert.Equal(ex.Message, r2.Error?.Message);
    }

    [Fact]
    public async Task ThenAsyncDelegatesToBindAsync()
    {
        var ctx = new DummyContext();
        var r1 = ContextResult<DummyContext>.FromSuccess(ctx, new DiagnosticsLog());
        var r2 = await r1.ThenAsync(async c =>
        {
            await Task.Delay(5);
            return ContextResult<DummyContext>.FromSuccess(c, new DiagnosticsLog());
        });
        Assert.True(r2.Success);
    }
}
