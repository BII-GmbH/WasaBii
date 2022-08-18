#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BII.WasaBii.Core {
    
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
    }
    
    /// <summary> Interface existing solely for working dynamically with Results via reflection </summary>
    public interface UntypedResult {
        bool WasSuccessful { get; }
    }
    
    /// <summary>
    /// Temporary object indicating a success of type <typeparamref name="TValue"/>.
    /// As we know that it is a success, we do not need to specify an error type.
    /// Implicitly and explicitly convertible to a <see cref="Result{T,E}"/> of any error type.
    /// </summary>
    public readonly struct Success<TValue> {
        public readonly TValue Result;
        public Success(TValue result) => this.Result = result;
        public override bool Equals(object obj) => ((Result<TValue, object>) this).Equals(obj);
        public override int GetHashCode() => ((Result<TValue, object>) this).GetHashCode();
    }
    
    /// <summary>
    /// Temporary object indicating a failure of type <typeparamref name="TError"/>.
    /// As we know that it is a failure, we do not need to specify a value type.
    /// Implicitly and explicitly convertible to a <see cref="Result{T,E}"/> of any value type.
    /// </summary>
    public readonly struct Failure<TError> {
        public readonly TError Error;
        public Failure(TError error) => this.Error = error;
        public override bool Equals(object obj) => ((Result<object, TError>) this).Equals(obj);
        public override int GetHashCode() => ((Result<object, TError>) this).GetHashCode();
    }

    /// <summary>
    /// Describes the result of a computation that can fail.
    /// Is either successful with a value of type <typeparamref name="TValue"/>
    ///  or a an error with a value of type <typeparamref name="TError"/>.
    /// Unlike throwing an exception, the programmer must handle the error case of a <see cref="Result{T,E}"/>.
    /// When you consistently use Results for computations that can fail based on the parameters, then you can use
    ///  exceptions as "panics" - programmer errors that need to be caught at the top-level only.
    /// </summary>
    [MustBeSerializable]
    public readonly struct Result<TValue, TError> : UntypedResult, IEquatable<Result<TValue, TError>> {

        // Since this is a struct, the `status` field will be default-initialized to the first value of this enum.
        // As a default-initialized Result has neither a value or an error, it will have the status `Default`.
        public enum ValueStatus { Default, Value, Error }
        
        // Fields are public for convenient support for pattern matching syntax

        public readonly ValueStatus Status;
        public readonly TValue? ResultOrDefault;
        public readonly TError? ErrorOrDefault;

        private Result(TValue result) {
            this.ResultOrDefault = result;
            this.ErrorOrDefault = default;
            this.Status = ValueStatus.Value;
        }
        
        private Result(TError error) {
            this.ResultOrDefault = default;
            this.ErrorOrDefault = error;
            this.Status = ValueStatus.Error;
        }

        public bool WasSuccessful => Status == ValueStatus.Value;
        public bool WasFailure => Status == ValueStatus.Error;
        
        public TValue ResultOrThrow(Func<Exception>? exception = null) => 
            WasSuccessful 
                ? ResultOrDefault! 
                : throw exception?.Invoke() ?? new InvalidOperationException("Not a successful result: " + ErrorOrDefault);
        
        public TError ErrorOrThrow(Func<Exception>? exception = null) => 
            WasSuccessful 
                ? throw exception?.Invoke() ?? new InvalidOperationException("Not an error: " + ResultOrDefault) 
                : ErrorOrDefault!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TRes Match<TRes>(Func<TValue, TRes> onSuccess, Func<TError, TRes> onFailure) =>
            Status switch {
                ValueStatus.Default => throw new InvalidOperationException("Cannot match on a default result."),
                ValueStatus.Value => onSuccess(ResultOrDefault!),
                ValueStatus.Error => onFailure(ErrorOrDefault!),
                _ => throw new UnsupportedEnumValueException(Status, $"{nameof(Result<TValue,TError>)}.{nameof(DoMatch)}")
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoMatch(Action<TValue> onSuccess, Action<TError> onFailure) {
            switch (Status) {
                case ValueStatus.Default: throw new InvalidOperationException("Cannot match on a default result.");
                case ValueStatus.Value: onSuccess(ResultOrDefault!); break;
                case ValueStatus.Error: onFailure(ErrorOrDefault!); break;
                default: 
                    throw new UnsupportedEnumValueException(Status, $"{nameof(Result<TValue, TError>)}.{nameof(DoMatch)}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TRes MatchDynamic<TRes>(Func<object, TRes> onSuccess, Func<TError, TRes> onFailure) =>
            Match(value => onSuccess(value!), onFailure);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MatchDynamic(Action<object> onSuccess, Action<TError> onFailure) =>
            DoMatch(value => onSuccess(value!), onFailure);

        public static implicit operator Result<TValue, TError>(Success<TValue> success) => new(success.Result);
        public static implicit operator Result<TValue, TError>(Failure<TError> failure) => new(failure.Error);
        
        public static implicit operator Result<TValue,TError>(TValue success) => new(success);
        public static implicit operator Result<TValue,TError>(TError failure) => new(failure);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue, TError> Success(TValue value) => new(value);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue, TError> Failure(TError error) => new(error);

        [Pure] public bool Equals(TValue other) => WasSuccessful && Equals(other, ResultOrDefault!);
        [Pure] public bool Equals(TError other) => WasFailure && Equals(other, ErrorOrDefault!);

        [Pure] public bool Equals(Result<TValue, TError> other) => 
            Equals(Status, other.Status) && Equals(ResultOrDefault, other.ResultOrDefault) && Equals(ErrorOrDefault, other.ErrorOrDefault);

        [Pure]
        public override bool Equals(object obj) =>
            obj is Success<TValue> success && Equals(success)
            || obj is Failure<TError> failure && Equals(failure)
            || obj is Result<TValue, TError> other && Equals(other);

        [Pure] public static bool operator ==(Result<TValue, TError> first, Result<TValue, TError> second) => first.Equals(second);
        [Pure] public static bool operator !=(Result<TValue, TError> first, Result<TValue, TError> second) => !first.Equals(second);

        [Pure] public override int GetHashCode() => HashCode.Combine(ResultOrDefault, ErrorOrDefault, Status);
        
        [Pure] public override string ToString() => Status switch {
            ValueStatus.Default => "Default Null-Result",
            ValueStatus.Value => $"Success({ResultOrDefault!})",
            ValueStatus.Error => $"Error({ErrorOrDefault!})",
            _ => throw new UnsupportedEnumValueException(Status, $"{nameof(Result<TValue,TError>)}.{nameof(ToString)}")
        };
        
#region Additional Methods

        // These are members and not extensions in order to avoid potential problems
        //  with extension method resolution and reduce the number of required generics in some cases.
        // There is really no need for these to be extension methods.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WhenSuccessful(Action<TValue> onSuccess) => DoMatch(onSuccess, _ => { });
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WhenFailure(Action<TError> onFailure) => DoMatch(_ => { }, onFailure);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public Result<TRes, TError> Map<TRes>(Func<TValue, TRes> mapping) => 
            Match(
                res => mapping(res).Success<TRes, TError>(),
                err => err.Failure()
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public Task<Result<TRes, TError>> MapAsync<TRes>(Func<TValue, Task<TRes>> mapping) => 
            Match(
                async res => (await mapping(res)).Success<TRes, TError>(),
                err => err.Failure<TRes, TError>().AsCompletedTask()
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public Result<TRes, TError> FlatMap<TRes>(Func<TValue, Result<TRes, TError>> mapping) => 
            Match(mapping, Result.Failure<TRes, TError>);

        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        [Pure] public async Task<Result<TRes, TError>> FlatMapAsync<TRes>(
            Func<TValue, Task<Result<TRes, TError>>> mapping
        ) => await Match(
                async res => await mapping(res),
                err => err.Failure<TRes, TError>().AsCompletedTask()
            );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public Result<TValue, TErrorRes> MapError<TErrorRes>(Func<TError, TErrorRes> mapping) => 
            Match(
                Result.Success<TValue, TErrorRes>,
                err => Result.Failure<TValue, TErrorRes>(mapping(err))
            );
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public Result<TValue, TErrorRes> FlatMapError<TErrorRes>(Func<TError, Result<TValue, TErrorRes>> mapping) => 
            Match(Result.Success<TValue, TErrorRes>, mapping);

        public TValue OrElse(TValue alt) => Match(val => val, _ => alt);
        
        public TValue OrElse(Func<TError, TValue> mapping) => Match(val => val, mapping);
        
        public Task<TValue> OrElse(Func<TError, Task<TValue>> mapping) => 
            WasSuccessful ? ResultOrDefault!.AsCompletedTask() : mapping(ErrorOrDefault!);
        
        [Pure] public Option<TValue> ResultOrNone() => Match(Option.Some, _ => Option.None);
        
        [Pure] public Option<TError> ErrorOrNone() => Match(_ => Option.None, Option.Some);

        [Pure] public Option<TValue> TryGetResult() => Match(Option.Some, _ => Option<TValue>.None);
        
        [Pure] public Option<TError> TryGetError() => Match(_ => Option<TError>.None, Option.Some);
        
        [Pure] public bool TryGetValue(out TValue val) {
            bool b;
            (val, b) = Match(
                res => (res, true),
                _ => (default!, false)
            );
            return b;
        }
        
        [Pure] public bool TryGetError(out TError err) {
            bool b;
            (err, b) = Match(
                _ => (default!, false),
                err => (err, true)
            );
            return b;
        }

#endregion
    }

    public static class ResultExtensions {
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfFailure<TError>(this Result<Nothing, TError> result) => result.ResultOrThrow();
        
        [Pure] public static Option<Result<TVal, TErr>> Flip<TVal, TErr>(this Result<Option<TVal>, TErr> result) =>
            result.Match(
                onSuccess: option => option.Map(Result.Success<TVal, TErr>),
                onFailure: err => Option.Some(err.Failure<TVal, TErr>())
            );
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Result<IEnumerable<TValue>, TError> Flip<TValue, TError>(
            this IEnumerable<Result<TValue, TError>> enumerable
        ) => enumerable.Aggregate(
            Enumerable.Empty<TValue>().Success<IEnumerable<TValue>, TError>(),
            (result, current) => result.FlatMap(r => current.Map(r.Append))
        );

        [Pure] public static Result<TRes, TErr> ToResult<TRes, TErr>(this Option<TRes> opt, Func<TErr> whenNoValue) =>
            opt.Match(onHasValue: Result.Success<TRes, TErr>, onNone: () => Result.Failure<TRes, TErr>(whenNoValue()));
        
        [Pure] public static Result<T, TErr> OrError<T, TErr>(this Option<T> opt, Func<TErr> errorGetter) =>
            opt.Match<Result<T,TErr>>(onHasValue: arg => arg.Success(), onNone: () => Result.Failure(errorGetter()));
        
        [Pure] public static Result<TSuccess, TFailure> AsSuccess<TSuccess, TFailure>(
            this Option<TSuccess> opt, Func<TFailure> failureIfNotPresent
        ) => opt.Match(
            onHasValue: Result.Success<TSuccess, TFailure>, 
            onNone: () => Result.Failure<TSuccess, TFailure>(failureIfNotPresent())
        );
        
        [Pure] public static Result<TSuccess, TFailure> AsFailure<TSuccess, TFailure>(
            this Option<TFailure> opt, Func<TSuccess> successIfNotPresent
        ) => opt.Match(
            onHasValue: Result.Failure<TSuccess, TFailure>, 
            onNone: () => Result.Success<TSuccess, TFailure>(successIfNotPresent())
        );
    }
}