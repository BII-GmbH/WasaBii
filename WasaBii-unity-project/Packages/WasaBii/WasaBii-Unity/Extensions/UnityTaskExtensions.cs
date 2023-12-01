#nullable enable

using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BII.WasaBii.Unity {
    public static class UnityTaskExtensions {
        // Note DG: Because Unity sucks, there is no typed variant of a resource request :/
        /// <exception cref="ArgumentException">If the provided resource is not of the given type.</exception>
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
            
            request.completed += _ => res.SetResult(
                request.asset as T ?? throw new ArgumentException(
                    $"The result of the operation {request.asset} was not of the expected type {typeof(T)}")
            );
            
            return res.Task;
        } 
        
        /// <summary> Convert a Task into an IEnumerator intended to be used as a Unity-Coroutine.</summary>
        public static IEnumerator AsCoroutine(this Task task) {
            while (!task.IsCompleted) yield return null;
            task.GetAwaiter().GetResult();
        }
    }
}