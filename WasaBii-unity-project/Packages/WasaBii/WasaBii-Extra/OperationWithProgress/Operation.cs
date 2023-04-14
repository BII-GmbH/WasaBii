using System;
using System.Threading;
using System.Threading.Tasks;
using BII.WasaBii.Core;

namespace BII.WasaBii.Extra
{
    // TODOS:
    // - custom error foo and Validate(Step)

    public interface OperationStepContext
    {
        Action<double> ReportProgressInStep { get; init; }
        CancellationToken? CancellationToken { get; init; }
    }

    public record OperationContext(
        Action<string> OnStepStarted,
        Action<int> OnStepCompleted,
        Action<int> OnNewStepCount,
        Action<double> ReportProgressInStep, 
        CancellationToken? CancellationToken
    ) : OperationStepContext {
        public OperationContext<T> WithStartValue<T>(T startValue) => new(
            startValue,
            OnStepStarted,
            OnStepCompleted,
            OnNewStepCount,
            ReportProgressInStep,
            CancellationToken
        );

        public OperationContext WithStepOffset(int stepsToAdd) =>
            this with {OnStepCompleted = steps => OnStepCompleted(steps + stepsToAdd)};
        
        public OperationContext WithStepCountOffset(int stepsToAdd) =>
            this with {OnNewStepCount = steps => OnNewStepCount(steps + stepsToAdd)};
    }

    public record OperationContext<T>(
        T StartValue,
        Action<string> OnStepStarted,
        Action<int> OnStepCompleted,
        Action<int> OnNewStepCount,
        Action<double> ReportProgressInStep, 
        CancellationToken? CancellationToken
    ) : OperationContext(OnStepStarted, OnStepCompleted, OnNewStepCount, ReportProgressInStep, CancellationToken) {
        public OperationContext WithoutStartValue() => new(
            OnStepStarted,
            OnStepCompleted,
            OnNewStepCount,
            ReportProgressInStep,
            CancellationToken
        );
        
        public new OperationContext<T> WithStepOffset(int stepsToAdd) =>
            this with {OnStepCompleted = steps => OnStepCompleted(steps + stepsToAdd)};
        
        public new OperationContext<T> WithStepCountOffset(int stepsToAdd) =>
            this with {OnNewStepCount = steps => OnNewStepCount(steps + stepsToAdd)};
    }
    
