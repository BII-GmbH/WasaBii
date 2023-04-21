#nullable enable

using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {
    public static class SplineSampleExtensions {
        
        /// This method samples the positions on the entire spline.
        /// The sample rate is a defined amount between each segment of spline handles.
        /// Returns all sampled positions in order from the begin of the spline to its end.
        /// The samples are not distributed equidistantly, meaning that the distance between two successive samples
        /// can vary, especially for splines where the segments vary in length and for some higher-order-segments.
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff>> SampleSplinePerSegment<TPos, TDiff>(
            this Spline<TPos, TDiff> spline, int samplesPerSegment
        ) where TPos : unmanaged where TDiff : unmanaged {

            var fromLoc = NormalizedSplineLocation.Zero;
            var toLoc = NormalizedSplineLocation.From(spline.SegmentCount);
            
            return SampleRange.From(fromLoc, inclusive: true)
                .To(toLoc, inclusive: true)
                .Sample(samplesPerSegment * spline.SegmentCount, NormalizedSplineLocation.Lerp)
                .Select(location => spline[location]);
        }

        /// Behaves similar to <see cref="SampleSplineBetween{TPos,TDiff}(Spline{TPos,TDiff},SplineLocation,SplineLocation,Length,int,bool)"/>
        /// but the spline is always sampled along its entire length.
        /// <param name="equidistant"> Whether the samples should be uniformly distributed with equal distances between them.
        /// This prevents samples "clumping together", which can happen especially with higher-order-curves. However,
        /// it is much more computationally intensive, so leaving this off is significantly faster.</param>
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff>> SampleSplineEvery<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            Length desiredSampleLength,
            int minSamples = 2,
            bool equidistant = false
        ) where TPos : unmanaged where TDiff : unmanaged 
            => spline.SampleSplineBetween(
                SplineLocation.Zero, 
                spline.Length(),
                desiredSampleLength,
                minSamples,
                equidistant
            );

        /// Behaves similar to <see cref="SampleSplineBetween{TPos,TDiff}(Spline{TPos,TDiff},SplineLocation,SplineLocation,int,bool)"/>
        /// but the spline is always sampled along its entire length.
        /// <param name="equidistant"> Whether the samples should be uniformly distributed with equal distances between them.
        /// This prevents samples "clumping together", which can happen especially with higher-order-curves. However,
        /// it is much more computationally intensive, so leaving this off is significantly faster.</param>
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff>> SampleSpline<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            int samples,
            bool equidistant = false
        ) where TPos : unmanaged where TDiff : unmanaged 
            => spline.SampleSplineBetween(
                SplineLocation.Zero, 
                spline.Length(),
                samples,
                equidistant
            );
        
        /// Samples locations on the spline between <paramref name="fromAbsolute"/> to <paramref name="toAbsolute"/>. The returned samples
        /// will have the same distance between each other, which is approximately equal to the <paramref name="desiredSampleLength"/>.
        /// <param name="equidistant"> Whether the samples should be uniformly distributed with equal distances between them.
        /// Turning this off can cause samples to "clump together" and thus deviate from the <paramref name="desiredSampleLength"/>,
        /// which can happen especially with higher-order-curves. However, it is much more computationally intensive,
        /// so turning this off is significantly faster.</param>
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff>> SampleSplineBetween<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            SplineLocation fromAbsolute,
            SplineLocation toAbsolute,
            Length desiredSampleLength,
            int minSamples = 2,
            bool equidistant = true
        ) where TPos : unmanaged where TDiff : unmanaged {
            
            if (desiredSampleLength <= Length.Zero)
                throw new ArgumentException($"The sampleLength cannot be 0 or smaller than 0 (was {desiredSampleLength})");

            var segments = Math.Max(minSamples, (int) Math.Ceiling((toAbsolute - fromAbsolute) / desiredSampleLength) + 1);

            return spline.SampleSplineBetween(fromAbsolute, toAbsolute, segments, equidistant);
        }

        /// Samples locations on the spline between <paramref name="fromAbsolute"/> to <paramref name="toAbsolute"/>.
        /// <returns><see cref="samples"/> uniformly distributed samples</returns>
        /// <param name="equidistant"> Whether the samples should be uniformly distributed with equal distances between them.
        /// This prevents samples "clumping together", which can happen especially with higher-order-curves. However,
        /// it is much more computationally intensive, so leaving this off is significantly faster.</param>
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff>> SampleSplineBetween<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            SplineLocation fromAbsolute,
            SplineLocation toAbsolute,
            int samples,
            bool equidistant = false
        ) where TPos : unmanaged where TDiff : unmanaged {
            
            var reverse = false;
            if (toAbsolute < fromAbsolute) {
                (toAbsolute, fromAbsolute) = (fromAbsolute, toAbsolute);
                reverse = true;
            }
            
            var sampleLocations = equidistant
                ? spline.BulkNormalizeOrdered(
                    SampleRange.From(fromAbsolute, inclusive: true)
                        .To(toAbsolute, inclusive: true)
                        .Sample(samples, SplineLocation.Lerp))
                : SampleRange.From(spline.Normalize(fromAbsolute).ResultOrThrow(error => error.AsException), inclusive: true)
                    .To(spline.Normalize(toAbsolute).ResultOrThrow(error => error.AsException), inclusive: true)
                    .Sample(samples, NormalizedSplineLocation.Lerp);
                
            var result = sampleLocations.Select(nl => spline[nl]);

            return reverse ? result.Reverse() : result;
        }

    }
}