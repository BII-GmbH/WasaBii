using System;
using System.Collections.Generic;

namespace BII.WasaBii.Core {
    public static class EnumerableDeconstructionExtensions {
        
        /// Allows IEnumerables to be deconstructed into head and tail pairs, recursively:
        /// <code>var (a, (b, (_, (c, rest)))) = myList;</code>
        public static void Deconstruct<T>(this IEnumerable<T> source, out T head, out IEnumerable<T> tail) {
            var it = source.GetEnumerator();
            if (!it.MoveNext()) 
                throw new IndexOutOfRangeException("Cannot deconstruct enumerable: no elements remaining.");
            head = it.Current;
            tail = it.RemainingToEnumerable();
        }
        
        /// Behaves like <see cref="Deconstruct{T}"/> except that it returns an option
        /// instead of throwing an exception when the <see cref="source"/> is empty.
        public static Option<(T head, IEnumerable<T> tail)> TryDeconstruct<T>(this IEnumerable<T> source) {
            var it = source.GetEnumerator();
            var result = Option.If(
                it.MoveNext(),
                // Only ever executed iff the result has a value => The `.Dispose()` later on does not apply.
                // ReSharper disable AccessToDisposedClosure
                () => (it.Current, it.RemainingToEnumerable())
                // ReSharper restore AccessToDisposedClosure
            );
            if(!result.HasValue) it.Dispose();
            return result;
        }
        
    }
}