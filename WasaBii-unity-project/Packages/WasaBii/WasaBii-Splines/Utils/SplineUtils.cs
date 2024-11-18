using System;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;

namespace BII.WasaBii.Splines {
    
    public static class SplineUtils {
        
        [Pure]
        public static Option<SplineSample<TPos, TDiff, TTime, TVel>> TryQuery<TPos, TDiff, TTime, TVel>(
            this Spline<TPos, TDiff, TTime, TVel> spline, NormalizedSplineLocation location
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged => 
            SplineSample<TPos, TDiff, TTime, TVel>.From(spline, location);

        [Pure]
        public static Option<SplineSample<TPos, TDiff, TTime, TVel>> TryQuery<TPos, TDiff, TTime, TVel>(
            this Spline<TPos, TDiff, TTime, TVel> spline, SplineLocation location
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged => 
            SplineSample<TPos, TDiff, TTime, TVel>.From(spline, location);
        
    }
    
}