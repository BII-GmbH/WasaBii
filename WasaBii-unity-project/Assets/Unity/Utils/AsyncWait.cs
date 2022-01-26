﻿using System;
using System.Collections;
using System.Threading.Tasks;
using BII.WasaBii.Units;
using UnityEngine;

namespace BII.WasaBii.Unity {

    /// Holds utility functions which enable you to await real
    /// time in linear Unity code using coroutines under the hood.
    public static class AsyncWait {

        public static Task ForCoroutine(IEnumerator coroutine) {
            var taskCompletionSource = new TaskCompletionSource<object>();
            coroutine.Afterwards(() => taskCompletionSource.SetResult(null)).Start();
            return taskCompletionSource.Task;
        }

        public static Task ForFrames(uint n) {
#if DEBUG
            if (!Application.isPlaying) {
                throw new Exception("cannot delay for frames while the application is not in playmode");
            }
#endif
            return ForCoroutine(Coroutines.DelayForFrames(n));
        }

        public static Task ForSeconds(Duration duration) => ForCoroutine(Coroutines.WaitForSeconds((float) duration.AsSeconds()));
    }
}