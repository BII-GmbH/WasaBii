using System;

namespace BII.WasaBii.Splines {
    /// <summary>
    /// A typed wrapper around an int for indexing the handles of a spline.
    /// Handles are the points that describe a spline and its trajectory.
    /// In our case, all handles (except the margin ones) will be interpolated.
    /// </summary>
    [Serializable]
    public readonly struct SplineHandleIndex : IEquatable<SplineHandleIndex> {
        public static readonly SplineHandleIndex Zero = At(0);

        public int Value { get; }
        public static SplineHandleIndex At(int value) => new SplineHandleIndex(value);
        public SplineHandleIndex(int value) => Value = value;

        public static implicit operator int(SplineHandleIndex l) => l.Value;
        public static explicit operator SplineHandleIndex(int l) => new SplineHandleIndex(l);

        public static SplineHandleIndex operator +(SplineHandleIndex l) => l;

        public static SplineHandleIndex operator -(SplineHandleIndex l) =>
            At(-l.Value);

        public static SplineHandleIndex operator +(SplineHandleIndex lhs, SplineHandleIndex rhs) =>
            new SplineHandleIndex(lhs.Value + rhs.Value);

        public static SplineHandleIndex operator +(SplineHandleIndex lhs, int rhs) =>
            new SplineHandleIndex(lhs.Value + rhs);

        public static SplineHandleIndex operator +(int lhs, SplineHandleIndex rhs) =>
            new SplineHandleIndex(lhs + rhs.Value);

        public static SplineHandleIndex operator -(SplineHandleIndex lhs, SplineHandleIndex rhs) =>
            new SplineHandleIndex(lhs.Value - rhs.Value);

        public static SplineHandleIndex operator -(SplineHandleIndex lhs, int rhs) =>
            new SplineHandleIndex(lhs.Value - rhs);

        public static SplineHandleIndex operator -(int lhs, SplineHandleIndex rhs) =>
            new SplineHandleIndex(lhs - rhs.Value);

        public static bool operator <(SplineHandleIndex a, SplineHandleIndex b) => a.Value < b.Value;
        public static bool operator >(SplineHandleIndex a, SplineHandleIndex b) => a.Value > b.Value;
        public static bool operator <=(SplineHandleIndex a, SplineHandleIndex b) => a.Value <= b.Value;
        public static bool operator >=(SplineHandleIndex a, SplineHandleIndex b) => a.Value >= b.Value;
        public static bool operator ==(SplineHandleIndex a, SplineHandleIndex b) => a.Value == b.Value;
        public static bool operator !=(SplineHandleIndex a, SplineHandleIndex b) => a.Value != b.Value;

        public override string ToString() => $"Spline HandleIndex: {Value}";

        public bool Equals(SplineHandleIndex other) => Value.Equals(other.Value);

        public override bool Equals(object obj) => obj is SplineHandleIndex other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();
    }
    
    /// <summary>
    /// A typed wrapper around an int for indexing a segment of a spline.
    /// A segment is the curve of a spline between two handles.
    /// </summary>
    [Serializable]
    public readonly struct SplineSegmentIndex : IEquatable<SplineSegmentIndex> {
        public static readonly SplineSegmentIndex Zero = At(0);

        public int Value { get; }
        public static SplineSegmentIndex At(int value) => new SplineSegmentIndex(value);
        public SplineSegmentIndex(int value) => Value = value;

        public static implicit operator int(SplineSegmentIndex l) => l.Value;
        public static explicit operator SplineSegmentIndex(int l) => new SplineSegmentIndex(l);

        public static SplineSegmentIndex operator +(SplineSegmentIndex l) => l;

        public static SplineSegmentIndex operator -(SplineSegmentIndex l) =>
            At(-l.Value);

        public static SplineSegmentIndex operator +(SplineSegmentIndex lhs, SplineSegmentIndex rhs) =>
            new SplineSegmentIndex(lhs.Value + rhs.Value);

        public static SplineSegmentIndex operator +(SplineSegmentIndex lhs, int rhs) =>
            new SplineSegmentIndex(lhs.Value + rhs);

        public static SplineSegmentIndex operator +(int lhs, SplineSegmentIndex rhs) =>
            new SplineSegmentIndex(lhs + rhs.Value);

        public static SplineSegmentIndex operator -(SplineSegmentIndex lhs, SplineSegmentIndex rhs) =>
            new SplineSegmentIndex(lhs.Value - rhs.Value);

        public static SplineSegmentIndex operator -(SplineSegmentIndex lhs, int rhs) =>
            new SplineSegmentIndex(lhs.Value - rhs);

        public static SplineSegmentIndex operator -(int lhs, SplineSegmentIndex rhs) =>
            new SplineSegmentIndex(lhs - rhs.Value);

        public static bool operator <(SplineSegmentIndex a, SplineSegmentIndex b) => a.Value < b.Value;
        public static bool operator >(SplineSegmentIndex a, SplineSegmentIndex b) => a.Value > b.Value;
        public static bool operator <=(SplineSegmentIndex a, SplineSegmentIndex b) => a.Value <= b.Value;
        public static bool operator >=(SplineSegmentIndex a, SplineSegmentIndex b) => a.Value >= b.Value;
        public static bool operator ==(SplineSegmentIndex a, SplineSegmentIndex b) => a.Value == b.Value;
        public static bool operator !=(SplineSegmentIndex a, SplineSegmentIndex b) => a.Value != b.Value;

        public (SplineHandleIndex S0, SplineHandleIndex S1, SplineHandleIndex S2, SplineHandleIndex S3) HandleIndices => (
            SplineHandleIndex.At(Value), 
            SplineHandleIndex.At(Value + 1), 
            SplineHandleIndex.At(Value + 2),
            SplineHandleIndex.At(Value + 3)
        );
        
        public override string ToString() => $"Spline SegmentIndex: {Value}";

        public bool Equals(SplineSegmentIndex other) => Value.Equals(other.Value);

        public override bool Equals(object obj) => obj is SplineSegmentIndex other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();
    }
}