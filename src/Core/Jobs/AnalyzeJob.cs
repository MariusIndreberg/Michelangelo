using Michelangelo.Types;

namespace Core.Jobs;

public sealed class AnalyzeJob : IContextJob<CorePipelineContext, CorePipelineContext>
{
    public Task<ContextResult<CorePipelineContext>> RunAsync(CorePipelineContext context, CancellationToken cancellationToken)
    {
        // Placeholder no-op analysis for now
        var diag = new DiagnosticsLog();
        diag.Info("AnalyzeJob (no-op)");
        return Task.FromResult(ContextResult<CorePipelineContext>.FromSuccess(context, diag));
    }
}