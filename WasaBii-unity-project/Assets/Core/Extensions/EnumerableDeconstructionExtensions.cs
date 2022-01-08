using System;
using System.Collections.Generic;

namespace BII.WasaBii.Core {
    public static class EnumerableDeconstructionExtensions {
        /// Allows IEnumerables to be deconstructed into head and tail pairs, recursively:
        /// <code>var (a, (b, (_, (c, rest)))) = myList;</code>
        public static void Deconstruct<T>(this IEnumerable<T> source, out T head, out IEnumerable<T> tail) {
            IEnumerable<T> remainingEnumeratorToEnumerable(IEnumerator<T> remainingTail) {
                while (remainingTail.MoveNext()) yield return remainingTail.Current;
                remainingTail.Dispose();
            }

            var it = source.GetEnumerator();
            if (!it.MoveNext()) 
                throw new IndexOutOfRangeException("Cannot deconstruct enumerable: no elements remaining.");
            head = it.Current;
            tail = remainingEnumeratorToEnumerable(it);
        }
    }
}