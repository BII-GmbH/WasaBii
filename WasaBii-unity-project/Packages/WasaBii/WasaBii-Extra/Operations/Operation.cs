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
        
        public Operation<Nothing, TRes, TError> Step<TRes, TError>(
            string label, 
            Func<OperationStepContext, Task<Result<TRes, TError>>> step
        ) where TRes : notnull => new(
            EstimatedStepCount + 1,
            runInOrder!, 
            async (_, ctx) => {
                var steps = ctx.StepOffset;
                ctx.OnStepStarted(label);
                var newRes = await step(ctx);
                if (newRes.TryGetError(out var err)) 
                    throw new ShortCircuitException<TError>(err);
                var finalSteps = steps + 1;
                ctx.OnStepCompleted(finalSteps);
                return (newRes.ResultOrDefault!, finalSteps);
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
}