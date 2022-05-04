using BII.WasaBii.Splines.Logic;

namespace BII.WasaBii.Splines {
    public readonly struct SplineSample<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        public readonly SplineSegment<TPos, TDiff> Segment;
        public readonly double T;

        public SplineSample(SplineSegment<TPos, TDiff> segment, double t) {
            Segment = segment;
            T = t;
        }

        public SplineSample(CubicPolynomial<TPos, TDiff> polynomial, double t) {
            Segment = new SplineSegment<TPos, TDiff>(polynomial, cachedLength: null);
            T = t;
        }

        public static SplineSample<TPos, TDiff>? From(Spline<TPos, TDiff> spline, SplineLocation location) =>
            From(spline, spline.NormalizedLocation(location));

        public static SplineSample<TPos, TDiff>? From(Spline<TPos, TDiff> spline, NormalizedSplineLocation location) {
            var segmentData = CatmullRomSegment.CatmullRomSegmentAt(spline, location);
            if (segmentData.HasValue)
                return new SplineSample<TPos, TDiff>(
                    CubicPolynomial.FromCatmullRomSegment(segmentData.Value.Segment, alpha: spline.Type.ToAlpha()),
                    segmentData.Value.NormalizedOvershoot
                );
            return null;
        }
        
        public TPos Position => Segment.Polynomial.Evaluate((float)T);

        public TDiff Tangent => Segment.Polynomial.EvaluateDerivative((float)T);

        public TDiff Curvature => Segment.Polynomial.EvaluateSecondDerivative((float)T);

        public (TPos Position, TDiff Tangent) PositionAndTangent => (Position, Tangent);
    }

}