using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace BII.WasaBii.Core {
    
    public static class EnumerableConversionExtensions {
        
        /// Wraps a single object into an enumerable
        public static IEnumerable<T> WrapAsEnumerable<T>(this T item) {
            yield return item;
        }

        public static IEnumerable<T> ToEnumerable<T>(this (T, T) tuple) {
            yield return tuple.Item1;
            yield return tuple.Item2;
        }

        public static IEnumerable<T> ToEnumerable<T>(this (T, T, T) tuple) {
            yield return tuple.Item1;
            yield return tuple.Item2;
            yield return tuple.Item3;
        }
        
        public static IEnumerable<T> ToEnumerable<T>(this (T, T, T, T) tuple) {
            yield return tuple.Item1;
            yield return tuple.Item2;
            yield return tuple.Item3;
            yield return tuple.Item4;
        }
        
        public static IEnumerable<T> ToEnumerable<T>(this (T, T, T, T, T) tuple) {
            yield return tuple.Item1;
            yield return tuple.Item2;
            yield return tuple.Item3;
            yield return tuple.Item4;
            yield return tuple.Item5;
        }
        
        /// <summary>
        /// Tries to cast <paramref name="source"/> to a <see cref="IReadOnlyCollection{T}"/>.
        /// If the cast fails, calls <see cref="Enumerable.ToList{T}"/>.
        /// <b>If you know that the cast will fail, you should just call <see cref="Enumerable.ToList{T}"/> instead.</b>
        /// </summary>
        public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> source)
            => source as IReadOnlyCollection<T> ?? source.ToList();

        /// <summary>
        /// Tries to cast <paramref name="source"/> to a <see cref="IReadOnlyList{T}"/>.
        /// If the cast fails, calls <see cref="Enumerable.ToList{T}"/>.
        /// <b>If you know that the cast will fail, you should just call <see cref="Enumerable.ToList{T}"/> instead.</b>
        /// </summary>
        public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> source)
            => source as IReadOnlyList<T> ?? source.ToList();
        
        public static Stack<T> ToStack<T>(this IEnumerable<T> source) => new(source);
        
        public static Queue<T> ToQueue<T>(this IEnumerable<T> source) => new(source);
        
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IEnumerable<(TKey key, TValue value)> tupleCollection
        ) => tupleCollection.ToDictionary(tuple => tuple.key, tuple => tuple.value);

        public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(
            this IEnumerable<(TKey, TValue)> entries
        ) where TKey : notnull => 
            ImmutableDictionary.CreateRange(entries.Select(e => new KeyValuePair<TKey, TValue>(e.Item1, e.Item2)));

        public static HashSet<T> AsHashSet<T>(this IEnumerable<T> source) =>
            source as HashSet<T> ?? new HashSet<T>(source);
        
        public static Stack<T> AsStack<T>(this IEnumerable<T> source) =>
            source as Stack<T> ?? new Stack<T>(source);
        
        public static Queue<T> AsQueue<T>(this IEnumerable<T> source) =>
            source as Queue<T> ?? new Queue<T>(source);
        
        public static T[] AsArray<T>(this IEnumerable<T> source)
            => source as T[] ?? source.ToArray();

        public static ImmutableList<T> AsImmutableList<T>(this IEnumerable<T> source)
            => source as ImmutableList<T> ?? source.ToImmutableList();

        public static ImmutableHashSet<T> AsImmutableHashSet<T>(this IEnumerable<T> source)
            => source as ImmutableHashSet<T> ?? source.ToImmutableHashSet();

        /// Always returns a new array with predefined size (as opposed to `ToArray()`)
        /// In a performance critical context, it is preferable to allocate a new array and fill it
        /// instead of calling `ToArray` on a Linq-Enumerable if the array size is known upfront.
        public static T[] ToNewArray<T>(this IEnumerable<T> enumerable, int count) {
            var ret = new T[count];
            var i = 0;
            foreach (var t in enumerable) {
                ret[i] = t; // Throws an IndexOutOfBoundsException if count was to small.
                i++;
            }

            return ret;
        }
        
    }
}