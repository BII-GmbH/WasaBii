#nullable enable 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BII.WasaBii.Core {
    public static class EnumerableExtensions {
        
        public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> source)
            => source as IReadOnlyCollection<T> ?? source.ToList();

        public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> source)
            => source as IReadOnlyList<T> ?? source.ToList();

        public static HashSet<T> AsHashSet<T>(this IEnumerable<T> source) =>
            source as HashSet<T> ?? new HashSet<T>(source);
        
        // TODO: change to OrderBy for compliance with LINQ naming and remove duplicates or other weirdness.
        
        /// <summary>
        /// Sorts the enumerable by comparing the values produced by `valueProvider`.
        /// With `thenBy`, more valueProviders can be specified to define the order
        /// when the original valueProvider produces equal values. `thenBy` will be
        /// iterated until distinct values are found. If all values are equal, the
        /// order between the two elements remains unchanged (just like normal sorting
        /// when the comparison returns 0).
        /// </summary>
        public static List<T> SortedBy<T>(
            this IEnumerable<T> enumerable, Func<T, IComparable> valueProvider, params Func<T, IComparable>[] thenBy
        ) => 
            enumerable.Sorted(
                (t1, t2) => thenBy.Prepend(valueProvider)
                    .Select(vP => vP(t1).CompareTo(vP(t2)))
                    .FirstOrDefault(result => result != 0)
            );

        public static List<T> SortedBy<T, S>(this IEnumerable<T> enumerable, Func<T, S> valueProvider, bool descending = false)
        where S : IComparable<S> =>
            enumerable.Sorted(
                (t1, t2) => 
                    valueProvider(t1).CompareTo(valueProvider(t2))
                        .NegateIf(descending));
        
        public static List<T> Sorted<T>(this IEnumerable<T> enumerable, System.Comparison<T> comparison) {
            var ret = new List<T>(enumerable);
            ret.Sort(comparison);
            return ret;
        }

        public static List<T> Sorted<T>(this IEnumerable<T> enumerable)
        where T : IComparable<T> {
            var ret = new List<T>(enumerable);
            ret.Sort();
            return ret;
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

        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source) => source.SelectMany(s => s);

        public static T Second<T>(this IEnumerable<T> enumerable) => enumerable.ElementAt(1);

        public static T SecondFromLast<T>(this IEnumerable<T> enumerable) {
            var iterations = 0;
            T current = default(T);
            T before = default(T);
            foreach (var item in enumerable) {
                ++iterations;
                before = current;
                current = item;
            }

            if (iterations < 2)
                throw new ArgumentOutOfRangeException(nameof(enumerable));
            return before;
        }

        public static bool AnyAdjacent<T>(this IEnumerable<T> enumerable, Func<T, T, bool> predicate) {
            using var it = enumerable.GetEnumerator();
            if (!it.MoveNext()) return false;
            for (var prev = it.Current; it.MoveNext(); prev = it.Current)
                if (predicate(prev, it.Current))
                    return true;
            return false;
        }
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, params T[] except) =>
            // Intentionally not used as extension method to prevent recursion 
            Enumerable.Except(source, except);


        public static IEnumerable<(A, B)> Zip<A, B>(this IEnumerable<A> first, IEnumerable<B> second) =>
            first.Zip(second, (a, b) => (a, b));

        public static IEnumerable<(A, B, C)> Zip<A, B, C>(
            this IEnumerable<A> first, IEnumerable<B> second, IEnumerable<C> third
        ) =>
            first.Zip<A, (B, C), (A, B, C)>(second.Zip(third), (a, tuple) => (a, tuple.Item1, tuple.Item2));

        public static IEnumerable<(A, B, C, D)> Zip<A, B, C, D>(
            this IEnumerable<A> first, IEnumerable<B> second, IEnumerable<C> third, IEnumerable<D> fourth
        ) =>
            first.Zip(second).Zip(third.Zip(fourth), 
                (aAndB, cAndD) => (aAndB.Item1, aAndB.Item2, cAndD.Item1, cAndD.Item2));

        public static IEnumerable<(A, B, C, D, E)> Zip<A, B, C, D, E>(
            this IEnumerable<A> first, IEnumerable<B> second, IEnumerable<C> third, IEnumerable<D> fourth, IEnumerable<E> fifth
        ) =>
            first.Zip(second).Zip(third.Zip(fourth, fifth), 
                (aAndB, cAndDAndE) => (aAndB.Item1, aAndB.Item2, cAndDAndE.Item1, cAndDAndE.Item2, cAndDAndE.Item3));

        public static IEnumerable<Out> Zip<A, B, C, Out>(
            this IEnumerable<A> first, IEnumerable<B> second, IEnumerable<C> third, Func<A, B, C, Out> mapping
        ) =>
            first.Zip(second.Zip(third), (a, tuple) => mapping(a, tuple.Item1, tuple.Item2));

        public static IEnumerable<Out> Zip<A, B, C, D, Out>(
            this IEnumerable<A> first, IEnumerable<B> second, IEnumerable<C> third, IEnumerable<D> fourth, Func<A, B, C, D, Out> mapping
        ) =>
            first.Zip(second).Zip(third.Zip(fourth), 
                (aAndB, cAndD) => mapping(aAndB.Item1, aAndB.Item2, cAndD.Item1, cAndD.Item2));

        public static IEnumerable<Out> Zip<A, B, C, D, E, Out>(
            this IEnumerable<A> first, IEnumerable<B> second, IEnumerable<C> third, IEnumerable<D> fourth, IEnumerable<E> fifth, Func<A, B, C, D, E, Out> mapping
        ) =>
            first.Zip(second).Zip(third.Zip(fourth, fifth), 
                (aAndB, cAndDAndE) => mapping(aAndB.Item1, aAndB.Item2, cAndDAndE.Item1, cAndDAndE.Item2, cAndDAndE.Item3));
        
        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? source) =>
            source ?? Enumerable.Empty<T>();

        public static Option<T> FirstOrNone<T>(this IEnumerable<T> enumerable, Func<T,bool>? predicate = null) {
            foreach (var element in enumerable) {
                if (predicate?.Invoke(element) ?? true) return element.Some();
            }
            return Option<T>.None;
        }

        public static T? FirstOrNull<T>(this IEnumerable<T> enumerable) where T : struct
            => enumerable.Select(e => e.AsNullable()).FirstOrDefault();

        public static T? FirstOrNull<T>(this IEnumerable<T> enumerable, Predicate<T> predicate)
        where T : struct {
            foreach (var item in enumerable)
                if (predicate(item))
                    return item;
            return null;
        }

        public static T FirstOr<T>(this IEnumerable<T> enumerable, T elseResult) {
            using (var enumerator = enumerable.GetEnumerator()) {
                return enumerator.MoveNext()
                    ? enumerator.Current
                    : elseResult;
            }
        }

        public static T FirstOr<T>(this IEnumerable<T> enumerable, Predicate<T> predicate, T elseResult) {
            foreach (var item in enumerable)
                if (predicate(item))
                    return item;
            return elseResult;
        }

        public static T FirstOrThrow<T>(
            this IEnumerable<T> enumerable, Exception elseException, Predicate<T>? predicate = null
        ) {
            if (predicate == null) predicate = t => t != null;
            using (var enumerator = enumerable.GetEnumerator()) {
                while (enumerator.MoveNext()) {
                    if (predicate(enumerator.Current)) return enumerator.Current;
                }
            }

            throw elseException;
        }

        public static T? LastOrNull<T>(this IEnumerable<T> enumerable) where T : struct
            => enumerable.Reverse().FirstOrNull();

        public static T? LastOrNull<T>(this IEnumerable<T> enumerable, Predicate<T> predicate)
        where T : struct
            => enumerable.Reverse().FirstOrNull(predicate);

        public static T LastOr<T>(this IEnumerable<T> enumerable, T elseResult)
            => enumerable.Reverse().FirstOr(elseResult);

        public static T LastOr<T>(this IEnumerable<T> enumerable, Predicate<T> predicate, T elseResult)
            => enumerable.Reverse().FirstOr(predicate, elseResult);

        public static Option<T> LastOrNone<T>(this IEnumerable<T> enumerable, Func<T, bool>? predicate = null) =>
            enumerable.Reverse().FirstOrNone(predicate);

        public static bool SameAs<T>(this IEnumerable<T> lhs, IEnumerable<T> rhs) where T : IEquatable<T> {
            using (var le = lhs.GetEnumerator())
            using (var re = rhs.GetEnumerator()) {
                while (le.MoveNext()) {
                    if (!re.MoveNext()) return false;
                    if (!le.Current.Equals(re.Current)) return false;
                }

                return !re.MoveNext();
            }
        }
        
        public static async Task<IEnumerable<B>> AsyncSelect<A, B>(this IEnumerable<A> seq, Func<A, Task<B>> fn) {
            var res = new List<B>();
            foreach (var arg in seq) res.Add(await fn(arg));
            return res;
        }

        public static void AddAll<T>(this ISet<T> set, IEnumerable<T> toAdd) => set.UnionWith(toAdd);

        /// <summary>
        /// Selects the element minT of the enumerable which has the minimal valueProvider(minT).
        /// If multiple elements have the same minimal value, the first one will be selected.
        /// Returns whether the enumerable contained any values.
        /// </summary>
        public static bool TryGetElementWithMinimalValue<T, TComparable>(
            this IEnumerable<T> enumerable, Func<T, TComparable> valueProvider, out T minT
        ) where TComparable : IComparable<TComparable> {
            using (var en = enumerable.GetEnumerator()) {
                if (!en.MoveNext()) {
                    minT = default;
                    return false;
                }

                minT = en.Current;
                var minVal = valueProvider(minT);
                while (en.MoveNext()) {
                    var currentT = en.Current;
                    var currentVal = valueProvider(currentT);
                    if (currentVal.CompareTo(minVal) < 0) {
                        minT = currentT;
                        minVal = currentVal;
                    }
                }

                return true;
            }
        }

        /// Finds and returns the minimum element of <paramref name="source"/> by
        /// mapping each element to an <see cref="IComparable{T}"/>.
        /// If there are multiple minimum elements, the first one is returned.
        /// <exception cref="InvalidOperationException">
        /// When an empty sequence is passed to this function
        /// </exception>
        public static TSource MinBy<TSource, TComparable>(
            this IEnumerable<TSource> source, Func<TSource, TComparable> fn
        ) where TComparable : IComparable<TComparable> => minOrMaxBy(source, fn, compareResult => compareResult < 0);

        /// Finds and returns the maximum element of <paramref name="source"/> by
        /// mapping each element to an <see cref="IComparable{T}"/>.
        /// If there are multiple maximum elements, the first one is returned.
        /// <exception cref="InvalidOperationException">
        /// When an empty sequence is passed to this function
        /// </exception>
        public static TSource MaxBy<TSource, TComparable>(
            this IEnumerable<TSource> source, Func<TSource, TComparable> fn
        ) where TComparable : IComparable<TComparable> => minOrMaxBy(source, fn, compareResult => compareResult > 0);

        private static TSource minOrMaxBy<TSource, TComparable>(
            IEnumerable<TSource> source, Func<TSource, TComparable> fn, Func<int, bool> comp
        ) where TComparable : IComparable<TComparable> {
            using var enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext())
                throw new InvalidOperationException($"Cannot call {nameof(minOrMaxBy)} on an empty sequence");

            var res = enumerator.Current;
            var resComparable = fn(res);

            while (enumerator.MoveNext()) {
                var current = enumerator.Current;
                var currentComparable = fn(current);
                if (comp(currentComparable.CompareTo(resComparable))) {
                    res = current;
                    resComparable = currentComparable;
                }
            }

            return res;
        }

        public static IEnumerable<T> OrIfEmpty<T>(this IEnumerable<T> enumerable, Func<IEnumerable<T>> thenReturn) =>
            IfNotEmpty(enumerable, v => v, thenReturn);

        public static TResult IfNotEmpty<TSource, TResult>(
            this IEnumerable<TSource> enumerable, Func<IEnumerable<TSource>, TResult> then, TResult elseResult
        ) => IfNotEmpty(enumerable, then, () => elseResult);
        
        public static TResult IfNotEmpty<TSource, TResult>(
            this IEnumerable<TSource> enumerable, Func<IEnumerable<TSource>, TResult> then, Func<TResult> elseResultGetter
        ) {
            var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext()) return elseResultGetter();
            
            IEnumerable<TSource> completeEnumerable() {
                do yield return enumerator.Current;
                while (enumerator.MoveNext());
            }

            return then(completeEnumerable());
        }

        public static void IfNotEmpty<T>(
            this IEnumerable<T> enumerable, Action<IEnumerable<T>>? thenDo, Action? elseDo = null
        ) {
            var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext()) {
                elseDo?.Invoke();
                return;
            }

            if (thenDo == null) return;

            IEnumerable<T> completeEnumerable() {
                do yield return enumerator.Current;
                while (enumerator.MoveNext());
            }

            thenDo(completeEnumerable());
        }
        
        public static TResult IfNotSingle<TSource, TResult>(
            this IEnumerable<TSource> enumerable, 
            Func<IEnumerable<TSource>, TResult> then, 
            Func<TSource, TResult> elseResultGetter
        ) {
            var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext()) return then(Enumerable.Empty<TSource>());

            var single = enumerator.Current;

            return enumerator.MoveNext() ? then(completeEnumerable()) : elseResultGetter(single);
            
            IEnumerable<TSource> completeEnumerable() {
                yield return single;
                do yield return enumerator.Current;
                while (enumerator.MoveNext());
            }
        }

        public static void IfNotSingle<T>(
            this IEnumerable<T> enumerable, 
            Action<IEnumerable<T>>? thenDo, 
            Action<T>? elseDo = null
        ) {
            var enumerator = enumerable.GetEnumerator();
            if (!enumerator.MoveNext()) {
                thenDo?.Invoke(Enumerable.Empty<T>());
            } else {
                var single = enumerator.Current;

                if (enumerator.MoveNext()) thenDo?.Invoke(completeEnumerable());
                else elseDo?.Invoke(single);
                
                IEnumerable<T> completeEnumerable() {
                    yield return single;
                    do yield return enumerator.Current;
                    while (enumerator.MoveNext());
                }
            }
        }
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

        public static IEnumerable<T> WithoutNull<T>(this IEnumerable<T?> enumerable) where T: class =>
            enumerable.Where(t => t != null)!;

        public static IEnumerable<T> WithoutNull<T>(this IEnumerable<T?> enumerable) where T : struct =>
            enumerable.Where(t => t != null).Select(t => t!.Value);


        /// Divides the contents of <paramref name="source"/> based on <paramref name="selector"/>
        /// to either be part of the true or false results.
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

        public static IEnumerable<TOut> Select<T1, T2, TOut>(
            this IEnumerable<(T1, T2)> enumerable, Func<T1, T2, TOut> mapping
        ) => enumerable.Select(tuple => mapping(tuple.Item1, tuple.Item2));
        
        public static IEnumerable<TOut> Select<T1, T2, T3, TOut>(
            this IEnumerable<(T1, T2, T3)> enumerable, Func<T1, T2, T3, TOut> mapping
        ) => enumerable.Select(tuple => mapping(tuple.Item1, tuple.Item2, tuple.Item3));

        public static IEnumerable<TOut> SelectMany<T1, T2, TOut>(
            this IEnumerable<(T1, T2)> enumerable, Func<T1, T2, IEnumerable<TOut>> mapping
        ) => enumerable.SelectMany(tuple => mapping(tuple.Item1, tuple.Item2));
        
        public static IEnumerable<TOut> SelectMany<T1, T2, T3, TOut>(
            this IEnumerable<(T1, T2, T3)> enumerable, Func<T1, T2, T3, IEnumerable<TOut>> mapping
        ) => enumerable.SelectMany(tuple => mapping(tuple.Item1, tuple.Item2, tuple.Item3));

        public static bool Any<T1, T2>(
            this IEnumerable<(T1, T2)> enumerable, Func<T1, T2, bool> mapping
        ) => enumerable.Any(tuple => mapping(tuple.Item1, tuple.Item2));
        
        public static bool Any<T1, T2, T3>(
            this IEnumerable<(T1, T2, T3)> enumerable, Func<T1, T2, T3, bool> mapping
        ) => enumerable.Any(tuple => mapping(tuple.Item1, tuple.Item2, tuple.Item3));

        public static IEnumerable<T> CollectValues<T>(this IEnumerable<Option<T>> enumerable) {
            foreach (var t in enumerable)
                if (t.HasValue)
                    yield return t.GetOrThrow();
        }

        public static IEnumerable<T> SkipLast<T>(this IReadOnlyCollection<T> collection, int count) => 
            collection.Take(collection.Count - count);

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

        /// Preserves the order the elements will have in the code.
        /// E.g. 1.PrependTo({2, 3, 4, 5}) = {1, 2, 3, 4, 5}
        /// as opposed to
        /// {2, 3, 4, 5}.Prepend(1) = {1, 2, 3, 4, 5}.
        public static IEnumerable<T> PrependTo<T>(this T head, IEnumerable<T> tail) => tail.Prepend(head);

        public static string Join(this IEnumerable<string> enumerable, string separator = "") => string.Join(separator, enumerable);

        public static bool TryPeek<T>(this Stack<T> stack, out T t) {
            if (!stack.Any()) {
                t = default;
                return false;
            } else {
                t = stack.Peek();
                return true;
            }
        }

        ///<inheritdoc cref="System.Linq.Enumerable.ToDictionary()"/>
        public static Dictionary<Key, Value> ToDictionary<Key, Value>(
            this IEnumerable<(Key key, Value value)> tupleCollection
        ) => tupleCollection.ToDictionary(tuple => tuple.key, tuple => tuple.value);

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable) => new HashSet<T>(enumerable);

        public static IEnumerable<(T item, int index)> ZipWithIndices<T>(this IEnumerable<T> source) {
            var index = 0;
            foreach (var item in source) {
                yield return (item, index++);
            }
        }

        public static T[] AsArray<T>(this IEnumerable<T> source)
            => source as T[] ?? source.ToArray();

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

        /// <summary>
        /// Splits an Enumerable into multiple smaller Enumerables.
        /// The Input Enumerable gets split between elements where the should split function returns true.
        /// </summary>
        /// <param name="enumerable"> Enumerable that should be split </param>
        /// <param name="shouldSplit"> function to determine between which elements the enumerable should be split </param>
        /// <returns> Enumerable containing the smaller split sections of the input Enumerable</returns>
        public static IEnumerable<IEnumerable<T>> SplitByFunction<T>(
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