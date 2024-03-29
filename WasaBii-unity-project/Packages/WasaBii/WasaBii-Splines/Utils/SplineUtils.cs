﻿using System.Diagnostics.Contracts;
using BII.WasaBii.Core;

namespace BII.WasaBii.Splines {
    
    public static class SplineUtils {
        
        [Pure]
        public static Option<SplineSample<TPos, TDiff>> TryQuery<TPos, TDiff>(
            this Spline<TPos, TDiff> spline, NormalizedSplineLocation location
        ) where TPos : unmanaged where TDiff : unmanaged => SplineSample<TPos, TDiff>.From(spline, location);

        [Pure]
        public static Option<SplineSample<TPos, TDiff>> TryQuery<TPos, TDiff>(
            this Spline<TPos, TDiff> spline, SplineLocation location
        ) where TPos : unmanaged where TDiff : unmanaged => SplineSample<TPos, TDiff>.From(spline, location);
        
    }
    
}