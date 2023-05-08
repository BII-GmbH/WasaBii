using System;
using System.Threading.Tasks;
using BII.WasaBii.Core;

namespace BII.WasaBii.Extra
{
    // TODO: consider removing Step()s in error stack trace

    public sealed class Operation
    {
        public static Operation Empty() => new(0, _ => Task.FromResult(0));
        public static Operation<T> From<T>(T initialResult) => new(0, _ => (initialResult, 0).AsCompletedTask());
        public static Operation<TInput, TInput> WithInput<TInput>() => new(0, v => (v.StartValue, 0).AsCompletedTask());

        public readonly int EstimatedStepCount;
        internal readonly Func<OperationContext, Task<int>> run;

        public Operation(int estimatedStepCount, Func<OperationContext, Task<int>> run) {
            EstimatedStepCount = estimatedStepCount;
            this.run = run;
        }

        public Task Run(OperationContext context) => run(context);

        public async Task<Result<Nothing, Exception>> RunSafe(OperationContext context) {
            try {
                await run(context);
                return new Nothing();
            } catch (Exception e) {
                return e;
            }
        }

        public Operation<T> WithResult<T>(Func<T> resultGetter) => new(
            EstimatedStepCount,
            async ctx => {
                var steps = await run(ctx);
                return (resultGetter(), steps);
            }
        );
        
        public Operation Step(string label, Func<OperationStepContext, Task> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var steps = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                await step(ctx);
                ctx.OnStepCompleted(steps + 1);
                return steps + 1;
            }
        );
        
