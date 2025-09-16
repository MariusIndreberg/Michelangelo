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

class RecordingJob : IJob
{
    private readonly Action _action;
    public RecordingJob(Action action) => _action = action;
    public Task RunAsync(CancellationToken cancellationToken)
    {
        _action();
        return Task.CompletedTask;
    }
}

class FailingJob : IJob
{
    private readonly Action _beforeThrow;
    public FailingJob(Action beforeThrow) => _beforeThrow = beforeThrow;
    public Task RunAsync(CancellationToken cancellationToken)
    {
        _beforeThrow();
        throw new InvalidOperationException("boom");
    }
}

public class JobPipelineTests
{
    [Fact]
    public async Task FailureShortCircuitsSecondJob()
    {
        var ctx = new FlagContext();
        var first = new FailingJob(() => ctx.AExecuted = true);
        var second = new RecordingJob(() => ctx.BExecuted = true);

        var r = await ctx
            .RunJob(first)
            .ThenJob(_ => second, c => c);

        Assert.False(r.Success);
        Assert.True(ctx.AExecuted);
        Assert.False(ctx.BExecuted);
    }
}
