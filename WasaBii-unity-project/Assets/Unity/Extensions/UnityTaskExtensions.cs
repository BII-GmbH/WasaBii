using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BII.WasaBii.Unity {
    public static class UnityTaskExtensions {
        // Note DG: Because Unity sucks, there is no typed variant of a resource request :/
        public static Task<T> AsTask<T>(
            this ResourceRequest request, 
            Action<float> reportProgressEachFrame = null
        ) where T : Object {
            var res = new TaskCompletionSource<T>();
            
            if(reportProgressEachFrame != null)
                Coroutines.RepeatWhile(
                    condition: () => !request.isDone, 
                    action: () => reportProgressEachFrame.Invoke(request.progress)
                ).Start();
            
            request.completed += op => {
                Contract.Assert(request.asset != null);
                res.SetResult(
                    request.asset as T ??
                    throw new Exception(
                        $"The result of the operation {request.asset} was not of the expected type {typeof(T)}"
                    )
                );
            };
            return res.Task;
        } 
    }
}