        public Operation<T> Step<T>(string label, Func<OperationStepContext, Task<T>> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var steps = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var res = await step(ctx);
                ctx.OnStepCompleted(steps + 1);
                return (res, steps + 1);
            }
        );

        public Operation Chain(Operation op) => new(
            EstimatedStepCount + op.EstimatedStepCount,
            async ctx => {
                var steps = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var chainedSteps = await op.run(ctx with { StepOffset = steps });
                return steps + chainedSteps;
            }
        );
        
        public Operation<T> Chain<T>(Operation<T> op) => new(
            EstimatedStepCount + op.EstimatedStepCount,
            async ctx => {
                var steps = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var (res, chainedSteps) = await op.run(ctx with { StepOffset = steps });
                return (res, steps + chainedSteps);
            }
        );
        
        public Operation<TStart, TRes> Chain<TStart, TRes>(Operation<TStart, TRes> op) => new(
            EstimatedStepCount + op.EstimatedStepCount,
            async ctx => {
                var steps = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var (res, chainedSteps) = await op.run(ctx with { StepOffset = steps });
                return (res, steps + chainedSteps);
            }
        );
    }

    public sealed class Operation<T>
    {
        public readonly int EstimatedStepCount;
        internal readonly Func<OperationContext, Task<(T Result, int DoneSteps)>> run;

        public Operation(int estimatedStepCount, Func<OperationContext, Task<(T Result, int DoneSteps)>> run) {
            EstimatedStepCount = estimatedStepCount;
            this.run = run;
        }

        public Task<T> Run(OperationContext context) => run(context).Map(r => r.Result);
        
        public async Task<Result<T, Exception>> RunSafe(OperationContext context) {
            try {
                return (await run(context)).Item1;
            } catch (Exception e) {
                return e;
            }
        }

        public Operation WithoutResult() => new(EstimatedStepCount, async ctx => (await run(ctx)).DoneSteps);

        public Operation<TRes> Map<TRes>(Func<T, TRes> mapper) => 
            new(EstimatedStepCount, ctx => run(ctx).Map(t => (mapper(t.Result), t.DoneSteps)));

        public Operation<TRes> Step<TRes>(string label, Func<OperationStepContext<T>, Task<TRes>> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var (curr, steps) = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var res = await step(new(curr, ctx));
                var finalSteps = ctx.StepOffset + steps + 1;
                ctx.OnStepCompleted(finalSteps);
                return (res, finalSteps);
            }
        );
        
        public Operation<T> Step(string label, Func<OperationStepContext<T>, Task> step) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var (res, steps) = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                await step(new(res, ctx));
                var finalSteps = ctx.StepOffset + steps + 1;
                ctx.OnStepCompleted(finalSteps);
                return (res, finalSteps);
            }
        );

        public Operation<T> Chain(Operation other) => new(
            EstimatedStepCount + other.EstimatedStepCount,
            async ctx => {
                var (res, steps) = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var chainedSteps = await other.run(ctx with { StepOffset = steps });
                return (res, chainedSteps);
            }
        );

        public Operation<TRes> Chain<TRes>(Operation<T, TRes> other) =>
            new(
                EstimatedStepCount + other.EstimatedStepCount,
                async ctx => {
                    var (curr, steps) = await run(ctx);
                    ctx.CancellationToken?.ThrowIfCancellationRequested();
                    var (res, chainedSteps) = await other.run(ctx.withStartValue(curr) with { StepOffset = steps });
                    return (res, chainedSteps);
                }
            );
        
        public Operation<TRes> FlatMap<TRes>(
            int stepCountPrediction, 
            Func<T, Operation<TRes>> mapper
        ) => new(
            EstimatedStepCount + stepCountPrediction,
            async ctx => {
                var (result, steps) = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var newOp = mapper(result);
                var stepDiff = newOp.EstimatedStepCount - stepCountPrediction;
                if (stepDiff != 0) ctx.OnStepCountDiff(stepDiff);
                var (res, newSteps) = await newOp.run(ctx with { StepOffset = steps });
                return (res, newSteps);
            }
        );
        
        public Operation<TStart, TRes> FlatMap<TStart, TRes>(
            int stepCountPrediction, 
            Func<T, Operation<TStart, TRes>> mapper
        ) => new(
            EstimatedStepCount + stepCountPrediction,
            async ctx => {
                var (result, steps) = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var newOp = mapper(result);
                var stepDiff = newOp.EstimatedStepCount - stepCountPrediction;
                if (stepDiff != 0) ctx.OnStepCountDiff(stepDiff);
                var (res, newSteps) = await newOp.run(ctx with { StepOffset = steps });
                return (res, newSteps);
            }
        );
    }

    public sealed class Operation<TStart, TResult>
    {
        public readonly int EstimatedStepCount;
        internal readonly Func<OperationContext<TStart>, Task<(TResult Result, int DoneSteps)>> run;

        public Operation(
            int estimatedStepCount, 
            Func<OperationContext<TStart>, Task<(TResult Result, int DoneSteps)>> run
        ) {
            EstimatedStepCount = estimatedStepCount;
            this.run = run;
        }

        public Task<TResult> Run(OperationContext<TStart> context) => run(context).Map(r => r.Result);
        
        public async Task<Result<TResult, Exception>> RunSafe(OperationContext<TStart> context) {
            try {
                return (await run(context)).Result;
            } catch (Exception e) {
                return e;
            }
        }

        public Operation<TResult> WithStartValue(TStart startValue) => 
            new(EstimatedStepCount, ctx => run(ctx.withStartValue(startValue)));

        public Operation<TStart, TRes> Map<TRes>(Func<TResult, TRes> mapper) => 
            new(EstimatedStepCount, ctx => run(ctx).Map(t => (mapper(t.Result), t.DoneSteps)));
        
        public Operation<TStart, TResult> Step(
            string label, 
            Func<OperationStepContext<TResult>, Task> step
        ) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var (res, steps) = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                await step(new(res, ctx));
                var finalSteps = ctx.StepOffset + steps + 1;
                ctx.OnStepCompleted(finalSteps);
                return (res, finalSteps);
            }
        );

        public Operation<TStart, TRes> Step<TRes>(
            string label, 
            Func<OperationStepContext<TResult>, Task<TRes>> step
        ) => new(
            EstimatedStepCount + 1,
            async ctx => {
                var (res, steps) = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                ctx.OnStepStarted(label);
                var newRes = await step(new(res, ctx));
                var finalSteps = ctx.StepOffset + steps + 1;
                ctx.OnStepCompleted(finalSteps);
                return (newRes, finalSteps);
            }
        );

        public Operation<TStart, TResult> Chain(Operation other) => new(
            EstimatedStepCount + other.EstimatedStepCount,
            async ctx => {
                var (res, steps) = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var chainedSteps = await other.run(ctx with { StepOffset = steps });
                return (res, chainedSteps);
            }
        );
        
        // Note CR: Cannot get rid of this unless we introduce an Operation with start value but without result
        public Operation<TStart, TRes> Chain<TRes>(Operation<TRes> other) =>
            new(
                EstimatedStepCount + other.EstimatedStepCount,
                async ctx => {
                    var (_, steps) = await run(ctx);
                    ctx.CancellationToken?.ThrowIfCancellationRequested();
                    var (res, chainedSteps) = await other.run(ctx with { StepOffset = steps });
                    return (res, chainedSteps);
                }
            );

        public Operation<TStart, TRes> Chain<TRes>(Operation<TResult, TRes> other) =>
            new(
                EstimatedStepCount + other.EstimatedStepCount,
                async ctx => {
                    var (curr, steps) = await run(ctx);
                    ctx.CancellationToken?.ThrowIfCancellationRequested();
                    var (res, chainedSteps) = await other.run(ctx.withStartValue(curr) with { StepOffset = steps });
                    return (res, chainedSteps);
                }
            );
        
        public Operation<TStart, TRes> FlatMap<TRes>(
            int stepCountPrediction, 
            Func<TResult, Operation<TRes>> mapper
        ) => new(
            EstimatedStepCount + stepCountPrediction,
            async ctx => {
                var (result, steps) = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var newOp = mapper(result);
                var stepDiff = newOp.EstimatedStepCount - stepCountPrediction;
                if (stepDiff != 0) ctx.OnStepCountDiff(stepDiff);
                var (res, newSteps) = await newOp.run(ctx with { StepOffset = steps });
                return (res, newSteps);
            }
        );
        
        public Operation<TStart, TRes> FlatMap<TRes>(
            int stepCountPrediction, 
            Func<TResult, Operation<TStart, TRes>> mapper
        ) => new(
            EstimatedStepCount + stepCountPrediction,
            async ctx => {
                var (result, steps) = await run(ctx);
                ctx.CancellationToken?.ThrowIfCancellationRequested();
                var newOp = mapper(result);
                var stepDiff = newOp.EstimatedStepCount - stepCountPrediction;
                if (stepDiff != 0) ctx.OnStepCountDiff(stepDiff);
                var (res, newSteps) = await newOp.run(ctx with { StepOffset = steps });
                return (res, newSteps);
            }
        );
    }
}