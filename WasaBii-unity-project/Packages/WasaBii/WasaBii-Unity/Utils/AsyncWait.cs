﻿using System.Collections;
using System.Threading.Tasks;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Unity {

    /// <summary>
    /// Holds utility functions which enable you to await real
    /// time in linear Unity code using coroutines under the hood.
    /// </summary>
    public static class AsyncWait {

        public static Task ForCoroutine(IEnumerator coroutine) {
            var taskCompletionSource = new TaskCompletionSource<object>();
            coroutine.Afterwards(() => taskCompletionSource.SetResult(new object())).Start();
            return taskCompletionSource.Task;
        }

        public static Task ForFrames(uint n) => ForCoroutine(Coroutines.DelayForFrames(n));

        public static Task For(Duration duration) => ForCoroutine(Coroutines.WaitForSeconds((float) duration.AsSeconds()));
    }
}