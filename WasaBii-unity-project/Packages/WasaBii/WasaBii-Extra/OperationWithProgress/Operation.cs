using System;
using System.Threading.Tasks;
using BII.WasaBii.Core;

namespace BII.WasaBii.Extra
{
    public record OperationContext(Action<int> OnStepCompleted, Action<string> OnStepStarted);
    public record OperationContext<T>(T Value, Action<int> OnStepCompleted, Action<string> OnStepStarted);
    
    public record Operation(int Steps, Func<OperationContext, Task> Code)
    {
        public static Operation Empty => new Operation(0, ctx => Task.CompletedTask);

        public async Task<Result<Nothing, Exception>> Run(Action<int> onStepCompleted, Action<string> onStepStarted) {
            try {
                await Code(new(onStepCompleted, onStepStarted));
                return new Nothing();
            } catch (Exception e) {
                return e;
            }
        }

        public Operation<T> WithResult<T>(Func<T> resultGetter) => new(
            Steps,
            async ctx => {
                await Code(ctx);
                return resultGetter();
            }
        );

        public Operation Step(string label, Func<Task> step) => new(
            Steps + 1,
            async ctx => {
                await Code(ctx);
                ctx.OnStepStarted(label);
                await step();
                ctx.OnStepCompleted(Steps + 1);
            }
        );
        
        public Operation<T> Step<T>(string label, Func<Task<T>> step) => new(
            Steps + 1,
            async ctx => {
                await Code(ctx);
                ctx.OnStepStarted(label);
                var res = await step();
                ctx.OnStepCompleted(Steps + 1);
                return res;
            }
        );

        public Operation Chain(Operation op) => new(
            Steps + op.Steps,
            async ctx => {
                await Code(ctx);
                await op.Code(ctx with {OnStepCompleted = step => ctx.OnStepCompleted(Steps + step)});
            }
        );
        
        public Operation<T> Chain<T>(Operation<T> op) => new(
            Steps + op.Steps,
            async ctx => {
                await Code(ctx);
                return await op.Code(ctx with {OnStepCompleted = step => ctx.OnStepCompleted(Steps + step)});
            }
        );
    }

    public record Operation<T>(int Steps, Func<OperationContext, Task<T>> Code)
    {
        public static Operation<T> From(T result) =>
            new(0, ctx => result.AsCompletedTask());
        
        public async Task<Result<T, Exception>> Run(Action<int> onStepCompleted, Action<string> onStepStarted) {
            try {
                return await Code(new(onStepCompleted, onStepStarted));
            } catch (Exception e) {
                return e;
            }
        }

        public Operation WithoutResult() => new(Steps, Code);

        public Operation<TRes> Map<TRes>(Func<T, TRes> mapper) => 
            new(Steps, ctx => Code(ctx).Map(mapper));
        
        public Operation<T> Step(string label, Func<Task> step) => new(
            Steps + 1,
            async ctx => {
                var res = await Code(ctx);
                ctx.OnStepStarted(label);
                await step();
                ctx.OnStepCompleted(Steps + 1);
                return res;
            }
        );
        
        public Operation<TRes> Step<TRes>(string label, Func<Task<TRes>> step) => new(
            Steps + 1,
            async ctx => {
                _ = await Code(ctx);
                ctx.OnStepStarted(label);
                var res = await step();
                ctx.OnStepCompleted(Steps + 1);
                return res;
            }
        );

        public Operation<TRes> Step<TRes>(string label, Func<T, Task<TRes>> step) =>
            new(Steps + 1, async ctx => {
                var curr = await Code(ctx);
                ctx.OnStepStarted(label);
                var res = await step(curr);
                ctx.OnStepCompleted(Steps + 1);
                return res;
            });

        public Operation<T> Chain(Operation other) => new(
            Steps + other.Steps,
            async ctx => {
                var res = await Code(ctx);
                await other.Code(ctx with {OnStepCompleted = step => ctx.OnStepCompleted(step + Steps)});
                return res;
            }
        );
        
