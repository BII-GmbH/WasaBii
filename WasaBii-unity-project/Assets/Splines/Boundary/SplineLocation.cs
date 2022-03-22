using System;
using System.Diagnostics.Contracts;
using BII.Units;
using BII.Utilities.Independent;
using BII.Utilities.Independent.Maths;
using BII.WasaBii.Core;
using BII.WasaBii.Units;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.CatmullRomSplines {
    /// A location on a spline, measured in meters away from the beginning along the spline (and not flight distance)
    [Serializable]
    [MustBeSerializable]
    public struct SplineLocation : IEquatable<SplineLocation>, IComparable<SplineLocation> {
        public static readonly SplineLocation Zero = new SplineLocation(0);

        // Should be readonly but isn't because that breaks unity serialization. Do not mutate except in constructor.        
        [SerializeField] private double __value;
        public double Value => __value;
        public Length DistanceFromBegin => Value.Meters();

        public static SplineLocation From(double value) => new SplineLocation(value);
        public static SplineLocation From(Length value) => new SplineLocation(value.AsMeters());
        public SplineLocation(double value) => __value = value;

        [Pure]
        public Length GetDistanceToClosestSideOf(Spline spline, Length? cachedLength = null) {
            var length = cachedLength ?? spline.Length();
            var distanceFromEnd = length - DistanceFromBegin;
            return DistanceFromBegin.Min(distanceFromEnd);
        }

        [Pure]
        public bool IsCloserToBeginOf(Spline spline, Length? cachedLength = null) {
            var length = cachedLength ?? spline.Length();
            return (DistanceFromBegin < length / 2f);
        }

        public static implicit operator double(SplineLocation l) => l.Value;
        public static implicit operator SplineLocation(Length l) => From(l.AsMeters());
        public static explicit operator SplineLocation(double l) => From(l);

        public static SplineLocation Lerp(SplineLocation from, SplineLocation to, double progress)
            => From(Mathd.Lerp(from, to, progress));

        public static SplineLocation operator +(SplineLocation l) => l;
        public static SplineLocation operator -(SplineLocation l) => From(-l.Value);

        public static SplineLocation operator +(SplineLocation lhs, SplineLocation rhs) =>
            new SplineLocation(lhs.Value + rhs.Value);

        public static SplineLocation operator +(SplineLocation lhs, float rhs) =>
            new SplineLocation(lhs.Value + rhs);

        public static SplineLocation operator +(float lhs, SplineLocation rhs) =>
            new SplineLocation(lhs + rhs.Value);

        public static SplineLocation operator -(SplineLocation lhs, SplineLocation rhs) =>
            new SplineLocation(lhs.Value - rhs.Value);

        public static SplineLocation operator -(SplineLocation lhs, float rhs) =>
            new SplineLocation(lhs.Value - rhs);

        public static SplineLocation operator -(float lhs, SplineLocation rhs) =>
            new SplineLocation(lhs - rhs.Value);

        public static bool operator <(SplineLocation a, SplineLocation b) => a.Value < b.Value;
        public static bool operator >(SplineLocation a, SplineLocation b) => a.Value > b.Value;

        public static bool operator <=(SplineLocation a, SplineLocation b) => a.Value <= b.Value;
        public static bool operator >=(SplineLocation a, SplineLocation b) => a.Value >= b.Value;
        public static bool operator ==(SplineLocation a, SplineLocation b) => a.Value == b.Value;
        public static bool operator !=(SplineLocation a, SplineLocation b) => a.Value != b.Value;

        // Spline Location + Length operators
        public static SplineLocation operator +(SplineLocation lhs, Length rhs) =>
            new SplineLocation(lhs.Value + rhs.SIValue);

        public static SplineLocation operator +(Length lhs, SplineLocation rhs) =>
            new SplineLocation(lhs.SIValue + rhs.Value);

        public static SplineLocation operator -(SplineLocation lhs, Length rhs) =>
            new SplineLocation(lhs.Value - rhs.SIValue);

        public static SplineLocation operator -(Length lhs, SplineLocation rhs) =>
            new SplineLocation(lhs.SIValue - rhs.Value);

        public static bool operator <(SplineLocation a, Length b) => a.Value < b.SIValue;
        public static bool operator >(SplineLocation a, Length b) => a.Value > b.SIValue;
        public static bool operator <(Length a, SplineLocation b) => a.SIValue < b.Value;
        public static bool operator >(Length a, SplineLocation b) => a.SIValue > b.Value;
        public static bool operator <=(Length a, SplineLocation b) => a.SIValue <= b.Value;
        public static bool operator >=(Length a, SplineLocation b) => a.SIValue >= b.Value;
        public static bool operator <=(SplineLocation a, Length b) => a.Value <= b.SIValue;
        public static bool operator >=(SplineLocation a, Length b) => a.Value >= b.SIValue;

        public static SplineLocation operator %(SplineLocation lhs, Length rhs) =>
            From(lhs.Value % rhs.AsMeters());

        public override string ToString() => $"(Absolute) Spline Location: {Value}";

        public bool Equals(SplineLocation other) => Value.Equals(other.Value);

        public int CompareTo(SplineLocation other) => Value.CompareTo(other.Value);
        public override bool Equals(object obj) => obj is SplineLocation other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();
    }

    /// Represents a location on a spline: It is equal to the index of the node plus the progress to the next
    [Serializable][MustBeSerializable]
    public readonly struct NormalizedSplineLocation : IEquatable<NormalizedSplineLocation> {
        public static readonly NormalizedSplineLocation Zero = From(0);

        public double Value { get; }
        public static NormalizedSplineLocation From(double value) => new NormalizedSplineLocation(value);
        public NormalizedSplineLocation(double value) => Value = value;

        public static implicit operator double(NormalizedSplineLocation l) => l.Value;
        public static explicit operator NormalizedSplineLocation(double l) => new NormalizedSplineLocation(l);

        public static NormalizedSplineLocation operator +(NormalizedSplineLocation l) => l;

        public static NormalizedSplineLocation operator -(NormalizedSplineLocation l) =>
            From(-l.Value);

        public static NormalizedSplineLocation operator +(NormalizedSplineLocation lhs, NormalizedSplineLocation rhs) =>
            new NormalizedSplineLocation(lhs.Value + rhs.Value);

        public static NormalizedSplineLocation operator +(NormalizedSplineLocation lhs, double rhs) =>
            new NormalizedSplineLocation(lhs.Value + rhs);

        public static NormalizedSplineLocation operator +(double lhs, NormalizedSplineLocation rhs) =>
            new NormalizedSplineLocation(lhs + rhs.Value);

        public static NormalizedSplineLocation operator -(NormalizedSplineLocation lhs, NormalizedSplineLocation rhs) =>
            new NormalizedSplineLocation(lhs.Value - rhs.Value);

        public static NormalizedSplineLocation operator -(NormalizedSplineLocation lhs, double rhs) =>
            new NormalizedSplineLocation(lhs.Value - rhs);

        public static NormalizedSplineLocation operator -(double lhs, NormalizedSplineLocation rhs) =>
            new NormalizedSplineLocation(lhs - rhs.Value);

        public static NormalizedSplineLocation operator *(double factor, NormalizedSplineLocation l) =>
            new NormalizedSplineLocation(factor * l.Value);

        public static bool operator <(NormalizedSplineLocation a, NormalizedSplineLocation b) => a.Value < b.Value;
        public static bool operator >(NormalizedSplineLocation a, NormalizedSplineLocation b) => a.Value > b.Value;
        public static bool operator <=(NormalizedSplineLocation a, NormalizedSplineLocation b) => a.Value <= b.Value;
        public static bool operator >=(NormalizedSplineLocation a, NormalizedSplineLocation b) => a.Value >= b.Value;
        public static bool operator ==(NormalizedSplineLocation a, NormalizedSplineLocation b) => a.Value == b.Value;
        public static bool operator !=(NormalizedSplineLocation a, NormalizedSplineLocation b) => a.Value != b.Value;

        public (SplineHandleIndex Index, double Overshoot) AsHandleIndex() {
            var index = SplineHandleIndex.At(Mathd.FloorToInt(Value));
            var overshoot = Value - index;
            return (index, overshoot);
        }

        public (SplineSegmentIndex Index, double Overshoot) AsSegmentIndex() {
            var (handleIdx, overshoot) = AsHandleIndex();
            return (SplineSegmentIndex.At(handleIdx), overshoot);
        }

        public override string ToString() => $"Normalized Spline Location: {Value}";

        public bool Equals(NormalizedSplineLocation other) => Value.Equals(other.Value);

        public override bool Equals(object obj) => obj is NormalizedSplineLocation other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();
    }
    
    
}