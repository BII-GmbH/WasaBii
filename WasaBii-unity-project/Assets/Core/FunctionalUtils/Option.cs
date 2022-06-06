#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BII.WasaBii.Core {

    public static class Option {
        public static Option<T> Some<T>(T value) => Option<T>.Some(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Option<T> SomeIfNotNull<T>(T? value) where T : class =>
            value != null ? Some(value) : None;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Option<T> SomeIfNotNull<T>(T? value) where T : struct =>
            value.HasValue ? Some(value.Value) : None;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> If<T>(bool predicate, Func<T> then) => predicate ? Some(then()) : Option<T>.None;
        
        public static Option<T> Try<T>(Func<T> valueConstructor) {
            try {
                return Some(valueConstructor());
            } catch {
                return None;
            }
        }
        
        public static readonly UniversalNone None = new();

        /// Implicitly convertible to Option{T}.None for any T
        public readonly struct UniversalNone { } 
    }
    
    /// Marker interface for option values without specifying the generic type.
    /// Used in reflection-based code.
    public interface UntypedOption { }
    
    /// A class that potentially wraps a value.
    /// Can be either Some(value) or None.
    /// 
    [MustBeSerializable]
    public readonly struct Option<T> : UntypedOption, IEquatable<T>, IEquatable<Option<T>> {

        private readonly T? value;
        public readonly bool HasValue;

        private Option(T value) {
            if (value == null) throw new ArgumentNullException(nameof(value));
            this.value = value;
            this.HasValue = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Option<T> Some(T value) => new(value);
        public static readonly Option<T> None = default;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public Option<TRes> Map<TRes>(Func<T, TRes> mapping) => 
            HasValue ? new(mapping(value!)) : default;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public async Task<Option<TRes>> AsyncMap<TRes>(Func<T, Task<TRes>> mapping) => 
            HasValue ? new(await mapping(value!)) : default;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public Option<TRes> FlatMap<TRes>(Func<T, Option<TRes>> mapping) => 
            HasValue ? mapping(value!) : default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public Task<Option<TRes>> AsyncFlatMap<TRes>(Func<T, Task<Option<TRes>>> mapping) =>
            HasValue ? mapping(value!) : Task.FromResult<Option<TRes>>(default);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public T GetOrElse(Func<T> elseResultGetter) => HasValue ? value! : elseResultGetter();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public T? GetOrDefault() => value;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WhenPresentDo(Action<T> fn) { if (HasValue) fn(value!); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public Option<TRes> AsOrNone<TRes>() => HasValue && value is TRes res ? res.Some() : Option<TRes>.None;
        
        public static implicit operator Option<T>(Option.UniversalNone _) => default;

        public static implicit operator Option<T>(T value) {
            if (value == null) return default;
            else return new Option<T>(value);
        }

        [Pure] public bool Equals(T other) => HasValue && EqualityComparer<T>.Default.Equals(other, value!);

        [Pure] public bool Equals(Option<T> other) =>
            (other.HasValue, HasValue) switch {
                (true, false) => false,
                (false, true) => false,
                (false, false) => true,
                (true, true) => EqualityComparer<T>.Default.Equals(other.value!, value!)
            };

        [Pure] public override bool Equals(object obj) =>
            obj is Option<T> other && Equals(other) || obj is T otherValue && Equals(otherValue);

        [Pure] public static bool operator ==(Option<T> first, Option<T> second) => first.Equals(second);
        [Pure] public static bool operator !=(Option<T> first, Option<T> second) => !first.Equals(second);
        
        [Pure] public override int GetHashCode() => HasValue ? EqualityComparer<T>.Default.GetHashCode(value!) : 0;
        
        [Pure] public override string ToString() => HasValue ? $"Some<{typeof(T)}>({value!.ToString()})" : $"None<{typeof(T)}>";
    }

    public static class OptionConstructionExtensions {
        public static Option<T> Some<T>(this T value) => Option<T>.Some(value);
    }

    public static class OptionQueryExtensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static bool IsNone<T>(this Option<T> opt) => !opt.HasValue;
        
        [Pure] public static T? GetOrNull<T>(this Option<T> opt) where T : struct => opt.TryGetValue(out var val) ? val : null;
        
        public static T GetOrThrow<T>(this Option<T> opt, Func<Exception>? exception = null) =>
            opt.GetOrElse(() => throw exception?.Invoke() ?? new InvalidOperationException("No value present."));
        
        [Pure] public static Option<T> OrWhenNone<T>(this Option<T> opt, Func<Option<T>> noneResultGetter) => opt.HasValue ? opt : noneResultGetter();

        [Pure] public static Option<T> Where<T>(this Option<T> opt, Func<T, bool> predicate) =>
            opt.FlatMap(v => Option.If(predicate(v), () => v));

        [Pure] public static bool TryGetValue<T>(this Option<T> opt, out T result) {
            result = opt.GetOrElse(elseResultGetter: () => default!);
            return opt.HasValue;
        }

        public static void Match<T>(this Option<T> opt, Action<T> onHasValue, Action onNone) {
            if (opt.TryGetValue(out var res)) onHasValue(res);
            else onNone();
        }
        
        public static TRes Match<T, TRes>(this Option<T> opt, Func<T, TRes> onHasValue, Func<TRes> onNone) {
            if (opt.TryGetValue(out var res)) return onHasValue(res);
            else return onNone();
        }

        [Pure] public static IEnumerable<Option<T>> Sequence<T>(this Option<IEnumerable<T>> option, int sizeIfNone) => 
            option.Map(enumerable => enumerable.Select(Option.Some))
                .GetOrElse(() => Enumerable.Repeat(Option<T>.None, sizeIfNone));

        [Pure] public static IReadOnlyCollection<Option<T>> Sequence<T>(this Option<IReadOnlyCollection<T>> option, int sizeIfNone) => 
            option.Map(enumerable => enumerable.Select(Option.Some))
                .GetOrElse(() => Enumerable.Repeat(Option<T>.None, sizeIfNone)).AsReadOnlyCollection();

        [Pure] public static IReadOnlyList<Option<T>> Sequence<T>(this Option<IReadOnlyList<T>> option, int sizeIfNone) => 
            option.Map(enumerable => enumerable.Select(Option.Some))
                .GetOrElse(() => Enumerable.Repeat(Option<T>.None, sizeIfNone)).AsReadOnlyList();

        [Pure] public static Option<IEnumerable<T>> Sequence<T>(this IEnumerable<Option<T>> enumerable) => 
            enumerable.Aggregate(
                Option.Some(Enumerable.Empty<T>()), 
                (resO, nowO) => resO.FlatMap(res => nowO.Map(res.Append))
            );

        [Pure] public static Option<IEnumerable<T>> Traverse<T>(this IEnumerable<T> enumerable, Func<T, Option<T>>? f = null) =>
            enumerable.Select(element => f?.Invoke(element) ?? Option.Some(element)).Sequence();
        
        
        [Pure] public static Option<IEnumerable<T>> TraverseNoneIfEmpty<T>(this IEnumerable<T> enumerable, Func<T, Option<T>>? f = null) =>
            enumerable.Select(element => f?.Invoke(element) ?? Option.Some(element)).OrIfEmpty(() => Option<T>.None.WrapAsEnumerable()).Sequence();
        
        [Pure] public static Option<T> Flatten<T>(this Option<Option<T>> option)
            => option.FlatMap(o => o);

        [Pure] public static IEnumerable<T> Collect<T>(this IEnumerable<Option<T>> options) {
            foreach (var element in options)
                if (element.TryGetValue(out var val))
                    yield return val;
        }

        [Pure] public static IEnumerable<S> Collect<T, S>(this IEnumerable<T> input, Func<T, Option<S>> mapping) =>
            input.Select(mapping).Collect();
        
        [Pure] public static IEnumerable<S> CollectMany<T, S>(this IEnumerable<T> input, Func<T, IEnumerable<Option<S>>> mapping) =>
            input.SelectMany(mapping).Collect();
        
        [Pure] public static Option<T> FirstSomeOrNone<T>(this IEnumerable<Option<T>> options)
            => options.Collect().FirstOrNone();

        [Pure] public static IEnumerable<T> Flatten<T>(IEnumerable<Option<T>> source) =>
            source.Where(opt => opt.HasValue).Select(opt => opt.GetOrThrow());

        [Pure] public static Result<Option<TVal>, TErr> Flip<TVal, TErr>(this Option<Result<TVal, TErr>> option) =>
            option.Match(
                value => value.Map(val => val.Some()),
                () => Option<TVal>.None.Success()
            );

        [Pure] public static Task<Option<TVal>> Flip<TVal>(this Option<Task<TVal>> option) =>
            option.Match(
                value => value.Map(val => val.Some()),
                () => Option<TVal>.None.AsCompletedTask()
            );

        [Pure] public static IEnumerable<T> AsEnumerable<T>(this Option<T> option) => option.Match(
            onHasValue: value => value.WrapAsEnumerable(),
            onNone: Enumerable.Empty<T>
        );
    }
}