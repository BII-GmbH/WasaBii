using System.Linq;
using System;
using System.Collections.Generic;
using BII.Units;
using UnityEngine;
using UnityEngine.Profiling;

namespace BII.CatmullRomSplines {
    public static class SplineSampleExtensions {

        private const float roundingErrorThreshold = 1E-5f;

        /// <summary>
        /// Returns a new Spline with (nearly) identical "shape" and handles with uniform distance
        /// </summary>
        public static Spline Resample(this Spline spline, Length desiredHandleDistance)
            => Splines.FromInterpolating(spline.SampleSplineEvery(desiredHandleDistance), TODO);

        /// <summary>
        /// This method samples the positions on the entire spline.
        /// The sample rate is a defined amount between each segment of spline handles.
        /// Returns all sampled positions in order from the begin of the spline to its end.
        /// </summary>
        public static List<Vector3> SampleSplinePerSegment(this WithSpline withSpline, int samplesPerSegment) {
            var spline = withSpline.Spline;
            var handleCount = spline.HandleCount();
            var increment = 1f / samplesPerSegment;
            var points = new List<Vector3>();
            for (var location = NormalizedSplineLocation.Zero; location < handleCount - 1; location += increment) {
                var position = spline.PositionAtNormalized(location);
                if (position.HasValue)
                    points.Add(position.Value);
            }

            var lastNormalizedSplinePosition = NormalizedSplineLocation.From(handleCount - 1);
            points.Add(spline.PositionAtNormalizedOrThrow(lastNormalizedSplinePosition));
            return points;
        }

        /// <summary>
        /// This function behaves similar to <see cref="SampleSplineBetween"/>,
        /// but always returns both the position and tangent for each sampled location.
        /// </summary>
        public static List<(Vector3 Position, Vector3 Tangent)> SampleSplinePositionAndTangentBetween(
            this WithSpline withSpline,
            SplineLocation fromAbsolute,
            SplineLocation toAbsolute,
            Length desiredSampleLength,
            CalculateSegments calculateSegments = null
        ) => withSpline.Spline.applySampleFunctionBetween<(Vector3, Vector3)>(
            (s, l) => s.PositionAndTangentAtNormalized(l).Value,
            fromAbsolute,
            toAbsolute,
            desiredSampleLength,
            calculateSegments ?? divideEquidistantlyWithMin2Segments
        );

        /// <summary>
        /// Behaves similar to <see cref="SampleSplineBetween"/>,
        /// but the spline is always sampled along its entire length.
        /// </summary>
        public static List<Vector3> SampleSplineEvery(
            this WithSpline withSpline,
            Length desiredSampleLength,
            SampleType type = SampleType.Position,
            CalculateSegments calculateSegments = null
        ) => withSpline.Spline.SampleSplineBetween(
                SplineLocation.Zero, 
                withSpline.Spline.Length(),
                desiredSampleLength,
                type, calculateSegments
            );
        
        /// <summary>
        /// Samples locations on the spline between <paramref name="fromAbsolute"/> to <paramref name="toAbsolute"/>.
        /// The position, tanget or curvature at each of these locations
        /// is returned, depending on <paramref name="type"/>.
        /// The returned samples will have the same distance between them,
        /// which is approximately equal to the <paramref name="desiredSampleLength"/>.
        /// </summary>
        public static List<Vector3> SampleSplineBetween(
            this WithSpline withSpline,
            SplineLocation fromAbsolute,
            SplineLocation toAbsolute,
            Length desiredSampleLength,
            SampleType type = SampleType.Position,
            CalculateSegments calculateSegments = null
        ) {
            var spline = withSpline.Spline;
            switch (type) {
                case SampleType.Position:
                    return spline.applySampleFunctionBetween<Vector3>(
                        (s, l) => s.PositionAtNormalizedOrThrow(l),
                        fromAbsolute,
                        toAbsolute,
                        desiredSampleLength,
                        calculateSegments ?? divideEquidistantlyWithMin2Segments
                    );
                case SampleType.Tangent:
                    return spline.applySampleFunctionBetween<Vector3>(
                        (s, l) => s.TangentAtNormalizedOrThrow(l),
                        fromAbsolute,
                        toAbsolute,
                        desiredSampleLength,
                        calculateSegments ?? divideEquidistantlyWithMin2Segments
                    );
                case SampleType.Curvature:
                    return spline.applySampleFunctionBetween<Vector3>(
                        (s, l) => s.CurvatureAtNormalizedOrThrow(l),
                        fromAbsolute,
                        toAbsolute,
                        desiredSampleLength,
                        calculateSegments ?? divideEquidistantlyWithMin2Segments
                    );
                default:
                    throw new ArgumentException($"SampleType {type} is not supported by SampleSplineBetween");
            }
        }
        
        /// Divides the total length into equally long segments
        /// with approximately the length of the desiredSegmentLength.
        /// Returns the count and length of the segments.
        /// Returns one segment with the total length as segment length
        /// if desiredSampleLength was greater than totalLength.
        public static (Length SegmentLength, int Segments) DivideEquidistantly(
            Length totalLength, Length desiredSegmentLength
        ) {
            var samples = (int)Math.Round((totalLength / desiredSegmentLength));
            if (samples <= 0) samples = 1;
            var segmentLength = totalLength / samples;
            return (segmentLength, samples);
        }
        
        /// Same as <see cref="DivideEquidistantly" /> but it ensure that there are at least 2 segments
        /// (and thus 3 sampled positions / tangents / ...).
        /// This is useful when there should be a minimum of sampled points,
        /// such as when the samples are turned back into a spline.
        private static (Length SegmentLength, int Segments) divideEquidistantlyWithMin2Segments(
            Length totalLength, Length desiredSegmentLength
        ) {
            var (segmentLength, segments) = DivideEquidistantly(totalLength, desiredSegmentLength);
            
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
        private static List<T> applySampleFunctionBetween<T>(
            this Spline spline,
            Func<Spline, NormalizedSplineLocation, T> sampleFunction,
            SplineLocation fromAbsolute,
            SplineLocation toAbsolute,
            Length desiredSampleLength,
            CalculateSegments calculateSegments
        ) {
            Profiler.BeginSample("SplineSampleExtensions.applySampleFunctionBetween()");
            
            var reverse = false;
            if (toAbsolute < fromAbsolute) {
                var temp = toAbsolute;
                toAbsolute = fromAbsolute;
                fromAbsolute = temp;
                reverse = true;
            }

            if (desiredSampleLength <= Length.Zero) {
                Profiler.EndSample();
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

            var result = spline.BulkNormalizedLocationsOrdered(locations)
                .Select(nl => sampleFunction(spline, nl))
                .ToList();

            if (reverse)
                result.Reverse();

            Profiler.EndSample();
            return result;
        }

        public enum SampleType {
            Position = 0,
            Tangent,
            Curvature
        }
    
    }
}