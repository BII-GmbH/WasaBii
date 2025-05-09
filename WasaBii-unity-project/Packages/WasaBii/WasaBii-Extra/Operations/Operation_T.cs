#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using BII.WasaBii.Core;

namespace BII.WasaBii.Extra
{
    using RunInOrderList = ImmutableList<Func<object?, OperationContext, Task<(object? Result, int DoneSteps)>>>;
    using SafeRunInOrderList = ImmutableList<Func<object, OperationContext, Task<(object Result, int DoneSteps)>>>;
        
    public sealed class Operation<T> where T : notnull
    {
        public readonly int EstimatedStepCount;
        internal readonly RunInOrderList runInOrder;
        internal readonly Func<object?, OperationContext, Task<(T Result, int DoneSteps)>>? run;

        internal RunInOrderList shiftRunInOrder() => 
            run == null 
                ? runInOrder 
                : runInOrder.Add(
                    async (v, ctx) => {
                        var (res, steps) = await run(v, ctx);
                        return (res, steps);
                    }
                );

        internal Operation(
            int estimatedStepCount, 
            RunInOrderList runInOrder, 
            Func<object?, OperationContext, Task<(T Result, int DoneSteps)>>? run
        ) {
            EstimatedStepCount = estimatedStepCount;
            this.runInOrder = runInOrder;
            this.run = run;
        }

        public async Task<T> Run(OperationContext context) {
            object? curr = null;
            var steps = 0;
            foreach (var earlierRun in runInOrder) {
                context.CancellationToken?.ThrowIfCancellationRequested();
                (curr, steps) = await earlierRun(curr, context with {StepOffset = steps});
            }
            if (run != null) return (await run(curr, context with {StepOffset = steps})).Result;
            else return (T) curr!;
        }

        public async Task<Result<T, Exception>> RunSafe(OperationContext context) {
            try {
                return await Run(context);
            } catch (Exception e) {
                return e;
            }
        }

        public Operation WithoutResult() => 
            new(EstimatedStepCount, shiftRunInOrder());

        public Operation<TRes> Map<TRes>(Func<T, TRes> mapper) where TRes : notnull => 
            new(EstimatedStepCount, shiftRunInOrder(), (v, ctx) => Task.FromResult((mapper((T) v!), ctx.StepOffset)));

        public Operation<Nothing, TRes, TError> TryMap<TRes, TError>(Func<T, Result<TRes, TError>> mapper)
        where TRes : notnull => new(
            EstimatedStepCount,
            shiftRunInOrder()!,
            (v, ctx) => Task.FromResult(
                (mapper((T)v!).Match(x => x, err => throw new ShortCircuitException<TError>(err)), ctx.StepOffset)
            )
        );
        
        public Operation<T> Step(string label, Func<OperationStepContext<T>, Task> step) => new(
            EstimatedStepCount + 1,
            shiftRunInOrder(),
            async (res, ctx) => {
                var steps = ctx.StepOffset;
                ctx.OnStepStarted(label);
                await step(new((T) res!, ctx));
                var finalSteps = steps + 1;
                ctx.OnStepCompleted(finalSteps);
                return ((T) res!, finalSteps);
            }
        );

        public Operation<TRes> Step<TRes>(string label, Func<OperationStepContext<T>, Task<TRes>> step) 
        where TRes : notnull => new(
            EstimatedStepCount + 1,
            shiftRunInOrder(),
            async (curr, ctx) => {
                var steps = ctx.StepOffset;
                ctx.OnStepStarted(label);
                var res = await step(new((T) curr!, ctx));
                var finalSteps = steps + 1;
                ctx.OnStepCompleted(finalSteps);
                return (res, finalSteps);
            }
        );
        
        public Operation<Nothing, TRes, TError> TryStep<TRes, TError>(
            string label, 
            Func<OperationStepContext<T>, Task<Result<TRes, TError>>> step
        ) where TRes : notnull => new(
            EstimatedStepCount + 1,
            shiftRunInOrder()!, 
            async (res, ctx) => {
                var steps = ctx.StepOffset;
                ctx.OnStepStarted(label);
                var newRes = await step(new((T) res, ctx));
                if (newRes.TryGetError(out var err)) 
                    throw new ShortCircuitException<TError>(err);
                var finalSteps = steps + 1;
                ctx.OnStepCompleted(finalSteps);
                return (newRes.ResultOrDefault!, finalSteps);
            }
        );

