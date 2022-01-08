using System.Collections;
using System.Threading.Tasks;

namespace BII.WasaBii.Unity {

    /// <summary>
    /// Holds utility functions which enable you to await real
    /// time in linear Unity code using coroutines under the hood.
    /// </summary>
    public static class AsyncWait {

        private static Task waitFor(IEnumerator coroutine) {
            var taskCompletionSource = new TaskCompletionSource<object>();
            coroutine.Afterwards(() => taskCompletionSource.SetResult(null)).Start();
            return taskCompletionSource.Task;
        }

        public static Task ForFrames(uint n) => waitFor(Coroutines.DelayForFrames(n));
        public static Task ForSeconds(float seconds) => waitFor(Coroutines.WaitForSeconds(seconds));
    }
}