using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {

    public enum SampleDirection {
        FromStart,
        FromEnd
    }
    
    /// A subset of the <see cref="Spline"/> in the interval from
    /// <see cref="StartLocation"/> to <see cref="EndLocation"/>.
    [MustBeImmutable][MustBeSerializable]
    public readonly struct PartialSpline<TPos, TDiff> where TPos : struct where TDiff : struct {
        public readonly Spline<TPos, TDiff> Spline;
        public readonly SplineLocation StartLocation;
        public readonly SplineLocation EndLocation;
        public readonly Length Length;

        public PartialSpline(Spline<TPos, TDiff> spline, SplineLocation startLocation, SplineLocation endLocation) {
            Spline = spline;
            StartLocation = startLocation;
            EndLocation = endLocation;
            Length = (endLocation - startLocation).DistanceFromBegin;
        }

        public SplineSample<TPos, TDiff> SampleAt(float percentage) => Spline[percentage * Length + StartLocation];

        public SplineSample<TPos, TDiff> SampleFromStart(Length distanceFromStart) {
            Contract.Assert(
                distanceFromStart >= Length.Zero, 
                $"Distance must be above 0, but was {distanceFromStart}"
            );
            Contract.Assert(
                distanceFromStart <= Length,
                $"Distance must be below the length of {Length}, but was {distanceFromStart}"
            );
            return Spline[distanceFromStart + StartLocation];
        }

        public SplineSample<TPos, TDiff> SampleFromEnd(Length distanceFromEnd) {
            Contract.Assert(
                distanceFromEnd >= Length.Zero, 
                $"Distance must be above 0, but was {distanceFromEnd}"
            );
            Contract.Assert(
                distanceFromEnd <= Length,
                $"Distance must be below the length of {Length}, but was {distanceFromEnd}"
            );
            return Spline[EndLocation - distanceFromEnd];
        }

        public SplineSample<TPos, TDiff> SampleFrom(SampleDirection direction, Length distance) =>
            direction == SampleDirection.FromStart ? SampleFromStart(distance) : SampleFromEnd(distance);
    }
}