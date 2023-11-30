using System;
using System.Collections.Generic;
using System.Linq;

namespace BII.WasaBii.Core {
    public static class EnumerableViewExtensions {
        
        /// <summary>
        /// Allows iterating a source collection in "chunks" or groups of a specified size.
        /// Useful for e.g. iterating the rows of a 2D matrix encoded in a single array.
        /// </summary>
        /// <remarks>
        /// If you don't want to allocate the source collection, use <see cref="LazyGrouped{T}"/>.
        /// </remarks>
        /// <exception cref="ArgumentException"> When the group size is less than 2 </exception>
        public static IEnumerable<ReadOnlyListSegment<T>> Grouped<T>(this IReadOnlyList<T> source, int groupSize, bool withPartial = true) {
            if (groupSize < 2)
                throw new ArgumentException($"The group size ({groupSize}) cannot be less than 2!");

            var totalGroups = withPartial 
                ? (int) Math.Ceiling(source.Count / (float) groupSize)
                : source.Count / groupSize;

            return Enumerable.Range(0, totalGroups)
                .Select(group =>
                    new ReadOnlyListSegment<T>(
                        source, 
                        offset: group * groupSize,
                        count: Math.Min(groupSize, source.Count - group * groupSize)
                    ));
        }

        /// <summary>
        /// Allows iterating a source enumerable in "chunks" or groups of a specified size.
        /// Useful for e.g. reading from a lazy enumerable in chunks.
        /// </summary>
        /// <remarks>
        /// If you have an allocated collection, prefer <see cref="Grouped{T}"/> for performance.
        /// </remarks>
        /// <exception cref="ArgumentException"> When the group size is less than 2 </exception>
        public static IEnumerable<IReadOnlyList<T>> LazyGrouped<T>(this IEnumerable<T> source, int groupSize, bool withPartial = true) {
            if (groupSize < 2)
                throw new ArgumentException($"The group size ({groupSize}) cannot be less than 2!");

            using var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext()) {
                var currentGroup = new List<T> {Capacity = groupSize};
                currentGroup.Add(enumerator.Current);
                
                for(var i = 0; i < groupSize && enumerator.MoveNext(); i++)
                    currentGroup.Add(enumerator.Current);
                
                if (currentGroup.Count == groupSize || currentGroup.Count > 0 && withPartial)
                    yield return currentGroup;
            }
        }

        /// <summary>
        /// Allows iterating a source collection in "sliding views" of a specified size.
        /// Consecutive chunks will have size-1 elements in common, with only the first and the last element changing.
        /// </summary>
        /// <example>
        /// For a slide size of 2, this will yield the parts in [] in order:
        /// <code>
        /// [1 2] 3 4 5
        /// 1 [2 3] 4 5
        /// 1 2 [3 4] 5
        /// 1 2 3 [4 5]
        /// </code>
        /// </example>
        /// <remarks>
        /// If you don't want to allocate the source collection, use <see cref="LazySliding{T}"/>.
        /// </remarks>
        /// <exception cref="ArgumentException"> When the slide size is less than 2 </exception>
        public static IEnumerable<ReadOnlyListSegment<T>> Sliding<T>(this IReadOnlyList<T> source, int slideSize) {
            if (slideSize < 2)
                throw new ArgumentException($"The slide size ({slideSize}) cannot be less than 2!");

            return Enumerable.Range(0, Math.Max(0, source.Count - slideSize + 1))
                .Select(offset => new ReadOnlyListSegment<T>(source, offset, slideSize));
        }
        
        /// <summary>
        /// Allows iterating a source enumerable in "sliding views" of a specified size.
        /// Consecutive slides will have size-1 elements in common, with only the first and the last element changing.
        /// </summary>
        /// <example>
        /// For a slide size of 2, this will yield the parts in [] in order:
        /// <code>
        /// [1 2] 3 4 5
        /// 1 [2 3] 4 5
        /// 1 2 [3 4] 5
        /// 1 2 3 [4 5]
        /// </code>
        /// </example>
        /// <remarks>
        /// If you have an allocated collection, prefer <see cref="Sliding{T}"/> for performance.
        /// </remarks>
        /// <exception cref="ArgumentException"> When the slide size is less than 2 </exception>
        public static IEnumerable<IReadOnlyList<T>> LazySliding<T>(this IEnumerable<T> source, int slideSize) {
            if (slideSize < 2)
                throw new ArgumentException($"The slide size ({slideSize}) cannot be less than 2!");

            using var enumerator = source.GetEnumerator();
            var currentElements = new Queue<T>(capacity: slideSize);
            for (var i = 0; i < slideSize; i++) {
                if (!enumerator.MoveNext()) yield break;
                currentElements.Enqueue(enumerator.Current);
            }

            yield return currentElements.ToArray();
            while (enumerator.MoveNext()) {
                currentElements.Dequeue();
                currentElements.Enqueue(enumerator.Current);
                yield return currentElements.ToArray();
            }
        }
        
        /// <summary>
        /// Equivalent <see cref="Grouped{T}"/> for a group size of 2, but returns tuples instead.
        /// </summary>
        public static IEnumerable<(T, T)> PairwiseGrouped<T>(this IEnumerable<T> source) {
            using var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext()) {
                var l = enumerator.Current;
                if (enumerator.MoveNext()) {
                    yield return (l, enumerator.Current);
                } else yield break;
            }
        }

        /// <summary>
        /// Equivalent <see cref="Sliding{T}"/> for a slide size of 2, but returns tuples instead.
        /// </summary>
        public static IEnumerable<(T, T)> PairwiseSliding<T>(this IEnumerable<T> source) {
            using var enumerator = source.GetEnumerator();
            if (enumerator.MoveNext()) {
                var l = enumerator.Current;
                while (enumerator.MoveNext()) {
                    yield return (l, enumerator.Current);
                    l = enumerator.Current;
                }
            }
        }
    }
}