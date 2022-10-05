using System;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines.Bezier {

    public static class BezierSegment {
        
        /// <summary>
        /// A curve starting at <see cref="start"/> with velocity <see cref="startVelocity"/>
        /// and ending at <see cref="end"/>.
        /// </summary>
        [Pure]
        public static BezierSegment<TPos, TDiff> Quadratic<TPos, TDiff>(
            TPos start, TDiff startVelocity, TPos end,
            GeometricOperations<TPos, TDiff> ops
        ) where TPos : struct where TDiff : struct 
            => new(
                start, 
                ops.Add(start, ops.Div(startVelocity, 2)), 
                end
            );

        /// <summary>
        /// A curve starting at <see cref="start"/> with velocity <see cref="startVelocity"/>
        /// and ending at <see cref="end"/> with velocity <see cref="endVelocity"/>.
        /// </summary>
        [Pure]
        public static BezierSegment<TPos, TDiff> Cubic<TPos, TDiff>(
            TPos start, TDiff startVelocity, TPos end, TDiff endVelocity,
            GeometricOperations<TPos, TDiff> ops
        ) where TPos : struct where TDiff : struct 
            => new(
                start, 
                ops.Add(start, ops.Div(startVelocity, 3)),
                ops.Sub(end, ops.Div(endVelocity, 3)), 
                end
            );

        /// <summary>
        /// A curve starting at <see cref="start"/> with velocity <see cref="startVelocity"/> and acceleration <see cref="startAcceleration"/>
        /// and ending at <see cref="end"/> with velocity <see cref="endVelocity"/>.
        /// </summary>
        [Pure]
        public static BezierSegment<TPos, TDiff> Quartic<TPos, TDiff>(
            TPos start, TDiff startVelocity, TDiff startAcceleration, TPos end, TDiff endVelocity,
            GeometricOperations<TPos, TDiff> ops
        ) where TPos : struct where TDiff : struct 
            => new(
                start, 
                ops.Add(start, ops.Div(startVelocity, 4)),
                ops.Add(start, ops.Div(ops.Add(startAcceleration, ops.Mul(startVelocity, 6)), 12)),
                ops.Sub(end, ops.Div(endVelocity, 4)), 
                end
            );

        /// <summary>
        /// A curve starting at <see cref="start"/> with velocity <see cref="startVelocity"/> and acceleration <see cref="startAcceleration"/>
        /// and ending at <see cref="end"/> with velocity <see cref="endVelocity"/> and acceleration <see cref="endAcceleration"/>.
        /// </summary>
        [Pure]
        public static BezierSegment<TPos, TDiff> Quintic<TPos, TDiff>(
            TPos start, TDiff startVelocity, TDiff startAcceleration, TPos end, TDiff endVelocity, TDiff endAcceleration,
            GeometricOperations<TPos, TDiff> ops
        ) where TPos : struct where TDiff : struct => new(
            start, 
            ops.Add(start, ops.Div(startVelocity, 5)),
            ops.Add(start, ops.Div(ops.Add(startAcceleration, ops.Mul(startVelocity, 8)), 20)),
            ops.Add(end, ops.Div(ops.Sub(endAcceleration, ops.Mul(endVelocity, 8)), 20)),
            ops.Sub(end, ops.Div(endVelocity, 5)), 
            end
        );

    }

    /// <summary>
    /// Describes a curve connecting a start position and an end position. The trajectory is influenced by
    /// up to 10 handles in between, although it is advisable to stick with few handles as pushing the limit
    /// could result in numerical instabilities and inaccurate results. The default is the cubic bezier curve
    /// with just 2 handles. The curve will usually go in the direction of the handles without ever touching them.
    /// </summary>
    [MustBeSerializable]
    public readonly struct BezierSegment<TPos, TDiff> where TPos : struct where TDiff : struct {

        public readonly TPos Start;
        public readonly ImmutableArray<TPos> Handles;
        public readonly TPos End;

        public int Degree => Handles.Length + 1;

        public TDiff StartVelocity(GeometricOperations<TPos, TDiff> ops) => Degree > 1 
            ? ops.Mul(ops.Sub(this[1], this[0]), Degree) 
            : ops.Sub(End, Start);
        public TDiff StartAcceleration(GeometricOperations<TPos, TDiff> ops) => Degree > 1
            ? ops.Mul(ops.Add(ops.Sub(this[0], this[1]), ops.Sub(this[2], this[1])), Degree * (Degree - 1))
            : ops.ZeroDiff;
        
        public TDiff EndVelocity(GeometricOperations<TPos, TDiff> ops) => Degree > 1
            ? ops.Mul(ops.Sub(this[^2], this[^1]), Degree) 
            : ops.Sub(End, Start);
        public TDiff EndAcceleration(GeometricOperations<TPos, TDiff> ops) => Degree > 1
            ? ops.Mul(ops.Add(ops.Sub(this[^1], this[^2]), ops.Sub(this[^3], this[^2])), Degree * (Degree - 1))
            : ops.ZeroDiff;

        
        public TPos this[Index i] => i.Value == 0
            ? i.IsFromEnd ? End : Start
            : i.Value == Degree
                ? i.IsFromEnd ? Start : End
                : Handles[new Index(i.Value - 1, i.IsFromEnd)];

        /// <summary>
        /// Since calculating the polynomial includes computing the factorial of the spline's degree, we need
        /// to limit the degree to be at most 12. Any larger degree would have a factorial than exceeds the
        /// range of <see cref="int"/>. Switching to <see cref="long"/> could potentially allow a degree of
        /// up to 20, but numbers this high could lead to very inaccurate <see cref="float"/> calculations.
        /// </summary>
        private const int maxDegree = 12;
        
        public BezierSegment(TPos start, ImmutableArray<TPos> handles, TPos end) {
            Start = start;
            Handles = handles;
            End = end;
            if (Degree > maxDegree)
                throw new ArgumentException($"A single bezier curve may only have at most {maxDegree - 2} handles.");
        }

        public BezierSegment(TPos p0, TPos p1, params TPos[] otherPos) : this(
            p0,
            p1.PrependTo(otherPos[..^1]).ToImmutableArray(),
            otherPos[^1]
        ) { }

        [Pure] internal Polynomial<TPos, TDiff> ToPolynomial(GeometricOperations<TPos, TDiff> ops) {
            var p0 = Start;
            var n = Handles.Length + 1;
            var p = new TDiff[n];
            var factorials = new int[n + 1];
            factorials[0] = factorials[1] = 1;
            for (var i = 2; i <= n; i++) factorials[i] = i * factorials[i - 1];
            
            for (var k = 1; k <= n; k++) {
                p[k - 1] = ops.ZeroDiff;
                var lastFactor = 0;
                // Every summand is multiplied by this. Theoretically, you could just do this once with the sum,
                // but doing it this way greatly reduces the maximum factors the handles are scaled with. Since 
                // the handles can well be backed by floats, this should reduce numerical instability.
                var commonFactor = factorials[n] / factorials[n - k];
                for (var i = 0; i < k; i++) {
                    // The factor always comes out to be integer, so integer division will not result in fraction
                    // truncation.
                    var factor = commonFactor / (factorials[i] * factorials[k - i]) - lastFactor;
                    lastFactor = factor;
                    if(((k + i) & 1) == 1) factor = -factor;
                    p[k - 1] = ops.Add(p[k - 1], ops.Mul(ops.Sub(this[i], this[i + 1]), factor));
                }
            }

            return new Polynomial<TPos, TDiff>(ops, p0, p);
        }

        [Pure] public SplineSegment<TPos, TDiff> ToSplineSegment(GeometricOperations<TPos, TDiff> ops, Lazy<Length>? cachedLength = null) 
            => new(ToPolynomial(ops), cachedLength);
        
        [Pure]
        public BezierSegment<TPosNew, TDiffNew> Map<TPosNew, TDiffNew>(Func<TPos, TPosNew> positionMapping)
        where TPosNew : struct where TDiffNew : struct => new(
            positionMapping(Start),
            Handles.Select(positionMapping).ToImmutableArray(),
            positionMapping(End)
        );
        
        [Pure] public BezierSegment<TPos, TDiff> Map(Func<TPos, TPos> positionMapping) => Map<TPos, TDiff>(positionMapping);

        public BezierSegment<TPos, TDiff> Reversed => new(End, Handles.ReverseList().ToImmutableArray(), Start);

    }

}
