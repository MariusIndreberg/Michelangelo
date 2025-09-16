using System;
using System.Threading;
using System.Threading.Tasks;
using Michelangelo.Types;
using Core;
using Xunit;

namespace Core.Tests;

class FlagContext : Context
{
    public bool AExecuted { get; set; }
    public bool BExecuted { get; set; }
}

class RecordingContextJob<TCtx> : IContextJob<TCtx, TCtx> where TCtx : FlagContext
{
    private readonly Action<TCtx> _action;
    public RecordingContextJob(Action<TCtx> action) => _action = action;
    public Task<ContextResult<TCtx>> RunAsync(TCtx context, CancellationToken cancellationToken)
    {
        _action(context);
        var diag = new DiagnosticsLog();
        diag.Info("RecordingContextJob executed");
        return Task.FromResult(ContextResult<TCtx>.FromSuccess(context, diag));
    }
}

class FailingContextJob<TCtx> : IContextJob<TCtx, TCtx> where TCtx : FlagContext
{
    private readonly Action<TCtx> _before;
    public FailingContextJob(Action<TCtx> before) => _before = before;
    public Task<ContextResult<TCtx>> RunAsync(TCtx context, CancellationToken cancellationToken)
    {
        _before(context);
        var ex = new InvalidOperationException("boom");
        return Task.FromResult(ContextResult<TCtx>.FromError(context, ex));
    }
}

public class JobPipelineTests
{
    [Fact]
    public async Task FailureShortCircuitsSecondJob()
    {
        var ctx = new FlagContext();
        var r = await ctx
            .RunContextJob<FlagContext>(new FailingContextJob<FlagContext>(c => c.AExecuted = true))
            .ThenContextJob(c => new RecordingContextJob<FlagContext>(c2 => c2.BExecuted = true));

    Assert.False(r.Success);
        Assert.True(ctx.AExecuted);
        Assert.False(ctx.BExecuted);
    }
}
