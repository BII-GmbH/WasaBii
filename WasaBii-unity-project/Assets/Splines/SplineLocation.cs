using System;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {
    
    /// A location on a spline, represented as the distance away from the beginning along the spline (and not euclidean distance)
    [Serializable]
    [MustBeSerializable]
    public readonly struct SplineLocation : IEquatable<SplineLocation>, IComparable<SplineLocation> {
        public static readonly SplineLocation Zero = new(Length.Zero);

        public Length Value { get; }

        public static SplineLocation From(double value) => new(value.Meters());
        public static SplineLocation From(Length value) => new(value);
        public SplineLocation(Length value) => Value = value;

        [Pure]
        public Length GetDistanceToClosestSideOf<TPos, TDiff>(Spline<TPos, TDiff> spline, Length? cachedLength = null) 
            where TPos : struct where TDiff : struct {
            var length = cachedLength ?? spline.Length();
            var distanceFromEnd = length - Value;
            return Units.Min(Value, distanceFromEnd);
        }

        [Pure]
        public bool IsCloserToBeginOf<TPos, TDiff>(Spline<TPos, TDiff> spline, Length? cachedLength = null) 
            where TPos : struct where TDiff : struct {
            var length = cachedLength ?? spline.Length();
            return (Value < length / 2f);
        }

        public static implicit operator Length(SplineLocation l) => l.Value;
        public static implicit operator double(SplineLocation l) => l.Value.AsMeters();
        public static implicit operator SplineLocation(Length l) => From(l);
        public static explicit operator SplineLocation(double l) => From(l);

        public static SplineLocation Lerp(SplineLocation from, SplineLocation to, double progress)
            => From(Mathd.Lerp(from, to, progress));

        public static SplineLocation operator +(SplineLocation l) => l;
        public static SplineLocation operator -(SplineLocation l) => From(-l.Value);

        public static SplineLocation operator +(SplineLocation lhs, SplineLocation rhs) =>
            new(lhs.Value + rhs.Value);

        public static SplineLocation operator +(SplineLocation lhs, float rhs) =>
            new(lhs.Value + rhs.Meters());

        public static SplineLocation operator +(float lhs, SplineLocation rhs) =>
            rhs + lhs;

        public static SplineLocation operator -(SplineLocation lhs, SplineLocation rhs) =>
            new(lhs.Value - rhs.Value);

        public static SplineLocation operator -(SplineLocation lhs, float rhs) =>
            new(lhs.Value - rhs.Meters());

        public static SplineLocation operator -(float lhs, SplineLocation rhs) =>
            new(lhs.Meters() - rhs.Value);

        public static bool operator <(SplineLocation a, SplineLocation b) => a.Value < b.Value;
        public static bool operator >(SplineLocation a, SplineLocation b) => a.Value > b.Value;

        public static bool operator <=(SplineLocation a, SplineLocation b) => a.Value <= b.Value;
        public static bool operator >=(SplineLocation a, SplineLocation b) => a.Value >= b.Value;
        public static bool operator ==(SplineLocation a, SplineLocation b) => a.Value == b.Value;
        public static bool operator !=(SplineLocation a, SplineLocation b) => a.Value != b.Value;

        // Spline Location + Length operators
        public static SplineLocation operator +(SplineLocation lhs, Length rhs) =>
            new(lhs.Value + rhs);

        public static SplineLocation operator +(Length lhs, SplineLocation rhs) =>
            new(lhs + rhs.Value);

        public static SplineLocation operator -(SplineLocation lhs, Length rhs) =>
            new(lhs.Value - rhs);

        public static SplineLocation operator -(Length lhs, SplineLocation rhs) =>
            new(lhs - rhs.Value);

        public static bool operator <(SplineLocation a, Length b) => a.Value < b;
        public static bool operator >(SplineLocation a, Length b) => a.Value > b;
        public static bool operator <(Length a, SplineLocation b) => a < b.Value;
        public static bool operator >(Length a, SplineLocation b) => a > b.Value;
        public static bool operator <=(Length a, SplineLocation b) => a <= b.Value;
        public static bool operator >=(Length a, SplineLocation b) => a >= b.Value;
        public static bool operator <=(SplineLocation a, Length b) => a.Value <= b;
        public static bool operator >=(SplineLocation a, Length b) => a.Value >= b;

        public static SplineLocation operator %(SplineLocation lhs, Length rhs) =>
            From(lhs % rhs.AsMeters());
        
        [Pure] public static SplineLocation Lerp(SplineLocation from, SplineLocation to, float progress) =>
            From(Units.Lerp(from.Value, to.Value, progress));

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
        public static NormalizedSplineLocation From(double value) => new(value);
        public NormalizedSplineLocation(double value) => Value = value;

        public static implicit operator double(NormalizedSplineLocation l) => l.Value;
        public static explicit operator NormalizedSplineLocation(double l) => new(l);

        public static NormalizedSplineLocation operator +(NormalizedSplineLocation l) => l;

        public static NormalizedSplineLocation operator -(NormalizedSplineLocation l) =>
            From(-l.Value);

        public static NormalizedSplineLocation operator +(NormalizedSplineLocation lhs, NormalizedSplineLocation rhs) =>
            new(lhs.Value + rhs.Value);

        public static NormalizedSplineLocation operator +(NormalizedSplineLocation lhs, double rhs) =>
            new(lhs.Value + rhs);

        public static NormalizedSplineLocation operator +(double lhs, NormalizedSplineLocation rhs) =>
            new(lhs + rhs.Value);

        public static NormalizedSplineLocation operator -(NormalizedSplineLocation lhs, NormalizedSplineLocation rhs) =>
            new(lhs.Value - rhs.Value);

        public static NormalizedSplineLocation operator -(NormalizedSplineLocation lhs, double rhs) =>
            new(lhs.Value - rhs);

        public static NormalizedSplineLocation operator -(double lhs, NormalizedSplineLocation rhs) =>
            new(lhs - rhs.Value);

        public static NormalizedSplineLocation operator *(double factor, NormalizedSplineLocation l) =>
            new(factor * l.Value);

        public static bool operator <(NormalizedSplineLocation a, NormalizedSplineLocation b) => a.Value < b.Value;
        public static bool operator >(NormalizedSplineLocation a, NormalizedSplineLocation b) => a.Value > b.Value;
        public static bool operator <=(NormalizedSplineLocation a, NormalizedSplineLocation b) => a.Value <= b.Value;
        public static bool operator >=(NormalizedSplineLocation a, NormalizedSplineLocation b) => a.Value >= b.Value;
        public static bool operator ==(NormalizedSplineLocation a, NormalizedSplineLocation b) => a.Value == b.Value;
        public static bool operator !=(NormalizedSplineLocation a, NormalizedSplineLocation b) => a.Value != b.Value;
        
        [Pure] public static NormalizedSplineLocation Lerp(NormalizedSplineLocation a, NormalizedSplineLocation b, double t) =>
            new(a.Value + t * (b.Value - a.Value));
        
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