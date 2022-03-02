using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace BII.WasaBii.Core {
    public static class PairwiseEnumerableExtensions {
        public static IEnumerable<IReadOnlyList<T>> Grouped<T>(this IEnumerable<T> source, int groupSize = 2, bool withPartial = true) {
            if (groupSize < 2)
                throw new ArgumentException($"The group size ({groupSize}) cannot be less than 2!");

            var sourceArray = source.AsArray();

            var totalGroups = withPartial 
                ? (int) Math.Ceiling(sourceArray.Length / (float) groupSize)
                : sourceArray.Length / groupSize;
            
            for (var group = 0; group < totalGroups; ++group) {
                yield return new ArraySegment<T>(sourceArray, offset: group * groupSize, groupSize);
            }
        }

        public static IEnumerable<IReadOnlyList<T>> Sliding<T>(this IEnumerable<T> source, int slideSize = 2) {
            if (slideSize < 2)
                throw new ArgumentException($"The slide size ({slideSize}) cannot be less than 2!");
          
            var sourceArray = source.AsArray();
            
            for (var offset = 0; offset <= sourceArray.Length - slideSize; ++offset)
                yield return new ArraySegment<T>(sourceArray, offset, slideSize);
        }
        
        public static IEnumerable<(T, T)> PairwiseGrouped<T>(this IEnumerable<T> source) =>
            source.Grouped(groupSize: 2).Select(toTuple);
        
        public static IEnumerable<(T, T)> PairwiseSliding<T>(this IEnumerable<T> source) =>
            source.Sliding(slideSize: 2).Select(toTuple);

        private static (T, T) toTuple<T>(IReadOnlyList<T> source) {
            Contract.Assert(source.Count == 2);
            return (source[0], source[1]);
        }
    }
}