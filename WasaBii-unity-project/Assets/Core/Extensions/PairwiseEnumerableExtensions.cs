using System;
using System.Collections.Generic;
using System.Linq;

namespace BII.WasaBii.Core {
    public static class PairwiseEnumerableExtensions {
        public static IEnumerable<IEnumerable<T>> Grouped<T>(this IEnumerable<T> source, int groupSize = 2, bool withPartial = true) {
            if (groupSize < 2)
                throw new ArgumentException($"The group size ({groupSize}) cannot be less than 2!");

            // Nested because exceptions cannot be thrown in a method that yields
            IEnumerable<IEnumerable<T>> doGrouping() {
                var sourceArray = source.ToArray();

                int totalGroups = withPartial 
                    ? (int) Math.Ceiling(sourceArray.Length / (float) groupSize)
                    : (int) Math.Floor(sourceArray.Length / (float) groupSize);
                
                for (var group = 0; group < totalGroups; ++group) {
                    yield return getSlice(sourceArray, offset: group * groupSize, groupSize);
                }
            }

            return doGrouping();
        }

        public static IEnumerable<IEnumerable<T>> Sliding<T>(this IEnumerable<T> source, int slideSize = 2) {
            if (slideSize < 2)
                throw new ArgumentException($"The slide size ({slideSize}) cannot be less than 2!");
          
            // Nested because exceptions cannot be thrown in a method that yields
            IEnumerable<IEnumerable<T>> doSliding() {
                var sourceArray = source.ToArray();
                
                for (var offset = 0; offset <= sourceArray.Length - slideSize; ++offset)
                    yield return getSlice(sourceArray, offset, slideSize);
            }

            return doSliding();
        }
        
        private static IEnumerable<T> getSlice<T>(T[] sourceArray, int offset, int sliceSize) {
            for (var i = 0; i < sliceSize && offset + i < sourceArray.Length; ++i)
                yield return sourceArray[offset + i];
        }

        public static IEnumerable<(T, T)> PairwiseGrouped<T>(this IEnumerable<T> source) =>
            source.Grouped(groupSize: 2).Select(toTuple);
        
        public static IEnumerable<(T, T)> PairwiseSliding<T>(this IEnumerable<T> source) =>
            source.Sliding(slideSize: 2).Select(toTuple);

        private static (T, T) toTuple<T>(IEnumerable<T> source) {
            var it = source.GetEnumerator();
            try {
                if (!it.MoveNext()) 
                    throw new ArgumentException("The source must have exactly 2 elements");
                var first = it.Current;
                
                while (!it.MoveNext())
                    throw new ArgumentException("The source must have exactly 2 elements");
                var last = it.Current;
                
                if(it.MoveNext())
                    throw new ArgumentException("The source must have exactly 2 elements");
                return (first, last);
            } finally {
                it.Dispose();
            }
        }
    }
}