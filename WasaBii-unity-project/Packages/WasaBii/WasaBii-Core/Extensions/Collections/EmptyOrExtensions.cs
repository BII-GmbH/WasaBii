#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace BII.WasaBii.Core {
    public static class EmptyOrSingleExtensions {
        
        /// <returns>True if the specified sequence contains no elements, false otherwise.</returns>
        public static bool IsEmpty<T>(this IEnumerable<T> sequence) => !sequence.Any();

        /// <returns>False if the specified sequence contains no elements, true otherwise.</returns>
        public static bool IsNotEmpty<T>(this IEnumerable<T> sequence) => sequence.Any();

        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? source) =>
            source ?? Enumerable.Empty<T>();

        public static IEnumerable<T> OrIfEmpty<T>(this IEnumerable<T> enumerable, Func<IEnumerable<T>> thenReturn) =>
            IfNotEmpty(enumerable, v => v, thenReturn);

        public static TResult IfNotEmpty<TSource, TResult>(
            this IEnumerable<TSource> enumerable, Func<IEnumerable<TSource>, TResult> then, TResult elseResult
        ) => IfNotEmpty(enumerable, then, () => elseResult);
        
        public static TResult IfNotEmpty<TSource, TResult>(
            this IEnumerable<TSource> enumerable, Func<IEnumerable<TSource>, TResult> then, Func<TResult> elseResultGetter
        ) {
            if (enumerable is IReadOnlyCollection<TSource> collection)
                return collection.Count > 0 ? then(collection) : elseResultGetter();
            
            var enumerator = enumerable.GetEnumerator();
            
            if (!enumerator.MoveNext()) {
                enumerator.Dispose();
                return elseResultGetter();
            }

            return then(enumerator.RemainingToEnumerable());
        }

        public static void IfNotEmpty<T>(
            this IEnumerable<T> enumerable, Action<IEnumerable<T>>? thenDo, Action? elseDo = null
        ) {
            if (enumerable is IReadOnlyCollection<T> collection) {
                if (collection.Count > 0)
                    thenDo?.Invoke(collection);
                else
                    elseDo?.Invoke();
                return;
            }
            
            var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext()) {
                elseDo?.Invoke();
                enumerator.Dispose();
                return;
            }

            if (thenDo == null) {
                enumerator.Dispose();
                return;
            }

            thenDo(enumerator.RemainingToEnumerable());
        }

    }
}