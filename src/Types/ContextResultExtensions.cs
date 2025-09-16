namespace Michelangelo.Types;

public static class ContextResultExtensions
{
    public static async Task<ContextResult<TContext>> RunJob<TContext>(this TContext ctx, IJob job, CancellationToken ct = default)
        where TContext : Context
    {
        var diag = new DiagnosticsLog();
        diag.Info($"Running job {job.GetType().Name}");
        try
        {
            await job.RunAsync(ct).ConfigureAwait(false);
            diag.Info($"Job {job.GetType().Name} succeeded");
            return ContextResult<TContext>.FromSuccess(ctx, diag);
        }
        catch (Exception ex)
        {
            return ContextResult<TContext>.FromError(ctx, ex, diag);
        }
    }

    public static async Task<ContextResult<TNext>> ThenJob<TCurrent, TNext>(this Task<ContextResult<TCurrent>> previousTask, Func<TCurrent, IJob> jobFactory, Func<TCurrent, TNext> contextProjector, CancellationToken ct = default)
        where TCurrent : Context
        where TNext : Context
    {
        var prev = await previousTask.ConfigureAwait(false);
        if (!prev.Success)
        {
            return ContextResult<TNext>.FromError(contextProjector(prev.Context), prev.Error ?? new InvalidOperationException("Previous job failed"), prev.Diagnostics);
        }
        var nextCtx = contextProjector(prev.Context);
        var job = jobFactory(prev.Context);
        var result = await nextCtx.RunJob(job, ct).ConfigureAwait(false);
        // Merge diagnostics
        if (prev.Diagnostics != null && result.Diagnostics != null)
        {
            var merged = new DiagnosticsLog();
            merged.Append(prev.Diagnostics);
            merged.Append(result.Diagnostics);
            return new ContextResult<TNext>
            {
                Context = result.Context,
                Success = result.Success,
                Error = result.Error,
                CompletedUtc = result.CompletedUtc,
                Diagnostics = merged
            };
        }
        if (result.Diagnostics == null) return new ContextResult<TNext>
        {
            Context = result.Context,
            Success = result.Success,
            Error = result.Error,
            CompletedUtc = result.CompletedUtc,
            Diagnostics = prev.Diagnostics
        };
        return result;
    }
}