#nullable enable
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue, TError> If<TValue, TError>(
            bool predicate, TValue thenValue, Func<TError> onError
        ) => predicate ? Success(thenValue) : Failure(onError());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue, TError> If<TValue, TError>(
            bool predicate, Func<TValue> then, TError errorValue
        ) => predicate ? Success(then()) : Failure(errorValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<TValue, TError> If<TValue, TError>(
            bool predicate, TValue thenValue, TError errorValue
        ) => predicate ? Success(thenValue) : Failure(errorValue);

        public static Result<TValue, TError> IfNotNull<TValue, TError>(
            TValue? value, Func<TError> whenNull
        ) where TValue : struct => If(value.HasValue, () => value!.Value, whenNull);

        public static Result<TValue, TError> IfNotNull<TValue, TError>(
            TValue? value, Func<TError> whenNull
        ) where TValue : class => If(value != null, () => value!, whenNull);
        
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
        
#region Additional Methods

        // These are members and not extensions in order to avoid potential problems
        //  with extension method resolution and reduce the number of required generics in some cases.
        // There is really no need for these to be extension methods.

#endregion
    }

    public static class ResultExtensions {
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfFailure<TValue, TError>(this Result<TValue, TError> result) => result.ResultOrThrow();
        
        [Pure] public static Result<TValue, TError> ToResult<TValue, TError>(this Option<TValue> opt, Func<TError> whenNoValue) =>
            opt.Match(onHasValue: Result.Success<TValue, TError>, onNone: () => Result.Failure<TValue, TError>(whenNoValue()));
        
        [Pure] public static Result<TValue, TError> OrError<TValue, TError>(this Option<TValue> opt, Func<TError> errorGetter) =>
            opt.Match<Result<TValue,TError>>(onHasValue: arg => arg.Success(), onNone: () => Result.Failure(errorGetter()));
        
        [Pure] public static Result<TValue, TError> AsSuccess<TValue, TError>(
            this Option<TValue> opt, Func<TError> failureIfNotPresent
        ) => opt.Match(
            onHasValue: Result.Success<TValue, TError>, 
            onNone: () => Result.Failure<TValue, TError>(failureIfNotPresent())
        );
        
        [Pure] public static Result<TValue, TError> AsFailure<TValue, TError>(
            this Option<TError> opt, Func<TValue> successIfNotPresent
        ) => opt.Match(
            onHasValue: Result.Failure<TValue, TError>, 
            onNone: () => Result.Success<TValue, TError>(successIfNotPresent())
        );
    }
}