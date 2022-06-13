using System.Diagnostics.Contracts;
using BII.WasaBii.Splines.Logic;

namespace BII.WasaBii.Splines {
    public readonly struct SplineSample<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        
        public readonly SplineSegment<TPos, TDiff> Segment;
        
        /// The percentage of the sample withing the segment.
        public readonly double T;

        public TPos Position => Segment.Polynomial.Evaluate(T);

        public TDiff Tangent => Segment.Polynomial.EvaluateDerivative(T);

        public TDiff Curvature => Segment.Polynomial.EvaluateSecondDerivative(T);

        public (TPos Position, TDiff Tangent) PositionAndTangent => (Position, Tangent);

        public SplineSample(SplineSegment<TPos, TDiff> segment, double t) {
            Segment = segment;
            T = t;
        }

        private SplineSample(CubicPolynomial<TPos, TDiff> polynomial, double t) {
            Segment = new SplineSegment<TPos, TDiff>(polynomial);
            T = t;
        }

        [Pure]
        public static SplineSample<TPos, TDiff>? From(Spline<TPos, TDiff> spline, SplineLocation location) =>
            From(spline, spline.Normalize(location));

        [Pure]
        public static SplineSample<TPos, TDiff>? From(Spline<TPos, TDiff> spline, NormalizedSplineLocation location) {
            var segmentData = CatmullRomSegment.CatmullRomSegmentAt(spline, location);
            if (segmentData.HasValue)
                return new SplineSample<TPos, TDiff>(
                    CubicPolynomial.FromCatmullRomSegment(segmentData.Value.Segment, alpha: spline.Type.ToAlpha()),
                    segmentData.Value.NormalizedOvershoot
                );
            return null;
        }
        
    }

}