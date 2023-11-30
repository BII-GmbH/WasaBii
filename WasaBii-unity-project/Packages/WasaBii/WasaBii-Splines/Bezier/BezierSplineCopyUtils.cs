using System;
using System.Collections.Immutable;
using System.Linq;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines.Bezier {
    public static class BezierSplineCopyUtils {
    
        /// <summary>
        /// Creates a new spline with a similar trajectory as <paramref name="original"/>, but with all handle
        /// positions being moved by a certain offset which depends on the spline's tangent at these points.
        /// </summary>
        public static BezierSpline<TPos, TDiff> CopyWithOffset<TPos, TDiff>(
            BezierSpline<TPos, TDiff> original, Func<TDiff, TDiff> tangentToOffset
        ) where TPos : unmanaged where TDiff : unmanaged => new(
            original.Segments.Select(s => {
                var ops = original.Ops;
                var startVelocity = s.StartVelocity(ops);
                var endVelocity = s.EndVelocity(ops);
                var startOffset = tangentToOffset(startVelocity);
                var endOffset = tangentToOffset(endVelocity);
                var newStart = original.Ops.Add(s.Start, startOffset);
                var newEnd = original.Ops.Add(s.End, endOffset);
                return s.Degree == 2 // Quadratic segment might lose velocity continuity if we don't make it cubic
                    ? BezierSegment.Cubic(newStart, startVelocity, newEnd, endVelocity, ops) 
                    : new BezierSegment<TPos, TDiff>(
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
        public static BezierSpline<TPos, TDiff> CopyWithStaticOffset<TPos, TDiff>(
            BezierSpline<TPos, TDiff> original, TDiff offset
        ) where TPos : unmanaged where TDiff : unmanaged {
            TPos computePosition(TPos node) => original.Ops.Add(node, offset);
            return new BezierSpline<TPos, TDiff>(
                original.Segments.Select(s => s.Map(computePosition)),
                original.Ops
            );
        }

        /// <summary>
        /// Creates a new spline with a similar trajectory as
        /// <paramref name="original"/>, but different spacing
        /// between the handles.
        /// </summary>
        public static BezierSpline<TPos, TDiff> CopyWithDifferentHandleDistance<TPos, TDiff>(BezierSpline<TPos, TDiff> original, Length desiredHandleDistance)
        where TPos : unmanaged where TDiff : unmanaged => BezierSpline.FromHandlesWithVelocities(
            original.SampleSplineEvery(desiredHandleDistance).Select(sample => sample.PositionAndTangent),
            original.Ops
        );
        
    }
}