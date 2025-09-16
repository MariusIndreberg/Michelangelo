using Michelangelo.Types;

namespace Core.Jobs;

public sealed class AnalyzeJob : IContextJob<CorePipelineContext, CorePipelineContext>
{
    public Task<ContextResult<CorePipelineContext>> RunAsync(CorePipelineContext context, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}