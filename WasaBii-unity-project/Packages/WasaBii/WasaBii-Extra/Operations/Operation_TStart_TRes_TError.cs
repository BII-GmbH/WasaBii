#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using BII.WasaBii.Core;

namespace BII.WasaBii.Extra
{
    using SafeRunInOrderList = ImmutableList<Func<object, OperationContext, Task<(object Result, int DoneSteps)>>>;
    
    // Used to short-circuit evaluation when TError happens
    // => This makes this distinct from Operation<TStart, Result<TResult, TError>>
    internal sealed class ShortCircuitException<TError> : Exception
    {
        public readonly TError Error;
        public ShortCircuitException(TError error) => this.Error = error;
    }
    
    public sealed class Operation<TStart, TResult, TError> where TStart : notnull where TResult : notnull
    {
        public readonly int EstimatedStepCount;
        internal readonly SafeRunInOrderList runInOrder;
        internal readonly Func<object, OperationContext, Task<(TResult Result, int DoneSteps)>>? run;
        
        internal SafeRunInOrderList shiftRunInOrder() => 
            run == null 
                ? runInOrder 
                : runInOrder.Add(
                    async (v, ctx) => {
                        var (res, steps) = await run(v!, ctx);
                        return (res, steps);
                    }
                );

        internal Operation(
            int estimatedStepCount, 
            SafeRunInOrderList runInOrder, 
            Func<object, OperationContext, Task<(TResult Result, int DoneSteps)>>? run
        ) {
            EstimatedStepCount = estimatedStepCount;
            this.runInOrder = runInOrder;
            this.run = run;
        }

        public async Task<Result<TResult, TError>> Run(OperationContext<TStart> context) {
            object curr = context.StartValue;
            var steps = 0;
            try {
                foreach (var earlierRun in runInOrder) {
                    context.CancellationToken?.ThrowIfCancellationRequested();
                    (curr, steps) = await earlierRun(curr, context with { StepOffset = steps });
                }

                if (run != null)
                    return (await run(curr, context with { StepOffset = steps })).Result;
                else
                    return (TResult)curr;
            } catch (ShortCircuitException<TError> shortCircuit) {
                return shortCircuit.Error;
            }
        }

        public async Task<Result<Result<TResult, TError>, Exception>> RunSafe(OperationContext<TStart> context) {
            try {
                return await Run(context);
            } catch (Exception e) {
                return e;
            }
        }

        public Operation<TStart, TRes, TError> Map<TRes>(Func<TResult, TRes> mapper) 
        where TRes : notnull => new(
            EstimatedStepCount, 
            shiftRunInOrder(), 
            (r, ctx) => Task.FromResult((mapper((TResult) r), ctx.StepOffset))
        );

        public Operation<TStart, TRes, TError> TryMap<TRes>(Func<TResult, Result<TRes, TError>> mapper)
        where TRes : notnull => new(
            EstimatedStepCount,
            shiftRunInOrder(),
            (r, ctx) => Task.FromResult((
                mapper((TResult)r).Match(
                    x => x, 
                    err => throw new ShortCircuitException<TError>(err)
                ), 
                ctx.StepOffset)
            )
        );
        
        public Operation<TStart, TResult, TError> Step(
            string label, 
            Func<OperationStepContext<TResult>, Task> step
        ) => new(
            EstimatedStepCount + 1,
            shiftRunInOrder(), 
            async (res, ctx) => {
                var steps = ctx.StepOffset;
                ctx.OnStepStarted(label);
                await step(new((TResult) res, ctx));
                var finalSteps = steps + 1;
                ctx.OnStepCompleted(finalSteps);
                return ((TResult) res, finalSteps);
            }
        );

        public Operation<TStart, TRes, TError> Step<TRes>(
            string label, 
            Func<OperationStepContext<TResult>, Task<TRes>> step
        ) where TRes : notnull => new(
            EstimatedStepCount + 1,
            shiftRunInOrder(), 
            async (res, ctx) => {
                var steps = ctx.StepOffset;
                ctx.OnStepStarted(label);
                var newRes = await step(new((TResult) res, ctx));
                var finalSteps = steps + 1;
                ctx.OnStepCompleted(finalSteps);
                return (newRes, finalSteps);
            }
        );
        
        public Operation<TStart, TRes, TError> TryStep<TRes>(
            string label, 
            Func<OperationStepContext<TResult>, Task<Result<TRes, TError>>> step
        ) where TRes : notnull => new(
            EstimatedStepCount + 1,
            shiftRunInOrder(), 
            async (res, ctx) => {
                var steps = ctx.StepOffset;
                ctx.OnStepStarted(label);
                var newRes = await step(new((TResult) res, ctx));
                if (newRes.TryGetError(out var err)) 
                    throw new ShortCircuitException<TError>(err);
                var finalSteps = steps + 1;
                ctx.OnStepCompleted(finalSteps);
                return (newRes.ResultOrDefault!, finalSteps);
            }
        );

