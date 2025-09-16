using System.Diagnostics;
using Michelangelo.Types;

namespace Core;

public class CloneJob : IJob
{
    private readonly CorePipelineContext _ctx;
    public CloneJob(CorePipelineContext ctx) => _ctx = ctx;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"[CloneJob] Preparing clone target {_ctx.WorkingDirectory}");
        await EnsureCleanDirectoryAsync(_ctx.WorkingDirectory, cancellationToken);
        Console.WriteLine($"[CloneJob] Cloning {_ctx.RepositoryUrl} into {_ctx.WorkingDirectory}");
        try
        {
            await RunGitAsync(["clone", "--depth", "1", _ctx.RepositoryUrl, _ctx.WorkingDirectory], cancellationToken);
            _ctx.CloneSucceeded = true;
            _ctx.CloneCompletedAt = DateTime.UtcNow;
            _ctx.Info("Clone succeeded");
            Console.WriteLine("[CloneJob] Clone complete");
        }
        catch (Exception ex)
        {
            _ctx.Error("Clone failed: " + ex.Message);
            throw;
        }
    }

    private static async Task EnsureCleanDirectoryAsync(string path, CancellationToken ct)
    {
        if (Directory.Exists(path))
        {
            const int maxAttempts = 5;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    Directory.Delete(path, recursive: true);
                }
                catch (IOException) { /* swallow and retry */ }
                catch (UnauthorizedAccessException) { /* swallow and retry */ }
                if (!Directory.Exists(path)) break;
                await Task.Delay(150 * attempt, ct).ConfigureAwait(false);
            }
            if (Directory.Exists(path))
            {
                throw new IOException($"Failed to clean existing directory '{path}' after retries");
            }
        }
        Directory.CreateDirectory(path);
    }

    private static async Task RunGitAsync(IEnumerable<string> args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine("[git] " + e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine("[git][err] " + e.Data); };
        proc.Exited += (_, _) => tcs.TrySetResult(proc.ExitCode);
        if (!proc.Start()) throw new InvalidOperationException("Failed to start git process");
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        using (ct.Register(() => { try { if (!proc.HasExited) proc.Kill(true); } catch { } }))
        {
            var exit = await tcs.Task.ConfigureAwait(false);
            if (exit != 0) throw new InvalidOperationException($"git exited with code {exit}");
        }
    }
}