        public Operation<TRes> Chain<TRes>(Operation<TRes> other) =>
            new(
                Steps + other.Steps,
                async ctx => {
                    var _ = await Code(ctx);
                    return await other.Code(
                        ctx with {OnStepCompleted = step => ctx.OnStepCompleted(step + Steps)}
                    );
                }
            );

        public Operation<TRes> Chain<TRes>(Operation<T, TRes> other) =>
            new(
                Steps + other.Steps,
                async ctx => {
                    var res = await Code(ctx);
                    return await other.Code(
                        new OperationContext<T>(
                            res,
                            step => ctx.OnStepCompleted(step + Steps),
                            ctx.OnStepStarted
                        )
                    );
                }
            );

        // TODO: how to flatMap?
        
        // public OperationWithProgress<TRes> FlatMap<TRes>(int stepCountPrediction, Func<T, OperationWithProgress<TRes>> mapper) => new(Steps + stepCountPrediction,
        //     async ctx => {
        //         var result = await Code(ctx);
        //         var nestedOperationWithProgress = mapper(result);
        //         
        //     })
    }

    public record Operation<TStart, TResult>(int Steps, Func<OperationContext<TStart>, Task<TResult>> Code)
    {
        public static Operation<TStart, TResult> From(Func<TStart, TResult> initialMapping) =>
            new(0, ctx => initialMapping(ctx.Value).AsCompletedTask());

        public async Task<Result<TResult, Exception>> Run(
            TStart startValue,
            Action<int> onStepCompleted,
            Action<string> onStepStarted
        ) {
            try { return await Code(new(startValue, onStepCompleted, onStepStarted)); } catch (Exception e) {
                return e;
            }
        }

        public Operation<TResult> WithStartValue(TStart startValue) => new(
            Steps,
            ctx => Code(new(startValue, ctx.OnStepCompleted, ctx.OnStepStarted))
        );

        public Operation<TStart, TRes> Map<TRes>(Func<TResult, TRes> mapper) => 
            new(Steps, ctx => Code(ctx).Map(mapper));
        
        public Operation<TStart, TResult> Step(string label, Func<Task> step) => new(
            Steps + 1,
            async ctx => {
                var res = await Code(ctx);
                ctx.OnStepStarted(label);
                await step();
                ctx.OnStepCompleted(Steps + 1);
                return res;
            }
        );
        
        public Operation<TStart, TRes> Step<TRes>(string label, Func<Task<TRes>> step) => new(
            Steps + 1,
            async ctx => {
                _ = await Code(ctx);
                ctx.OnStepStarted(label);
                var res = await step();
                ctx.OnStepCompleted(Steps + 1);
                return res;
            }
        );

        public Operation<TStart, TRes> Step<TRes>(string label, Func<TResult, Task<TRes>> step) =>
            new(Steps + 1, async ctx => {
                var curr = await Code(ctx);
                ctx.OnStepStarted(label);
                var res = await step(curr);
                ctx.OnStepCompleted(Steps + 1);
                return res;
            });

        public Operation<TStart, TResult> Chain(Operation other) => new(
            Steps + other.Steps,
            async ctx => {
                var res = await Code(ctx);
                await other.Code(new(step => ctx.OnStepCompleted(step + Steps), ctx.OnStepStarted));
                return res;
            }
        );
        
        public Operation<TStart, TRes> Chain<TRes>(Operation<TRes> other) =>
            new(
                Steps + other.Steps,
                async ctx => {
                    var _ = await Code(ctx);
                    return await other.Code(new(step => ctx.OnStepCompleted(step + Steps), ctx.OnStepStarted));
                }
            );

        public Operation<TStart, TRes> Chain<TRes>(Operation<TResult, TRes> other) =>
            new(
                Steps + other.Steps,
                async ctx => {
                    var res = await Code(ctx);
                    return await other.Code(
                        new OperationContext<TResult>(
                            res,
                            step => ctx.OnStepCompleted(step + Steps),
                            ctx.OnStepStarted
                        )
                    );
                }
            );
    }
}