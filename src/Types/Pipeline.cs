using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Michelangelo.Types;

public interface IPipeline<in TIn, TOut>
    where TIn : Context
    where TOut : Context
{
    Task<ContextResult<TOut>> ExecuteAsync(TIn input, CancellationToken ct = default);
}

public interface IPipelineStep<TIn, TOut>
    where TIn : Context
    where TOut : Context
{
    Task<ContextResult<TOut>> InvokeAsync(TIn ctx, CancellationToken ct);
}

internal sealed class JobStep<TIn, TOut, TJob> : IPipelineStep<TIn, TOut>
    where TIn : Context
    where TOut : Context
    where TJob : IContextJob<TIn, TOut>, new()
{
    public async Task<ContextResult<TOut>> InvokeAsync(TIn ctx, CancellationToken ct)
    {
        var job = new TJob();
        return await job.RunAsync(ctx, ct).ConfigureAwait(false);
    }
}

internal sealed class NestedPipelineStep<TIn, TMid, TOut> : IPipelineStep<TIn, TOut>
    where TIn : Context
    where TMid : Context
    where TOut : Context
{
    private readonly IPipeline<TIn, TMid> _first;
    private readonly IPipeline<TMid, TOut> _second;
    public NestedPipelineStep(IPipeline<TIn, TMid> first, IPipeline<TMid, TOut> second)
    { _first = first; _second = second; }

    public async Task<ContextResult<TOut>> InvokeAsync(TIn ctx, CancellationToken ct)
    {
        var firstResult = await _first.ExecuteAsync(ctx, ct).ConfigureAwait(false);
        if (!firstResult.Success)
        {
            return ContextResult<TOut>.FromError((TOut)(object)firstResult.Context, firstResult.Error ?? new InvalidOperationException("Nested pipeline failed"), firstResult.Diagnostics);
        }
        var secondResult = await _second.ExecuteAsync((TMid)firstResult.Context, ct).ConfigureAwait(false);
        // Merge diagnostics from first and second
        if (firstResult.Diagnostics != null)
        {
            if (secondResult.Diagnostics != null)
            {
                var merged = new DiagnosticsLog();
                merged.Append(firstResult.Diagnostics);
                merged.Append(secondResult.Diagnostics);
                return new ContextResult<TOut>
                {
                    Context = secondResult.Context,
                    Success = secondResult.Success,
                    Error = secondResult.Error,
                    CompletedUtc = secondResult.CompletedUtc,
                    Diagnostics = merged
                };
            }
            else
            {
                return new ContextResult<TOut>
                {
                    Context = secondResult.Context,
                    Success = secondResult.Success,
                    Error = secondResult.Error,
                    CompletedUtc = secondResult.CompletedUtc,
                    Diagnostics = firstResult.Diagnostics
                };
            }
        }
        return secondResult;
    }
}

