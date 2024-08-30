using System;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;

namespace BII.WasaBii.Splines {
    public readonly struct SplineSample<TPos, TDiff, TTime, TVel>
    where TPos : unmanaged 
    where TDiff : unmanaged 
    where TTime : unmanaged, IComparable<TTime>
    where TVel : unmanaged {

        public readonly SplineSegment<TPos, TDiff, TTime, TVel> Segment;

        public readonly TTime GlobalT;
        public readonly TTime LocalT;
        /// The progress of the sample withing the segment.
        public readonly double TNormalized;
        public readonly SplineSegmentIndex SegmentIndex;
        public NormalizedSplineLocation NormalizedLocation => new(SegmentIndex + TNormalized);

        public TPos Position => Segment.Polynomial.EvaluateNormalized(TNormalized);

        /// <summary>
        /// The first derivative, which is the direction of the spline at the queried point multiplied by the current speed.
        /// </summary>
        public TVel Velocity => Segment.Polynomial.Ops.Div(DerivativeInSegment, Segment.Duration);
        public TDiff DerivativeInSegment => Segment.Polynomial.EvaluateDerivativeNormalized(TNormalized);

        /// <summary>
        /// The second derivative, which is the direction into which the spline bends at the
        /// queried point multiplied by the current rate of change in speed.
        /// </summary>
        public TVel Acceleration => Segment.Polynomial.Ops.Div(SecondDerivativeInSegment, Segment.Duration);
        public TDiff SecondDerivativeInSegment => Segment.Polynomial.EvaluateSecondDerivativeNormalized(TNormalized);
        
        public TVel NthDerivative(int n) => Segment.Polynomial.Ops.Div(NthDerivativeInSegment(n), Segment.Duration);
        public TDiff NthDerivativeInSegment(int n) => Segment.Polynomial.EvaluateNthDerivativeNormalized(TNormalized, n);

        public SplineSample(SplineSegment<TPos, TDiff, TTime, TVel> segment, TTime globalT, TTime segmentOffset, SplineSegmentIndex segmentIndex) {
            Segment = segment;
            GlobalT = globalT;
            SegmentIndex = segmentIndex;
            LocalT = segment.Polynomial.Ops.Sub(globalT, segmentOffset);
            TNormalized = segment.Polynomial.Ops.Div(LocalT, Segment.Duration);
        }

        public SplineSample(SplineSegment<TPos, TDiff, TTime, TVel> segment, double tNormalized, TTime segmentOffset, SplineSegmentIndex segmentIndex) {
            Segment = segment;
            TNormalized = tNormalized;
            SegmentIndex = segmentIndex;
            LocalT = segment.Polynomial.Ops.Mul(segment.Duration, tNormalized);
            GlobalT = segment.Polynomial.Ops.Add(LocalT, segmentOffset);
        }

        [Pure]
        public static Option<SplineSample<TPos, TDiff, TTime, TVel>> From(Spline<TPos, TDiff, TTime, TVel> spline, SplineLocation location) =>
            From(spline, spline.NormalizeOrThrow(location));

        [Pure]
        public static Option<SplineSample<TPos, TDiff, TTime, TVel>> From(Spline<TPos, TDiff, TTime, TVel> spline, NormalizedSplineLocation location) {
            var (segmentIndex, t) = location.AsSegmentIndex();
            if (t.IsNearly(0) && segmentIndex.Value == spline.SegmentCount) {
                segmentIndex -= 1;
                t = 1;
            } else if (segmentIndex.Value >= spline.SegmentCount) return Option.None;
            
            var segment = spline[segmentIndex];
            var minT = spline.TemporalSegmentOffsets[segmentIndex];
            
            return new SplineSample<TPos, TDiff, TTime, TVel>(segment, t, minT, segmentIndex);
        }
        
        [Pure]
        public static SplineSample<TPos, TDiff, TTime, TVel> From(Spline<TPos, TDiff, TTime, TVel> spline, TTime time) {
            var i = spline.TemporalSegmentOffsets.BinarySearch(time);
            if (i < 0) i = ~i - 1;
            if (i >= spline.SegmentCount) i = spline.SegmentCount - 1;
            if (i < 0) i = 0;
            var minT = spline.TemporalSegmentOffsets[i];
            var segmentIndex = new SplineSegmentIndex(i);
            var segment = spline[segmentIndex];
            
            return new SplineSample<TPos, TDiff, TTime, TVel>(segment, time, minT, segmentIndex);
        }

    }

}