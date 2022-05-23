#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace BII.WasaBii.Core {
    public static class EnumerableExtensions {

        public static Stack<T> ToStack<T>(this IEnumerable<T> source) => new(source);
        
        public static Queue<T> ToQueue<T>(this IEnumerable<T> source) => new(source);

        /// <inheritdoc cref="System.Linq.Enumerable.ToDictionary()"/>
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IEnumerable<(TKey key, TValue value)> tupleCollection
        ) => tupleCollection.ToDictionary(tuple => tuple.key, tuple => tuple.value);

        public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(
            this IEnumerable<(TKey, TValue)> entries
        ) => ImmutableDictionary.CreateRange(entries.Select(e => new KeyValuePair<TKey, TValue>(e.Item1, e.Item2)));

        public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> source)
            => source as IReadOnlyCollection<T> ?? source.ToList();

        public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> source)
            => source as IReadOnlyList<T> ?? source.ToList();

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
            T? current = default(T);
            T? before = default(T);
            foreach (var item in enumerable) {
                ++iterations;
                before = current;
                current = item;
            }

            if (iterations < 2)
                throw new ArgumentOutOfRangeException(nameof(enumerable));
            return before!;
        }

        public static bool AnyAdjacent<T>(this IEnumerable<T> enumerable, Func<T, T, bool> predicate) {
            var it = enumerable.GetEnumerator();
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

        public static Option<T> FirstOrNone<T>(this IEnumerable<T> enumerable, Predicate<T>? predicate = null) {
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
            this IEnumerable<T> enumerable, Func<Exception> elseException, Predicate<T>? predicate = null
        ) {
            if (predicate == null) predicate = t => t != null;
            using var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext()) {
                if (predicate(enumerator.Current)) return enumerator.Current;
            }

            throw elseException();
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

        public static Option<T> LastOrNone<T>(this IEnumerable<T> enumerable, Predicate<T>? predicate = null) =>
            enumerable.Reverse().FirstOrNone(predicate);

        public static T LastOrThrow<T>(
            this IEnumerable<T> enumerable,
            Func<Exception> elseException,
            Predicate<T> predicate = null
        ) => enumerable.LastOrNone(predicate).GetOrThrow(elseException);
        
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

        public static ImmutableHashSet<T> AddAllImmutable<T>(this ImmutableHashSet<T> set, IEnumerable<T> toAdd) {
            var builder = set.ToBuilder();
            builder.AddAll(toAdd);
            return builder.ToImmutable();
        }

        /// Finds and returns the minimum element of <paramref name="source"/> by
        /// mapping each element to an <see cref="IComparable{T}"/>.
        /// If there are multiple minimum elements, the first one is returned.
        /// Returns null if the <see cref="source"/> is empty.
        public static Option<TSource> MinBy<TSource, TComparable>(
            this IEnumerable<TSource> source, Func<TSource, TComparable> comparableSelector
        ) where TComparable : IComparable<TComparable> => minOrMaxBy(source, comparableSelector, preferredComparisonResult: -1);

        /// Finds and returns the maximum element of <paramref name="source"/> by
        /// mapping each element to an <see cref="IComparable{T}"/>.
        /// If there are multiple maximum elements, the first one is returned.
        /// Returns null if the <see cref="source"/> is empty.
        public static Option<TSource> MaxBy<TSource, TComparable>(
            this IEnumerable<TSource> source, Func<TSource, TComparable> comparableSelector
        ) where TComparable : IComparable<TComparable> => minOrMaxBy(source, comparableSelector, preferredComparisonResult: 1);

        private static Option<TSource> minOrMaxBy<TSource, TComparable>(
            IEnumerable<TSource> source, Func<TSource, TComparable> comparableSelector, int preferredComparisonResult
        ) where TComparable : IComparable<TComparable>
            => source.IfNotEmpty(
                notEmpty => notEmpty
                    .Select(t => (val: t, comp: comparableSelector(t)))
                    .Aggregate((l, r) => l.comp.CompareTo(r.comp) == preferredComparisonResult ? l : r)
                    .val.Some(),
                elseResult: Option.None
            );

        public static IEnumerable<T> OrIfEmpty<T>(this IEnumerable<T> enumerable, Func<IEnumerable<T>> thenReturn) =>
            IfNotEmpty(enumerable, v => v, thenReturn);

        public static TResult IfNotEmpty<TSource, TResult>(
            this IEnumerable<TSource> enumerable, Func<IEnumerable<TSource>, TResult> then, TResult elseResult
        ) => IfNotEmpty(enumerable, then, () => elseResult);
        
        public static TResult IfNotEmpty<TSource, TResult>(
            this IEnumerable<TSource> enumerable, Func<IEnumerable<TSource>, TResult> then, Func<TResult> elseResultGetter
        ) {
            if (enumerable is IReadOnlyCollection<TSource> collection)
                return collection.Count > 0 ? then(collection) : elseResultGetter();
            
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
            if (enumerable is IReadOnlyCollection<T> collection) {
                if (collection.Count > 0)
                    thenDo?.Invoke(collection);
                else
                    elseDo?.Invoke();
                return;
            }
            
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

        public static TResult IfSingle<TSource, TResult>(
            this IEnumerable<TSource> enumerable,
            Func<TSource, TResult> then,
            Func<TResult> ifEmpty,
            Func<IEnumerable<TSource>, TResult> ifMultiple
        ) => enumerable.IfNotEmpty(
            then: notEmpty => notEmpty.IfNotSingle(then: ifMultiple, elseResultGetter: then),
            elseResultGetter: ifEmpty
        );
        
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
            if (stack.Any()) {
                t = stack.Peek();
                return true;
            } else {
                t = default;
                return false;
            }
        }

        public static IEnumerable<(T item, int index)> ZipWithIndices<T>(this IEnumerable<T> source) {
            var index = 0;
            foreach (var item in source) {
                yield return (item, index++);
            }
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

        /// Determines whether <see cref="enumerable"/> is equal to the concatenation
        /// of <see cref="init"/> and another enumerable <see cref="tail"/>.
        public static bool StartsWith<T>(
            this IEnumerable<T> enumerable,
            IEnumerable<T> init,
            out IEnumerable<T> tail,
            Func<T, T, bool>? areEqual = null
        ) {
            using var enumerator1 = enumerable.GetEnumerator();
            using var enumerator2 = init.GetEnumerator();
            areEqual ??= (t1, t2) => Equals(t1, t2);
            while (enumerator2.MoveNext()) {
                if (!(enumerator1.MoveNext() && areEqual(enumerator1.Current, enumerator2.Current))) {
                    tail = null;
                    return false;
                }
            }
            tail = enumerator2.RemainingToEnumerable();
            return true;
        }

        /// All remaining elements of the enumerator as an enumerable.
        /// Disposes the consumed enumerator.
        public static IEnumerable<T> RemainingToEnumerable<T>(this IEnumerator<T> enumerator) {
            using var e = enumerator;
            while (e.MoveNext()) yield return e.Current;
        }
// TODO: Sort and stuff
#region CoreLibrary 

        /// Executes the specified action with side effects for each element in this sequence,
        /// thereby consuming the sequence if it was only iterable once.
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action) {
            foreach (var item in sequence) action(item);
        }

        /// <inheritdoc cref="ForEach{T}(System.Collections.Generic.IEnumerable{T},System.Action{T})"/>
        public static void ForEach<T1, T2>(this IEnumerable<(T1, T2)> sequence, Action<T1, T2> action) {
            foreach (var (t1, t2) in sequence) action(t1, t2);
        }

        /// Executes the specified action with side effects for each element in this sequence,
        /// thereby consuming the sequence if it was only iterable once. The action also takes
        /// the index of the element as second argument, thus allowing you to potentially replace
        /// simple counting for loops with this function.
        public static void ForEachWithIndex<T>(this IEnumerable<T> sequence, Action<T, int> action)
            => sequence.ZipWithIndices().ForEach(action);

        /// Equal to calling <code>.Select(mapping).Where(v => v != null)</code>
        /// <br/>
        /// Nice for calling functions that may return no result such as
        /// <code>.Collect(v => v.As&lt;Whatever&gt;())</code>
        public static IEnumerable<TRes> Collect<T, TRes>(
            this IEnumerable<T> sequence, Func<T, TRes?> mapping
        ) where TRes : class => sequence.Select(mapping).WithoutNull();

        /// <inheritdoc cref="Collect{T,TRes}(System.Collections.Generic.IEnumerable{T},System.Func{T,TRes})"/>
        public static IEnumerable<TRes> Collect<T, TRes>(
            this IEnumerable<T> sequence, Func<T, int, TRes?> mappingWithIndex
        ) where TRes : class => sequence.Select(mappingWithIndex).WithoutNull();

        /// Similar to <see cref="Collect{T,TRes}(System.Collections.Generic.IEnumerable{T},System.Func{T,TRes})"/>.
        /// This method works on mappings that return nullable values,
        /// but returns a non-nullable enumerable instead.
        public static IEnumerable<TRes> Collect<T, TRes>(
            this IEnumerable<T> sequence, Func<T, TRes?> mapping
        ) where TRes : struct => sequence.Select(mapping).WithoutNull();

        /// Equal to calling <code>.SelectMany(mapping).Where(v => v != null)</code>
        /// <br/>
        /// Basically the flattening equivalent to <see cref="Collect{T,TRes}(System.Collections.Generic.IEnumerable{T},System.Func{T,TRes})"/>
        public static IEnumerable<TRes> CollectMany<T, TRes>(
            this IEnumerable<T> sequence, Func<T, IEnumerable<TRes?>> mapping
        ) where TRes : class => sequence.SelectMany(mapping).WithoutNull();
        
        /// Equal to calling <code>.SelectMany(mapping).Where(v => v != null)</code>
        /// <br/>
        /// This method works on mappings that return nullable values,
        /// but returns a non-nullable enumerable instead.
        /// Basically the flattening equivalent to <see cref="Collect{T,TRes}(System.Collections.Generic.IEnumerable{T},System.Func{T,Nullable{TRes}})"/>
        public static IEnumerable<TRes> CollectMany<T, TRes>(
            this IEnumerable<T> sequence, Func<T, IEnumerable<TRes?>> mapping
        ) where TRes : struct => sequence.SelectMany(mapping).WithoutNull();

        public static IEnumerable<T> AfterwardsDo<T>(this IEnumerable<T> enumerable, Action afterwards) {
            try {
                foreach (var value in enumerable) yield return value;
            } finally {
                afterwards();
            }
        }

        private static readonly System.Random Rng = new System.Random();

        /// Shuffles this sequence, yielding a <b>new</b> IEnumerable with all elements in random order.
        /// Uses the <a href="https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle">Fisher–Yates algorithm</a>.
        /// <br/>If the passed IEnumerable is only iterable once it is consumed in the process.
        public static List<T> Shuffled<T>(this IEnumerable<T> l, System.Random? random = null) {
            random ??= Rng;
            var list = new List<T>(l);
            var n = list.Count;
            while (n > 1) {
                n--;
                var k = random.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }

            return list;
        }
#endregion CoreLibrary
        
    }
}