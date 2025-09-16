using Michelangelo.Types;

namespace Core;

public class CleanupJob : IJob
{
    private readonly CorePipelineContext _ctx;
    private readonly bool _skip;
    public CleanupJob(CorePipelineContext ctx, bool skipDeletion = false)
    {
        _ctx = ctx;
        _skip = skipDeletion;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (_skip)
        {
            var msg = $"[CleanupJob] Skipping deletion of {_ctx.WorkingDirectory}";
            Console.WriteLine(msg);
            _ctx.Info(msg);
            return;
        }
        if (!Directory.Exists(_ctx.WorkingDirectory)) return;
        Console.WriteLine($"[CleanupJob] Deleting {_ctx.WorkingDirectory}");
        var success = await TryDeleteWithRetriesAsync(_ctx.WorkingDirectory, 5, cancellationToken).ConfigureAwait(false);
        if (success)
        {
            _ctx.Info("Cleanup deleted working directory");
        }
        else
        {
            var msg = "[CleanupJob][Warn] Gave up deleting directory after retries";
            Console.WriteLine(msg);
            _ctx.Warn(msg);
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
