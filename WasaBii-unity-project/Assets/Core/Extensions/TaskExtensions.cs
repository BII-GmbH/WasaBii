using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        
        // TODO CR: naming: should probably be called `TryGetResultIfCompleted` or simply `TryGetResult` or sth

        /// If the task has already completed, returns the result without the need to await the task.
        /// Returns None if the task has not completed yet or if it is canceled or failed.
        public static Option<T> GetIfCompletedSuccessfully<T>(this Task<T> task) =>
            task.Status == TaskStatus.RanToCompletion ? Option.Some(task.Result) : Option.None;
        
        /// Convert a Task into an IEnumerator intended to be used as a Unity-Coroutine
        public static IEnumerator AsCoroutine(this Task task) {
            while (!task.IsCompleted) yield return null;
            task.GetAwaiter().GetResult();
        }
    }

}