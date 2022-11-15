using System;
using BII.WasaBii.Core;

namespace BII.WasaBii.Splines {
    
    [Serializable]
    public readonly struct SplineInterval {

        public SplineLocation Start { get; }
        public SplineLocation End { get; }

        public SplineLocation Center => new((Start.Value + End.Value) / 2);

        public SplineInterval(SplineLocation start, SplineLocation end) {
            this.Start = MathExtensions.Min(start,end);
            this.End = MathExtensions.Max(start, end);
        }

        public static implicit operator SplineInterval((SplineLocation start, SplineLocation end) tuple) =>
            new(tuple.start, tuple.end);

        public void Deconstruct(out SplineLocation start, out SplineLocation end) {
            start = this.Start;
            end = this.End;
        }
        
        public bool Equals(SplineInterval other) => Start.Equals(other.Start) && End.Equals(other.End);
        public override bool Equals(object obj) => obj is SplineInterval other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Start, End);
    }
}