        public Operation<T> Chain(Operation other) {
            // We sneak in two run steps: the first before 'other' sets this variable,
            //  and the second at the end restores it as the operation's result.
            T resultOfThisClosureSlot = default!;
            return new(
                EstimatedStepCount + other.EstimatedStepCount,
                shiftRunInOrder()
                    .Add((r, ctx) => {
                        resultOfThisClosureSlot = (T)r!;
                        return Task.FromResult((r, ctx.StepOffset));
                    })
                    .AddRange(other.runInOrder),
                (_, ctx) => Task.FromResult((magicClosureResult: resultOfThisClosureSlot, ctx.StepOffset))
            );
        }

        public Operation<TRes> Chain<TRes>(Operation<T, TRes> op) where TRes : notnull =>
            new(
                EstimatedStepCount + op.EstimatedStepCount,
                shiftRunInOrder().AddRange(op.runInOrder!),
                op.run == null ? null : (r, ctx) => op.run(r!, ctx)
            );
        
        public Operation<Nothing, TRes, TError> Chain<TRes, TError>(Operation<T, TRes, TError> other) 
        where TRes : notnull => new(
            EstimatedStepCount + other.EstimatedStepCount,
            shiftRunInOrder().AddRange(other.runInOrder!)!, 
            other.run
        );
        
        public Operation<TRes> FlatMap<TRes>(
            int stepCountPrediction, 
            Func<T, Operation<TRes>> mapper
        ) where TRes : notnull => new(
            EstimatedStepCount + stepCountPrediction,
            shiftRunInOrder(),
            async (result, ctx) => {
                var steps = ctx.StepOffset;
                var newOp = mapper((T) result!);
                var stepDiff = newOp.EstimatedStepCount - stepCountPrediction;
                if (stepDiff != 0) ctx.OnStepCountDiff(stepDiff);
                
                object? curr = null;
                foreach (var newRun in newOp.runInOrder) {
                    ctx.CancellationToken?.ThrowIfCancellationRequested();
                    (curr, steps) = await newRun(curr, ctx with {StepOffset = steps});
                }

                if (newOp.run != null) return await newOp.run(curr, ctx with {StepOffset = steps});
                else {
                    var finalResult = curr!; // Operation<T> always returns a value
                    return ((TRes) finalResult, steps);
                }
            }
        );
        
        public Operation<TStart, TRes> FlatMap<TStart, TRes>(
            int stepCountPrediction, 
            Func<T, Operation<TStart, TRes>> mapper
        ) where TStart : notnull where TRes : notnull => new(
            EstimatedStepCount + stepCountPrediction,
            SafeRunInOrderList.Empty.Add(
                async (start, ctx) => {
                    // First run the current operation
                    var steps = 0;
                    object? currentRes = null;
                    foreach (var earlierRun in runInOrder) {
                        ctx.CancellationToken?.ThrowIfCancellationRequested();
                        (currentRes, steps) = await earlierRun(currentRes, ctx with {StepOffset = steps});
                    }
                    
                    // Then get the new operation
                    var newOp = mapper((T) currentRes!);
                    var stepDiff = newOp.EstimatedStepCount - stepCountPrediction;
                    if (stepDiff != 0) ctx.OnStepCountDiff(stepDiff);
                    
                    // finally run the new operation
                    object newRes = start!;
                    foreach (var newRun in newOp.runInOrder) {
                        ctx.CancellationToken?.ThrowIfCancellationRequested();
                        (newRes, steps) = await newRun(newRes, ctx with {StepOffset = steps});
                    }
                    
                    if (newOp.run != null) return await newOp.run(newRes, ctx with {StepOffset = steps});
                    else return ((TRes) newRes, steps);
                }),
            null // nothing to do
        );
        
        public Operation<TStart, TRes, TError> FlatMap<TStart, TRes, TError>(
            int stepCountPrediction, 
            Func<T, Operation<TStart, TRes, TError>> mapper
        ) where TRes : notnull where TStart : notnull => new(
            EstimatedStepCount + stepCountPrediction,
            SafeRunInOrderList.Empty.Add(
                async (start, ctx) => {
                    // First run the current operation
                    var steps = 0;
                    object? curr = start;
                    foreach (var earlierRun in runInOrder) {
                        ctx.CancellationToken?.ThrowIfCancellationRequested();
                        (curr, steps) = await earlierRun(curr, ctx with {StepOffset = steps});
                    }
                    
                    // Then get the new operation
                    var newOp = mapper((T) curr!);
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