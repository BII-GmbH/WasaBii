#nullable enable
using System;
using System.Collections;
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
        ) => predicate ? Success<TValue, TError>(then()) : Failure<TValue, TError>(onError());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue, TError> If<TValue, TError>(
            bool predicate, TValue thenValue, Func<TError> onError
        ) => predicate ? Success<TValue, TError>(thenValue) : Failure<TValue, TError>(onError());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue, TError> If<TValue, TError>(
            bool predicate, Func<TValue> then, TError errorValue
        ) => predicate ? Success<TValue, TError>(then()) : Failure<TValue, TError>(errorValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue, TError> If<TValue, TError>(
            bool predicate, TValue thenValue, TError errorValue
        ) => predicate ? Success<TValue, TError>(thenValue) : Failure<TValue, TError>(errorValue);

        public static Result<TValue, TError> IfNotNull<TValue, TError>(TValue? value, Func<TError> whenNull)
        where TValue : struct => value.HasValue ? value.Value.Success() : whenNull().Failure();

        public static Result<TValue, TError> IfNotNull<TValue, TError>(
            TValue? value, Func<TError> whenNull
        ) where TValue : class => value != null ? value.Success() : whenNull().Failure();
        
        public static Result<TValue, Exception> Try<TValue>(
            Func<TValue> valueConstructor
        ) {
            try { return valueConstructor(); } catch (Exception e) { return e; }
        }

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
    
    /// <summary>
    /// Temporary object indicating a success of type <typeparamref name="TValue"/>.
    /// As we know that it is a success, we do not need to specify an error type.
    /// Implicitly and explicitly convertible to a <see cref="Result{T,E}"/> of any error type.
    /// </summary>
    public readonly struct Success<TValue> {
        public readonly TValue Result;
        public Success(TValue result) => this.Result = result;
        public override bool Equals(object? obj) => ((Result<TValue, object>) this).Equals(obj);
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
        public override bool Equals(object? obj) => ((Result<object, TError>) this).Equals(obj);
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
    [Serializable]
    public readonly struct Result<TValue, TError> : IEquatable<Result<TValue, TError>> {

        // Since this is a struct, the `status` field will be default-initialized to the first value of this enum.
        // As a default-initialized Result has neither a value or an error, it will have the status `Default`.
        [Serializable]
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

        public TValue ResultOrThrow(Func<Exception>? exception = null) => WasSuccessful
            ? ResultOrDefault!
            : throw exception?.Invoke()
                ?? (ErrorOrDefault is Exception ex
                    ? new InvalidOperationException("Not a successful result", ex)
                    : new InvalidOperationException("Not a successful result: " + ErrorOrDefault));
        
        public TValue ResultOrThrow(Func<TError, Exception> exception) => 
            WasSuccessful 
                ? ResultOrDefault! 
                : throw exception(ErrorOrDefault!);
        
        public TError ErrorOrThrow(Func<Exception>? exception = null) => 
            WasSuccessful 
                ? throw exception?.Invoke() ?? new InvalidOperationException("Not an error: " + ResultOrDefault) 
                : ErrorOrDefault!;

        public TError ErrorOrThrow(Func<TValue, Exception> exception) => 
            WasSuccessful 
                ? throw exception(ResultOrDefault!)
                : ErrorOrDefault!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TRes Match<TRes>(Func<TValue, TRes> onSuccess, Func<TError, TRes> onFailure) =>
            Status switch {
                ValueStatus.Default => throw new InvalidOperationException("Cannot match on a default result."),
                ValueStatus.Value => onSuccess(ResultOrDefault!),
                ValueStatus.Error => onFailure(ErrorOrDefault!),
                _ => throw new UnsupportedEnumValueException(Status)
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoMatch(Action<TValue> onSuccess, Action<TError> onFailure) {
            switch (Status) {
                case ValueStatus.Default: throw new InvalidOperationException("Cannot match on a default result.");
                case ValueStatus.Value: onSuccess(ResultOrDefault!); break;
                case ValueStatus.Error: onFailure(ErrorOrDefault!); break;
                default: 
                    throw new UnsupportedEnumValueException(Status);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TRes MatchDynamic<TRes>(Func<object, TRes> onSuccess, Func<TError, TRes> onFailure) =>
            Status switch {
                ValueStatus.Default => throw new InvalidOperationException("Cannot match on a default result."),
                ValueStatus.Value => onSuccess(ResultOrDefault!),
                ValueStatus.Error => onFailure(ErrorOrDefault!),
                _ => throw new UnsupportedEnumValueException(Status)
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoMatchDynamic(Action<object> onSuccess, Action<TError> onFailure) {
            switch (Status) {
                case ValueStatus.Default: throw new InvalidOperationException("Cannot match on a default result.");
                case ValueStatus.Value: onSuccess(ResultOrDefault!); break;
                case ValueStatus.Error: onFailure(ErrorOrDefault!); break;
                default: 
                    throw new UnsupportedEnumValueException(Status);
            }
        }

        public static implicit operator Result<TValue, TError>(Success<TValue> success) => new(success.Result);
        public static implicit operator Result<TValue, TError>(Failure<TError> failure) => new(failure.Error);
        
        public static implicit operator Result<TValue,TError>(TValue success) => new(success);
        public static implicit operator Result<TValue,TError>(TError failure) => new(failure);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue, TError> Success(TValue value) => new(value);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue, TError> Failure(TError error) => new(error);

        [Pure] public bool Equals(TValue? other) => WasSuccessful && Equals(other, ResultOrDefault!);
        [Pure] public bool Equals(TError? other) => WasFailure && Equals(other, ErrorOrDefault!);

        [Pure] public bool Equals(Result<TValue, TError> other) => 
            Equals(Status, other.Status) && Equals(ResultOrDefault, other.ResultOrDefault) && Equals(ErrorOrDefault, other.ErrorOrDefault);

        [Pure]
        public override bool Equals(object? obj) =>
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
            _ => throw new UnsupportedEnumValueException(Status)
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
            Status switch {
                ValueStatus.Default => throw new InvalidOperationException("Cannot match on a default result."),
                ValueStatus.Value => mapping(ResultOrDefault!).Success(),
                ValueStatus.Error => ErrorOrDefault!.Failure(),
                _ => throw new UnsupportedEnumValueException(Status)
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public async Task<Result<TRes, TError>> MapAsync<TRes>(Func<TValue, Task<TRes>> mapping) => 
            Status switch {
                ValueStatus.Default => throw new InvalidOperationException("Cannot match on a default result."),
                ValueStatus.Value => (await mapping(ResultOrDefault!)).Success(),
                ValueStatus.Error => ErrorOrDefault!.Failure(),
                _ => throw new UnsupportedEnumValueException(Status)
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public Result<TRes, TError> FlatMap<TRes>(Func<TValue, Result<TRes, TError>> mapping) => 
            Status switch {
                ValueStatus.Default => throw new InvalidOperationException("Cannot match on a default result."),
                ValueStatus.Value => mapping(ResultOrDefault!),
                ValueStatus.Error => ErrorOrDefault!.Failure(),
                _ => throw new UnsupportedEnumValueException(Status)
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        [Pure] public async Task<Result<TRes, TError>> FlatMapAsync<TRes>(
            Func<TValue, Task<Result<TRes, TError>>> mapping
        ) => Status switch {
                ValueStatus.Default => throw new InvalidOperationException("Cannot match on a default result."),
                ValueStatus.Value => await mapping(ResultOrDefault!),
                ValueStatus.Error => ErrorOrDefault!.Failure(),
                _ => throw new UnsupportedEnumValueException(Status)
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public Result<TValue, TErrorRes> MapError<TErrorRes>(Func<TError, TErrorRes> mapping) => 
            Status switch {
                ValueStatus.Default => throw new InvalidOperationException("Cannot match on a default result."),
                ValueStatus.Value => ResultOrDefault!.Success(),
                ValueStatus.Error => mapping(ErrorOrDefault!).Failure(),
                _ => throw new UnsupportedEnumValueException(Status)
            };
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public Result<TValue, TErrorRes> FlatMapError<TErrorRes>(Func<TError, Result<TValue, TErrorRes>> mapping) => 
            Status switch {
                ValueStatus.Default => throw new InvalidOperationException("Cannot match on a default result."),
                ValueStatus.Value => ResultOrDefault!.Success(),
                ValueStatus.Error => mapping(ErrorOrDefault!),
                _ => throw new UnsupportedEnumValueException(Status)
            };

        public TValue OrElse(TValue alt) => WasSuccessful ? ResultOrDefault! : alt;
        
        public TValue OrElse(Func<TError, TValue> mapping) => Status switch {
            ValueStatus.Default => throw new InvalidOperationException("Cannot match on a default result."),
            ValueStatus.Value => ResultOrDefault!,
            ValueStatus.Error => mapping(ErrorOrDefault!),
            _ => throw new UnsupportedEnumValueException(Status)
        };
        
        public Task<TValue> OrElse(Func<TError, Task<TValue>> mapping) => 
            WasSuccessful ? ResultOrDefault!.AsCompletedTask() : mapping(ErrorOrDefault!);

        [Pure] public Option<TValue> TryGetResult() => WasSuccessful ? ResultOrDefault!.Some() : Option.None;
        
        [Pure] public Option<TError> TryGetError() => WasFailure ? ErrorOrDefault!.Some() : Option.None;
        
        [Pure] public bool TryGetValue(out TValue val) {
            val = ResultOrDefault!;
            return WasSuccessful;
        }
        
        [Pure] public bool TryGetError(out TError err) {
            err = ErrorOrDefault!;
            return WasFailure;
        }
        
        [Pure] public bool TryGetValue(out TValue val, out TError err) {
            if (Status == ValueStatus.Default) 
                throw new InvalidOperationException("Result is default and neither has a value nor an error.");
            val = ResultOrDefault!;
            err = ErrorOrDefault!;
            return WasSuccessful;
        }

        [Pure] public bool TryGetError(out TError err, out TValue val) {
            if (Status == ValueStatus.Default) 
                throw new InvalidOperationException("Result is default and neither has a value nor an error.");
            val = ResultOrDefault!;
            err = ErrorOrDefault!;
            return WasFailure;
        }

#endregion
    }

    public static class ResultExtensions {
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfFailure<TValue, TError>(this Result<TValue, TError> result) => result.ResultOrThrow();
        
        [Pure] public static Option<Result<TValue, TError>> Flip<TValue, TError>(this Result<Option<TValue>, TError> result) =>
            result.Match(
                onSuccess: option => option is {HasValue: true, ValueOrDefault: {} value} 
                    ? value.Success<TValue, TError>().Some() 
                    : Option.None,
                onFailure: err => Option.Some(err.Failure<TValue, TError>())
            );
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure] public static Result<IReadOnlyList<TValue>, TError> Flip<TValue, TError>(
            this IEnumerable<Result<TValue, TError>> enumerable
        ) {
            // Allocate with proper capacity if we know it. Optimistically assume no errors.
            List<TValue> res = enumerable is ICollection collection ? new(collection.Count) : new();
            foreach (var curr in enumerable) {
                if (curr.TryGetError(out var err, out var result)) return err.Failure();
                res.Add(result);
            }
            return res.Success<IReadOnlyList<TValue>>();
        }

        [Pure] public static Result<TValue, TError> OrError<TValue, TError>(
            this Option<TValue> opt, 
            Func<TError> errorGetter
        ) => opt is {HasValue: true, ValueOrDefault: { } value} 
            ? value.Success() 
            : errorGetter().Failure();
        
        [Pure] public static Result<TValue, TError> OrResult<TValue, TError>(
            this Option<TError> opt, Func<TValue> resultGetter
        ) => opt is {HasValue: true, ValueOrDefault: { } value} 
            ? value.Failure() 
            : resultGetter().Success();
    }
}