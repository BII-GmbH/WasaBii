#nullable enable

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
        Action<string> OnStepStarted, // Label of the started step
        Action<int> OnStepCompleted,  // Index of step completed, starts at 1
        Action<int> OnStepCountDiff,  // Add value to total step count to get the updated value
        Action<double> ReportProgressInStep, 
        CancellationToken? CancellationToken,
        int StepOffset = 0
    ) : OperationStepContext {
        internal OperationContext<T> withStartValue<T>(T startValue) => new(
            startValue,
            OnStepStarted,
            OnStepCompleted,
            OnStepCountDiff,
            ReportProgressInStep,
            CancellationToken
        );
    }

    public record OperationContext<T>(
        T StartValue,
        Action<string> OnStepStarted, // Label of the started step
        Action<int> OnStepCompleted,  // Index of step completed, starts at 1
        Action<int> OnStepCountDiff,  // Add value to total step count to get the updated value
        Action<double> ReportProgressInStep,
        CancellationToken? CancellationToken,
        int StepOffset = 0
    ) : OperationContext(
        OnStepStarted, 
        OnStepCompleted, 
        OnStepCountDiff, 
        ReportProgressInStep, 
        CancellationToken, 
        StepOffset
    );
}