    public record Operation(int EstimatedStepCount, Func<OperationContext, Task> Run)
    {
        public static Operation Empty => new(0, _ => Task.CompletedTask);

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
                ctx.OnStepCompleted(EstimatedStepCount + 1);
            }
        );
        
        public Operation Step(string label, Func<OperationStepContext, Task> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                await step(ctx);
                ctx.OnStepCompleted(EstimatedStepCount + 1);
            }
        );
        
        public Operation<T> Step<T>(string label, Func<OperationStepContext, Task<T>> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var res = await step(ctx);
                ctx.OnStepCompleted(EstimatedStepCount + 1);
                return res;
            }
        );

        public Operation Chain(Operation op) => new(
            EstimatedStepCount + op.EstimatedStepCount,
            async ctx => {
                await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                await op.Run(ctx.WithStepOffset(EstimatedStepCount));
            }
        );
        
        public Operation<T> Chain<T>(Operation<T> op) => new(
            EstimatedStepCount + op.EstimatedStepCount,
            async ctx => {
                await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                return await op.Run(ctx.WithStepOffset(EstimatedStepCount));
            }
        );
        
        public Operation<TStart, TRes> Chain<TStart, TRes>(Operation<TStart, TRes> op) => new(
            EstimatedStepCount + op.EstimatedStepCount,
            async ctx => {
                await Run(ctx.WithoutStartValue());
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                return await op.Run(ctx.WithStepOffset(EstimatedStepCount));
            }
        );
    }

    public record Operation<T>(int EstimatedStepCount, Func<OperationContext, Task<T>> Run)
    {
        public static Operation<T> From(T result) =>
            new(0, _ => result.AsCompletedTask());
        
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
                ctx.OnStepCompleted(EstimatedStepCount + 1);
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
                ctx.OnStepCompleted(EstimatedStepCount + 1);
                return res;
            }
        );

        public Operation<TRes> Step<TRes>(string label, Func<T, Task<TRes>> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var curr = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var res = await step(curr);
                ctx.OnStepCompleted(EstimatedStepCount + 1);
                return res;
            }
        );
        
        public Operation<T> Step(string label, Func<OperationStepContext, Task> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var res = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                await step(ctx);
                ctx.OnStepCompleted(EstimatedStepCount + 1);
                return res;
            }
        );
        
        public Operation<TRes> Step<TRes>(string label, Func<OperationStepContext, Task<TRes>> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                _ = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var res = await step(ctx);
                ctx.OnStepCompleted(EstimatedStepCount + 1);
                return res;
            }
        );

        public Operation<TRes> Step<TRes>(string label, Func<OperationStepContext, T, Task<TRes>> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var curr = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var res = await step(ctx, curr);
                ctx.OnStepCompleted(EstimatedStepCount + 1);
                return res;
            }
        );

        public Operation<T> Chain(Operation other) => new(
            EstimatedStepCount + other.EstimatedStepCount,
            async ctx => {
                var res = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                await other.Run(ctx.WithStepOffset(EstimatedStepCount));
                return res;
            }
        );
        
        public Operation<TRes> Chain<TRes>(Operation<TRes> other) =>
            new(
                EstimatedStepCount + other.EstimatedStepCount,
                async ctx => {
                    var _ = await Run(ctx);
                    ctx.CancellationToken?.ThrowIfCancellationRequested();
                    return await other.Run(ctx.WithStepOffset(EstimatedStepCount));
                }
            );

        public Operation<TRes> Chain<TRes>(Operation<T, TRes> other) =>
            new(
                EstimatedStepCount + other.EstimatedStepCount,
                async ctx => {
                    var res = await Run(ctx);
                    ctx.CancellationToken?.ThrowIfCancellationRequested();
                    return await other.Run(ctx.WithStartValue(res).WithStepOffset(EstimatedStepCount));
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
                ctx.OnNewStepCount(EstimatedStepCount + newOp.EstimatedStepCount);
                return await newOp.Run(ctx.WithStepOffset(EstimatedStepCount).WithStepCountOffset(EstimatedStepCount));
            }
        );
        
        public Operation<TStart, TRes> FlatMap<TStart, TRes>(
            int stepCountPrediction, 
            Func<T, Operation<TStart, TRes>> mapper
        ) => new(
            EstimatedStepCount + stepCountPrediction,
            async ctx => {
                var result = await Run(ctx.WithoutStartValue());
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var newOp = mapper(result);
                ctx.OnNewStepCount(EstimatedStepCount + newOp.EstimatedStepCount);
                return await newOp.Run(ctx.WithStepOffset(EstimatedStepCount).WithStepCountOffset(EstimatedStepCount));
            }
        );
    }

    public record Operation<TStart, TResult>(int EstimatedStepCount, Func<OperationContext<TStart>, Task<TResult>> Run)
    {
        public static Operation<TStart, TResult> From(Func<TStart, TResult> initialMapping) =>
            new(0, ctx => initialMapping(ctx.StartValue).AsCompletedTask());

        public async Task<Result<TResult, Exception>> RunSafe(OperationContext<TStart> context) {
            try {
                return await Run(context);
            } catch (Exception e) {
                return e;
            }
        }

        public Operation<TResult> WithStartValue(TStart startValue) => 
            new(EstimatedStepCount, ctx => Run(ctx.WithStartValue(startValue)));

        public Operation<TStart, TRes> Map<TRes>(Func<TResult, TRes> mapper) => 
            new(EstimatedStepCount, ctx => Run(ctx).Map(mapper));
        
        public Operation<TStart, TResult> Step(string label, Func<Task> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var res = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                await step();
                ctx.OnStepCompleted(EstimatedStepCount + 1);
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
                ctx.OnStepCompleted(EstimatedStepCount + 1);
                return res;
            }
        );

        public Operation<TStart, TRes> Step<TRes>(string label, Func<TResult, Task<TRes>> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var curr = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var res = await step(curr);
                ctx.OnStepCompleted(EstimatedStepCount + 1);
                return res;
            }
        );
        
        public Operation<TStart, TResult> Step(string label, Func<OperationStepContext, Task> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var res = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                await step(ctx);
                ctx.OnStepCompleted(EstimatedStepCount + 1);
                return res;
            }
        );

        public Operation<TStart, TRes> Step<TRes>(string label, Func<OperationStepContext, Task<TRes>> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                _ = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var res = await step(ctx);
                ctx.OnStepCompleted(EstimatedStepCount + 1);
                return res;
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
                ctx.OnStepCompleted(EstimatedStepCount + 1);
                return res;
            }
        );

        public Operation<TStart, TResult> Chain(Operation other) => new(
            EstimatedStepCount + other.EstimatedStepCount,
            async ctx => {
                var res = await Run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                await other.Run(ctx.WithoutStartValue().WithStepOffset(EstimatedStepCount));
                return res;
            }
        );
        
        public Operation<TStart, TRes> Chain<TRes>(Operation<TRes> other) =>
            new(
                EstimatedStepCount + other.EstimatedStepCount,
                async ctx => {
                    var _ = await Run(ctx);
                    ctx.CancellationToken?.ThrowIfCancellationRequested();
                    return await other.Run(ctx.WithoutStartValue().WithStepOffset(EstimatedStepCount));
                }
            );

        public Operation<TStart, TRes> Chain<TRes>(Operation<TResult, TRes> other) =>
            new(
                EstimatedStepCount + other.EstimatedStepCount,
                async ctx => {
                    var res = await Run(ctx);
                    ctx.CancellationToken?.ThrowIfCancellationRequested();
                    return await other.Run(ctx.WithStartValue(res).WithStepOffset(EstimatedStepCount));
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
                ctx.OnNewStepCount(EstimatedStepCount + newOp.EstimatedStepCount);
                return await newOp.Run(
                    ctx.WithoutStartValue().WithStepOffset(EstimatedStepCount).WithStepCountOffset(EstimatedStepCount)
                );
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
                ctx.OnNewStepCount(EstimatedStepCount + newOp.EstimatedStepCount);
                return await newOp.Run(ctx.WithStepOffset(EstimatedStepCount).WithStepCountOffset(EstimatedStepCount));
            }
        );
    }
}