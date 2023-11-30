using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BII.WasaBii.Core {
    public static class TaskExtensions {
        
        public static async Task AndThen(this Task task, Func<Task> func) {
            await task;
            await func();
        }
        
        public static async Task AndThen<TResult>(this Task<TResult> task, Action<TResult> func) =>
            func(await task);
        public static async Task<TMapped> Map<TResult, TMapped>(this Task<TResult> task, Func<TResult, TMapped> func) =>
            func(await task);
        
        public static async Task<TMapped> FlatMap<TResult, TMapped>(this Task<TResult> task, Func<TResult, Task<TMapped>> func) =>
            await func(await task);
        
        public static async Task FlatMap<TResult>(this Task<TResult> task, Func<TResult, Task> func) =>
            await func(await task);
        
        /// Wrap <typeparam name="T"/> in a Task that is instantly finished
        public static Task<T> AsCompletedTask<T>(this T result) => Task.FromResult(result);

        /// If the task has already completed, returns the result without the need to await the task.
        /// Returns None if the task has not completed yet or if it is canceled or failed.
        public static Option<T> GetIfCompletedSuccessfully<T>(this Task<T> task) => 
            Option.If(task.Status == TaskStatus.RanToCompletion, () => task.Result);

        public static async Task<Option<T>> NoneIfFailed<T>(this Task<T> task) {
            try {
                return Option.Some(await task);
            } catch {
                return Option.None;
            }
        }
        
        public static async Task<Result<T, TError>> FailureIfException<T, TError>(
            this Task<T> task, Func<Exception, TError> onException
        ) {
            try {
                return (await task).Success();
            } catch (Exception e) {
                return onException(e).Failure();
            }
        }

        public static Task<Result<S, F>> Flip<S, F>(this Result<Task<S>, F> result) => result.Match(
            async success => (await success).Success<S, F>(),
            failure => failure.Failure<S, F>().AsCompletedTask()
        );

        public static Task<IEnumerable<T>> Flip<T>(this IEnumerable<Task<T>> tasks) => 
            Task.WhenAll(tasks).Map(array => (IEnumerable<T>) array);
    }

}