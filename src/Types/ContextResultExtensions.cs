namespace Michelangelo.Types;

public static class ContextResultExtensions
{
    // Overload: instantiate job that changes context type
    public static Task<ContextResult<TOut>> RunContextJob<TIn, TOut, TJob>(this TIn ctx, CancellationToken ct = default)
        where TIn : Context
        where TOut : Context
        where TJob : IContextJob<TIn, TOut>, new() => new TJob().RunAsync(ctx, ct);

    // Context -> Context job chaining (jobs that themselves produce a new ContextResult<TOut>)
    public static async Task<ContextResult<TOut>> ThenContextJob<TIn, TOut>(this Task<ContextResult<TIn>> prevTask, Func<TIn, IContextJob<TIn, TOut>> jobFactory, CancellationToken ct = default)
        where TIn : Context
        where TOut : Context
    {
        var prev = await prevTask.ConfigureAwait(false);
        if (!prev.Success)
        {
            return ContextResult<TOut>.FromError((TOut)(object)prev.Context, prev.Error ?? new InvalidOperationException("Previous context step failed"), prev.Diagnostics);
        }
        var job = jobFactory(prev.Context);
        ContextResult<TOut> next;
        try
        {
            next = await job.RunAsync(prev.Context, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ContextResult<TOut>.FromError((TOut)(object)prev.Context, ex, prev.Diagnostics);
        }
        // Merge diagnostics
        if (prev.Diagnostics != null && next.Diagnostics != null)
        {
            var merged = new DiagnosticsLog();
            merged.Append(prev.Diagnostics);
            merged.Append(next.Diagnostics);
            next = new ContextResult<TOut>
            {
                Context = next.Context,
                Success = next.Success,
                Error = next.Error,
                CompletedUtc = next.CompletedUtc,
                Diagnostics = merged
            };
        }
        else if (prev.Diagnostics != null && next.Diagnostics == null)
        {
            next = new ContextResult<TOut>
            {
                Context = next.Context,
                Success = next.Success,
                Error = next.Error,
                CompletedUtc = next.CompletedUtc,
                Diagnostics = prev.Diagnostics
            };
        }
    return next;
    }

    // Overload: ThenContextJob specifying only job type (TIn -> TOut)
    public static Task<ContextResult<TOut>> ThenContextJob<TIn, TOut, TJob>(this Task<ContextResult<TIn>> prevTask, CancellationToken ct = default)
        where TIn : Context
        where TOut : Context
        where TJob : IContextJob<TIn, TOut>, new() => prevTask.ThenContextJob(_ => new TJob(), ct);

    // Overload: same-type context job by job type
    public static Task<ContextResult<TContext>> ThenContextJob<TContext, TJob>(this Task<ContextResult<TContext>> prevTask, CancellationToken ct = default)
        where TContext : Context
        where TJob : IContextJob<TContext, TContext>, new() => prevTask.ThenContextJob<TContext, TContext, TJob>(ct);
}