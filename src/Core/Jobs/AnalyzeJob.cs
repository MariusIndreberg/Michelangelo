using Michelangelo.Types;
using System.IO;

namespace Core.Jobs;

public sealed class AnalyzeJob : IContextJob<CorePipelineContext, CorePipelineContext>
{
    public async Task<ContextResult<CorePipelineContext>> RunAsync(CorePipelineContext context, CancellationToken cancellationToken)
    {
        var diag = new DiagnosticsLog();
        diag.Info("AnalyzeJob starting endpoint discovery");
        try
        {
            if (string.IsNullOrWhiteSpace(context.WorkingDirectory) || !Directory.Exists(context.WorkingDirectory))
            {
                diag.Warn("Working directory missing; skipping endpoint analysis");
                return ContextResult<CorePipelineContext>.FromSuccess(context, diag);
            }

            var tool = new Tools.SearchRepoTool();
            var endpoints = await tool.FindEndpointsAsync(context.WorkingDirectory, cancellationToken).ConfigureAwait(false);
            diag.Info($"AnalyzeJob discovered {endpoints.Count} endpoint(s)");
            foreach (var e in endpoints.Take(5))
            {
                diag.Info($"Endpoint: {e.HttpMethod} {e.Route} ({Path.GetFileName(e.SourceFile)})");
            }
            return ContextResult<CorePipelineContext>.FromSuccess(context, diag);
        }
        catch (Exception ex)
        {
            diag.Error("AnalyzeJob failed: " + ex.Message);
            return ContextResult<CorePipelineContext>.FromError(context, ex, diag);
        }
    }
}