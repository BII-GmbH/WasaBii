using System;
using System.Linq;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines.Bezier {
    public static class BezierSplineCopyExtensions {
    
        /// Creates a new spline with a similar trajectory as <paramref name="original"/>, but with all handle
        /// positions being moved by a certain offset which depends on the spline's tangent at these points.
        public static BezierSpline<TPos, TDiff> CopyWithOffset<TPos, TDiff>(
            this BezierSpline<TPos, TDiff> original, Func<TDiff, TDiff> tangentToOffset
        ) where TPos : struct where TDiff : struct => new(
            original.Segments.Select(s => BezierSegment.Cubic(
                start: s.Ops.Add(s.Start, tangentToOffset(s.StartVelocity)), 
                s.StartVelocity,
                s.Ops.Add(s.End, tangentToOffset(s.EndVelocity)),
                endVelocity: s.EndVelocity, 
                s.Ops
            )),
            original.Ops
        );

        /// Creates a new spline with the same trajectory as
        /// <paramref name="original"/>, but with all handle positions
        /// being moved along a certain <paramref name="offset"/>,
        /// independent of the spline's tangent at these points.
        public static BezierSpline<TPos, TDiff> CopyWithStaticOffset<TPos, TDiff>(
            this BezierSpline<TPos, TDiff> original, TDiff offset
        ) where TPos : struct where TDiff : struct {
            TPos computePosition(TPos node) => original.Ops.Add(node, offset);
            return new BezierSpline<TPos, TDiff>(
                original.Segments.Select(s => s.Map(computePosition)),
                original.Ops
            );
        }

        /// Creates a new spline with a similar trajectory as
        /// <paramref name="original"/>, but different spacing
        /// between the handles.
        public static BezierSpline<TPos, TDiff> CopyWithDifferentHandleDistance<TPos, TDiff>(this BezierSpline<TPos, TDiff> original, Length desiredHandleDistance)
        where TPos : struct where TDiff : struct => BezierSpline.FromHandlesWithVelocities(
            original.SampleSplineEvery(desiredHandleDistance).Select(sample => sample.PositionAndTangent),
            original.Ops
        );
        
        /// Creates a new spline that is the reverse of the original
        /// but has the same handles and spline type
        public static BezierSpline<TPos, TDiff> Reversed<TPos, TDiff>(this BezierSpline<TPos, TDiff> original) 
            where TPos : struct where TDiff : struct => 
                new(original.Segments.Reverse().Select(s => s.Reversed), original.Ops);
    }
}