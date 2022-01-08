﻿using System;
using System.Collections;
using System.Threading.Tasks;
using BII.WasaBii.Coroutines;

namespace BII.Utilities.Independent {
    public static class AsyncUtils {
        public static async Task RepeatUntilCancelled(Func<Task> toRepeat, Action afterEachIteration = null) {
            while (true) {
                try {
                    await toRepeat();
                } catch (TaskCanceledException) {
                    break;
                } finally {
                    afterEachIteration?.Invoke();
                }
            }
        }

        public static async Task<T> WithCustomCancelResult<T>(Func<Task<T>> toExecute, Func<T> cancelResult) {
            try {
                return await toExecute();
            } catch (TaskCanceledException) {
                return cancelResult();
            }
        }


        /// <param name="disposeAction">
        /// Code that is executed either if task of <paramref name="toExecute"/>
        /// completes, is cancelled or fails with an exception. 
        /// </param>
        public static async Task<T> WithCustomDisposeAction<T>(Func<Task<T>> toExecute, Action disposeAction) {
            try {
                return await toExecute();
            } finally {
                disposeAction();
            }
        }
        
        public static Task StartCoroutineAndDelayUntilFinished(IEnumerator coroutineToStart) {
            // The result of the completion source does not matter
            var taskCompletionSource = new TaskCompletionSource<object>();
            coroutineToStart.Afterwards(() => taskCompletionSource.SetResult(null)).Start();
            return taskCompletionSource.Task;
        }

        public static Task DelayForFrames(uint frames) => 
            StartCoroutineAndDelayUntilFinished(Coroutines.DelayForFrames(frames));
    }
}