public sealed class Pipeline<TIn, TOut> : IPipeline<TIn, TOut>
    where TIn : Context
    where TOut : Context
{
    private readonly IReadOnlyList<object> _steps; // list of IPipelineStep<*,*> boxed
    internal Pipeline(List<object> steps) => _steps = steps;

    public async Task<ContextResult<TOut>> ExecuteAsync(TIn input, CancellationToken ct = default)
    {
        Context current = input;
        DiagnosticsLog? aggregateDiag = null;
        for (int i = 0; i < _steps.Count; i++)
        {
            var step = _steps[i];
            ContextResult<Context> stepResult;
            switch (step)
            {
                case IPipelineStep<Context, Context> homo:
                    stepResult = await homo.InvokeAsync(current, ct).ConfigureAwait(false);
                    break;
                default:
                    // Use dynamic dispatch when generics differ; safe but slower.
                    stepResult = await InvokeDynamic(step, current, ct).ConfigureAwait(false);
                    break;
            }
            if (stepResult.Diagnostics != null)
            {
                aggregateDiag ??= new DiagnosticsLog();
                aggregateDiag.Append(stepResult.Diagnostics);
            }
            if (!stepResult.Success)
            {
                // If failure occurs before reaching final context type, we need a safe return.
                // Construct a synthetic TOut when possible; if types mismatch, attempt best-effort cast or wrap error.
                if (stepResult.Context is TOut finalCtx)
                {
                    return new ContextResult<TOut>
                    {
                        Context = finalCtx,
                        Success = false,
                        Error = stepResult.Error,
                        CompletedUtc = stepResult.CompletedUtc,
                        Diagnostics = aggregateDiag
                    };
                }
                // Create placeholder instance if TOut has parameterless ctor.
                TOut placeholder;
                try
                {
                    placeholder = Activator.CreateInstance<TOut>();
                }
                catch
                {
                    // last resort: throw wrapped exception to signal incompatible failure context.
                    throw new InvalidOperationException($"Pipeline failed producing context of type {stepResult.Context.GetType().Name} which cannot be cast to {typeof(TOut).Name} and no parameterless constructor available.", stepResult.Error);
                }
                return new ContextResult<TOut>
                {
                    Context = placeholder,
                    Success = false,
                    Error = stepResult.Error,
                    CompletedUtc = stepResult.CompletedUtc,
                    Diagnostics = aggregateDiag
                };                
            }
            current = stepResult.Context;
        }
        return new ContextResult<TOut>
        {
            Context = (TOut)current,
            Success = true,
            CompletedUtc = DateTime.UtcNow,
            Diagnostics = aggregateDiag
        };
    }

    private static async Task<ContextResult<Context>> InvokeDynamic(object step, Context ctx, CancellationToken ct)
    {
        // reflection path; call InvokeAsync
        var method = step.GetType().GetMethod("InvokeAsync");
        if (method == null) throw new MissingMethodException(step.GetType().Name, "InvokeAsync");
        var task = (Task)method.Invoke(step, new object[] { ctx, ct })!;
        await task.ConfigureAwait(false);
        var resultProp = task.GetType().GetProperty("Result");
        var result = resultProp!.GetValue(task)!; // ContextResult<TSpecific>
        // Coerce to ContextResult<Context>
        var contextProp = result.GetType().GetProperty("Context")!;
        var successProp = result.GetType().GetProperty("Success")!;
        var errorProp = result.GetType().GetProperty("Error")!;
        var completedProp = result.GetType().GetProperty("CompletedUtc")!;
        var diagProp = result.GetType().GetProperty("Diagnostics");
        return new ContextResult<Context>
        {
            Context = (Context)contextProp.GetValue(result)!,
            Success = (bool)successProp.GetValue(result)!,
            Error = (Exception?)errorProp.GetValue(result),
            CompletedUtc = (DateTime?)completedProp.GetValue(result),
            Diagnostics = (DiagnosticsLog?)diagProp?.GetValue(result)
        };
    }
}

public sealed class PipelineBuilder<TIn, TCurrent>
    where TIn : Context
    where TCurrent : Context
{
    private readonly List<object> _steps;
    internal PipelineBuilder(List<object> steps) => _steps = steps;

    public PipelineBuilder() : this(new List<object>()) { }

    public PipelineBuilder<TIn, TNext> ThenJob<TNext, TJob>()
        where TNext : Context
        where TJob : IContextJob<TCurrent, TNext>, new()
    {
        _steps.Add(new JobStep<TCurrent, TNext, TJob>());
        return new PipelineBuilder<TIn, TNext>(_steps);
    }

    public PipelineBuilder<TIn, TOut> ThenPipeline<TMid, TOut>(IPipeline<TCurrent, TMid> first, IPipeline<TMid, TOut> second)
        where TMid : Context
        where TOut : Context
    {
        _steps.Add(new NestedPipelineStep<TCurrent, TMid, TOut>(first, second));
        return new PipelineBuilder<TIn, TOut>(_steps);
    }

    public IPipeline<TIn, TCurrent> Build()
        => new Pipeline<TIn, TCurrent>(_steps);
}

public static class Pipeline
{
    public static PipelineBuilder<TContext, TContext> Start<TContext>() where TContext : Context
        => new PipelineBuilder<TContext, TContext>();
}