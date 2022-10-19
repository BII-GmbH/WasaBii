using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
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
        public readonly NormalizedSplineLocation StartLocationNormalized;
        public readonly NormalizedSplineLocation EndLocationNormalized;
        public readonly Length Length;

        public PartialSpline(Spline<TPos, TDiff> spline, SplineLocation startLocation, SplineLocation endLocation) {
            Spline = spline;
            StartLocation = startLocation;
            EndLocation = endLocation;
            StartLocationNormalized = spline.Normalize(startLocation);
            EndLocationNormalized = spline.Normalize(endLocation);
            Length = endLocation - startLocation;
            if(StartLocation > EndLocation) throw new ArgumentException($"StartLocation ({StartLocation}) must be before EndLocation ({EndLocation})");
            if(Length < Length.Zero) throw new ArgumentException($"PartialSpline must have a positive length (was {Length})");
        }

        public SplineSample<TPos, TDiff> SampleAt(double percentage) => Spline[NormalizedSplineLocation.Lerp(StartLocationNormalized, EndLocationNormalized, percentage)];

        public SplineSample<TPos, TDiff> SampleFromStart(Length distanceFromStart) {
            Contract.Assert(
                distanceFromStart >= -Length.Epsilon, 
                $"Distance must be above 0, but was {distanceFromStart}"
            );
            Contract.Assert(
                distanceFromStart <= Length + Length.Epsilon, 
                $"Distance must be below the length of {Length}, but was {distanceFromStart}"
            );
            return Spline[distanceFromStart + StartLocation];
        }

        public SplineSample<TPos, TDiff> SampleFromEnd(Length distanceFromEnd) {
            Contract.Assert(
                distanceFromEnd >= -Length.Epsilon, 
                $"Distance must be above 0, but was {distanceFromEnd}"
            );
            Contract.Assert(
                distanceFromEnd <= Length + Length.Epsilon, 
                $"Distance must be below the length of {Length}, but was {distanceFromEnd}"
            );
            return Spline[EndLocation - distanceFromEnd];
        }

        public SplineSample<TPos, TDiff> SampleFrom(SampleDirection direction, Length distance) => direction switch {
            SampleDirection.FromStart => SampleFromStart(distance),
            SampleDirection.FromEnd => SampleFromEnd(distance),
            _ => throw new InvalidEnumArgumentException(nameof(direction), (int) direction, typeof(SampleDirection))
        };
    }
}