        public Operation<TStart, TResult, TError> Chain(Operation other) {
            // We sneak in two run steps: the first before 'other' sets this variable,
            //  and the second at the end restores it as the operation's result.
            TResult resultOfThisClosureSlot = default!;
            return new(
                EstimatedStepCount + other.EstimatedStepCount,
                shiftRunInOrder()
                    .Add((r, ctx) => {
                        resultOfThisClosureSlot = (TResult) r!;
                        return Task.FromResult((r, ctx.StepOffset));
                    })
                    .AddRange(other.runInOrder!),
                (_, ctx) => Task.FromResult((magicClosureResult: resultOfThisClosureSlot, ctx.StepOffset))
            );
        }

        // Note CR: Cannot get rid of this unless we introduce an Operation with start value but without result
        public Operation<TStart, TRes, TError> Chain<TRes>(Operation<TRes> other) 
        where TRes : notnull => new(
            EstimatedStepCount + other.EstimatedStepCount,
            shiftRunInOrder().AddRange(other.runInOrder!), 
            other.run
        );

        public Operation<TStart, TRes, TError> Chain<TRes>(Operation<TResult, TRes> other) 
        where TRes : notnull => new(
            EstimatedStepCount + other.EstimatedStepCount,
            shiftRunInOrder().AddRange(other.runInOrder), 
            other.run
        );
        
        public Operation<TStart, TRes, TError> Chain<TRes>(Operation<TResult, TRes, TError> other) 
        where TRes : notnull => new(
            EstimatedStepCount + other.EstimatedStepCount,
            shiftRunInOrder().AddRange(other.runInOrder), 
            other.run
        );
        
        public Operation<TStart, TRes, TError> FlatMap<TRes>(
            int stepCountPrediction, 
            Func<TResult, Operation<TRes>> mapper
        ) where TRes : notnull => new(
            EstimatedStepCount + stepCountPrediction,
            shiftRunInOrder(),
            async (result, ctx) => {
                var steps = ctx.StepOffset;
                var newOp = mapper((TResult) result);
                var stepDiff = newOp.EstimatedStepCount - stepCountPrediction;
                if (stepDiff != 0) ctx.OnStepCountDiff(stepDiff);
                
                object? curr = null;
                foreach (var newRun in newOp.runInOrder) {
                    ctx.CancellationToken?.ThrowIfCancellationRequested();
                    (curr, steps) = await newRun(curr, ctx with {StepOffset = steps});
                }

                if (newOp.run != null) return await newOp.run(curr, ctx with {StepOffset = steps});
                else return ((TRes) curr!, steps);
            }
        );
        
        public Operation<TStart, TRes, TError> FlatMap<TRes>(
            int stepCountPrediction, 
            Func<TResult, Operation<TStart, TRes>> mapper
        ) where TRes : notnull => new(
            EstimatedStepCount + stepCountPrediction,
            SafeRunInOrderList.Empty.Add(
                async (start, ctx) => {
                    // First run the current operation
                    var steps = 0;
                    object curr = start;
                    foreach (var earlierRun in runInOrder) {
                        ctx.CancellationToken?.ThrowIfCancellationRequested();
                        (curr, steps) = await earlierRun(curr, ctx with {StepOffset = steps});
                    }
                    
                    // Then get the new operation
                    var newOp = mapper((TResult) curr);
                    var stepDiff = newOp.EstimatedStepCount - stepCountPrediction;
                    if (stepDiff != 0) ctx.OnStepCountDiff(stepDiff);
                    
                    // finally run the new operation
                    curr = start;
                    foreach (var newRun in newOp.runInOrder) {
                        ctx.CancellationToken?.ThrowIfCancellationRequested();
                        (curr, steps) = await newRun(curr, ctx with {StepOffset = steps});
                    }
                    
                    if (newOp.run != null) return await newOp.run(curr, ctx with {StepOffset = steps});
                    else return ((TRes) curr, steps);
                }),
            null // nothing to do
        );
        
        public Operation<TStart, TRes, TError> FlatMap<TRes>(
            int stepCountPrediction, 
            Func<TResult, Operation<TStart, TRes, TError>> mapper
        ) where TRes : notnull => new(
            EstimatedStepCount + stepCountPrediction,
            SafeRunInOrderList.Empty.Add(
                async (start, ctx) => {
                    // First run the current operation
                    var steps = 0;
                    object curr = start;
                    foreach (var earlierRun in runInOrder) {
                        ctx.CancellationToken?.ThrowIfCancellationRequested();
                        (curr, steps) = await earlierRun(curr, ctx with {StepOffset = steps});
                    }
                    
                    // Then get the new operation
                    var newOp = mapper((TResult) curr);
                    var stepDiff = newOp.EstimatedStepCount - stepCountPrediction;
                    if (stepDiff != 0) ctx.OnStepCountDiff(stepDiff);
                    
                    // finally run the new operation
                    curr = start;
                    foreach (var newRun in newOp.runInOrder) {
                        ctx.CancellationToken?.ThrowIfCancellationRequested();
                        (curr, steps) = await newRun(curr, ctx with {StepOffset = steps});
                    }
                    
                    if (newOp.run != null) return await newOp.run(curr, ctx with {StepOffset = steps});
                    else return ((TRes) curr, steps);
                }),
            null // nothing to do
        );
    }
}