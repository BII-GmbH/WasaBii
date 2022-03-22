using System.Collections.Generic;
using static BII.CatmullRomSplines.Logic.SplineNormalizationUtility;

namespace BII.CatmullRomSplines {
    public static class SplineNormalizationExtensions {
        /// Converts a location on the spline from <see cref="SplineLocation"/>
        /// to <see cref="NormalizedSplineLocation"/>.
        /// <br/>
        /// Such a conversion is desirable when performance is relevant,
        /// since operations on <see cref="NormalizedSplineLocation"/> are faster.
        public static NormalizedSplineLocation NormalizedLocation<TPos, TDiff>(this Spline<TPos, TDiff> spline, SplineLocation location) 
            where TPos : struct 
            where TDiff : struct =>
            Normalize(
                spline,
                location
            );
        
        /// Converts multiple locations on the spline from <see cref="SplineLocation"/>
        /// to <see cref="NormalizedSplineLocation"/>.
        /// The provided locations have to be sorted in ascending order,
        /// otherwise an exception is thrown!
        /// The sorting has to be done by the caller beforehand,
        /// to avoid situations where points are returned in a different
        /// order than they were provided, leading to hard-to-understand bugs.
        /// <br/>
        /// Such a conversion is desirable when performance is relevant,
        /// since operations on <see cref="NormalizedSplineLocation"/> are faster.
        public static IEnumerable<NormalizedSplineLocation> BulkNormalizedLocationsOrdered<TPos, TDiff>(
            this Spline<TPos, TDiff> spline, IEnumerable<SplineLocation> locations
        ) 
            where TPos : struct 
            where TDiff : struct =>
            BulkNormalizeOrdered(
                spline,
                locations
            );

        /// Converts a location on the spline from <see cref="NormalizedSplineLocation"/>
        /// to <see cref="SplineLocation"/>.
        /// <br/>
        /// Such a conversion is desirable when the location value needs to be interpreted,
        /// since <see cref="SplineLocation"/> is equal to the distance
        /// from the beginning of the spline to the location, in meters.
        public static SplineLocation DeNormalizedLocation<TPos, TDiff>(this Spline<TPos, TDiff> spline, NormalizedSplineLocation location) 
            where TPos : struct 
            where TDiff : struct =>
            DeNormalize(
                spline,
                location
            );
    }
}