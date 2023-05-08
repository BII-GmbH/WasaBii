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
        Action<string> OnStepCompleted, // Note: It's a lot of overhead to track the accurate step number
        Action<int> OnStepCountDiff,
        Action<double> ReportProgressInStep, 
        CancellationToken? CancellationToken
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
        Action<string> OnStepStarted,
        Action<string> OnStepCompleted, // Note: It's a lot of overhead to track the accurate step number
        Action<int> OnStepCountDiff,
        Action<double> ReportProgressInStep,
        CancellationToken? CancellationToken
    ) : OperationContext(OnStepStarted, OnStepCompleted, OnStepCountDiff, ReportProgressInStep, CancellationToken);
}