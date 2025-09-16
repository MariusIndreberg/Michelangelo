namespace Michelangelo.Types;

public static class ContextResultExtensions
{
    // Start a pipeline with an initial context job (input == output context type for first step)
    public static Task<ContextResult<TContext>> RunContextJob<TContext>(this TContext ctx, IContextJob<TContext, TContext> job, CancellationToken ct = default)
        where TContext : Context => job.RunAsync(ctx, ct);

    // Start a pipeline with an initial context job that changes the context type
    public static Task<ContextResult<TOut>> RunContextJob<TIn, TOut>(this TIn ctx, IContextJob<TIn, TOut> job, CancellationToken ct = default)
        where TIn : Context
        where TOut : Context => job.RunAsync(ctx, ct);

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
}