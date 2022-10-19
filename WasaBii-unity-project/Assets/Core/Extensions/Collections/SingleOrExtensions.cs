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
            if (!enumerator.MoveNext()) return then(Enumerable.Empty<TSource>());

            var single = enumerator.Current;

            return enumerator.MoveNext() ? then(completeEnumerable()) : elseResultGetter(single);
            
            IEnumerable<TSource> completeEnumerable() {
                yield return single;
                do yield return enumerator.Current;
                while (enumerator.MoveNext());
            }
        }

        public static void IfNotSingle<T>(
            this IEnumerable<T> enumerable, 
            Action<IEnumerable<T>>? thenDo, 
            Action<T>? elseDo = null
        ) {
            var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext()) {
                thenDo?.Invoke(Enumerable.Empty<T>());
            } else {
                var single = enumerator.Current;

                if (enumerator.MoveNext()) thenDo?.Invoke(completeEnumerable());
                else elseDo?.Invoke(single);
                
                IEnumerable<T> completeEnumerable() {
                    yield return single;
                    do yield return enumerator.Current;
                    while (enumerator.MoveNext());
                }
            }
        }
        
    }
}