using Core.Contexts;
using Core.Jobs;
using Michelangelo.Types;

namespace Core.Pipelines;

public static class AnalyzePipeline
{
    public static IPipeline<FileSearchContext, FileSearchContext> Build()
        => Pipeline.Start<FileSearchContext>()
            .Build();
}