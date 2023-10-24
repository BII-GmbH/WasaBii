using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;

namespace BII.WasaBii.Splines.Bezier {
    
    /// <summary>
    /// Contains generic factory methods for building bezier splines.
    /// For explicitly typed variants with <see cref="GeometricOperations{TPos,TDiff}"/>
    /// included, use `UnitySpline`, `GlobalSpline` or `LocalSpline` in the Unity assembly.
    /// </summary>
    public static class BezierSpline {

        [Pure]
        public static BezierSpline<TPos, TDiff> FromQuadraticHandles<TPos, TDiff>(
            IEnumerable<TPos> handles,
            GeometricOperations<TPos, TDiff> ops
        ) where TPos : unmanaged where TDiff : unmanaged {
            using var enumerator = handles.GetEnumerator();
            var segments = new List<BezierSegment<TPos, TDiff>>();
            if (!enumerator.MoveNext()) throw new ArgumentException("No handles passed. A bezier spline from quadratic segments needs at least 3 handles");
            var a = enumerator.Current;
            Exception incorrectHandleCountException(int offset) => new ArgumentException(
                $"Incorrect number of handles passed. A bezier spline from n quadratic segments has 2 * n + 1 handles (provided: {1 + 2 * segments.Count + offset})"
            );
            while (enumerator.MoveNext()) {
                var b = enumerator.Current;
                if (!enumerator.MoveNext()) throw incorrectHandleCountException(1);
                var c = enumerator.Current;
                segments.Add(new BezierSegment<TPos, TDiff>(a, b, c));
                a = c;
            }
            
            if(segments.IsEmpty()) throw new ArgumentException("Only one handle passed. A bezier spline from quadratic segments needs at least 3 handles");

            return new BezierSpline<TPos, TDiff>(segments, ops);
        }

        [Pure]
        public static BezierSpline<TPos, TDiff> FromCubicHandles<TPos, TDiff>(
            IEnumerable<TPos> handles,
            GeometricOperations<TPos, TDiff> ops
        ) where TPos : unmanaged where TDiff : unmanaged {
            using var enumerator = handles.GetEnumerator();
            var segments = new List<BezierSegment<TPos, TDiff>>();
            if (!enumerator.MoveNext()) throw new ArgumentException("No handles passed. A bezier spline from cubic segments needs at least 4 handles");
            var a = enumerator.Current;
            Exception incorrectHandleCountException(int offset) => new ArgumentException(
                $"Incorrect number of handles passed. A bezier spline from n cubic segments has 3 * n + 1 handles (provided: {1 + 3 * segments.Count + offset})"
            );
            while (enumerator.MoveNext()) {
                var b = enumerator.Current;
                if (!enumerator.MoveNext()) throw incorrectHandleCountException(1);
                var c = enumerator.Current;
                if (!enumerator.MoveNext()) throw incorrectHandleCountException(2);
                var d = enumerator.Current;
                segments.Add(new BezierSegment<TPos, TDiff>(a, b, c, d));
                a = d;
            }
            
            if(segments.IsEmpty()) throw new ArgumentException("Only one handle passed. A bezier spline from cubic segments needs at least 4 handles");

            return new BezierSpline<TPos, TDiff>(segments, ops);
        }

        /// <summary>
        /// Constructs a spline that traverses each handle in order at the desired position and velocity.
        /// </summary>
        /// <param name="handles">The positions the spline should visit along with the desired velocity at these points.</param>
        /// <param name="ops">The geometric operations necessary for calculation.</param>
        /// <param name="shouldLoop">Whether the spline should come back to the first handle or stop at the last.</param>
        /// <param name="shouldAccelerationBeContinuous">If true, the spline's trajectory is altered to ensure a
        /// continuous acceleration. This is usually desirable for animations since jumps in the acceleration might
        /// make the movement look less smooth. Since this makes the trajectory less predictable and increases the
        /// computational load, you should not enable it unless you actually need it.</param>
        [Pure]
        public static BezierSpline<TPos, TDiff> FromHandlesWithVelocities<TPos, TDiff>(
            IEnumerable<(TPos position, TDiff velocity)> handles,
            GeometricOperations<TPos, TDiff> ops, 
            bool shouldLoop = false,
            bool shouldAccelerationBeContinuous = false
        ) where TPos : unmanaged where TDiff : unmanaged {
            if (shouldAccelerationBeContinuous)
                return FromHandlesWithVelocitiesAndAccelerations(
                    handles.Select(h => (h.position, h.velocity, ops.ZeroDiff)),
                    ops,
                    shouldLoop
                );
            else {
                var (first, tail) = handles;
                var allHandles = shouldLoop ? first.PrependTo(tail).Append(first) : first.PrependTo(tail);
                var segments = allHandles.PairwiseSliding().SelectTuple((left, right) => 
                    BezierSegment.Cubic(left.position, left.velocity, right.position, right.velocity, ops)
                );
                return new BezierSpline<TPos, TDiff>(segments, ops);
            }
        }
        [Pure]
        public static BezierSpline<TPos, TDiff> FromHandlesWithVelocitiesAndAccelerations<TPos, TDiff>(
            IEnumerable<(TPos position, TDiff velocity, TDiff acceleration)> handles,
            GeometricOperations<TPos, TDiff> ops, 
            bool shouldLoop = false
        ) where TPos : unmanaged where TDiff : unmanaged {
            var (first, tail) = handles;
            var allHandles = shouldLoop ? first.PrependTo(tail).Append(first) : first.PrependTo(tail);
            var segments = allHandles.PairwiseSliding().SelectTuple((left, right) => 
                BezierSegment.Quintic(left.position, left.velocity, left.acceleration, right.position, right.velocity, right.acceleration, ops)
            );
            return new BezierSpline<TPos, TDiff>(segments, ops);
        }
    }
}