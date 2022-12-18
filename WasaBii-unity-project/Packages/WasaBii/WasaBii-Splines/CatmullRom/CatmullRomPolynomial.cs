using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;

namespace BII.WasaBii.Splines.CatmullRom {
    
    internal static class CatmullRomPolynomial {
        
        [Pure]
        internal static Option<Polynomial<TPos, TDiff>> FromSplineAt<TPos, TDiff>(CatmullRomSpline<TPos, TDiff> spline, SplineSegmentIndex idx) 
        where TPos : unmanaged 
        where TDiff : unmanaged => 
            CatmullRomSegment.CatmullRomSegmentAt(spline, NormalizedSplineLocation.From(idx)) is { Segment: var segment } 
                ? segment.ToPolynomial(spline.Type.ToAlpha()) 
                : Option.None;

    }
    
}