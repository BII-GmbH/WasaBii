#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BII.WasaBii.Core {

    /// Interface existing solely for working dynamically with Results via reflection
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
        [Pure] TValue? ResultOrDefault(ForInType<TValue>? _ = default);
        [Pure] TError? ErrorOrDefault(ForInType<TError>? _ = default);
    }
    
    public static class Result {
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Result<TValue, TError> Success<TValue, TError>(this TValue result) => 
            Result<TValue, TError>.Success(result);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Success<TValue> Success<TValue>(this TValue result) => new(result);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Result<TValue, TError> Failure<TValue, TError>(this TError error) =>
            Result<TValue, TError>.Failure(error);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Failure<TError> Failure<TError>(this TError error) => new(error);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue, TError> If<TValue, TError>(
            bool predicate, Func<TValue> then, Func<TError> onError
        ) => predicate 
            ? Result<TValue, TError>.Success(then()) 
            : Result<TValue, TError>.Failure(onError());

        public static Result<TValue, TError> IfNotNull<TValue, TError>(
            TValue? value, Func<TError> whenNull
        ) where TValue : struct => If(value.HasValue, () => value!.Value, whenNull);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WhenSuccessful<TValue, TError>(this Result<TValue, TError> source, Action<TValue> onSuccess) =>
            source.Match(onSuccess, _ => { });
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WhenSuccessful<TError>(this IResult<TError> source, Action<object> onSuccess) =>
            source.MatchDynamic(onSuccess, _ => { });
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WhenError<TError>(this IResult<TError> source, Action<TError> onError) =>
            source.MatchDynamic(_ => { }, onError);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Result<TRes, TError> Map<TValue, TRes, TError>(
            this Result<TValue, TError> source, Func<TValue, TRes> mapping
        ) => source.Match(
            res => Success<TRes, TError>(mapping(res)),
            Failure<TRes, TError>
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Task<Result<TRes, TError>> MapAsync<TValue, TRes, TError>(
            this Result<TValue, TError> source, Func<TValue, Task<TRes>> mapping
        ) => source.Match(
            async result => Success<TRes, TError>(await mapping(result)),
            err => Failure<TRes, TError>(err).AsCompletedTask()
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Result<TRes, TError> FlatMap<TValue, TRes, TError>(
            this Result<TValue, TError> source, Func<TValue, Result<TRes, TError>> mapping
        ) => source.Match(mapping, Failure<TRes, TError>);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static async Task<Result<TRes, TError>> FlatMapAsync<TValue, TRes, TError>(
            this Result<TValue, TError> source, Func<TValue, Task<Result<TRes, TError>>> mapping
        ) => await source.Match(
            async result => await mapping(result),
            err => Failure<TRes, TError>(err).AsCompletedTask()
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Result<IEnumerable<TValue>, TError> Sequence<TValue, TError>(
            this IEnumerable<Result<TValue, TError>> enumerable
        ) => enumerable.Aggregate(
            Success<IEnumerable<TValue>, TError>(Enumerable.Empty<TValue>()),
            (result, current) => result.FlatMap(r => current.Map(r.Append))
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Result<TValue, TErrorRes> MapError<TValue, TError, TErrorRes>(
            this Result<TValue, TError> source, Func<TError, TErrorRes> mapping
        ) => source.Match(
            Success<TValue, TErrorRes>,
            err => Failure<TValue, TErrorRes>(mapping(err))
        );
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Result<TValue, TErrorRes> FlatMapError<TValue, TError, TErrorRes>(
            this Result<TValue, TError> source, Func<TError, Result<TValue, TErrorRes>> mapping
        ) => source.Match(Success<TValue, TErrorRes>, mapping);
        
        [Pure] public static Result<TRes, TError> MapDynamic<TRes, TError>(
            this IResult<TError> source, Func<object, TRes> mapping
        ) => source.MatchDynamic(
            res => Success<TRes, TError>(mapping(res)),
            Failure<TRes, TError>
        );
        
        [Pure] public static Result<TRes, TError> FlatMapDynamic<TRes, TError>(
            this IResult<TError> source, Func<object, Result<TRes, TError>> mapping
        ) => source.MatchDynamic(mapping, Failure<TRes, TError>);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        
        [Pure] public static Option<TValue> ResultOrNone<TValue, TError>(this Result<TValue, TError> source) => 
            source.Match(Option.Some, _ => Option.None);
        
        [Pure] public static Option<TError> ErrorOrNone<TValue, TError>(this Result<TValue, TError> source) => 
            source.Match(_ => Option.None, Option.Some);
        
        [Pure] public static Option<Result<TVal, TErr>> Flip<TVal, TErr>(this Result<Option<TVal>, TErr> result) =>
            result.Match(
                onSuccess: option => option.Map(Success<TVal, TErr>),
                onError: err => Option.Some(Failure<TVal, TErr>(err))
            );
        
        [Pure] public static Option<TRes> TryGetValue<TRes, TError>(this Result<TRes, TError> result) =>
            result.Match(Option.Some, _ => Option<TRes>.None);
        
        [Pure] public static Option<TError> TryGetError<TRes, TError>(this Result<TRes, TError> result) =>
            result.Match(res => Option<TError>.None, Option.Some);
        
        [Pure] public static bool TryGetValue<TRes, TError>(this Result<TRes, TError> result, out TRes val) {
            bool b;
            (val, b) = result.Match(
                res => (res, true),
                err => (default!, false)
            );
            return b;
        }
        
        [Pure] public static bool TryGetError<TRes, TError>(this Result<TRes, TError> result, out TError err) {
            bool b;
            (err, b) = result.Match(
                _ => (default!, false),
                err => (err, true)
            );
            return b;
        }
        
        [Pure] public static Result<TRes, TErr> ToResult<TRes, TErr>(this Option<TRes> opt, Func<TErr> whenNoValue) =>
            opt.Match(onHasValue: Success<TRes, TErr>, onNone: () => Failure<TRes, TErr>(whenNoValue()));
        
        [Pure] public static Result<T, TErr> OrError<T, TErr>(this Option<T> opt, Func<TErr> errorGetter) =>
            opt.Match<T, Result<T,TErr>>(onHasValue: arg => arg.Success(), onNone: () => Failure(errorGetter()));
        
        [Pure] public static Result<TSuccess, TFailure> AsSuccess<TSuccess, TFailure>(
            this Option<TSuccess> opt, Func<TFailure> failureIfNotPresent
        ) => opt.Match(onHasValue: Success<TSuccess, TFailure>, onNone: () => Failure<TSuccess, TFailure>(failureIfNotPresent()));
        
        [Pure] public static Result<TSuccess, TFailure> AsFailure<TSuccess, TFailure>(
            this Option<TFailure> opt, Func<TSuccess> successIfNotPresent
        ) => opt.Match(onHasValue: Failure<TSuccess, TFailure>, onNone: () => Success<TSuccess, TFailure>(successIfNotPresent()));
    }
    
    public readonly struct Success<TValue> {
        public readonly TValue Result;
        public Success(TValue result) => this.Result = result;
    }
    
    public readonly struct Failure<TError> {
        public readonly TError Error;
        public Failure(TError error) => this.Error = error;
    }

    /// <summary>
    /// Describes the result of a computation that can fail.
    /// Is either successful with a value of type <typeparamref name="TValue"/>
    ///  or a failure with an error value of type <typeparamref name="TError"/>.
    /// Unlike throwing an exception, the programmer must handle the error case of a <see cref="Result{T,E}"/>.
    /// When you consistently use Results for computations that can fail based on the parameters, then you can use
    ///  exceptions as "panics" - programmer errors that need to be caught at the top-level only.
    /// </summary>
    [MustBeSerializable]
    public readonly struct Result<TValue, TError> : IResult<TValue, TError> {
        public bool WasSuccessful { get; }
        private readonly TValue? result;
        private readonly TError? error;

        private Result(TValue? result, TError? error, bool wasSuccessful)
        {
            this.WasSuccessful = wasSuccessful;
            this.result = result;
            this.error = error;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue ResultOrThrow(ForInType<TValue>? _ = default) => 
            WasSuccessful ? result! : throw new InvalidOperationException("Not a successful result: " + error);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TError ErrorOrThrow(ForInType<TError>? _ = default) => 
            WasSuccessful ? throw new InvalidOperationException("Not an error: " + result) : error!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public TValue? ResultOrDefault(ForInType<TValue>? _ = default) => result;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public TError? ErrorOrDefault(ForInType<TError>? _ = default) => error;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TRes Match<TRes>(Func<TValue, TRes> onSuccess, Func<TError, TRes> onError) =>
            WasSuccessful ? onSuccess(result!) : onError(error!);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Match(Action<TValue> onSuccess, Action<TError> onError) {
            if (WasSuccessful) onSuccess(result!);
            else onError(error!);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TRes MatchDynamic<TRes>(Func<object, TRes> onSuccess, Func<TError, TRes> onError) =>
            Match(value => onSuccess(value!), onError);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MatchDynamic(Action<object> onSuccess, Action<TError> onError) =>
            Match(value => onSuccess(value!), onError);

        public static implicit operator Result<TValue, TError>(Success<TValue> success) => new(success.Result, default, true);
        public static implicit operator Result<TValue, TError>(Failure<TError> failure) => new(default, failure.Error, false);
        
        public static implicit operator Result<TValue,TError>(TValue success) => new(success, default, true);
        public static implicit operator Result<TValue,TError>(TError failure) => new(default, failure, false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue, TError> Success(TValue value) => new(value, default, true);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue, TError> Failure(TError error) => new(default, error, false);
    }
}