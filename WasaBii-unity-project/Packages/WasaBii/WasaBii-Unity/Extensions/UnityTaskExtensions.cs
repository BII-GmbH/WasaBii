#nullable enable

using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BII.WasaBii.Unity {
    public static class UnityTaskExtensions {
        // Note DG: Because Unity sucks, there is no typed variant of a resource request :/
        /// Throws an exception if the provided resource is not of the given type.
        public static Task<T> AsTask<T>(
            this ResourceRequest request, 
            Action<float>? reportProgressEachFrame = null
        ) where T : Object {
            var res = new TaskCompletionSource<T>();

            if (reportProgressEachFrame != null)
                Coroutines.RepeatWhile(
                    condition: () => !request.isDone, 
                    action: () => reportProgressEachFrame.Invoke(request.progress)
                ).Start();
            
            request.completed += _ => {
                Debug.Assert(request.asset != null);
                res.SetResult(
                    request.asset as T ?? throw new Exception(
                        $"The result of the operation {request.asset} was not of the expected type {typeof(T)}")
                );
            };
            
            return res.Task;
        } 
        
        /// Convert a Task into an IEnumerator intended to be used as a Unity-Coroutine
        public static IEnumerator AsCoroutine(this Task task) {
            while (!task.IsCompleted) yield return null;
            task.GetAwaiter().GetResult();
        }
    }
}