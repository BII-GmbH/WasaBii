using System;
using System.Collections.Generic;

namespace BII.WasaBii.Core {
    
    public static class EnumerablePartitioningExtensions {
        
        /// <summary>
        /// Divides the contents of <paramref name="source"/> based on <paramref name="selector"/>
        /// to either be part of the true or false results.
        /// </summary>
        /// <remarks>
        /// Strictly speaking, this does not create partitions, since set theories
        /// defines partitions to be non-empty. However, the results can be empty.
        /// </remarks>
        public static (IEnumerable<T> trueResults, IEnumerable<T> falseResults) PartitionBy<T>(
            this IEnumerable<T> source, Predicate<T> selector
        ) {
            var trueResults = new List<T>();
            var falseResults = new List<T>();

            foreach (var item in source) {
                if(selector(item)) trueResults.Add(item);
                else falseResults.Add(item);
            }

            return (trueResults, falseResults);
        }

        /// <summary>
        /// Splits an Enumerable into multiple smaller Enumerables.
        /// The Input Enumerable gets split between elements where the should split function returns true.
        /// </summary>
        /// <param name="enumerable"> Enumerable that should be split </param>
        /// <param name="shouldSplit"> function to determine between which elements the enumerable should be split </param>
        /// <returns> Enumerable containing the smaller split sections of the input Enumerable</returns>
        public static IEnumerable<IEnumerable<T>> SplitBy<T>(
            this IEnumerable<T> enumerable, Func<T, T, bool> shouldSplit
        ) {
            using var it = enumerable.GetEnumerator();

            var accum = new List<T>();

            if (!it.MoveNext()) yield break;
            var prev = it.Current;
            accum.Add(prev);

            while (it.MoveNext()) {
                if (shouldSplit(prev, it.Current)) {
                    yield return accum;
                    accum = new List<T> {it.Current};
                } else {
                    accum.Add(it.Current);
                }

                prev = it.Current;
            }

            yield return accum;
        }
        
    }
}