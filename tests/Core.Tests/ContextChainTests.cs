using System;
using System.Threading;
using System.Threading.Tasks;
using Michelangelo.Types;
using Xunit;

namespace Core.Tests;

class StartContext : Context
{
    public string Seed { get; set; } = "start";
}

class MidContext : Context
{
    public string Combined { get; set; } = string.Empty;
}

class FinalContext : Context
{
    public int Length { get; set; }
    public string Upper { get; set; } = string.Empty;
}

class MidJob : IContextJob<StartContext, MidContext>
{
    public Task<ContextResult<MidContext>> RunAsync(StartContext context, CancellationToken cancellationToken)
    {
        var mid = new MidContext { Combined = context.Seed + ":mid" };
        var diag = new DiagnosticsLog();
        diag.Info("ToMid executed");
        return Task.FromResult(ContextResult<MidContext>.FromSuccess(mid, diag));
    }
}

class FinalJob : IContextJob<MidContext, FinalContext>
{
    public Task<ContextResult<FinalContext>> RunAsync(MidContext context, CancellationToken cancellationToken)
    {
        var final = new FinalContext { Upper = context.Combined.ToUpperInvariant(), Length = context.Combined.Length };
        var diag = new DiagnosticsLog();
        diag.Info("ToFinal executed");
        return Task.FromResult(ContextResult<FinalContext>.FromSuccess(final, diag));
    }
}

public class ContextChainTests
{
    [Fact]
    public async Task MultiContextPipeline_Succeeds()
    {
        var start = new StartContext { Seed = "alpha" };
        var result = await start
            .RunContextJob<StartContext, MidContext, MidJob>()
            .ThenContextJob<MidContext, FinalContext, FinalJob>();

        Assert.True(result.Success);
        Assert.Equal("ALPHA:MID", result.Context.Upper);
        Assert.Equal("alpha:mid".Length, result.Context.Length);
        Assert.NotNull(result.Diagnostics);
    }

}
