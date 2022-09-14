#nullable enable

using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;
using Range = BII.WasaBii.Core.Range;

namespace BII.WasaBii.Splines {
    public static class SplineSampleExtensions {
        
        /// This method samples the positions on the entire spline.
        /// The sample rate is a defined amount between each segment of spline handles.
        /// Returns all sampled positions in order from the begin of the spline to its end.
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff>> SampleSplinePerSegment<TPos, TDiff>(this WithSpline<TPos, TDiff> withSpline, int samplesPerSegment) 
            where TPos : struct where TDiff : struct {
            var spline = withSpline.Spline;

            var fromLoc = NormalizedSplineLocation.Zero;
            var toLoc = NormalizedSplineLocation.From(spline.SegmentCount);
            
            return Range.From(fromLoc, inclusive: true)
                .To(toLoc, inclusive: true)
                .Sample(samplesPerSegment * spline.SegmentCount, NormalizedSplineLocation.Lerp)
                .Select(location => spline[location]);
        }

        /// Behaves similar to <see cref="SampleSplineBetween{TPos,TDiff}(Spline{TPos,TDiff},SplineLocation,SplineLocation,Length,int)"/>
        /// but the spline is always sampled along its entire length.
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff>> SampleSplineEvery<TPos, TDiff>(
            this WithSpline<TPos, TDiff> withSpline,
            Length desiredSampleLength,
            int minSegments = 2
        ) where TPos : struct where TDiff : struct 
            => withSpline.Spline.SampleSplineBetween(
                SplineLocation.Zero, 
                withSpline.Spline.Length,
                desiredSampleLength,
                minSegments
            );

        /// Behaves similar to <see cref="SampleSplineBetween{TPos,TDiff}(Spline{TPos,TDiff},SplineLocation,SplineLocation,int)"/>
        /// but the spline is always sampled along its entire length.
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff>> SampleSplineEvery<TPos, TDiff>(
            this WithSpline<TPos, TDiff> withSpline,
            int count
        ) where TPos : struct where TDiff : struct 
            => withSpline.Spline.SampleSplineBetween(
                SplineLocation.Zero, 
                withSpline.Spline.Length,
                count
            );
        
        /// Samples locations on the spline between <paramref name="fromAbsolute"/> to <paramref name="toAbsolute"/>. The returned samples
        /// will have the same distance between each other, which is approximately equal to the <paramref name="desiredSampleLength"/>.
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff>> SampleSplineBetween<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            SplineLocation fromAbsolute,
            SplineLocation toAbsolute,
            Length desiredSampleLength,
            int minSegments = 2
        ) where TPos : struct where TDiff : struct {
            
            if (desiredSampleLength <= Length.Zero)
                throw new ArgumentException($"The sampleLength cannot be 0 or smaller than 0 (was {desiredSampleLength})");

            var segments = Math.Max(minSegments, (int) Math.Ceiling((toAbsolute - fromAbsolute) / desiredSampleLength) + 1);

            return spline.SampleSplineBetween(fromAbsolute, toAbsolute, segments);
        }

        /// Samples locations on the spline between <paramref name="fromAbsolute"/> to <paramref name="toAbsolute"/>.
        /// <returns><see cref="count"/> uniformly distributed samples</returns>
        [Pure] public static IEnumerable<SplineSample<TPos, TDiff>> SampleSplineBetween<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            SplineLocation fromAbsolute,
            SplineLocation toAbsolute,
            int count
        ) where TPos : struct where TDiff : struct {
            
            var reverse = false;
            if (toAbsolute < fromAbsolute) {
                (toAbsolute, fromAbsolute) = (fromAbsolute, toAbsolute);
                reverse = true;
            }

            var locations = Range.From(fromAbsolute, inclusive: true)
                .To(toAbsolute, inclusive: true)
                .Sample(count, SplineLocation.Lerp);
                
            var result = spline.BulkNormalizeOrdered(locations)
                .Select(nl => spline[nl]);

            return reverse ? result.Reverse() : result;
        }

    }
}