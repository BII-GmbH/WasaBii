using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;

namespace BII.WasaBii.Splines.Bezier {
    
    /// <summary>
    /// Contains generic factory methods for building bezier splines.
    /// For explicitly typed variants with <see cref="GeometricOperations{TPos,TDiff,TTime,TVel}"/>
    /// included, use `UnitySpline`, `GlobalSpline` or `LocalSpline` in the Extra assembly.
    /// </summary>
    public static class BezierSpline {
        
        /// <summary>
        /// Constructs a non-uniform spline from quadratic segments.
        /// </summary>
        /// <param name="handles">The positions the spline should visit along with the time at which the handle
        /// should be traversed. Every other handle will only be used for influencing the segments' trajectory,
        /// it will likely not be be traversed and its timestamp is ignored.</param>
        /// <param name="ops">The geometric operations necessary for calculation.</param>
        [Pure]
        public static BezierSpline<TPos, TDiff, TTime, TVel> FromQuadraticHandles<TPos, TDiff, TTime, TVel>(
            IEnumerable<(TPos, TTime)> handles,
            GeometricOperations<TPos, TDiff, TTime, TVel> ops
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            using var enumerator = handles.GetEnumerator();
            var segments = new List<BezierSegment<TPos, TDiff, TTime, TVel>>();
            if (!enumerator.MoveNext()) throw new ArgumentException("No handles passed. A bezier spline from quadratic segments needs at least 3 handles");
            var (posA, timeA) = enumerator.Current;
            Exception incorrectHandleCountException(int offset) => new ArgumentException(
                $"Incorrect number of handles passed. A bezier spline from n quadratic segments has 2 * n + 1 handles (provided: {1 + 2 * segments.Count + offset})"
            );
            while (enumerator.MoveNext()) {
                var (posB, _) = enumerator.Current;
                if (!enumerator.MoveNext()) throw incorrectHandleCountException(1);
                var (posC, timeC) = enumerator.Current;
                if (timeA.CompareTo(timeC) != -1)
                    throw new ArgumentException(
                        "Failed to construct spline, handles must be given in chronological order"
                    );
                var duration = ops.Sub(timeC, timeA);
                segments.Add(new BezierSegment<TPos, TDiff, TTime, TVel>(duration, posA, posB, posC));
                (posA, timeA) = (posC, timeC);
            }
            
            if(segments.IsEmpty()) throw new ArgumentException("Only one handle passed. A bezier spline from quadratic segments needs at least 3 handles");

            return new BezierSpline<TPos, TDiff, TTime, TVel>(segments, ops);
        }
 
        /// <summary>
        /// Constructs a spline from quadratic segments with uniform lengths. The spline has a duration of 1.
        /// </summary>
        /// <param name="handles">The positions the spline should visit. Every other handle will only be used for
        /// influencing the segments' trajectory, it will likely not be be traversed.</param>
        /// <param name="ops">The geometric operations necessary for calculation.</param>
        [Pure]
        public static BezierSpline<TPos, TDiff, double, TDiff> UniformFromQuadraticHandles<TPos, TDiff>(
            IEnumerable<TPos> handles,
            GeometricOperations<TPos, TDiff, double, TDiff> ops
        ) where TPos : unmanaged where TDiff : unmanaged {
            var handleList = handles.AsReadOnlyList();
            if (handleList.IsEmpty())
                throw new ArgumentException("No handles passed. A bezier spline from quadratic segments needs at least 3 handles");
            else if (handleList.Count == 1)
                throw new ArgumentException("Only one handle passed. A bezier spline from quadratic segments needs at least 3 handles");
            else if ((handleList.Count - 1) % 2 != 0)
                throw new ArgumentException(
                    $"Incorrect number of handles passed. A bezier spline from n quadratic segments has 2 * n + 1 handles (provided: {handleList.Count})"
                );
            var segments = new BezierSegment<TPos, TDiff, double, TDiff>[handleList.Count / 2];
            var durationPerSegment = 1.0 / segments.Length;
            for (var i = 0; i < segments.Length; i ++) {
                var j = i << 1;
                segments[i] = new BezierSegment<TPos, TDiff, double, TDiff>(
                    durationPerSegment,
                    handleList[j],
                    handleList[j + 1],
                    handleList[j + 2]
                );
            }
            
            return new BezierSpline<TPos, TDiff, double, TDiff>(segments, ops);
        }

