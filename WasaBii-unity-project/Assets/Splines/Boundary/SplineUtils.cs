using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace BII.WasaBii.Splines {
    
    public static class SplineUtils {
        public static bool IsValid<TPos, TDiff>(this Spline<TPos, TDiff> spline)
            where TPos : struct where TDiff : struct =>
            spline.HandleCountIncludingMargin >= 4;

        internal static T WhenValidOrThrow<T, TPos, TDiff>(this Spline<TPos, TDiff> spline, Func<Spline<TPos, TDiff>, T> resultGetter) 
            where TPos : struct where TDiff : struct =>
            spline.IsValid() ? resultGetter(spline) : throw new InvalidSplineException<TPos, TDiff>(spline, "Not enough handles");

        public static int SegmentCount<TPos, TDiff>(this Spline<TPos, TDiff> spline) 
            where TPos : struct where TDiff : struct =>
            spline.IsValid() ? spline.HandleCountIncludingMargin - 3 : 0;

        public static bool TryQuery<TPos, TDiff>(
            this Spline<TPos, TDiff> spline, NormalizedSplineLocation location, out SplineSample<TPos, TDiff> sample
        ) where TPos : struct where TDiff : struct {
            var res = SplineSample<TPos, TDiff>.From(spline, location);
            sample = res ?? new SplineSample<TPos, TDiff>();
            return res != null;
        }

        public static bool TryQuery<TPos, TDiff>(
            this Spline<TPos, TDiff> spline, SplineLocation location, out SplineSample<TPos, TDiff> sample
        ) where TPos : struct where TDiff : struct {
            var res = SplineSample<TPos, TDiff>.From(spline, location);
            sample = res ?? new SplineSample<TPos, TDiff>();
            return res != null;
        }
        
        /// Returns all the positions of spline handles that are between the given locations on the spline.
        /// The positions of the locations on the spline themselves are included at begin and end.
        /// This is a more performant way to sample a spline used for spline driving than the methods in
        /// <see cref="SplineSampleExtensions"/>.
        /// This is because sampling has to normalize the locations for every sample made,
        /// while this operation is only done twice here.
        [Pure]
        public static IEnumerable<TPos> HandlesBetween<TPos, TDiff>(
            this Spline<TPos, TDiff> spline, SplineLocation start, SplineLocation end
        ) where TPos : struct where TDiff : struct {
            var fromNormalized = spline.NormalizedLocation(start);
            var toNormalized = spline.NormalizedLocation(end);

            yield return spline[fromNormalized].Position;

            if (fromNormalized > toNormalized) {
                // Iterate from end of spline to begin
                for (var nodeIndex = spline.HandleCount - 1; nodeIndex >= 0; --nodeIndex)
                    if (nodeIndex < fromNormalized && nodeIndex > toNormalized)
                        yield return spline.Handles[nodeIndex];
            } else {
                // Iterate from begin of spline to end
                for (var nodeIndex = 0; nodeIndex < spline.HandleCount; ++nodeIndex)
                    if (nodeIndex > fromNormalized && nodeIndex < toNormalized)
                        yield return spline.Handles[nodeIndex];
            }

            yield return spline[toNormalized].Position;
        }

        public static TPos BeginMarginHandle<TPos, TDiff>(this Spline<TPos, TDiff> spline)
            where TPos : struct where TDiff : struct =>
            spline.WhenValidOrThrow(s => s.HandlesIncludingMargin[0]);

        public static TPos FirstHandle<TPos, TDiff>(this Spline<TPos, TDiff> spline)
            where TPos : struct where TDiff : struct =>
            spline.WhenValidOrThrow(s => s.Handles[0]);

        public static TPos LastHandle<TPos, TDiff>(this Spline<TPos, TDiff> spline)
            where TPos : struct where TDiff : struct =>
            spline.WhenValidOrThrow(s => s.Handles[^1]);

        public static TPos EndMarginHandle<TPos, TDiff>(this Spline<TPos, TDiff> spline)
            where TPos : struct where TDiff : struct =>
            spline.WhenValidOrThrow(s => s.HandlesIncludingMargin[^1]);

    }
    
}