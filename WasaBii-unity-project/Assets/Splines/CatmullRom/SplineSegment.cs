using System;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines.CatmullRom {
    
    public static class SplineSegment {
        
        [Pure]
        public static Option<SplineSegment<TPos, TDiff>> From<TPos, TDiff>(
            CatmullRomSpline<TPos, TDiff> spline, SplineSegmentIndex idx, Lazy<Length>? cachedSegmentLength = null
        ) where TPos : struct where TDiff : struct =>
            CatmullRomPolynomial.FromSplineAt(spline, idx)
                .Map(val => new SplineSegment<TPos, TDiff>(val, cachedSegmentLength));

    }
    
}