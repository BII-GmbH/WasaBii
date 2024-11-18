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
        
        /// <summary>
        /// This method samples the positions on the entire spline.
        /// The sample rate is a defined amount between each segment of spline handles.
        /// Returns all sampled positions in order from the beginning of the spline to its end.
        /// The samples are not distributed equidistantly, meaning that the distance between two successive samples
        /// can vary, especially for splines where the segments vary in length and for some higher-order-segments.
        /// </summary>
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff, TTime, TVel>> SampleSplinePerSegment<TPos, TDiff, TTime, TVel>(
            this Spline<TPos, TDiff, TTime, TVel> spline, int samplesPerSegment
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {

            var fromLoc = NormalizedSplineLocation.Zero;
            var toLoc = NormalizedSplineLocation.From(spline.SegmentCount);
            
            return SampleRange.From(fromLoc, inclusive: true)
                .To(toLoc, inclusive: true)
                .Sample(samplesPerSegment * spline.SegmentCount, NormalizedSplineLocation.Lerp)
                .Select(location => spline[location]);
        }

        /// <summary>
        /// Behaves similar to <see cref="SampleSplineBetween{TPos,TDiff,TTime,TVel}(Spline{TPos,TDiff,TTime,TVel},SplineLocation,SplineLocation,Length,int,bool)"/>
        /// but the spline is always sampled along its entire length.
        /// </summary>
        /// <param name="equidistant"> Whether the samples should be uniformly distributed with equal distances between them.
        /// This prevents samples "clumping together", which can happen especially with higher-order-curves. However,
        /// it is much more computationally intensive, so leaving this off is significantly faster.</param>
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff, TTime, TVel>> SampleSplineEvery<TPos, TDiff, TTime, TVel>(
            this Spline<TPos, TDiff, TTime, TVel> spline,
            Length desiredSampleLength,
            int minSamples = 2
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged => spline.SampleSplineBetween(
                SplineLocation.Zero, 
                spline.Length,
                desiredSampleLength,
                minSamples
            );

        /// <summary>
        /// Behaves similar to <see cref="SampleSplineBetween{TPos,TDiff}(Spline{TPos,TDiff},SplineLocation,SplineLocation,int,bool)"/>
        /// but the spline is always sampled along its entire length.
        /// </summary>
        /// <param name="equidistant"> Whether the samples should be uniformly distributed with equal distances between them.
        /// This prevents samples "clumping together", which can happen especially with higher-order-curves. However,
        /// it is much more computationally intensive, so leaving this off is significantly faster.</param>
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff, TTime, TVel>> SampleSpline<TPos, TDiff, TTime, TVel>(
            this Spline<TPos, TDiff, TTime, TVel> spline,
            int samples,
            bool equidistant = false
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged => spline.SampleSplineBetween(
                NormalizedSplineLocation.Zero, 
                NormalizedSplineLocation.From(spline.SegmentCount), 
                samples,
                equidistant
            );
        
        /// <summary>
        /// Samples locations on the spline between <paramref name="fromAbsolute"/> to <paramref name="toAbsolute"/>. The returned samples
        /// will have the same distance between each other, which is approximately equal to the <paramref name="desiredSampleLength"/>.\
        /// </summary>
        /// <param name="equidistant"> Whether the samples should be uniformly distributed with equal distances between them.
        /// Turning this off can cause samples to "clump together" and thus deviate from the <paramref name="desiredSampleLength"/>,
        /// which can happen especially with higher-order-curves. However, it is much more computationally intensive,
        /// so turning this off is significantly faster.</param>
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff, TTime, TVel>> SampleSplineBetween<TPos, TDiff, TTime, TVel>(
            this Spline<TPos, TDiff, TTime, TVel> spline,
            SplineLocation fromAbsolute,
            SplineLocation toAbsolute,
            Length desiredSampleLength,
            int minSamples = 2
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            
            if (desiredSampleLength <= Length.Zero)
                throw new ArgumentException($"The sampleLength cannot be 0 or smaller than 0 (was {desiredSampleLength})");

            var segments = Math.Max(minSamples, (int) Math.Ceiling((toAbsolute - fromAbsolute) / desiredSampleLength) + 1);

            return spline.SampleSplineBetween(fromAbsolute, toAbsolute, segments, equidistant: true);
        }

        /// <summary>
        /// Samples locations on the spline between <paramref name="fromAbsolute"/> to <paramref name="toAbsolute"/>.
        /// </summary>
        /// <param name="equidistant"> Whether the samples should be uniformly distributed with equal distances between them.
        /// This prevents samples "clumping together", which can happen especially with higher-order-curves. However,
        /// it is much more computationally intensive, so leaving this off is significantly faster.</param>
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff, TTime, TVel>> SampleSplineBetween<TPos, TDiff, TTime, TVel>(
            this Spline<TPos, TDiff, TTime, TVel> spline,
            SplineLocation fromAbsolute,
            SplineLocation toAbsolute,
            int samples,
            bool equidistant = false
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            if(!equidistant)
                return spline.SampleSplineBetween(spline.NormalizeOrThrow(fromAbsolute), spline.NormalizeOrThrow(toAbsolute), samples, equidistant: false);
            
            var reverse = false;
            if (toAbsolute < fromAbsolute) {
                (toAbsolute, fromAbsolute) = (fromAbsolute, toAbsolute);
                reverse = true;
            }
            
            var sampleLocations = spline.BulkNormalizeOrdered(
                SampleRange.From(fromAbsolute, inclusive: true).To(toAbsolute, inclusive: true)
                    .Sample(samples, SplineLocation.Lerp));
                
            var result = sampleLocations.Select(nl => spline[nl]);

            return reverse ? result.Reverse() : result;
        }

        /// <summary>
        /// Samples locations on the spline between <paramref name="from"/> to <paramref name="to"/>.
        /// </summary>
        /// <param name="equidistant"> Whether the samples should be uniformly distributed with equal distances between them.
        /// This prevents samples "clumping together", which can happen especially with higher-order-curves. However,
        /// it is much more computationally intensive, so leaving this off is significantly faster.</param>
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff, TTime, TVel>> SampleSplineBetween<TPos, TDiff, TTime, TVel>(
            this Spline<TPos, TDiff, TTime, TVel> spline,
            NormalizedSplineLocation from,
            NormalizedSplineLocation to,
            int samples,
            bool equidistant = false
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            if (equidistant) return spline.SampleSplineBetween(spline.DeNormalizeOrThrow(from), spline.DeNormalizeOrThrow(to), samples, equidistant: true);
            
            return SampleRange.From(from, inclusive: true).To(to, inclusive: true)
                .Sample(samples, NormalizedSplineLocation.Lerp)
                .Select(nl => spline[nl]);
        }

    }
}