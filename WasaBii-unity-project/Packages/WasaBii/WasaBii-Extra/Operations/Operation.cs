using System;
using System.Threading.Tasks;
using BII.WasaBii.Core;

namespace BII.WasaBii.Extra
{
    // TODO: consider variants with Error, incorporating Result in the monad transformer stack
    // TODO: ensure that exceptions are debuggable, especially when thrown on another thread
    // TODO: consider removing "downgrading" or lossy methods

    public record Operation(int EstimatedStepCount, Func<OperationContext, Task> Run)
    {
        // TODO CR: Accurate completed step number by letting `Run` return an index

        public static Operation Empty() => new(0, _ => Task.CompletedTask);
        public static Operation<T> From<T>(T initialResult) => new(0, _ => initialResult.AsCompletedTask());
        public static Operation<TInput, TInput> WithInput<TInput>() => new(0, v => v.StartValue.AsCompletedTask());

        public async Task<Result<Nothing, Exception>> RunSafe(OperationContext context) {
            try {
                await Run(context);
                return new Nothing();
            } catch (Exception e) {
                return e;
            }
        }

        public Operation<T> WithResult<T>(Func<T> resultGetter) => new(
            EstimatedStepCount,
            async ctx => {
                await Run(ctx);
                return resultGetter();
            }
        );

        public Operation Step(string label, Func<Task> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                await step();
                ctx.OnStepCompleted(label);
            }
        );
        
