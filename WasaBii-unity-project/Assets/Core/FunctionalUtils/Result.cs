#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BII.WasaBii.Core {

    /// Interface existing solely for working dynamically with Results
    public interface ResultBase {
        bool WasSuccessful { get; }
    }

    public interface IResult<out TError> : ResultBase {
        T MatchDynamic<T>(Func<object, T> onSuccess, Func<TError, T> onError);
        void MatchDynamic(Action<object> onSuccess, Action<TError> onError);
    }
    
    public interface IResult<out TValue, out TError> : IResult<TError> {
        T Match<T>(Func<TValue, T> onSuccess, Func<TError, T> onError);
        void Match(Action<TValue> onSuccess, Action<TError> onError);
        TValue ResultOrThrow(ForInType<TValue>? _ = default);
        TError ErrorOrThrow(ForInType<TError>? _ = default);
    }
    
    public static class Result {
        
        public static Result<TValue, TError> Success<TValue, TError>(this TValue result) => 
            new SuccessResult<TValue, TError>(result);
        
        public static Success<TValue> Success<TValue>(this TValue result) => new(result);
        
        public static Result<TValue, TError> Failure<TValue, TError>(this TError error) =>
            new ErrorResult<TValue, TError>(error);

        public static Failure<TError> Failure<TError>(this TError error) => new(error);

        public static Result<TValue, TError> If<TValue, TError>(
            bool predicate, Func<TValue> then, Func<TError> onError
        ) => predicate 
            ? Success<TValue, TError>(then()) 
            : Failure<TValue, TError>(onError());

        public static Result<TValue, TError> IfNotNull<TValue, TError>(
            TValue? value, Func<TError> whenNull
        ) where TValue : struct
            // ReSharper disable once PossibleInvalidOperationException because .Value will obviously succeed
            => If(value.HasValue, () => value.Value, whenNull);

        public static Result<TValue, TError> IfNotNull<TValue, TError>(
            TValue? value, Func<TError> whenNull
        ) where TValue : class => If(value != null, () => value!, whenNull);

        public static Result<TValue, TError> Try<TValue, TError>(
            Func<TValue> valueConstructor,
            Func<Exception, TError> onException
        ) {
            try {
                return valueConstructor().Success();
            }
            catch (Exception e) {
                return onException(e).Failure();
            }
        }

        public static void WhenSuccessful<TValue, TError>(this Result<TValue, TError> source, Action<TValue> onSuccess) =>
            source.Match(onSuccess, _ => { });
        
        public static void WhenSuccessful<TError>(this IResult<TError> source, Action<object> onSuccess) =>
            source.MatchDynamic(onSuccess, _ => { });
        
        public static void WhenError<TError>(this IResult<TError> source, Action<TError> onError) =>
            source.MatchDynamic(_ => { }, onError);

        public static Result<TRes, TError> Map<TValue, TRes, TError>(
            this Result<TValue, TError> source, Func<TValue, TRes> mapping
        ) => source.Match(
            res => Success<TRes, TError>(mapping(res)),
            Failure<TRes, TError>
        );

        public static async Task<Result<TRes, TError>> MapAsync<TValue, TRes, TError>(
            this Result<TValue, TError> source, Func<TValue, Task<TRes>> mapping
        ) => await source.Match(
            async result => Success<TRes, TError>(await mapping(result)),
            err => Failure<TRes, TError>(err).AsCompletedTask()
        );

        public static Result<TRes, TError> FlatMap<TValue, TRes, TError>(
            this Result<TValue, TError> source, Func<TValue, Result<TRes, TError>> mapping
        ) => source.Match(mapping, Failure<TRes, TError>);

        public static async Task<Result<TRes, TError>> FlatMapAsync<TValue, TRes, TError>(
            this Result<TValue, TError> source, Func<TValue, Task<Result<TRes, TError>>> mapping
        ) => await source.Match(
            async result => await mapping(result),
            err => Failure<TRes, TError>(err).AsCompletedTask()
        );

        public static Result<IEnumerable<TValue>, TError> Sequence<TValue, TError>(
            this IEnumerable<Result<TValue, TError>> enumerable
        ) => enumerable.Aggregate(
            Success<IEnumerable<TValue>, TError>(Enumerable.Empty<TValue>()),
            (result, current) => result.FlatMap(r => current.Map(r.Append))
        );

        public static Result<TValue, TErrorRes> MapError<TValue, TError, TErrorRes>(
            this Result<TValue, TError> source, Func<TError, TErrorRes> mapping
        ) => source.Match(
            Success<TValue, TErrorRes>,
            err => Failure<TValue, TErrorRes>(mapping(err))
        );
        
        public static Result<TValue, TErrorRes> FlatMapError<TValue, TError, TErrorRes>(
            this Result<TValue, TError> source, Func<TError, Result<TValue, TErrorRes>> mapping
        ) => source.Match(Success<TValue, TErrorRes>, mapping);
        
        public static Result<TRes, TError> MapDynamic<TRes, TError>(
            this IResult<TError> source, Func<object, TRes> mapping
        ) => source.MatchDynamic(
            res => Success<TRes, TError>(mapping(res)),
            Failure<TRes, TError>
        );
        
        public static Result<TRes, TError> FlatMapDynamic<TRes, TError>(
            this IResult<TError> source, Func<object, Result<TRes, TError>> mapping
        ) => source.MatchDynamic(mapping, Failure<TRes, TError>);

        public static void ThrowIfError<TError>(this Result<Nothing, TError> result) => result.ResultOrThrow();
        
        public static TRes OrElse<TRes, TError>(
            this Result<TRes, TError> source, TRes alt
        ) => source.Match(val => val, _ => alt);
        public static TRes OrElse<TRes, TError>(
            this Result<TRes, TError> source, Func<TError, TRes> mapping
        ) => source.Match(val => val, mapping);

        public static Task<TRes> OrElse<TRes, TError>(
            this Result<TRes, TError> source, Func<TError, Task<TRes>> mapping
        ) => source.WasSuccessful ? source.ResultOrThrow().AsCompletedTask() : mapping(source.ErrorOrThrow());
        
        public static TValue ResultOrDefault<TValue, TError>(this Result<TValue, TError> source)
            => source.Match(success => success, _ => default);
        
        public static TError ErrorOrDefault<TValue, TError>(this Result<TValue, TError> source)
            => source.Match(_ => default, error => error);

        public static Option<TValue> ResultOrNone<TValue, TError>(this Result<TValue, TError> source)
            => source.Match(Option.Some, _ => Option.None);
        
        public static Option<TError> ErrorOrNone<TValue, TError>(this Result<TValue, TError> source)
            => source.Match(_ => Option.None, Option.Some);

        public static Option<Result<TVal, TErr>> Flip<TVal, TErr>(this Result<Option<TVal>, TErr> result) =>
            result.Match(
                onSuccess: option => option.Map(Success<TVal, TErr>),
                onError: err => Option.Some(Failure<TVal, TErr>(err))
            );

        public static Option<TRes> TryGetValue<TRes, TError>(this Result<TRes, TError> result) =>
            result.Match(Option.Some, _ => Option<TRes>.None);
        
        public static Option<TError> TryGetError<TRes, TError>(this Result<TRes, TError> result) =>
            result.Match(res => Option<TError>.None, Option.Some);
        
        public static bool TryGetValue<TRes, TError>(this Result<TRes, TError> result, out TRes val) {
            bool b;
            (val, b) = result.Match(
                res => (res, true),
                err => (default, false)
            );
            return b;
        }
        
        public static bool TryGetError<TRes, TError>(this Result<TRes, TError> result, out TError err) {
            bool b;
            (err, b) = result.Match(
                (TRes res) => (default, false),
                e => (e, true)
            );
            return b;
        }
        
        public static Result<TRes, TErr> ToResult<TRes, TErr>(this Option<TRes> opt, Func<TErr> whenNoValue) =>
            opt.Match(onHasValue: Success<TRes, TErr>, onNone: () => Failure<TRes, TErr>(whenNoValue()));

        [MustBeSerializable]
        internal sealed class ErrorResult<TValue, TError> : Result<TValue, TError> {
            private readonly TError error;
            public ErrorResult(TError error) => this.error = error;

            public override bool WasSuccessful => false;

            protected override TValue resultOrThrow() => throw new InvalidOperationException("Not a successful result: " + error);

            protected override TError errorOrThrow() => error;

            public override TRes Match<TRes>(
                Func<TValue, TRes> onSuccess, Func<TError, TRes> onError
            ) => onError(error); 
        
            public override void Match(
                Action<TValue> onSuccess, Action<TError> onError
            ) => onError(error);
        }

        [MustBeSerializable]
        internal sealed class SuccessResult<TValue, TError> : Result<TValue, TError> {
            private readonly TValue result;
            public SuccessResult(TValue result) => this.result = result;

            public override bool WasSuccessful => true;
            protected override TValue resultOrThrow() => result;
            protected override TError errorOrThrow() => throw new InvalidOperationException("Not an error: " + result);

            public override TRes Match<TRes>(
                Func<TValue, TRes> onSuccess, Func<TError, TRes> onError
            ) => onSuccess(result);
        
            public override void Match(
                Action<TValue> onSuccess, Action<TError> onError
            ) => onSuccess(result);
        }
    }
    
    public readonly struct Success<TValue> {
        internal readonly TValue result;
        public Success(TValue result) => this.result = result;
    }
    
    public readonly struct Failure<TError> {
        internal readonly TError error;
        public Failure(TError error) => this.error = error;
    }

    [MustBeSerializable]
    public abstract class Result<TValue, TError> : IResult<TValue, TError> {
        public abstract bool WasSuccessful { get; }

        protected abstract TValue resultOrThrow();
        public TValue ResultOrThrow(ForInType<TValue> _ = default) => resultOrThrow();
        
        protected abstract TError errorOrThrow();
        public TError ErrorOrThrow(ForInType<TError> _ = default) => errorOrThrow();

        public abstract TRes Match<TRes>(Func<TValue, TRes> onSuccess, Func<TError, TRes> onError);
        public abstract void Match(Action<TValue> onSuccess, Action<TError> onError);

        public TRes MatchDynamic<TRes>(Func<object, TRes> onSuccess, Func<TError, TRes> onError) =>
            Match(value => onSuccess(value), onError);

        public void MatchDynamic(Action<object> onSuccess, Action<TError> onError) =>
            Match(value => onSuccess(value), onError);

        public static implicit operator Result<TValue, TError>(Success<TValue> success) => new Result.SuccessResult<TValue,TError>(success.result);
        public static implicit operator Result<TValue, TError>(Failure<TError> failure) => new Result.ErrorResult<TValue,TError>(failure.error);
        
        public static implicit operator Result<TValue,TError>(TValue success) => new Result.SuccessResult<TValue,TError>(success);
        public static implicit operator Result<TValue,TError>(TError failure) => new Result.ErrorResult<TValue,TError>(failure);
    }
}