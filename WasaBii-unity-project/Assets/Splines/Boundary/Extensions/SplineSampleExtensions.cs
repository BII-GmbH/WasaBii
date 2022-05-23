﻿using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using BII.WasaBii.Splines.Logic;
using BII.WasaBii.Units;
#nullable enable

namespace BII.WasaBii.Splines {
    public static class SplineSampleExtensions {
        
        /// This method samples the positions on the entire spline.
        /// The sample rate is a defined amount between each segment of spline handles.
        /// Returns all sampled positions in order from the begin of the spline to its end.
        [Pure] public static List<TPos> SampleSplinePerSegment<TPos, TDiff>(this WithSpline<TPos, TDiff> withSpline, int samplesPerSegment) 
            where TPos : struct where TDiff : struct {
            var spline = withSpline.Spline;
            var handleCount = spline.HandleCount;
            var increment = 1f / samplesPerSegment;
            var points = new List<TPos>();
            for (var location = NormalizedSplineLocation.Zero; location < handleCount - 1; location += increment) {
                points.Add(spline[location].Position);
            }

            var lastNormalizedSplinePosition = NormalizedSplineLocation.From(handleCount - 1);
            points.Add(spline[lastNormalizedSplinePosition].Position);
            return points;
        }

        /// Behaves similar to <see cref="SampleSplineBetween{TPos,TDiff,TRes}"/>
        /// but the spline is always sampled along its entire length.
        [Pure] public static List<SplineSample<TPos, TDiff>> SampleSplineEvery<TPos, TDiff>(
            this WithSpline<TPos, TDiff> withSpline,
            Length desiredSampleLength,
            CalculateSegments? calculateSegments = null
        ) where TPos : struct where TDiff : struct 
            => withSpline.SampleSplineBetween(
                SplineLocation.Zero, 
                withSpline.Spline.Length(),
                desiredSampleLength,
                calculateSegments
            );
        
        /// Samples locations on the spline between <paramref name="fromAbsolute"/> to <paramref name="toAbsolute"/>.
        /// The position, tanget or curvature at each of these locations
        /// is returned, depending on <paramref name="type"/>.
        /// The returned samples will have the same distance between them,
        /// which is approximately equal to the <paramref name="desiredSampleLength"/>.
        [Pure] public static List<SplineSample<TPos, TDiff>> SampleSplineBetween<TPos, TDiff>(
            this WithSpline<TPos, TDiff> withSpline,
            SplineLocation fromAbsolute,
            SplineLocation toAbsolute,
            Length desiredSampleLength,
            CalculateSegments? calculateSegments = null
        ) where TPos : struct where TDiff : struct =>
            withSpline.Spline.applySampleFunctionBetween(
                (s, l) => s[l],
                fromAbsolute,
                toAbsolute,
                desiredSampleLength,
                calculateSegments ?? divideEquidistantlyWithMin2Segments
            );

        /// Divides the total length into equally long segments
        /// with approximately the length of the desiredSegmentLength.
        /// Returns the count and length of the segments.
        /// Returns one segment with the total length as segment length
        /// if desiredSampleLength was greater than totalLength.
        [Pure] private static (Length SegmentLength, int Segments) divideEquidistantly(
            Length totalLength, Length desiredSegmentLength
        ) {
            var samples = (int) Math.Max(1, Math.Round(totalLength / desiredSegmentLength));
            var segmentLength = totalLength / samples;
            return (segmentLength, samples);
        }
        
        /// Same as <see cref="divideEquidistantly" /> but it ensure that there are at least 2 segments
        /// (and thus 3 sampled positions / tangents / ...).
        /// This is useful when there should be a minimum of sampled points,
        /// such as when the samples are turned back into a spline.
        private static (Length SegmentLength, int Segments) divideEquidistantlyWithMin2Segments(
            Length totalLength, Length desiredSegmentLength
        ) {
            var (segmentLength, segments) = divideEquidistantly(totalLength, desiredSegmentLength);
            
            if (segments < 2) {
                segmentLength = totalLength / 2;
                segments = 2;
            }
            return (segmentLength, segments);
        }
        
        /// Enables to change how segments on a spline are calculated by their distance and the total segment amount
        public delegate (Length SegmentLength, int Segments) CalculateSegments(
            Length totalLength, Length desiredSegmentLength
        );
        private static List<T> applySampleFunctionBetween<TPos, TDiff, T>(
            this Spline<TPos, TDiff> spline,
            Func<Spline<TPos, TDiff>, NormalizedSplineLocation, T> sampleFunction,
            SplineLocation fromAbsolute,
            SplineLocation toAbsolute,
            Length desiredSampleLength,
            CalculateSegments calculateSegments
        ) where TPos : struct where TDiff : struct {
            
            var reverse = false;
            if (toAbsolute < fromAbsolute) {
                (toAbsolute, fromAbsolute) = (fromAbsolute, toAbsolute);
                reverse = true;
            }

            if (desiredSampleLength <= Length.Zero) {
                throw new ArgumentException($"The sampleLength cannot be 0 or smaller than 0 (was {desiredSampleLength})");
            }

            var (actualSampleLength, segments) = calculateSegments(
                (toAbsolute - fromAbsolute).DistanceFromBegin,
                desiredSampleLength
            );

            var locations = new List<SplineLocation>();
            for (var iteration = 0; iteration <= segments; ++iteration) {
                var location = SplineLocation.From(Math.Min(fromAbsolute + iteration * actualSampleLength, toAbsolute));
                locations.Add(location);
            }

            var result = spline.BulkNormalizeOrdered(locations)
                .Select(nl => sampleFunction(spline, nl))
                .ToList();

            if (reverse)
                result.Reverse();

            return result;
        }
    }
}