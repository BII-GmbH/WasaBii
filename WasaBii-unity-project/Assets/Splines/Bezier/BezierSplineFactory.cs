using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;

namespace BII.WasaBii.Splines.Bezier {
    
    /// Contains generic factory methods for building bezier splines.
    /// For explicitly typed variants with <see cref="GeometricOperations{TPos,TDiff}"/>
    /// included, use `UnitySpline`, `GlobalSpline` or `LocalSpline` in the Unity assembly.
    public static class BezierSpline {

        [Pure]
        public static BezierSpline<TPos, TDiff> FromQuadraticHandles<TPos, TDiff>(
            IEnumerable<TPos> handles,
            GeometricOperations<TPos, TDiff> ops
        ) where TPos : struct where TDiff : struct {
            using var enumerator = handles.GetEnumerator();
            var segments = new List<BezierSegment<TPos, TDiff>.Quadratic>();
            if (!enumerator.MoveNext()) throw new ArgumentException("No handles passed. A bezier spline from quadratic segments needs at least 3 handles");
            var a = enumerator.Current;
            Exception incorrectHandleCountException(int offset) => new ArgumentException(
                $"Incorrect number of handles passed. A bezier spline from n quadratic segments has 2 * n + 1 handles (provided: {1 + 2 * segments.Count + offset})"
            );
            while (enumerator.MoveNext()) {
                var b = enumerator.Current;
                if (!enumerator.MoveNext()) throw incorrectHandleCountException(2);
                var c = enumerator.Current;
                segments.Add(BezierSegment.MkQuadratic(a, b, c, ops));
                a = c;
            }

            return new BezierSpline<TPos, TDiff>(segments, ops);
        }

        [Pure]
        public static BezierSpline<TPos, TDiff> FromCubicHandles<TPos, TDiff>(
            IEnumerable<TPos> handles,
            GeometricOperations<TPos, TDiff> ops
        ) where TPos : struct where TDiff : struct {
            using var enumerator = handles.GetEnumerator();
            var segments = new List<BezierSegment<TPos, TDiff>.Cubic>();
            if (!enumerator.MoveNext()) throw new ArgumentException("No handles passed. A bezier spline from cubic segments needs at least 4 handles");
            var a = enumerator.Current;
            Exception incorrectHandleCountException(int offset) => new ArgumentException(
                $"Incorrect number of handles passed. A bezier spline from n cubic segments has 3 * n + 1 handles (provided: {1 + 3 * segments.Count + offset})"
            );
            while (enumerator.MoveNext()) {
                var b = enumerator.Current;
                if (!enumerator.MoveNext()) throw incorrectHandleCountException(2);
                var c = enumerator.Current;
                if (!enumerator.MoveNext()) throw incorrectHandleCountException(3);
                var d = enumerator.Current;
                segments.Add(BezierSegment.MkCubic(a, b, c, d, ops));
                a = d;
            }

            return new BezierSpline<TPos, TDiff>(segments, ops);
        }

        [Pure]
        public static BezierSpline<TPos, TDiff> FromHandlesWithVelocities<TPos, TDiff>(
            IEnumerable<(TPos position, TDiff velocity)> handles,
            GeometricOperations<TPos, TDiff> ops, 
            bool shouldLoop = false
        ) where TPos : struct where TDiff : struct {
            var (first, tail) = handles;
            var allHandles = shouldLoop ? first.PrependTo(tail).Append(first) : first.PrependTo(tail);
            var segments = allHandles.Select((p, v) => {
                var offset = ops.Div(v, 3);
                var leftHandle = ops.Sub(p, offset);
                var rightHandle = ops.Add(p, offset);
                return (leftHandle, p, rightHandle);
            }).PairwiseSliding().Select((left, right) => 
                BezierSegment.MkCubic(left.p, left.rightHandle, right.leftHandle, right.p, ops)
            ).ToArray();
            return new BezierSpline<TPos, TDiff>(segments, ops);
        }
    }
}