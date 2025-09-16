using System.Diagnostics;
using Michelangelo.Types;

namespace Core;

public class CloneJob : IContextJob<CorePipelineContext, CorePipelineContext>
{
    public async Task<ContextResult<CorePipelineContext>> RunAsync(CorePipelineContext ctx, CancellationToken cancellationToken)
    {
        var diag = new DiagnosticsLog();
        diag.Info("CloneJob started");
        Console.WriteLine($"[CloneJob] Preparing clone target {ctx.WorkingDirectory}");
        try
        {
            await EnsureCleanDirectoryAsync(ctx.WorkingDirectory, cancellationToken);
            Console.WriteLine($"[CloneJob] Cloning {ctx.RepositoryUrl} into {ctx.WorkingDirectory}");
            await RunGitAsync(["clone", "--depth", "1", ctx.RepositoryUrl, ctx.WorkingDirectory], cancellationToken);
            ctx.CloneSucceeded = true;
            ctx.CloneCompletedAt = DateTime.UtcNow;
            ctx.Info("Clone succeeded");
            diag.Info("Clone completed successfully");
            return ContextResult<CorePipelineContext>.FromSuccess(ctx, diag);
        }
        catch (Exception ex)
        {
            ctx.Error("Clone failed: " + ex.Message);
            diag.Error("Clone failed: " + ex.Message);
            return ContextResult<CorePipelineContext>.FromError(ctx, ex, diag);
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
