using System;
using Michelangelo.Types;
using Core.Jobs;

namespace Core.Pipelines;

public static class MainPipeline
{
    /// <summary>
    /// Build the pipeline (jobs are defined here).
    /// </summary>
    public static IPipeline<CorePipelineContext, CorePipelineContext> Build()
    {
        return Pipeline.Start<CorePipelineContext>()
            .ThenJob<CorePipelineContext, CloneJob>()
            .ThenJob<CorePipelineContext, CleanupJob>()
            .Build();
    }

    /// <summary>
    /// High-level entry point: create context, execute pipeline, print summary. Returns process exit code (0/1).
    /// </summary>
    public static async Task<int> RunAsync(string repoUrl, bool keepWorkingDir, CancellationToken ct = default)
    {
        var context = CreateContext(repoUrl, keepWorkingDir);
        Console.WriteLine("[Runner] Executing pipeline...");
        var pipeline = Build();
        var result = await pipeline.ExecuteAsync(context, ct);
        PrintSummary(context, result);
        return result.Success ? 0 : 1;
    }

    /// <summary>
    /// Create a new pipeline context with a unique working directory under project-local tmp.
    /// </summary>
    public static CorePipelineContext CreateContext(string repoUrl, bool keepWorkingDir)
    {
        var projectRoot = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!; // from bin/Debug/netX.Y
        var tmpRoot = Path.Combine(projectRoot.FullName, "tmp");
        Directory.CreateDirectory(tmpRoot);
        var baseName = SafeRepoFolder(repoUrl);
        var workDir = UniqueWorkingDirectory(tmpRoot, baseName);
        return new CorePipelineContext
        {
            RepositoryUrl = repoUrl,
            WorkingDirectory = workDir,
            KeepWorkingDirectory = keepWorkingDir
        };
    }

    private static string SafeRepoFolder(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.Segments.Last().TrimEnd('/');
            if (segments.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) segments = segments[..^4];
            var hostPart = uri.Host.Replace('.', '_');
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(url)))[..8];
            return $"{hostPart}_{segments}_{hash}";
        }
        catch
        {
            return "repo_" + Guid.NewGuid().ToString("N");
        }
    }

    private static string UniqueWorkingDirectory(string tmpRoot, string baseName)
    {
        var basePath = Path.Combine(tmpRoot, baseName);
        if (!Directory.Exists(basePath)) return basePath;
        int i = 1;
        while (true)
        {
            var candidate = basePath + "_" + i.ToString("D2");
            if (!Directory.Exists(candidate)) return candidate;
            i++;
        }
    }

    private static void PrintSummary(CorePipelineContext ctx, ContextResult<CorePipelineContext> result)
    {
        if (!result.Success)
            Console.WriteLine("[Runner] Pipeline failed: " + (result.Error?.Message ?? "unknown error"));
        else
            Console.WriteLine("[Runner] Pipeline succeeded.");

        Console.WriteLine();
        Console.WriteLine("=== Pipeline Summary ===");
        Console.WriteLine($"Repository: {ctx.RepositoryUrl}");
        Console.WriteLine($"Working Directory: {ctx.WorkingDirectory}");
        Console.WriteLine($"Clone Succeeded: {ctx.CloneSucceeded}");
        Console.WriteLine($"Clone Completed (UTC): {ctx.CloneCompletedAt?.ToString("O") ?? "<none>"}");
        var entries = ctx.Diagnostics.Entries;
        if (entries.Count > 0)
        {
            Console.WriteLine("Diagnostics:");
            foreach (var e in entries)
                Console.WriteLine(" - " + e.ToString());
        }
        else
        {
            Console.WriteLine("No diagnostics recorded.");
        }
    }
}
