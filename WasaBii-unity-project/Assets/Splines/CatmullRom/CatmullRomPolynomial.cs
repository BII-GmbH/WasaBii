using System;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines.CatmullRom {
    
    internal static class CatmullRomPolynomial {
        
        [Pure]
        internal static Option<Polynomial<TPos, TDiff>> FromSplineAt<TPos, TDiff>(CatmullRomSpline<TPos, TDiff> spline, SplineSegmentIndex idx) 
        where TPos : struct 
        where TDiff : struct => 
            CatmullRomSegment.CatmullRomSegmentAt(spline, NormalizedSplineLocation.From(idx)) is { Segment: var segment } 
                ? FromSegment(segment, spline.Type.ToAlpha()) 
                : Option.None;

        [Pure]
        internal static Polynomial<TPos, TDiff> FromSegment<TPos, TDiff>(CatmullRomSegment<TPos, TDiff> segment, float alpha) 
        where TPos : struct 
        where TDiff : struct {
            var p0 = segment.P0;
            var p1 = segment.P1;
            var p2 = segment.P2;
            var p3 = segment.P3;

            var ops = segment.Ops;

            double DTFor(TPos pos1, TPos pos2, double orWhenZero) =>
                Math.Pow(ops.Distance(pos1, pos2).AsMeters(), alpha)
                    .If(dt => dt < float.Epsilon, _ => orWhenZero);

            var dt1 = DTFor(p1, p2, orWhenZero: 1.0f);
            var dt0 = DTFor(p0, p1, orWhenZero: dt1);
            var dt2 = DTFor(p2, p3, orWhenZero: dt1);
            
            TDiff TFor(TPos pa, TPos pb, TPos pc, double dta, double dtb) =>
                ops.Mul(ops.Add(
                    ops.Sub(
                        ops.Div(ops.Sub(pb, pa), dta), 
                        ops.Div(ops.Sub(pc, pa), dta + dtb)
                    ), 
                    ops.Div(ops.Sub(pc, pb), dtb)
                ), dt1);

            var t1 = TFor(p0, p1, p2, dt0, dt1);
            var t2 = TFor(p1, p2, p3, dt1, dt2);

            var poly = Polynomial.Cubic(
                a: p1,
                b: t1,
                c: ops.Sub(ops.Mul(ops.Sub(p2, p1), 3), ops.Mul(t1, 2), t2),
                d: ops.Add(ops.Mul(ops.Sub(p1, p2), 2), t1, t2),
                ops
            );

            return poly;
        }

    }
    
}