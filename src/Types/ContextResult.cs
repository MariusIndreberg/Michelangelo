namespace Michelangelo.Types;

/// <summary>
/// Represents the outcome of executing a pipeline (a sequence of IJob operations) using a Context.
/// Wraps the final context plus success indicators and diagnostics.
/// </summary>
/// <typeparam name="TContext">Concrete context type.</typeparam>
public sealed class ContextResult<TContext> where TContext : Context
{
    public required TContext Context { get; init; }
    public bool Success { get; init; }
    public Exception? Error { get; init; }
    public DateTime StartedUtc { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedUtc { get; init; }
    public TimeSpan? Duration => CompletedUtc is null ? null : CompletedUtc - StartedUtc;
    public DiagnosticsLog? Diagnostics { get; init; }

    private DiagnosticsLog EnsureDiagnostics()
    {
        if (Diagnostics != null) return Diagnostics;
        var log = new DiagnosticsLog();
        log.Info("(auto) diagnostics log created");
        return log;
    }

    public ContextResult<TNext> Bind<TNext>(Func<TContext, ContextResult<TNext>> binder)
        where TNext : Context
    {
        if (!Success)
        {
            var diag = new DiagnosticsLog();
            diag.Error("Bind skipped due to previous failure");
            if (Diagnostics != null) diag.Append(Diagnostics);
            return new ContextResult<TNext>
            {
                Context = (TNext)(object)Context, // will only be valid if cast acceptable; for different context supply projection
                Success = false,
                Error = Error,
                CompletedUtc = DateTime.UtcNow,
                Diagnostics = diag
            };
        }
        ContextResult<TNext> next;
        try
        {
            next = binder(Context);
        }
        catch (Exception ex)
        {
            var diag = EnsureDiagnostics();
            diag.Error("Exception in bind: " + ex.Message);
            return ContextResult<TNext>.FromError((TNext)(object)Context, ex, diag);
        }
        // Merge diagnostics
        if (Diagnostics != null && next.Diagnostics != null)
        {
            var merged = new DiagnosticsLog();
            merged.Append(Diagnostics);
            merged.Append(next.Diagnostics);
            next = new ContextResult<TNext>
            {
                Context = next.Context,
                Success = next.Success,
                Error = next.Error,
                CompletedUtc = next.CompletedUtc,
                Diagnostics = merged
            };
        }
        else if (Diagnostics != null && next.Diagnostics == null)
        {
            next = new ContextResult<TNext>
            {
                Context = next.Context,
                Success = next.Success,
                Error = next.Error,
                CompletedUtc = next.CompletedUtc,
                Diagnostics = Diagnostics
            };
        }
        return next;
    }

    public ContextResult<TNext> Then<TNext>(Func<TContext, ContextResult<TNext>> next)
        where TNext : Context => Bind(next);

    public async Task<ContextResult<TNext>> BindAsync<TNext>(Func<TContext, Task<ContextResult<TNext>>> binder)
        where TNext : Context
    {
        if (!Success)
        {
            var diag = new DiagnosticsLog();
            diag.Error("BindAsync skipped due to previous failure");
            if (Diagnostics != null) diag.Append(Diagnostics);
            return new ContextResult<TNext>
            {
                Context = (TNext)(object)Context,
                Success = false,
                Error = Error,
                CompletedUtc = DateTime.UtcNow,
                Diagnostics = diag
            };
        }
        ContextResult<TNext> nextResult;
        try
        {
            nextResult = await binder(Context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var diag = EnsureDiagnostics();
            diag.Error("Exception in bind async: " + ex.Message);
            return ContextResult<TNext>.FromError((TNext)(object)Context, ex, diag);
        }
        if (Diagnostics != null && nextResult.Diagnostics != null)
        {
            var merged = new DiagnosticsLog();
            merged.Append(Diagnostics);
            merged.Append(nextResult.Diagnostics);
            nextResult = new ContextResult<TNext>
            {
                Context = nextResult.Context,
                Success = nextResult.Success,
                Error = nextResult.Error,
                CompletedUtc = nextResult.CompletedUtc,
                Diagnostics = merged
            };
        }
        else if (Diagnostics != null && nextResult.Diagnostics == null)
        {
            nextResult = new ContextResult<TNext>
            {
                Context = nextResult.Context,
                Success = nextResult.Success,
                Error = nextResult.Error,
                CompletedUtc = nextResult.CompletedUtc,
                Diagnostics = Diagnostics
            };
        }
        return nextResult;
    }

    public async Task<ContextResult<TNext>> ThenAsync<TNext>(Func<TContext, Task<ContextResult<TNext>>> next)
        where TNext : Context => await BindAsync(next).ConfigureAwait(false);

    public static ContextResult<TContext> FromSuccess(TContext ctx, DiagnosticsLog? diagnostics = null)
        => new() { Context = ctx, Success = true, CompletedUtc = DateTime.UtcNow, Diagnostics = diagnostics };

    public static ContextResult<TContext> FromError(TContext ctx, Exception ex, DiagnosticsLog? diagnostics = null)
    {
        diagnostics ??= new DiagnosticsLog();
        diagnostics.Error(ex.Message);
        return new ContextResult<TContext>
        {
            Context = ctx,
            Success = false,
            Error = ex,
            CompletedUtc = DateTime.UtcNow,
            Diagnostics = diagnostics
        };
    }
}