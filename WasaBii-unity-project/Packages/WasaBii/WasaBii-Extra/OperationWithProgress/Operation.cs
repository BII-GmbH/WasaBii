using System;
using System.Threading.Tasks;
using BII.WasaBii.Core;

namespace BII.WasaBii.Extra
{
    public record OperationContext(Action<int> OnStepCompleted, Action<string> OnStepStarted);
    public record OperationContext<T>(T Value, Action<int> OnStepCompleted, Action<string> OnStepStarted);
    
    public record Operation(int EstimatedSteps, Func<OperationContext, Task> Run)
    {
        public static Operation Empty => new Operation(0, ctx => Task.CompletedTask);

        public async Task<Result<Nothing, Exception>> RunSafe(OperationContext context) {
            try {
                await Run(context);
                return new Nothing();
            } catch (Exception e) {
                return e;
            }
        }

        public Operation<T> WithResult<T>(Func<T> resultGetter) => new(
            EstimatedSteps,
            async ctx => {
                await Run(ctx);
                return resultGetter();
            }
        );

        public Operation Step(string label, Func<Task> step) => new(
            EstimatedSteps + 1,
            async ctx => {
                await Run(ctx);
                ctx.OnStepStarted(label);
                await step();
                ctx.OnStepCompleted(EstimatedSteps + 1);
            }
        );
        
        public Operation<T> Step<T>(string label, Func<Task<T>> step) => new(
            EstimatedSteps + 1,
            async ctx => {
                await Run(ctx);
                ctx.OnStepStarted(label);
                var res = await step();
                ctx.OnStepCompleted(EstimatedSteps + 1);
                return res;
            }
        );

        public Operation Chain(Operation op) => new(
            EstimatedSteps + op.EstimatedSteps,
            async ctx => {
                await Run(ctx);
                await op.Run(ctx with {OnStepCompleted = step => ctx.OnStepCompleted(EstimatedSteps + step)});
            }
        );
        
        public Operation<T> Chain<T>(Operation<T> op) => new(
            EstimatedSteps + op.EstimatedSteps,
            async ctx => {
                await Run(ctx);
                return await op.Run(ctx with {OnStepCompleted = step => ctx.OnStepCompleted(EstimatedSteps + step)});
            }
        );
        
        public Operation<TStart, TRes> Chain<TStart, TRes>(Operation<TStart, TRes> op) => new(
            EstimatedSteps + op.EstimatedSteps,
            async ctx => {
                await Run(new(ctx.OnStepCompleted, ctx.OnStepStarted));
                return await op.Run(ctx with {OnStepCompleted = step => ctx.OnStepCompleted(EstimatedSteps + step)});
            }
        );
    }

    public record Operation<T>(int EstimatedSteps, Func<OperationContext, Task<T>> Run)
    {
        public static Operation<T> From(T result) =>
            new(0, ctx => result.AsCompletedTask());
        
        public async Task<Result<T, Exception>> RunSafe(OperationContext context) {
            try {
                return await Run(context);
            } catch (Exception e) {
                return e;
            }
        }

        public Operation WithoutResult() => new(EstimatedSteps, Run);

        public Operation<TRes> Map<TRes>(Func<T, TRes> mapper) => 
            new(EstimatedSteps, ctx => Run(ctx).Map(mapper));
        
        public Operation<T> Step(string label, Func<Task> step) => new(
            EstimatedSteps + 1,
            async ctx => {
                var res = await Run(ctx);
                ctx.OnStepStarted(label);
                await step();
                ctx.OnStepCompleted(EstimatedSteps + 1);
                return res;
            }
        );
        
        public Operation<TRes> Step<TRes>(string label, Func<Task<TRes>> step) => new(
            EstimatedSteps + 1,
            async ctx => {
                _ = await Run(ctx);
                ctx.OnStepStarted(label);
                var res = await step();
                ctx.OnStepCompleted(EstimatedSteps + 1);
                return res;
            }
        );

        public Operation<TRes> Step<TRes>(string label, Func<T, Task<TRes>> step) =>
            new(EstimatedSteps + 1, async ctx => {
                var curr = await Run(ctx);
                ctx.OnStepStarted(label);
                var res = await step(curr);
                ctx.OnStepCompleted(EstimatedSteps + 1);
                return res;
            });

