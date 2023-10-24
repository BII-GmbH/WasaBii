using System;
using System.Collections.Generic;

namespace BII.WasaBii.Core {
    
    public static class ForEachExtensions {
        
        /// <summary>
        /// Executes the specified action with side effects for each element in this sequence,
        /// thereby consuming the sequence if it was only iterable once.
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action) {
            foreach (var item in sequence) action(item);
        }

        /// <inheritdoc cref="ForEach{T}(System.Collections.Generic.IEnumerable{T},System.Action{T})"/>
        public static void ForEach<T1, T2>(this IEnumerable<(T1, T2)> sequence, Action<T1, T2> action) {
            foreach (var (t1, t2) in sequence) action(t1, t2);
        }

        /// <summary>
        /// Executes the specified action with side effects for each element in this sequence,
        /// thereby consuming the sequence if it was only iterable once. The action also takes
        /// the index of the element as second argument, thus allowing you to potentially replace
        /// simple counting for loops with this function.
        /// </summary>
        public static void ForEachWithIndex<T>(this IEnumerable<T> sequence, Action<T, int> action) => 
            sequence.ZipWithIndices().ForEach(action);
        
    }
}