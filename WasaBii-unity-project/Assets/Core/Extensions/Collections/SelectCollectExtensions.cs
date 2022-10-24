#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BII.WasaBii.Core {
    public static class SelectCollectExtensions {
        
        public static async Task<IEnumerable<B>> AsyncSelect<A, B>(this IEnumerable<A> seq, Func<A, Task<B>> fn) {
            var res = new List<B>();
            foreach (var arg in seq) res.Add(await fn(arg));
            return res;
        }
        
        public static IEnumerable<TRes> SelectTuple<T1, T2, TRes>(
            this IEnumerable<(T1, T2)> source, Func<T1, T2, TRes> fn
        ) => source.Select(t => fn(t.Item1, t.Item2));
        
        public static IEnumerable<TRes> SelectTuple<T1, T2, T3, TRes>(
            this IEnumerable<(T1, T2, T3)> source, Func<T1, T2, T3, TRes> fn
        ) => source.Select(t => fn(t.Item1, t.Item2, t.Item3));
        
        public static IEnumerable<TRes> SelectTuple<T1, T2, T3, T4, TRes>(
            this IEnumerable<(T1, T2, T3, T4)> source, Func<T1, T2, T3, T4, TRes> fn
        ) => source.Select(t => fn(t.Item1, t.Item2, t.Item3, t.Item4));
        
        public static IEnumerable<TOut> SelectManyTuple<T1, T2, TOut>(
            this IEnumerable<(T1, T2)> enumerable, Func<T1, T2, IEnumerable<TOut>> mapping
        ) => enumerable.SelectMany(tuple => mapping(tuple.Item1, tuple.Item2));
        
        public static IEnumerable<TOut> SelectManyTuple<T1, T2, T3, TOut>(
            this IEnumerable<(T1, T2, T3)> enumerable, Func<T1, T2, T3, IEnumerable<TOut>> mapping
        ) => enumerable.SelectMany(tuple => mapping(tuple.Item1, tuple.Item2, tuple.Item3));
        
        /// <summary>
        /// Equal to calling <code>.Select(mapping).Where(v => v != null)</code>
        /// Nice for calling functions that may return no result such as
        /// <code>.Collect(v => v.As&lt;Whatever&gt;())</code>
        /// </summary>
        public static IEnumerable<TRes> Collect<T, TRes>(
            this IEnumerable<T> sequence, Func<T, TRes?> mapping
        ) where TRes : class => sequence.Select(mapping).WithoutNull();

        /// <inheritdoc cref="Collect{T,TRes}(System.Collections.Generic.IEnumerable{T},System.Func{T,TRes})"/>
        public static IEnumerable<TRes> Collect<T, TRes>(
            this IEnumerable<T> sequence, Func<T, int, TRes?> mappingWithIndex
        ) where TRes : class => sequence.Select(mappingWithIndex).WithoutNull();

        /// <summary>
        /// Similar to <see cref="Collect{T,TRes}(System.Collections.Generic.IEnumerable{T},System.Func{T,TRes})"/>.
        /// This method works on mappings that return nullable values,
        /// but returns a non-nullable enumerable instead.
        /// </summary>
        public static IEnumerable<TRes> Collect<T, TRes>(
            this IEnumerable<T> sequence, Func<T, TRes?> mapping
        ) where TRes : struct => sequence.Select(mapping).WithoutNull();

        /// <summary>
        /// Equal to calling <code>.SelectMany(mapping).Where(v => v != null)</code>.
        /// Basically the flattening equivalent to <see cref="Collect{T,TRes}(System.Collections.Generic.IEnumerable{T},System.Func{T,TRes})"/>
        /// </summary>
        public static IEnumerable<TRes> CollectMany<T, TRes>(
            this IEnumerable<T> sequence, Func<T, IEnumerable<TRes?>> mapping
        ) where TRes : class => sequence.SelectMany(mapping).WithoutNull();
        
        /// <summary>
        /// Equal to calling <code>.SelectMany(mapping).Where(v => v != null)</code>.
        /// This method works on mappings that return nullable values,
        /// but returns a non-nullable enumerable instead.
        /// Basically the flattening equivalent to <see cref="Collect{T,TRes}(System.Collections.Generic.IEnumerable{T},System.Func{T,Nullable{TRes}})"/>
        /// </summary>
        public static IEnumerable<TRes> CollectMany<T, TRes>(
            this IEnumerable<T> sequence, Func<T, IEnumerable<TRes?>> mapping
        ) where TRes : struct => sequence.SelectMany(mapping).WithoutNull();

        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source) => source.SelectMany(s => s);

        public static IEnumerable<T> WithoutNull<T>(this IEnumerable<T?> enumerable) where T : class {
            foreach (var e in enumerable)
                if (e != null)
                    yield return e;
        }

        public static IEnumerable<T> WithoutNull<T>(this IEnumerable<T?> enumerable) where T: struct {
            foreach (var e in enumerable)
                if (e is {} res)
                    yield return res;
        }
    }
}