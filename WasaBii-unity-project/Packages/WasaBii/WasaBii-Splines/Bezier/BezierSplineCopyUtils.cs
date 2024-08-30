using System;
using System.Collections.Immutable;
using System.Linq;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines.Bezier {
    public static class BezierSplineCopyUtils {
    
        /// <summary>
        /// Creates a new spline with a similar trajectory as <paramref name="original"/>, but with all handle
        /// positions being moved by a certain offset which depends on the spline's velocity at these points.
        /// </summary>
        public static BezierSpline<TPos, TDiff, TTime, TVel> CopyWithOffset<TPos, TDiff, TTime, TVel>(
            BezierSpline<TPos, TDiff, TTime, TVel> original, Func<TVel, TDiff> tangentToOffset
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged => new(
            original.Segments.Select(s => {
                var ops = original.Ops;
                var startVelocity = s.ToPolynomial(ops).EvaluateDerivative(ops.ZeroTime);
                var endVelocity = s.ToPolynomial(ops).EvaluateDerivative(s.Duration);
                var startOffset = tangentToOffset(startVelocity);
                var endOffset = tangentToOffset(endVelocity);
                var newStart = original.Ops.Add(s.Start, startOffset);
                var newEnd = original.Ops.Add(s.End, endOffset);
                return s.Degree == 2 // Quadratic segment might lose tangent continuity if we don't make it cubic
                    ? BezierSegment.CubicWithVelocity(newStart, startVelocity, newEnd, endVelocity, s.Duration, ops) 
                    : new BezierSegment<TPos, TDiff, TTime, TVel>(
                        s.Duration,
                        newStart,
                        s.Handles.Select((h, i) => ops.Add(h, ops.Lerp(startOffset, endOffset, i / (s.Handles.Length - 1.0)))).ToImmutableArray(),
                        newEnd
                    );
            }),
            original.Ops
        );

        /// <summary>
        /// Creates a new spline with the same trajectory as
        /// <paramref name="original"/>, but with all handle positions
        /// being moved along a certain <paramref name="offset"/>,
        /// independent of the spline's tangent at these points.
        /// </summary>
        public static BezierSpline<TPos, TDiff, TTime, TVel> CopyWithStaticOffset<TPos, TDiff, TTime, TVel>(
            BezierSpline<TPos, TDiff, TTime, TVel> original, TDiff offset
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            TPos computePosition(TPos node) => original.Ops.Add(node, offset);
            return new BezierSpline<TPos, TDiff, TTime, TVel>(
                original.Segments.Select(s => s.Map(computePosition)),
                original.Ops
            );
        }

        /// <summary>
        /// Creates a new spline with a similar trajectory as <paramref name="original"/>, but
        /// with a uniform spacing of <paramref name="desiredHandleDistance"/> between the handles.
        /// </summary>
        /// <remarks>The velocities at the new handles are preserved, the accelerations are not.</remarks>
        public static BezierSpline<TPos, TDiff, TTime, TVel> CopyWithDifferentHandleDistance<TPos, TDiff, TTime, TVel>(
            BezierSpline<TPos, TDiff, TTime, TVel> original, 
            Length desiredHandleDistance
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged => BezierSpline.FromHandlesWithVelocities(
            original.SampleSplineEvery(desiredHandleDistance)
                .Select(sample => (sample.Position, sample.Velocity, sample.GlobalT)),
            original.Ops
        );
        
    }
}