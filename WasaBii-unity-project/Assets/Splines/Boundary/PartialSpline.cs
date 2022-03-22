using System.Diagnostics.Contracts;
using BII.Units;
using BII.Utilities.Independent;

namespace BII.CatmullRomSplines {

    public enum SampleDirection {
        FromStart,
        FromEnd
    }
    
    /// A subset of the <see cref="Spline"/> in the interval from
    /// <see cref="StartLocation"/> to <see cref="EndLocation"/>.
    [MustBeImmutable][MustBeSerializable]
    public readonly struct PartialSpline {
        public readonly Spline Spline;
        public readonly SplineLocation StartLocation;
        public readonly SplineLocation EndLocation;
        public readonly Length Length;

        public PartialSpline(Spline spline, SplineLocation startLocation, SplineLocation endLocation) {
            Spline = spline;
            StartLocation = startLocation;
            EndLocation = endLocation;
            Length = (endLocation - startLocation).DistanceFromBegin;
        }

        public SplineSample SampleAt(float percentage) => Spline[percentage * Length + StartLocation];

        public SplineSample SampleFromStart(Length distanceFromStart) {
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

        public SplineSample SampleFromEnd(Length distanceFromEnd) {
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

        public SplineSample SampleFrom(SampleDirection direction, Length distance) =>
            direction == SampleDirection.FromStart ? SampleFromStart(distance) : SampleFromEnd(distance);
    }
}