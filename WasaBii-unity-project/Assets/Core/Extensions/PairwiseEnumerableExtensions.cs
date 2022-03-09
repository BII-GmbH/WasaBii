using System;
using System.Collections.Generic;
using System.Linq;

namespace BII.WasaBii.Core {
    public static class PairwiseEnumerableExtensions {
        
        public static IEnumerable<IReadOnlyList<T>> Grouped<T>(this IReadOnlyList<T> source, int groupSize = 2, bool withPartial = true) {
            if (groupSize < 2)
                throw new ArgumentException($"The group size ({groupSize}) cannot be less than 2!");

            var totalGroups = withPartial 
                ? (int) Math.Ceiling(source.Count / (float) groupSize)
                : source.Count / groupSize;

            return Enumerable.Range(0, totalGroups)
                .Select(group => (IReadOnlyList<T>) 
                    new ReadOnlyListSegment<T>(
                        source, 
                        offset: group * groupSize,
                        count: Math.Min(groupSize, source.Count - group * groupSize)
                    ));
        }

        public static IEnumerable<IReadOnlyList<T>> LazyGrouped<T>(this IEnumerable<T> source, int groupSize = 2, bool withPartial = true) {
            if (groupSize < 2)
                throw new ArgumentException($"The group size ({groupSize}) cannot be less than 2!");

            using var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext()) {
                var currentGroup = new List<T> { enumerator.Current };
                for(var i = 0; i < groupSize && enumerator.MoveNext(); i++)
                    currentGroup.Add(enumerator.Current);
                
                if (currentGroup.Count == groupSize || currentGroup.Count > 0 && withPartial)
                    yield return currentGroup;
            }
            
        }

        public static IEnumerable<IReadOnlyList<T>> Sliding<T>(this IReadOnlyList<T> source, int slideSize = 2) {
            if (slideSize < 2)
                throw new ArgumentException($"The slide size ({slideSize}) cannot be less than 2!");

            return Enumerable.Range(0, source.Count - slideSize + 1)
                .Select(offset => (IReadOnlyList<T>) new ReadOnlyListSegment<T>(source, offset, slideSize));
        }
        
        public static IEnumerable<IReadOnlyList<T>> LazySliding<T>(this IEnumerable<T> source, int slideSize = 2) {
            if (slideSize < 2)
                throw new ArgumentException($"The slide size ({slideSize}) cannot be less than 2!");

            using var enumerator = source.GetEnumerator();
            var currentElements = new Queue<T>();
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
        
        public static IEnumerable<(T, T)> PairwiseGrouped<T>(this IEnumerable<T> source) {
            using var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext()) {
                var l = enumerator.Current;
                if (enumerator.MoveNext()) {
                    yield return (l, enumerator.Current);
                } else yield break;
            }
        }

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