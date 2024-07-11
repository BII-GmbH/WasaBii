using System;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {

    public enum SampleDirection {
        FromStart,
        FromEnd
    }
    
    /// <summary>
    /// A subset of the <see cref="Spline"/> in the interval from
    /// <see cref="StartLocation"/> to <see cref="EndLocation"/>.
    /// </summary>
    [MustBeImmutable][Serializable]
    public readonly struct PartialSpline<TPos, TDiff, TTime, TVel> where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged
    {
        public readonly Spline<TPos, TDiff, TTime, TVel> Spline;
        public readonly SplineLocation StartLocation;
        public readonly SplineLocation EndLocation;
        public readonly NormalizedSplineLocation StartLocationNormalized;
        public readonly NormalizedSplineLocation EndLocationNormalized;
        public readonly Length Length;

        public PartialSpline(Spline<TPos, TDiff, TTime, TVel> spline, SplineLocation startLocation, SplineLocation endLocation) {
            Spline = spline;
            StartLocation = startLocation;
            EndLocation = endLocation;
            StartLocationNormalized = spline.NormalizeOrThrow(startLocation);
            EndLocationNormalized = spline.NormalizeOrThrow(endLocation);
            Length = endLocation - startLocation;
            if(StartLocation > EndLocation) throw new ArgumentException($"StartLocation ({StartLocation}) must be before EndLocation ({EndLocation})");
            if(Length < Length.Zero) throw new ArgumentException($"PartialSpline must have a positive length (was {Length})");
        }

        public SplineSample<TPos, TDiff, TTime, TVel> SampleAt(double percentage) => Spline[NormalizedSplineLocation.Lerp(StartLocationNormalized, EndLocationNormalized, percentage)];

        public SplineSample<TPos, TDiff, TTime, TVel> SampleFromStart(Length distanceFromStart) {
            if(distanceFromStart < -Length.Epsilon) throw new ArgumentException(
                $"Distance must be above 0, but was {distanceFromStart}"
            );
            if(distanceFromStart > Length + Length.Epsilon) throw new ArgumentException(
                $"Distance must be below the length of {Length}, but was {distanceFromStart}"
            );
            return Spline[distanceFromStart + StartLocation];
        }

        public SplineSample<TPos, TDiff, TTime, TVel> SampleFromEnd(Length distanceFromEnd) {
            if(distanceFromEnd < -Length.Epsilon) throw new ArgumentException(
                $"Distance must be above 0, but was {distanceFromEnd}"
            );
            if(distanceFromEnd > Length + Length.Epsilon) throw new ArgumentException(
                $"Distance must be below the length of {Length}, but was {distanceFromEnd}"
            );
            return Spline[EndLocation - distanceFromEnd];
        }

        public SplineSample<TPos, TDiff, TTime, TVel> SampleFrom(SampleDirection direction, Length distance) => direction switch {
            SampleDirection.FromStart => SampleFromStart(distance),
            SampleDirection.FromEnd => SampleFromEnd(distance),
            _ => throw new UnsupportedEnumValueException(direction)
        };
    }
}