        /// <summary>
        /// Constructs a non-uniform spline from cubic segments.
        /// </summary>
        /// <param name="handles">The positions the spline should visit along with the time at which the handle
        /// should be traversed. Every two other handles will only be used for influencing the segments' trajectory,
        /// they will likely not be be traversed and their timestamp is ignored.</param>
        /// <param name="ops">The geometric operations necessary for calculation.</param>
        [Pure]
        public static BezierSpline<TPos, TDiff, TTime, TVel> FromCubicHandles<TPos, TDiff, TTime, TVel>(
            IEnumerable<(TPos, TTime)> handles,
            GeometricOperations<TPos, TDiff, TTime, TVel> ops
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            using var enumerator = handles.GetEnumerator();
            var segments = new List<BezierSegment<TPos, TDiff, TTime, TVel>>();
            if (!enumerator.MoveNext()) throw new ArgumentException("No handles passed. A bezier spline from cubic segments needs at least 4 handles");
            var (posA, timeA) = enumerator.Current;
            Exception incorrectHandleCountException(int offset) => new ArgumentException(
                $"Incorrect number of handles passed. A bezier spline from n cubic segments has 3 * n + 1 handles (provided: {1 + 3 * segments.Count + offset})"
            );
            while (enumerator.MoveNext()) {
                var (posB, _) = enumerator.Current;
                if (!enumerator.MoveNext()) throw incorrectHandleCountException(1);
                var (posC, _) = enumerator.Current;
                if (!enumerator.MoveNext()) throw incorrectHandleCountException(2);
                var (posD, timeD) = enumerator.Current;
                if (timeA.CompareTo(timeD) != -1)
                    throw new ArgumentException(
                        "Failed to construct spline, handles must be given in chronological order"
                    );
                var duration = ops.Sub(timeD, timeA);
                segments.Add(new BezierSegment<TPos, TDiff, TTime, TVel>(duration, posA, posB, posC, posD));
                posA = posD;
            }
            
            if(segments.IsEmpty()) throw new ArgumentException("Only one handle passed. A bezier spline from cubic segments needs at least 4 handles");

            return new BezierSpline<TPos, TDiff, TTime, TVel>(segments, ops);
        }
 
        /// <summary>
        /// Constructs a spline from cubic segments with uniform lengths. The spline has a duration of 1.
        /// </summary>
        /// <param name="handles">The positions the spline should visit. Every two other handles will only be used for
        /// influencing the segments' trajectory, they will likely not be be traversed.</param>
        /// <param name="ops">The geometric operations necessary for calculation.</param>
        [Pure]
        public static BezierSpline<TPos, TDiff, double, TDiff> UniformFromCubicHandles<TPos, TDiff>(
            IEnumerable<TPos> handles,
            GeometricOperations<TPos, TDiff, double, TDiff> ops
        ) where TPos : unmanaged where TDiff : unmanaged {
            var handleList = handles.AsReadOnlyList();
            if (handleList.IsEmpty())
                throw new ArgumentException("No handles passed. A bezier spline from cubic segments needs at least 4 handles");
            else if (handleList.Count == 1)
                throw new ArgumentException("Only one handle passed. A bezier spline from cubic segments needs at least 4 handles");
            else if ((handleList.Count - 1) % 3 != 0)
                throw new ArgumentException(
                    $"Incorrect number of handles passed. A bezier spline from n cubic segments has 3 * n + 1 handles (provided: {handleList.Count})"
                );
            var segments = new BezierSegment<TPos, TDiff, double, TDiff>[handleList.Count / 3];
            var durationPerSegment = 1.0 / segments.Length;
            for (var i = 0; i < segments.Length; i ++) {
                var j = i * 3;
                segments[i] = new BezierSegment<TPos, TDiff, double, TDiff>(
                    durationPerSegment,
                    handleList[j],
                    handleList[j + 1],
                    handleList[j + 2],
                    handleList[j + 3]
                );
            }
            
            return new BezierSpline<TPos, TDiff, double, TDiff>(segments, ops);
        }

        /// <summary>
        /// Constructs a spline that traverses each handle in order at the desired position
        /// and velocity and in the specified time.
        /// </summary>
        /// <param name="handles">The positions the spline should visit along with the desired velocity at these points.</param>
        /// <param name="ops">The geometric operations necessary for calculation.</param>
        /// <param name="shouldAccelerationBeContinuous">If true, the spline's trajectory is altered to ensure a
        /// continuous acceleration. This is usually desirable for animations since jumps in the acceleration might
        /// make the movement look less smooth. Since this makes the trajectory less predictable and increases the
        /// computational load, you should not enable it unless you actually need it.</param>
        [Pure]
        public static BezierSpline<TPos, TDiff, TTime, TVel> FromHandlesWithVelocities<TPos, TDiff, TTime, TVel>(
            IEnumerable<(TPos Position, TVel Velocity, TTime Time)> handles,
            GeometricOperations<TPos, TDiff, TTime, TVel> ops, 
            bool shouldAccelerationBeContinuous = false
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            if (shouldAccelerationBeContinuous)
                return FromHandlesWithVelocitiesAndAccelerations(
                    handles.Select(h => (h.Position, h.Velocity, ops.ZeroVel, h.Time)),
                    ops
                );
            else {
                var segments = handles.PairwiseSliding().SelectTuple((left, right) => 
                    BezierSegment.Cubic(
                        left.Position, left.Velocity, 
                        right.Position, right.Velocity,
                        ops.Sub(right.Time, left.Time), 
                        ops));
                return new BezierSpline<TPos, TDiff, TTime, TVel>(segments, ops);
            }
        }
        
