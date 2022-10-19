using System;
using System.Collections.Generic;

namespace BII.WasaBii.Core {
    public static class EnumerableDeconstructionExtensions {
        
        /// Allows IEnumerables to be deconstructed into head and tail pairs, recursively:
        /// <c>var (a, (b, (_, (c, rest)))) = myList;</c>
        public static void Deconstruct<T>(this IEnumerable<T> source, out T head, out IEnumerable<T> tail) {
            // ReSharper disable once GenericEnumeratorNotDisposed // disposed in .RemainingToEnumerable()
            var it = source.GetEnumerator();
            if (!it.MoveNext()) 
                throw new IndexOutOfRangeException("Cannot deconstruct enumerable: no elements remaining.");
            head = it.Current;
            tail = it.remainingToEnumerable();
        }
        
        /// Behaves like <see cref="Deconstruct{T}"/> except that it returns an option
        /// instead of throwing an exception when the <see cref="source"/> is empty.
        public static Option<(T head, IEnumerable<T> tail)> TryDeconstruct<T>(this IEnumerable<T> source) {
            var it = source.GetEnumerator();
            var result = Option.If(
                it.MoveNext(),
                // Only ever executed immediately iff the result has a value => The `.Dispose()` later on does not apply.
                // ReSharper disable twice AccessToDisposedClosure
                () => (it.Current, it.remainingToEnumerable())
            );
            if(!result.HasValue) it.Dispose();
            return result;
        }
        
        /// All remaining elements of the enumerator as an enumerable.
        /// Disposes the consumed enumerator.
        private static IEnumerable<T> remainingToEnumerable<T>(this IEnumerator<T> enumerator) {
            using var e = enumerator;
            while (e.MoveNext()) yield return e.Current;
        }
        
    }
}