#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using BII.WasaBii.Core;

namespace BII.WasaBii.Extra
{
    using RunInOrderList = ImmutableList<Func<object?, OperationContext, Task<(object? Result, int DoneSteps)>>>;
    using SafeRunInOrderList = ImmutableList<Func<object, OperationContext, Task<(object Result, int DoneSteps)>>>;

    public sealed class Operation
    {
        public static Operation Empty() => new(0, RunInOrderList.Empty);
        
        public static Operation<T> From<T>(T initialResult) where T : notnull => 
            new(0, RunInOrderList.Empty, (_, ctx) => (initialResult, ctx.StepOffset).AsCompletedTask());
        
        public static Operation<TInput, TInput> WithInput<TInput>() where TInput : notnull => 
            new(0, SafeRunInOrderList.Empty, null);

        public readonly int EstimatedStepCount;
        internal readonly RunInOrderList runInOrder;

        internal Operation(
            int estimatedStepCount, 
            RunInOrderList runInOrder
        ) {
            EstimatedStepCount = estimatedStepCount;
            this.runInOrder = runInOrder;
        }

        public async Task Run(OperationContext context) {
            object? curr = null;
            var steps = 0;
            foreach (var run in runInOrder) {
                context.CancellationToken?.ThrowIfCancellationRequested();
                (curr, steps) = await run(curr, context with {StepOffset = steps});
            }
        }

        public async Task<Result<Nothing, Exception>> RunSafe(OperationContext context) {
            try {
                await Run(context);
                return new Nothing();
            } catch (Exception e) {
                return e;
            }
        }

        public Operation<T> WithResult<T>(Func<T> resultGetter)
        where T : notnull => new(
            EstimatedStepCount,
            runInOrder,
            (_, ctx) => Task.FromResult((resultGetter(), ctx.StepOffset))
        );
        
        public Operation Step(string label, Func<OperationStepContext, Task> step) => new(
            EstimatedStepCount + 1,
            runInOrder.Add(async (r, ctx) => {
                var steps = ctx.StepOffset;
                ctx.OnStepStarted(label);
                await step(ctx);
                ctx.OnStepCompleted(steps + 1);
                return (r, steps + 1);
            })
        );
        
        public Operation<T> Step<T>(string label, Func<OperationStepContext, Task<T>> step) 
        where T : notnull => new(
            EstimatedStepCount + 1,
            runInOrder,
            async (_, ctx) => {
                var steps = ctx.StepOffset;
                ctx.OnStepStarted(label);
                var res = await step(ctx);
                ctx.OnStepCompleted(steps + 1);
                return (res, steps + 1);
            }
        );

        public Operation Chain(Operation op) => new(
            EstimatedStepCount + op.EstimatedStepCount,
            runInOrder.AddRange(op.runInOrder)
        );
        
        public Operation<T> Chain<T>(Operation<T> op) 
        where T : notnull => new(
            EstimatedStepCount + op.EstimatedStepCount,
            runInOrder.AddRange(op.runInOrder),
            op.run
        );
        
        public Operation<TStart, TRes> Chain<TStart, TRes>(Operation<TStart, TRes> op) 
        where TStart : notnull where TRes : notnull {
            // We sneak in two run steps: the first saves the start value,
            //  and the second after this' runInOrder restores the start value as result.
            TStart startClosureSlot = default!;
            return new Operation<TStart, TRes>(
                EstimatedStepCount + op.EstimatedStepCount, 
                runInOrder
                    .Insert(0, (r, ctx) => {
                        startClosureSlot = (TStart) r!;
                        return Task.FromResult((r, ctx.StepOffset));
                    })
                    .Add((_, ctx) => Task.FromResult(((object?) startClosureSlot, ctx.StepOffset)))
                    .AddRange(op.runInOrder!)!, 
                op.run
            );
        }
    }
    
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
    }

    public sealed class Operation<TStart, TResult> where TStart : notnull where TResult : notnull
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

        public async Task<TResult> Run(OperationContext<TStart> context) {
            object curr = context.StartValue;
            var steps = 0;
            foreach (var earlierRun in runInOrder) {
                context.CancellationToken?.ThrowIfCancellationRequested();
                (curr, steps) = await earlierRun(curr, context with {StepOffset = steps});
            }
            if (run != null) return (await run(curr, context with {StepOffset = steps})).Result;
            else return (TResult) curr;
        }

        public async Task<Result<TResult, Exception>> RunSafe(OperationContext<TStart> context) {
            try {
                return await Run(context);
            } catch (Exception e) {
                return e;
            }
        }

        public Operation<TResult> WithStartValue(TStart startValue) => new(
            EstimatedStepCount, 
            shiftRunInOrder().Insert(0, (_, _) => Task.FromResult(((object) startValue, 0)))!, 
            null
        );

        public Operation<TStart, TRes> Map<TRes>(Func<TResult, TRes> mapper) 
        where TRes : notnull => new(
            EstimatedStepCount, 
            shiftRunInOrder(), 
            (r, ctx) => Task.FromResult((mapper((TResult) r), ctx.StepOffset))
        );
        
        public Operation<TStart, TResult> Step(
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

        public Operation<TStart, TRes> Step<TRes>(
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

        public Operation<TStart, TResult> Chain(Operation other) {
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
        public Operation<TStart, TRes> Chain<TRes>(Operation<TRes> other) 
        where TRes : notnull => new(
            EstimatedStepCount + other.EstimatedStepCount,
            shiftRunInOrder().AddRange(other.runInOrder!), 
            other.run
        );

        public Operation<TStart, TRes> Chain<TRes>(Operation<TResult, TRes> other) 
        where TRes : notnull => new(
            EstimatedStepCount + other.EstimatedStepCount,
            shiftRunInOrder().AddRange(other.runInOrder), 
            other.run
        );
        
        public Operation<TStart, TRes> FlatMap<TRes>(
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
        
        public Operation<TStart, TRes> FlatMap<TRes>(
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
    }
}