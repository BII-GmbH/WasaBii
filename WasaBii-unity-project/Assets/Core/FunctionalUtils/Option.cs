#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BII.WasaBii.Core {

    // TODO: consistent nullability etc

    public static class Option {

        public static Option<T> Some<T>(T value) => Option<T>.Some(value);
        
        public static Option<T> SomeIfNotNull<T>(T? value) where T : class =>
            value?.Some() ?? Option<T>.None;
        
        public static Option<T> SomeIfNotNull<T>(T? value) where T : struct =>
            value.HasValue ? Some(value.Value) : None;
        
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
    /// Only used for cases when absence of a value might have different semantic than the presence of a null-value.
    [MustBeSerializable]//[CannotApplyEqualityOperator]
    public abstract class Option<T> : UntypedOption, IEquatable<T>, IEquatable<Option<T>> {
        private Option() { }

        public static Option<T> Some(T value) => new SomeImpl(value);
        public static Option<T> None => new NoneImpl();

        public abstract bool HasValue { get; }
        public abstract Option<TRes> Map<TRes>(Func<T, TRes> mapping);
        public abstract Task<Option<TRes>> AsyncMap<TRes>(Func<T, Task<TRes>> mapping);
        public abstract Option<TRes> FlatMap<TRes>(Func<T, Option<TRes>> mapping);
        public abstract Task<Option<TRes>> AsyncFlatMap<TRes>(Func<T, Task<Option<TRes>>> mapping);
        public abstract T GetOrElse(Func<T> elseResultGetter);
        public abstract void WhenPresentDo(Action<T> fn);

        public Option<TRes> AsOrNone<TRes>() => FlatMap(val => val is TRes res ? res.Some() : Option<TRes>.None);
        public Option<T> Filter(Func<T, bool> predicate) => FlatMap(val => predicate(val) ? val.Some() : Option.None);
        
        public static implicit operator Option<T>(Option.UniversalNone _) => new NoneImpl();

        public static implicit operator Option<T>(T value) {
            if (value == null) return new NoneImpl();
            else return new SomeImpl(value);
        }

        public abstract bool Equals(T other);
        public abstract bool Equals(Option<T> other);
     
        [MustBeSerializable]
        private sealed class SomeImpl : Option<T> {
            private readonly T value;
            public SomeImpl(T value) => this.value = value;

            public override bool HasValue => true;
            public override Option<TRes> Map<TRes>(Func<T, TRes> mapping) => new Option<TRes>.SomeImpl(mapping(value));
            public override async Task<Option<TRes>> AsyncMap<TRes>(Func<T, Task<TRes>> mapping) =>
                new Option<TRes>.SomeImpl(await mapping(value));
            public override Option<TRes> FlatMap<TRes>(Func<T, Option<TRes>> mapping) => mapping(value);
            public override Task<Option<TRes>> AsyncFlatMap<TRes>(Func<T, Task<Option<TRes>>> mapping) =>
                mapping(value);
            public override T GetOrElse(Func<T> elseResultGetter) => value;
            public override void WhenPresentDo(Action<T> fn) => fn(value);
            public override bool Equals(T other) => EqualityComparer<T>.Default.Equals(other, value);
            public override bool Equals(Option<T> other) => 
                ReferenceEquals(this, other) || other is SomeImpl some && equals(some);
            public override bool Equals(object obj) => 
                ReferenceEquals(this, obj) || obj is SomeImpl other && equals(other);
            public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(value);
            
            private bool equals(SomeImpl other) => EqualityComparer<T>.Default.Equals(value, other.value);

            public override string ToString() => $"Some<{typeof(T)}> {value.ToString()}";
        }

        [MustBeSerializable]
        private sealed class NoneImpl : Option<T> {
            public override bool HasValue => false;
            public override Option<TRes> Map<TRes>(Func<T, TRes> mapping) => new Option<TRes>.NoneImpl();
            public override Task<Option<TRes>> AsyncMap<TRes>(Func<T, Task<TRes>> mapping) =>
                new Option<TRes>.NoneImpl().AsCompletedTask<Option<TRes>>();
            public override Option<TRes> FlatMap<TRes>(Func<T, Option<TRes>> mapping) => new Option<TRes>.NoneImpl();
            public override Task<Option<TRes>> AsyncFlatMap<TRes>(Func<T, Task<Option<TRes>>> mapping) =>
                new Option<TRes>.NoneImpl().AsCompletedTask<Option<TRes>>();
            public override T GetOrElse(Func<T> elseResultGetter) => elseResultGetter();
            public override void WhenPresentDo(Action<T> fn) { }
            public override bool Equals(T other) => false;
            public override bool Equals(Option<T> other) => ReferenceEquals(this, other) || other is NoneImpl;
            public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is NoneImpl;
            public override int GetHashCode() => typeof(T).GetHashCode();

            public override string ToString() => $"None<{typeof(T)}>";
        }

    }

    public static class OptionConstructionExtensions {
        public static Option<T> Some<T>(this T value) => Option<T>.Some(value);

        public static Option<T> ToOption<T>(this bool cond, Func<T> whenTrue) =>
            cond ? Option.Some(whenTrue()) : Option.None;
    }

    public static class OptionQueryExtensions {
        public static bool IsNone<T>(this Option<T> opt) => !opt.HasValue;
        public static T GetOrDefault<T>(this Option<T> opt) => opt.GetOrElse(elseResultGetter: () => default);
        
        public static T GetOrThrow<T>(this Option<T> opt, Func<Exception> exception) =>
            opt.GetOrElse(() => throw exception());
        
        // Note DS: Intentionally an overload instead of making the exception optional so that 
        // Func<T> get = option.GetOrThrow;
        // works.
        public static T GetOrThrow<T>(this Option<T> opt) => opt.GetOrThrow(
            () => new InvalidOperationException("No value present."));
        
        public static Option<T> OrWhenNone<T>(this Option<T> opt, Func<Option<T>> noneResultGetter) => opt.HasValue ? opt : noneResultGetter();

        public static Option<T> Where<T>(this Option<T> opt, Func<T, bool> predicate) =>
            opt.FlatMap(v => Option.If(predicate(v), () => v));

        public static bool TryGetValue<T>(this Option<T> opt, out T result) {
            result = opt.GetOrElse(elseResultGetter: () => default);
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

        public static Result<TSuccess, TFailure> AsSuccess<TSuccess, TFailure>(
            this Option<TSuccess> opt, Func<TFailure> failureIfNotPresent
        ) => Result.If(
            opt.HasValue,
            opt.GetOrThrow,
            failureIfNotPresent
        );

        public static Result<TSuccess, TFailure> AsFailure<TSuccess, TFailure>(
            this Option<TFailure> opt, Func<TSuccess> successIfNotPresent
        ) => Result.If(
            !opt.HasValue,
            successIfNotPresent,
            opt.GetOrThrow
        );

        public static IEnumerable<Option<T>> Sequence<T>(this Option<IEnumerable<T>> option, int sizeIfNone)
            => option.Map(enumerable => enumerable.Select(Option.Some))
                .GetOrElse(() => Enumerable.Repeat(Option<T>.None, sizeIfNone));

        public static IReadOnlyCollection<Option<T>> Sequence<T>(this Option<IReadOnlyCollection<T>> option, int sizeIfNone)
            => option.Map(enumerable => enumerable.Select(Option.Some))
                .GetOrElse(() => Enumerable.Repeat(Option<T>.None, sizeIfNone)).AsReadOnlyCollection();

        public static IReadOnlyList<Option<T>> Sequence<T>(this Option<IReadOnlyList<T>> option, int sizeIfNone)
            => option.Map(enumerable => enumerable.Select(Option.Some))
                .GetOrElse(() => Enumerable.Repeat(Option<T>.None, sizeIfNone)).AsReadOnlyList();

        public static Option<IEnumerable<T>> Sequence<T>(this IEnumerable<Option<T>> enumerable)
            => enumerable.Aggregate(
                Option.Some(Enumerable.Empty<T>()), 
                (resO, nowO) => resO.FlatMap(res => nowO.Map(res.Append)));

        public static Option<IEnumerable<T>> Traverse<T>(this IEnumerable<T> enumerable, Func<T, Option<T>>? f = null) =>
            enumerable.Select(element => f == null ? Option.Some(element) : f(element)).Sequence();
        
        
        public static Option<IEnumerable<T>> TraverseNoneIfEmpty<T>(this IEnumerable<T> enumerable, Func<T, Option<T>>? f = null) =>
            enumerable.Select(element => f == null ? Option.Some(element) : f(element)).OrIfEmpty(() => Option<T>.None.WrapAsEnumerable()).Sequence();
        
        public static Option<T> Flatten<T>(this Option<Option<T>> option)
            => option.FlatMap(o => o);

        public static Result<T, TErr> OrError<T, TErr>(this Option<T> opt, Func<TErr> errorGetter) =>
            opt.Match<T, Result<T,TErr>>(
                arg => Result.Success(arg),
                () => Result.Failure(errorGetter())
            );

        public static IEnumerable<T> Collect<T>(this IEnumerable<Option<T>> options) {
            foreach (var element in options)
                if (element.TryGetValue(out var val))
                    yield return val;
        }

        public static IEnumerable<S> Collect<T, S>(this IEnumerable<T> input, Func<T, Option<S>> mapping) =>
            input.Select(mapping).Collect();
        
        public static IEnumerable<S> CollectMany<T, S>(this IEnumerable<T> input, Func<T, IEnumerable<Option<S>>> mapping) =>
            input.SelectMany(mapping).Collect();
        
        public static Option<T> FirstSomeOrNone<T>(this IEnumerable<Option<T>> options)
            => options.Collect().FirstOrNone();

        public static IEnumerable<T> Flatten<T>(IEnumerable<Option<T>> source) =>
            source.Where(opt => opt.HasValue).Select(opt => opt.GetOrThrow());

        public static Result<Option<TVal>, TErr> Flip<TVal, TErr>(this Option<Result<TVal, TErr>> option) =>
            option.Match(
                value => value.Map(val => val.Some()),
                () => Option<TVal>.None.Success()
            );

        public static Task<Option<TVal>> Flip<TVal>(this Option<Task<TVal>> option) =>
            option.Match(
                value => value.Map(val => val.Some()),
                () => Option<TVal>.None.AsCompletedTask()
            );

        public static IEnumerable<T> AsEnumerable<T>(this Option<T> option) => option.Match(
            onHasValue: value => value.WrapAsEnumerable(),
            onNone: Enumerable.Empty<T>
        );
    }
}