        public Operation<T> Chain(Operation other) => new(
            EstimatedSteps + other.EstimatedSteps,
            async ctx => {
                var res = await Run(ctx);
                await other.Run(ctx with {OnStepCompleted = step => ctx.OnStepCompleted(step + EstimatedSteps)});
                return res;
            }
        );
        
        public Operation<TRes> Chain<TRes>(Operation<TRes> other) =>
            new(
                EstimatedSteps + other.EstimatedSteps,
                async ctx => {
                    var _ = await Run(ctx);
                    return await other.Run(
                        ctx with {OnStepCompleted = step => ctx.OnStepCompleted(step + EstimatedSteps)}
                    );
                }
            );

        public Operation<TRes> Chain<TRes>(Operation<T, TRes> other) =>
            new(
                EstimatedSteps + other.EstimatedSteps,
                async ctx => {
                    var res = await Run(ctx);
                    return await other.Run(
                        new OperationContext<T>(
                            res,
                            step => ctx.OnStepCompleted(step + EstimatedSteps),
                            ctx.OnStepStarted
                        )
                    );
                }
            );

        // TODO: how to flatMap?
        
        // TODO: how to update step count estimate? context?
        // public Operation<TRes> FlatMap<TRes>(int stepCountPrediction, Func<T, Operation<TRes>> mapper) => 
        //     new(Steps + stepCountPrediction, async ctx => {
        //         var result = await Code(ctx);
        //         var newOp = mapper(result);
        //         return newOp.
        //     })
    }

    public record Operation<TStart, TResult>(int EstimatedSteps, Func<OperationContext<TStart>, Task<TResult>> Run)
    {
        public static Operation<TStart, TResult> From(Func<TStart, TResult> initialMapping) =>
            new(0, ctx => initialMapping(ctx.Value).AsCompletedTask());

        public async Task<Result<TResult, Exception>> RunSafe(OperationContext<TStart> context) {
            try {
                return await Run(context);
            } catch (Exception e) {
                return e;
            }
        }

        public Operation<TResult> WithStartValue(TStart startValue) => new(
            EstimatedSteps,
            ctx => Run(new(startValue, ctx.OnStepCompleted, ctx.OnStepStarted))
        );

        public Operation<TStart, TRes> Map<TRes>(Func<TResult, TRes> mapper) => 
            new(EstimatedSteps, ctx => Run(ctx).Map(mapper));
        
        public Operation<TStart, TResult> Step(string label, Func<Task> step) => new(
            EstimatedSteps + 1,
            async ctx => {
                var res = await Run(ctx);
                ctx.OnStepStarted(label);
                await step();
                ctx.OnStepCompleted(EstimatedSteps + 1);
                return res;
            }
        );
        
        public Operation<TStart, TRes> Step<TRes>(string label, Func<Task<TRes>> step) => new(
            EstimatedSteps + 1,
            async ctx => {
                _ = await Run(ctx);
                ctx.OnStepStarted(label);
                var res = await step();
                ctx.OnStepCompleted(EstimatedSteps + 1);
                return res;
            }
        );

        public Operation<TStart, TRes> Step<TRes>(string label, Func<TResult, Task<TRes>> step) =>
            new(EstimatedSteps + 1, async ctx => {
                var curr = await Run(ctx);
                ctx.OnStepStarted(label);
                var res = await step(curr);
                ctx.OnStepCompleted(EstimatedSteps + 1);
                return res;
            });

        public Operation<TStart, TResult> Chain(Operation other) => new(
            EstimatedSteps + other.EstimatedSteps,
            async ctx => {
                var res = await Run(ctx);
                await other.Run(new(step => ctx.OnStepCompleted(step + EstimatedSteps), ctx.OnStepStarted));
                return res;
            }
        );
        
        public Operation<TStart, TRes> Chain<TRes>(Operation<TRes> other) =>
            new(
                EstimatedSteps + other.EstimatedSteps,
                async ctx => {
                    var _ = await Run(ctx);
                    return await other.Run(new(step => ctx.OnStepCompleted(step + EstimatedSteps), ctx.OnStepStarted));
                }
            );

        public Operation<TStart, TRes> Chain<TRes>(Operation<TResult, TRes> other) =>
            new(
                EstimatedSteps + other.EstimatedSteps,
                async ctx => {
                    var res = await Run(ctx);
                    return await other.Run(
                        new OperationContext<TResult>(
                            res,
                            step => ctx.OnStepCompleted(step + EstimatedSteps),
                            ctx.OnStepStarted
                        )
                    );
                }
            );
    }
}