        /// <summary>
        /// Constructs a spline that traverses each handle in order at the desired position
        /// and velocity and in uniform intervals. The spline has a duration of 1.
        /// </summary>
        /// <param name="handles">The positions the spline should visit along with the desired velocity at these points.</param>
        /// <param name="ops">The geometric operations necessary for calculation.</param>
        /// <param name="shouldLoop">Whether the spline should come back to the first handle or stop at the last.</param>
        /// <param name="shouldAccelerationBeContinuous">If true, the spline's trajectory is altered to ensure a
        /// continuous acceleration. This is usually desirable for animations since jumps in the acceleration might
        /// make the movement look less smooth. Since this makes the trajectory less predictable and increases the
        /// computational load, you should not enable it unless you actually need it.</param>
        [Pure]
        public static BezierSpline<TPos, TDiff, double, TDiff> UniformFromHandlesWithVelocities<TPos, TDiff>(
            IEnumerable<(TPos Position, TDiff Velocity)> handles,
            GeometricOperations<TPos, TDiff, double, TDiff> ops, 
            bool shouldLoop = false, 
            bool shouldAccelerationBeContinuous = false
        ) where TPos : unmanaged where TDiff : unmanaged {
            if (shouldAccelerationBeContinuous)
                return UniformFromHandlesWithVelocitiesAndAccelerations(
                    handles.Select(h => (h.Position, h.Velocity, ops.ZeroDiff)),
                    ops,
                    shouldLoop
                );
            else {
                var handleList = handles.ToList();
                var durationPerSegment = 1.0 / (shouldLoop ? handleList.Count + 1 : handleList.Count);
                var segments = (shouldLoop ? handleList.Append(handleList[0]) : handleList).PairwiseSliding().SelectTuple((left, right) => 
                    BezierSegment.CubicWithUniformVelocity(
                        left.Position, left.Velocity, 
                        right.Position, right.Velocity,
                        durationPerSegment, 
                        ops));
                return new BezierSpline<TPos, TDiff, double, TDiff>(segments, ops);
            }
        }
        
        /// <summary>
        /// Constructs a spline that traverses each handle in order at the desired position,
        /// velocity and acceleration and in the specified time.
        /// </summary>
        /// <param name="handles">The positions the spline should visit along with the desired velocity at these points.</param>
        /// <param name="ops">The geometric operations necessary for calculation.</param>
        [Pure]
        public static BezierSpline<TPos, TDiff, TTime, TVel> FromHandlesWithVelocitiesAndAccelerations<TPos, TDiff, TTime, TVel>(
            IEnumerable<(TPos Position, TVel Velocity, TVel Acceleration, TTime Time)> handles,
            GeometricOperations<TPos, TDiff, TTime, TVel> ops
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            var segments = handles.PairwiseSliding().SelectTuple((left, right) => 
                BezierSegment.Quintic(
                    left.Position, left.Velocity, left.Acceleration, 
                    right.Position, right.Velocity, right.Acceleration, 
                    ops.Sub(right.Time, left.Time),
                    ops));
            return new BezierSpline<TPos, TDiff, TTime, TVel>(segments, ops);
        }
        
        /// <summary>
        /// Constructs a spline that traverses each handle in order at the desired position,
        /// velocity and acceleration and in uniform intervals. The spline has a duration of 1.
        /// </summary>
        /// <param name="handles">The positions the spline should visit along with the desired velocity at these points.</param>
        /// <param name="ops">The geometric operations necessary for calculation.</param>
        /// <param name="shouldLoop">Whether the spline should come back to the first handle or stop at the last.</param>
        [Pure]
        public static BezierSpline<TPos, TDiff, double, TDiff> UniformFromHandlesWithVelocitiesAndAccelerations<TPos, TDiff>(
            IEnumerable<(TPos Position, TDiff Velocity, TDiff Acceleration)> handles,
            GeometricOperations<TPos, TDiff, double, TDiff> ops, 
            bool shouldLoop = false
        ) where TPos : unmanaged where TDiff : unmanaged {
            var handleList = handles.ToList();
            var durationPerSegment = 1.0 / (shouldLoop ? handleList.Count + 1 : handleList.Count);
            var segments = (shouldLoop ? handleList.Append(handleList[0]) : handleList).PairwiseSliding().SelectTuple((left, right) => 
                BezierSegment.Quintic(
                    left.Position, left.Velocity, left.Acceleration, 
                    right.Position, right.Velocity, right.Acceleration, 
                    durationPerSegment,
                    ops));
            return new BezierSpline<TPos, TDiff, double, TDiff>(segments, ops);
        }
    }
}