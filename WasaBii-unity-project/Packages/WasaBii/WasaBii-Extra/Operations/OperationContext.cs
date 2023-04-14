using System;
using System.Threading;

namespace BII.WasaBii.Extra
{
    public interface OperationStepContext
    {
        Action<double> ReportProgressInStep { get; init; }
        CancellationToken? CancellationToken { get; init; }
    }

    public record OperationContext(
        Action<string> OnStepStarted,
        Action<int> OnStepCompleted,
        Action<int> OnNewStepCount,
        Action<double> ReportProgressInStep, 
        CancellationToken? CancellationToken
    ) : OperationStepContext {
        public OperationContext<T> WithStartValue<T>(T startValue) => new(
            startValue,
            OnStepStarted,
            OnStepCompleted,
            OnNewStepCount,
            ReportProgressInStep,
            CancellationToken
        );

        public OperationContext WithStepOffset(int stepsToAdd) =>
            this with {OnStepCompleted = steps => OnStepCompleted(steps + stepsToAdd)};
        
        public OperationContext WithStepCountOffset(int stepsToAdd) =>
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
        public OperationContext WithoutStartValue() => new(
            OnStepStarted,
            OnStepCompleted,
            OnNewStepCount,
            ReportProgressInStep,
            CancellationToken
        );
        
        public new OperationContext<T> WithStepOffset(int stepsToAdd) =>
            this with {OnStepCompleted = steps => OnStepCompleted(steps + stepsToAdd)};
        
        public new OperationContext<T> WithStepCountOffset(int stepsToAdd) =>
            this with {OnNewStepCount = steps => OnNewStepCount(steps + stepsToAdd)};
    }
}