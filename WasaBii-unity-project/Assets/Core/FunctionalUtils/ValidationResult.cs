using System;

namespace BII.WasaBii.Core {

    public readonly struct ValidationError {
        public readonly string Message;
        public ValidationError(string msg) => Message = msg;
    }

    public static class ValidationResult {
        public static ValidationResult<T> Success<T>(T result) => ValidationResult<T>.Success(result);
        public static ValidationResult<T> Failure<T>(ValidationError error) => ValidationResult<T>.Failure(error);
        
        public static ValidationResult<TRes> Map<TValue, TRes>(
            this ValidationResult<TValue> source, Func<TValue, TRes> mapping
        ) => source.Match(res => Success(mapping(res)), Failure<TRes>);
        
        public static ValidationResult<TRes> FlatMap<TValue, TRes>(
            this ValidationResult<TValue> source, Func<TValue, ValidationResult<TRes>> mapping
        ) => source.Match(mapping, Failure<TRes>);
    }
    
    public abstract class ValidationResult<T> : Result<T, ValidationError> {
        
        public static ValidationResult<T> Success(T result) => 
            new SuccessValidationResult(result);
        
        public static ValidationResult<T> Failure(ValidationError error) => 
            new ErrorValidationResult(error);
        
        private sealed class ErrorValidationResult : ValidationResult<T> {
            private readonly ValidationError error;
            public ErrorValidationResult(ValidationError error) => this.error = error;

            public override bool WasSuccessful => false;
            protected override T resultOrThrow() => throw new InvalidOperationException("No result: " + error);
            protected override ValidationError errorOrThrow() => error;

            public override TRes Match<TRes>(
                Func<T, TRes> onSuccess, Func<ValidationError, TRes> onError
            ) => onError(error); 
        
            public override void Match(
                Action<T> onSuccess, Action<ValidationError> onError
            ) => onError(error);
        }

        private sealed class SuccessValidationResult : ValidationResult<T> {
            private readonly T result;
            public SuccessValidationResult(T result) => this.result = result;

            public override bool WasSuccessful => true;
            protected override T resultOrThrow() => result;
            protected override ValidationError errorOrThrow() => throw new InvalidOperationException("Not an error: " + result);

            public override TRes Match<TRes>(
                Func<T, TRes> onSuccess, Func<ValidationError, TRes> onError
            ) => onSuccess(result);
        
            public override void Match(
                Action<T> onSuccess, Action<ValidationError> onError
            ) => onSuccess(result);
        }
    }
}