using Michelangelo.Types;

namespace Core.Jobs;

public class CleanupJob : IContextJob<CorePipelineContext, CorePipelineContext>
{
    public async Task<ContextResult<CorePipelineContext>> RunAsync(CorePipelineContext ctx, CancellationToken cancellationToken)
    {
        var diag = new DiagnosticsLog();
        diag.Info("CleanupJob started");
        if (ctx.KeepWorkingDirectory)
        {
            var msg = $"[CleanupJob] Skipping deletion of {ctx.WorkingDirectory}";
            Console.WriteLine(msg);
            ctx.Info(msg);
            diag.Info("Skipped deletion per flag");
            return ContextResult<CorePipelineContext>.FromSuccess(ctx, diag);
        }
        if (!Directory.Exists(ctx.WorkingDirectory))
        {
            diag.Warn("Working directory already missing; nothing to delete");
            return ContextResult<CorePipelineContext>.FromSuccess(ctx, diag);
        }
        Console.WriteLine($"[CleanupJob] Deleting {ctx.WorkingDirectory}");
        var success = await TryDeleteWithRetriesAsync(ctx.WorkingDirectory, 5, cancellationToken).ConfigureAwait(false);
        if (success)
        {
            ctx.Info("Cleanup deleted working directory");
            diag.Info("Deletion succeeded");
            return ContextResult<CorePipelineContext>.FromSuccess(ctx, diag);
        }
        else
        {
            var msg = "[CleanupJob][Warn] Gave up deleting directory after retries";
            Console.WriteLine(msg);
            ctx.Warn(msg);
            diag.Warn(msg);
            return ContextResult<CorePipelineContext>.FromSuccess(ctx, diag); // treat as non-fatal
        }
    }

    private static async Task<bool> TryDeleteWithRetriesAsync(string path, int attempts, CancellationToken ct)
    {
        for (int i = 1; i <= attempts; i++)
        {
            try
            {
                if (!Directory.Exists(path)) return true;
                ClearAttributes(path);
                Directory.Delete(path, recursive: true);
                if (!Directory.Exists(path)) return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // swallow and retry
            }
            await Task.Delay(150 * i, ct).ConfigureAwait(false);
        }
        return !Directory.Exists(path);
    }

    private static void ClearAttributes(string root)
    {
        if (!Directory.Exists(root)) return;
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            try
            {
                var attr = File.GetAttributes(file);
                if ((attr & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(file, attr & ~FileAttributes.ReadOnly);
                }
            }
            catch { }
        }
        foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
        {
            try
            {
                var attr = File.GetAttributes(dir);
                if ((attr & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(dir, attr & ~FileAttributes.ReadOnly);
                }
            }
            catch { }
        }
    }
}