        public Operation Step(string label, Func<OperationStepContext, Task> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                await step(ctx);
                ctx.OnStepCompleted(label);
            }
        );
        
        public Operation<T> Step<T>(string label, Func<OperationStepContext, Task<T>> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var res = await step(ctx);
                ctx.OnStepCompleted(label);
                return res;
            }
        );

        public Operation Chain(Operation op) => new(
            EstimatedStepCount + op.EstimatedStepCount,
            async ctx => {
                await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                await op.Run(ctx);
            }
        );
        
        public Operation<T> Chain<T>(Operation<T> op) => new(
            EstimatedStepCount + op.EstimatedStepCount,
            async ctx => {
                await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                return await op.Run(ctx);
            }
        );
        
        public Operation<TStart, TRes> Chain<TStart, TRes>(Operation<TStart, TRes> op) => new(
            EstimatedStepCount + op.EstimatedStepCount,
            async ctx => {
                await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                return await op.Run(ctx);
            }
        );
    }

    public record Operation<T>(int EstimatedStepCount, Func<OperationContext, Task<T>> Run)
    {
        public async Task<Result<T, Exception>> RunSafe(OperationContext context) {
            try {
                return await Run(context);
            } catch (Exception e) {
                return e;
            }
        }

        public Operation WithoutResult() => new(EstimatedStepCount, Run);

        public Operation<TRes> Map<TRes>(Func<T, TRes> mapper) => 
            new(EstimatedStepCount, ctx => Run(ctx).Map(mapper));
        
        public Operation<T> Step(string label, Func<Task> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var res = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                await step();
                ctx.OnStepCompleted(label);
                return res;
            }
        );
        
        public Operation<TRes> Step<TRes>(string label, Func<Task<TRes>> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                _ = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var res = await step();
                ctx.OnStepCompleted(label);
                return res;
            }
        );

        public Operation<TRes> Step<TRes>(string label, Func<OperationStepContext<T>, Task<TRes>> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var curr = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var res = await step(new(curr, ctx));
                ctx.OnStepCompleted(label);
                return res;
            }
        );
        
        public Operation<T> Step(string label, Func<OperationStepContext<T>, Task> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var res = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                await step(new(res, ctx));
                ctx.OnStepCompleted(label);
                return res;
            }
        );

        public Operation<T> Chain(Operation other) => new(
            EstimatedStepCount + other.EstimatedStepCount,
            async ctx => {
                var res = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                await other.Run(ctx);
                return res;
            }
        );
        
        public Operation<TRes> Chain<TRes>(Operation<TRes> other) =>
            new(
                EstimatedStepCount + other.EstimatedStepCount,
                async ctx => {
                    var _ = await Run(ctx);
                    ctx.CancellationToken?.ThrowIfCancellationRequested();
                    return await other.Run(ctx);
                }
            );

        public Operation<TRes> Chain<TRes>(Operation<T, TRes> other) =>
            new(
                EstimatedStepCount + other.EstimatedStepCount,
                async ctx => {
                    var res = await Run(ctx);
                    ctx.CancellationToken?.ThrowIfCancellationRequested();
                    return await other.Run(ctx.withStartValue(res));
                }
            );
        
        public Operation<TRes> FlatMap<TRes>(
            int stepCountPrediction, 
            Func<T, Operation<TRes>> mapper
        ) => new(
            EstimatedStepCount + stepCountPrediction,
            async ctx => {
                var result = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var newOp = mapper(result);
                var stepDiff = newOp.EstimatedStepCount - stepCountPrediction;
                if (stepDiff != 0) ctx.OnStepCountDiff(stepDiff);
                return await newOp.Run(ctx);
            }
        );
        
        public Operation<TStart, TRes> FlatMap<TStart, TRes>(
            int stepCountPrediction, 
            Func<T, Operation<TStart, TRes>> mapper
        ) => new(
            EstimatedStepCount + stepCountPrediction,
            async ctx => {
                var result = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var newOp = mapper(result);
                var stepDiff = newOp.EstimatedStepCount - stepCountPrediction;
                if (stepDiff != 0) ctx.OnStepCountDiff(stepDiff);
                return await newOp.Run(ctx);
            }
        );
    }

    public record Operation<TStart, TResult>(int EstimatedStepCount, Func<OperationContext<TStart>, Task<TResult>> Run)
    {
        public async Task<Result<TResult, Exception>> RunSafe(OperationContext<TStart> context) {
            try {
                return await Run(context);
            } catch (Exception e) {
                return e;
            }
        }

        public Operation<TResult> WithStartValue(TStart startValue) => 
            new(EstimatedStepCount, ctx => Run(ctx.withStartValue(startValue)));

        public Operation<TStart, TRes> Map<TRes>(Func<TResult, TRes> mapper) => 
            new(EstimatedStepCount, ctx => Run(ctx).Map(mapper));
        
        public Operation<TStart, TResult> Step(string label, Func<Task> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var res = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                await step();
                ctx.OnStepCompleted(label);
                return res;
            }
        );
        
        public Operation<TStart, TRes> Step<TRes>(string label, Func<Task<TRes>> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                _ = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var res = await step();
                ctx.OnStepCompleted(label);
                return res;
            }
        );
        
        public Operation<TStart, TResult> Step(string label, Func<OperationStepContext<TResult>, Task> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var res = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                await step(new(res, ctx));
                ctx.OnStepCompleted(label);
                return res;
            }
        );

        public Operation<TStart, TRes> Step<TRes>(string label, Func<OperationStepContext<TResult>, Task<TRes>> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var res = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var newRes = await step(new(res, ctx));
                ctx.OnStepCompleted(label);
                return newRes;
            }
        );

        public Operation<TStart, TRes> Step<TRes>(
            string label, 
            Func<OperationStepContext, TResult, Task<TRes>> step
        ) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var curr = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var res = await step(ctx, curr);
                ctx.OnStepCompleted(label);
                return res;
            }
        );

        public Operation<TStart, TResult> Chain(Operation other) => new(
            EstimatedStepCount + other.EstimatedStepCount,
            async ctx => {
                var res = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                await other.Run(ctx);
                return res;
            }
        );
        
        public Operation<TStart, TRes> Chain<TRes>(Operation<TRes> other) =>
            new(
                EstimatedStepCount + other.EstimatedStepCount,
                async ctx => {
                    var _ = await Run(ctx);
                    ctx.CancellationToken?.ThrowIfCancellationRequested();
                    return await other.Run(ctx);
                }
            );

        public Operation<TStart, TRes> Chain<TRes>(Operation<TResult, TRes> other) =>
            new(
                EstimatedStepCount + other.EstimatedStepCount,
                async ctx => {
                    var res = await Run(ctx);
                    ctx.CancellationToken?.ThrowIfCancellationRequested();
                    return await other.Run(ctx.withStartValue(res));
                }
            );
        
        public Operation<TStart, TRes> FlatMap<TRes>(
            int stepCountPrediction, 
            Func<TResult, Operation<TRes>> mapper
        ) => new(
            EstimatedStepCount + stepCountPrediction,
            async ctx => {
                var result = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var newOp = mapper(result);
                var stepDiff = newOp.EstimatedStepCount - stepCountPrediction;
                if (stepDiff != 0) ctx.OnStepCountDiff(stepDiff);
                return await newOp.Run(ctx);
            }
        );
        
        public Operation<TStart, TRes> FlatMap<TRes>(
            int stepCountPrediction, 
            Func<TResult, Operation<TStart, TRes>> mapper
        ) => new(
            EstimatedStepCount + stepCountPrediction,
            async ctx => {
                var result = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var newOp = mapper(result);
                var stepDiff = newOp.EstimatedStepCount - stepCountPrediction;
                if (stepDiff != 0) ctx.OnStepCountDiff(stepDiff);
                return await newOp.Run(ctx);
            }
        );
    }
}