using System.Diagnostics.Contracts;

namespace BII.WasaBii.Splines {
    
    public static class SplineUtils {
        
        [Pure]
        public static SplineSample<TPos, TDiff>? TryQuery<TPos, TDiff>(
            this Spline<TPos, TDiff> spline, NormalizedSplineLocation location
        ) where TPos : struct where TDiff : struct => SplineSample<TPos, TDiff>.From(spline, location);

        [Pure]
        public static SplineSample<TPos, TDiff>? TryQuery<TPos, TDiff>(
            this Spline<TPos, TDiff> spline, SplineLocation location
        ) where TPos : struct where TDiff : struct => SplineSample<TPos, TDiff>.From(spline, location);
        
    }
    
}