namespace Michelangelo.Types;

// A job that takes an input context type and produces a new context wrapped in ContextResult for richer chaining.
public interface IContextJob<TIn, TOut>
    where TIn : Context
    where TOut : Context
{
    Task<ContextResult<TOut>> RunAsync(TIn context, CancellationToken cancellationToken);
}
