using System;
using System.Collections.Generic;
using System.Linq;

namespace BII.WasaBii.Core {
    
    public static class SingleOrExtensions {

        public static TResult IfSingle<TSource, TResult>(
            this IEnumerable<TSource> enumerable,
            Func<TSource, TResult> then,
            Func<TResult> ifEmpty,
            Func<IEnumerable<TSource>, TResult> ifMultiple
        ) => enumerable.IfNotEmpty(
            then: notEmpty => notEmpty.IfNotSingle(then: ifMultiple, elseResultGetter: then),
            elseResultGetter: ifEmpty
        );
        
        public static TResult IfNotSingle<TSource, TResult>(
            this IEnumerable<TSource> enumerable, 
            Func<IEnumerable<TSource>, TResult> then, 
            Func<TSource, TResult> elseResultGetter
        ) {
            var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext()) {
                enumerator.Dispose();
                return then(Enumerable.Empty<TSource>());
            }

            var single = enumerator.Current;
            
            if (!enumerator.MoveNext()) {
                enumerator.Dispose();
                return elseResultGetter(single);
            } 
                
            return then(enumerator.RemainingToEnumerable());
        }

        public static void IfNotSingle<T>(
            this IEnumerable<T> enumerable, 
            Action<IEnumerable<T>>? thenDo, 
            Action<T>? elseDo = null
        ) {
            var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext()) {
                enumerator.Dispose();
                thenDo?.Invoke(Enumerable.Empty<T>());
            } else {
                var single = enumerator.Current;

                if (!enumerator.MoveNext()) {
                    enumerator.Dispose();
                    elseDo?.Invoke(single);
                } else thenDo?.Invoke(enumerator.RemainingToEnumerable());
            }
        }
        
    }
}