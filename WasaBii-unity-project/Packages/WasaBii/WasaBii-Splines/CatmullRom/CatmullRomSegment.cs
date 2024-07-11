
#nullable enable

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

[assembly:InternalsVisibleTo("WasaBii-Splines-CatmullRom-Tests")]

namespace BII.WasaBii.Splines.CatmullRom {
   
    /// <summary>
    /// Describes the area between two spline handles (p1 and p2), 
    /// with the supporting handles p0 and p3.
    /// </summary>
    internal readonly struct CatmullRomSegment<TPos, TDiff, TTime, TVel> 
        where TPos : unmanaged 
        where TDiff : unmanaged
        where TTime : unmanaged
        where TVel : unmanaged
    {

        public readonly TPos P0, P1, P2, P3;
        public TPos Start => P1;
        public TPos End => P2;
        
        public readonly TTime Duration;
        public readonly TTime PrevDur;
        public readonly TTime NextDur;
        
        internal readonly GeometricOperations<TPos, TDiff, TTime, TVel> Ops;

        public CatmullRomSegment(TPos p0, TPos p1, TPos p2, TPos p3, TTime duration, TTime prevDur, TTime nextDur, GeometricOperations<TPos, TDiff, TTime, TVel> ops) {
            P0 = p0;
            P1 = p1;
            P2 = p2;
            P3 = p3;
            Ops = ops;
            Duration = duration;
            PrevDur = prevDur;
            NextDur = nextDur;
            NextDur = nextDur;
        }
        
        [Pure]
        internal Polynomial<TPos, TDiff, TTime, TVel> ToPolynomial(float alpha) {
            var ops = Ops;

            double DTFor(TPos pos1, TPos pos2, double orWhenZero) {
                var dt = Math.Pow(ops.Distance(pos1, pos2).AsMeters(), alpha);
                return dt < float.Epsilon ? orWhenZero : dt;
            }
            
            // Catmull-Rom splines are a special case of cubic hermite splines where the tangents
            // at each segment endpoint are calculated by using the adjacent ("margin") control points.
            // P1 and P2 are the segment endpoints, P0 and P3 are the adjacent control points.
            // The tangents are m1 and m2.

            TDiff m1, m2;
            
            if (alpha == 0) { // uniform catmull-rom
                m1 = ops.Div(ops.Sub(P2, P0), 2);
                m2 = ops.Div(ops.Sub(P3, P1), 2);
            } else {
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

                m1 = TFor(P0, P1, P2, dt0, dt1);
                m2 = TFor(P1, P2, P3, dt1, dt2);
            }

            m1 = ops.Mul(m1, ops.Div(Duration, PrevDur));
            m2 = ops.Mul(m2, ops.Div(Duration, NextDur));
            
            return Polynomial.Cubic(
                a: P1,
                b: m1,
                c: ops.Sub(ops.Mul(ops.Sub(P2, P1), 3), ops.Add(ops.Mul(m1, 2), m2)),
                d: ops.Add(ops.Mul(ops.Sub(P1, P2), 2), ops.Add(m1, m2)),
                Duration,
                ops
            );
        }

    }

    internal static class CatmullRomSegment {
        public const double EndOfSplineOvershootTolerance = 0.01;
        
        /// <summary>
        /// Given a SplineNode and a normalized location relative to that node,
        /// this method returns the four nodes around that location that are needed
        /// to calculate the position / tangent / etc. of that location on the spline.
        /// 
        /// The given location will be between the nodes P1 and P2 of the returned segment.
        /// The exact position between P1 and P2 is determined by the returned NormalizedOvershoot:
        /// 0.0f is at P1, 1.0f is at P2 and the values in between are lerped.
        /// </summary>
        public static (CatmullRomSegment<TPos, TDiff, TTime, TVel> Segment, double NormalizedOvershoot)? 
            CatmullRomSegmentAt<TPos, TDiff, TTime, TVel>(
                CatmullRomSpline<TPos, TDiff, TTime, TVel> spline, 
                NormalizedSplineLocation location
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            if(spline == null)
                throw new ArgumentNullException(nameof(spline));
            
            if(double.IsNaN(location.Value))
                throw new ArgumentException("The spline location is NaN", nameof(location));
                
            if (location.Value < 0 || location.Value > spline.SegmentCount + EndOfSplineOvershootTolerance)
                return null;
            
            var (s0, overshoot) = location.Value >= spline.SegmentCount
                // The location was almost at, or slightly above the end of the spline
                // but within tolerance. The used segment automatically
                // becomes the last valid catmull rom segment.
                ? (SplineHandleIndex.At(spline.SegmentCount - 1), 1.0f)
                // Otherwise the location is simply converted to a handle index and overshoot
                : location.AsHandleIndex();

            var dur = spline.Ops.Sub(spline.TemporalSegmentOffsets[s0 + 1], spline.TemporalSegmentOffsets[s0]);
            
            return (new CatmullRomSegment<TPos, TDiff, TTime, TVel>(
                spline[s0],
                spline[s0 + 1],
                spline[s0 + 2],
                spline[s0 + 3],
                dur,
                s0 == 0 ? dur : spline.Ops.Sub(spline.TemporalSegmentOffsets[s0], spline.TemporalSegmentOffsets[s0 - 1]),
                s0 == spline.SegmentCount ? dur : spline.Ops.Sub(spline.TemporalSegmentOffsets[s0 + 1], spline.TemporalSegmentOffsets[s0]),
                spline.Ops
            ), overshoot);
        }
    }
}
