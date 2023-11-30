#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace BII.WasaBii.Core {
    
    public static class MoreLinqExtensions {
        
        /// <summary>
        /// Preserves the order the elements will have in the code.
        /// E.g. <c>1.PrependTo({2, 3, 4, 5}) = {1, 2, 3, 4, 5}</c>
        /// as opposed to
        /// <c>{2, 3, 4, 5}.Prepend(1) = {1, 2, 3, 4, 5}.</c>
        /// </summary>
        public static IEnumerable<T> PrependTo<T>(this T head, IEnumerable<T> tail) => tail.Prepend(head);
        
        public static bool Any<T1, T2>(
            this IEnumerable<(T1, T2)> enumerable, Func<T1, T2, bool> mapping
        ) => enumerable.Any(tuple => mapping(tuple.Item1, tuple.Item2));
        
        public static bool Any<T1, T2, T3>(
            this IEnumerable<(T1, T2, T3)> enumerable, Func<T1, T2, T3, bool> mapping
        ) => enumerable.Any(tuple => mapping(tuple.Item1, tuple.Item2, tuple.Item3));
        
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, params T[] except) =>
            // Intentionally not used as extension method to prevent recursion 
            Enumerable.Except(source, except);

        public static IEnumerable<T> SkipLast<T>(this IReadOnlyCollection<T> collection, int count) => 
            collection.Take(collection.Count - count);
        
        /// <summary>
        /// Finds and returns the minimum element of <paramref name="source"/> by
        /// mapping each element to an <see cref="IComparable{T}"/>.
        /// If there are multiple minimum elements, the first one is returned.
        /// Returns None if the <see cref="source"/> is empty.
        /// </summary>
        public static Option<TSource> MinBy<TSource, TComparable>(
            this IEnumerable<TSource> source, Func<TSource, TComparable> comparableSelector
        ) where TComparable : IComparable<TComparable> => minOrMaxBy(source, comparableSelector, prefersPositiveComparisonResult: false);

        /// <summary>
        /// Finds and returns the maximum element of <paramref name="source"/> by
        /// mapping each element to an <see cref="IComparable{T}"/>.
        /// If there are multiple maximum elements, the first one is returned.
        /// Returns None if the <see cref="source"/> is empty.
        /// </summary>
        public static Option<TSource> MaxBy<TSource, TComparable>(
            this IEnumerable<TSource> source, Func<TSource, TComparable> comparableSelector
        ) where TComparable : IComparable<TComparable> => minOrMaxBy(source, comparableSelector, prefersPositiveComparisonResult: true);

        private static Option<TSource> minOrMaxBy<TSource, TComparable>(
            IEnumerable<TSource> source, Func<TSource, TComparable> comparableSelector, bool prefersPositiveComparisonResult
        ) where TComparable : IComparable<TComparable>
            => source.IfNotEmpty(
                notEmpty => notEmpty
                    .Select(t => (val: t, comp: comparableSelector(t)))
                    .Aggregate((l, r) => l.comp.CompareTo(r.comp) switch {
                        > 0 => prefersPositiveComparisonResult ? l : r,
                        0 => l,
                        < 0 => prefersPositiveComparisonResult ? r : l
                    })
                    .val.Some(),
                elseResult: Option.None
            );
        
        public static bool TryFindIndexOf<T>(this IEnumerable<T> collection, Predicate<T> predicate, out int index) {
            using var enumerator = collection.GetEnumerator();
            for (index = 0; enumerator.MoveNext(); index++) {
                if (predicate(enumerator.Current)) return true;
            }

            index = -1;
            return false;
        }

        public static bool TryFindIndexOf<T>(this IEnumerable<T> collection, T element, out int index) => 
            TryFindIndexOf(collection, current => Equals(element, current), out index);
        
        /// <summary>
        /// Uses the <a href="https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle">Fisher–Yates algorithm</a>
        /// to shuffle the passed collection. Returns a new array with the shuffled elements.
        /// If the passed enumerable is only iterable once, then it is consumed in the process.
        /// </summary>
        public static T[] Shuffled<T>(this IEnumerable<T> l, Random? random = null) {
            random ??= new Random();
            var list = l.ToArray(); // force shallow copy
            var n = list.Length;
            while (n > 1) {
                n--;
                var k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }

            return list;
        }
        
        /// <exception cref="ArgumentException"> When the passed <paramref name="enumerable"/> is empty. </exception>
        public static T Average<T>(
            this IEnumerable<T> enumerable,
            Func<T, T, T> addition,
            Func<T, int, T> division
        ) {
            using var it = enumerable.GetEnumerator();
            if (!it.MoveNext()) 
                throw new ArgumentException("Cannot average over an empty enumerable.");
            var count = 1;
            var sum = it.Current;
            while (it.MoveNext()) {
                count += 1;
                sum = addition(sum, it.Current);
            }
            return division(sum, count);
        }
    }
}