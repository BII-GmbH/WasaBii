using System;
using System.Threading;

namespace BII.WasaBii.Extra
{
    public interface OperationStepContext
    {
        Action<double> ReportProgressInStep { get; }
        CancellationToken? CancellationToken { get; }
    }

    public readonly struct OperationStepContext<T> : OperationStepContext
    {
        public readonly T PreviousResult;
        private readonly OperationStepContext backingContext;
        
        internal OperationStepContext(T previousResult, OperationStepContext backingContext) {
            PreviousResult = previousResult;
            this.backingContext = backingContext;
        }

        public Action<double> ReportProgressInStep => backingContext.ReportProgressInStep;
        public CancellationToken? CancellationToken => backingContext.CancellationToken;
    }

    public record OperationContext(
        Action<string> OnStepStarted,
        Action<int> OnStepCompleted,
        Action<int> OnNewStepCount,
        Action<double> ReportProgressInStep, 
        CancellationToken? CancellationToken
    ) : OperationStepContext {
        internal OperationContext<T> withStartValue<T>(T startValue) => new(
            startValue,
            OnStepStarted,
            OnStepCompleted,
            OnNewStepCount,
            ReportProgressInStep,
            CancellationToken
        );

        internal OperationContext withStepOffset(int stepsToAdd) =>
            this with {OnStepCompleted = steps => OnStepCompleted(steps + stepsToAdd)};
        
        internal OperationContext withStepCountOffset(int stepsToAdd) =>
            this with {OnNewStepCount = steps => OnNewStepCount(steps + stepsToAdd)};
    }

    public record OperationContext<T>(
        T StartValue,
        Action<string> OnStepStarted,
        Action<int> OnStepCompleted,
        Action<int> OnNewStepCount,
        Action<double> ReportProgressInStep, 
        CancellationToken? CancellationToken
    ) : OperationContext(OnStepStarted, OnStepCompleted, OnNewStepCount, ReportProgressInStep, CancellationToken) {
        internal OperationContext withoutStartValue() => new(
            OnStepStarted,
            OnStepCompleted,
            OnNewStepCount,
            ReportProgressInStep,
            CancellationToken
        );
        
        internal new OperationContext<T> withStepOffset(int stepsToAdd) =>
            this with {OnStepCompleted = steps => OnStepCompleted(steps + stepsToAdd)};
        
        internal new OperationContext<T> withStepCountOffset(int stepsToAdd) =>
            this with {OnNewStepCount = steps => OnNewStepCount(steps + stepsToAdd)};
    }
}