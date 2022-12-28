using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;

namespace BII.WasaBii.Splines {
    public readonly struct SplineSample<TPos, TDiff>
        where TPos : unmanaged 
        where TDiff : unmanaged {
        
        public readonly SplineSegment<TPos, TDiff> Segment;
        
        /// The percentage of the sample withing the segment.
        public readonly double T;

        public TPos Position => Segment.Polynomial.Evaluate(T);

        /// <summary>
        /// The first derivative, which is the direction of the spline at the queried point.
        /// This is also the velocity when traversing the spline at a constant rate.
        /// </summary>
        public TDiff Tangent => Segment.Polynomial.EvaluateDerivative(T);

        /// <summary>
        /// The second derivative, which is the direction into which the spline bends at the
        /// queried point. This is also the acceleration when traversing the spline at a constant rate.
        /// </summary>
        public TDiff Curvature => Segment.Polynomial.EvaluateSecondDerivative(T);
        
        public TDiff NthDerivative(int n) => Segment.Polynomial.EvaluateNthDerivative(T, n);

        public (TPos Position, TDiff Tangent) PositionAndTangent => (Position, Tangent);

        public SplineSample(SplineSegment<TPos, TDiff> segment, double t) {
            Segment = segment;
            T = t;
        }

        [Pure]
        public static Option<SplineSample<TPos, TDiff>> From(Spline<TPos, TDiff> spline, SplineLocation location) =>
            From(spline, spline.Normalize(location));

        [Pure]
        public static Option<SplineSample<TPos, TDiff>> From(Spline<TPos, TDiff> spline, NormalizedSplineLocation location) {
            var (segmentIndex, t) = location.AsSegmentIndex();
            if (t.IsNearly(0) && segmentIndex.Value == spline.SegmentCount) {
                segmentIndex -= 1;
                t = 1;
            } else if (segmentIndex.Value >= spline.SegmentCount) return Option.None;
            
            var segment = spline[segmentIndex];
            return new SplineSample<TPos, TDiff>(segment, t);
        }
        
    }

}