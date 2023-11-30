
#nullable enable

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

[assembly:InternalsVisibleTo("WasaBii-Splines-CatmullRom-Tests")]

namespace BII.WasaBii.Splines.CatmullRom {
   
    /// Describes the area between two spline handles (p1 and p2), 
    /// with the supporting handles p0 and p3
    internal readonly struct CatmullRomSegment<TPos, TDiff> 
        where TPos : unmanaged 
        where TDiff : unmanaged {

        public readonly TPos P0, P1, P2, P3;
        public TPos Start => P1;
        public TPos End => P2;
        
        internal readonly GeometricOperations<TPos, TDiff> Ops;

        public CatmullRomSegment(TPos p0, TPos p1, TPos p2, TPos p3, GeometricOperations<TPos, TDiff> ops) {
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
            Ops = ops;
        }
        
        [Pure]
        internal Polynomial<TPos, TDiff> ToPolynomial(float alpha) {
            var ops = Ops;

            double DTFor(TPos pos1, TPos pos2, double orWhenZero) {
                var dt = Math.Pow(ops.Distance(pos1, pos2).AsMeters(), alpha);
                return dt < float.Epsilon ? orWhenZero : dt;
            }

            var dt1 = DTFor(P1, P2, orWhenZero: 1.0f);
            var dt0 = DTFor(P0, P1, orWhenZero: dt1);
            var dt2 = DTFor(P2, P3, orWhenZero: dt1);
            
            TDiff TFor(TPos pa, TPos pb, TPos pc, double dta, double dtb) =>
                ops.Mul(ops.Add(
                    ops.Sub(
                        ops.Div(ops.Sub(pb, pa), dta), 
                        ops.Div(ops.Sub(pc, pa), dta + dtb)
                    ), 
                    ops.Div(ops.Sub(pc, pb), dtb)
                ), dt1);

            var t1 = TFor(P0, P1, P2, dt0, dt1);
            var t2 = TFor(P1, P2, P3, dt1, dt2);

            return Polynomial.Cubic(
                a: P1,
                b: t1,
                c: ops.Sub(ops.Mul(ops.Sub(P2, P1), 3), ops.Mul(t1, 2), t2),
                d: ops.Add(ops.Mul(ops.Sub(P1, P2), 2), t1, t2),
                ops
            );
        }

    }

    internal static class CatmullRomSegment {
        public const double EndOfSplineOvershootTolerance = 0.01;
        
        /// Given a SplineNode and a normalized location relative to that node,
        /// this method returns the four nodes around that location that are needed
        /// to calculate the position / tangent / etc. of that location on the spline.
        /// 
        /// The given location will be between the nodes P1 and P2 of the returned segment.
        /// The exact position between P1 and P2 is determined by the returned NormalizedOvershoot:
        /// 0.0f is at P1, 1.0f is at P2 and the values in between are lerped. 
        public static (CatmullRomSegment<TPos, TDiff> Segment, double NormalizedOvershoot)? 
            CatmullRomSegmentAt<TPos, TDiff>(CatmullRomSpline<TPos, TDiff> spline, NormalizedSplineLocation location) 
        where TPos : unmanaged 
        where TDiff : unmanaged {
            if(spline == null)
                throw new ArgumentNullException(nameof(spline));
            
            if(double.IsNaN(location.Value))
                throw new ArgumentException("The spline location is NaN", nameof(location));
                
            if (location < 0 || location > spline.SegmentCount + EndOfSplineOvershootTolerance)
                return null;
            
            var (s0, overshoot) = location >= spline.SegmentCount
                // The location was almost at, or slightly above the end of the spline
                // but within tolerance. The used segment automatically
                // becomes the last valid catmull rom segment.
                ? (SplineHandleIndex.At(spline.SegmentCount - 1), 1.0f)
                // Otherwise the location is simply converted to a handle index and overshoot
                : location.AsHandleIndex();
            
            return (new CatmullRomSegment<TPos, TDiff>(
                spline[s0],
                spline[s0 + 1],
                spline[s0 + 2],
                spline[s0 + 3],
                spline.Ops
            ), overshoot);
        }
    }
}
