using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using BII.WasaBii.Units;

namespace BII.WasaBii.Splines.Logic {
    /// When querying positions, tangents etc on a spline, the parameters t or location can be used
    /// t is a normalized parameter, which means that the nodes of the spline are at 0, 1, 2, ..
    /// and 0.5 is for instance in the middle between node 0 and 1
    /// Location is denormalized, which means that it is the position location units away from the start node
    /// along the spline
    /// 
    /// This class contains function for converting from t to location and back
    /// 
    /// In general, using t is more performant, especially on splines with many nodes
    /// But location is generally used more
    public static class SplineNormalizationUtility {
        /// The amount of measurements taken when normalizing / denormalizing values
        /// Higher values yield higher accuracy at the cost of performance
        /// 
        /// Samples are needed during the (de)norminalization process, since in both cases
        /// the length of the spline is needed as an operation.
        /// Since calculating the length of a spline is only ever an approximation, a sample rate is needed
        public const int DefaultNormalizationSamples = 10;

        /// Normalizing a spline location to calculate the normalized spline location for a given spline
        /// is normally not possible when the location is above the spline's length.
        /// This is the tolerance the location can be above the length and to be considered
        /// to exactly match the length of the spline.
        ///
        /// Such a threshold is necessary since the normalization algorithm is inherently inaccurate
        /// because calculating a spline's length is always an approximation of its actual length.
        private static readonly SplineLocation splineLocationOvershootTolerance = 0.1f.Meters();

        /// Converts a location on the spline from <see cref="SplineLocation"/>
        /// to <see cref="NormalizedSplineLocation"/>.
        /// <br/>
        /// Such a conversion is desirable when performance is relevant,
        /// since operations on <see cref="NormalizedSplineLocation"/> are faster.
        public static NormalizedSplineLocation Normalize<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            SplineLocation location,
            int normalizationSamplesPerSegment = DefaultNormalizationSamples
        ) 
            where TPos : struct 
            where TDiff : struct {
            // Profiler.BeginSample("SplineNormalizationUtility.Normalize()");

            var currentSegmentIdx = SplineSegmentIndex.Zero;
            var remainingDistanceToLocation = location.DistanceFromBegin;

            // Iterate from each node and subtract its length from the location. 
            // Once the location is no longer greater than the node's length, 
            // the remaining location relative to the length is the normalized value t
            while (true) {
                if (currentSegmentIdx < spline.SegmentCount()) {
                    var segmentLength = spline[currentSegmentIdx].Length(normalizationSamplesPerSegment);
                    if (remainingDistanceToLocation > segmentLength) {
                        remainingDistanceToLocation -= segmentLength;
                        currentSegmentIdx += 1;
                    } else {

                        var progressToNextHandle = remainingDistanceToLocation switch {
                            var d when d.IsNearly(Length.Zero, threshold: 1E-3.Meters()) => 0d,
                            var d when d.IsNearly(segmentLength, threshold: 1E-3.Meters()) => 1d,
                            var d => d / segmentLength
                        };
                        
                        // Profiler.EndSample();
                        return NormalizedSplineLocation.From(currentSegmentIdx) + progressToNextHandle;
                    }
                } else {
                    // Profiler.EndSample();
                    
                    
                    if(remainingDistanceToLocation < splineLocationOvershootTolerance)
                        return NormalizedSplineLocation.From(currentSegmentIdx);
                    else // The spline location is outside the spline's length,
                         // so an out-of-range normalized spline location is returned 
                        return NormalizedSplineLocation.From(currentSegmentIdx + 1);
                }
            }
        }

        /// Converts a location on the spline from <see cref="NormalizedSplineLocation"/>
        /// to <see cref="SplineLocation"/>.
        /// <br/>
        /// Such a conversion is desirable when the location value needs to be interpreted,
        /// since <see cref="SplineLocation"/> is equal to the distance
        /// from the beginning of the spline to the location, in meters.
        public static SplineLocation DeNormalize<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            NormalizedSplineLocation t,
            int normalizationSamplesPerSegment = DefaultNormalizationSamples
        )  
            where TPos : struct 
            where TDiff : struct {
            // Profiler.BeginSample("SplineNormalizationUtility.DeNormalize()");

            var location = SplineLocation.Zero;
            var segmentIdx = SplineSegmentIndex.Zero;
            for (var remainingT = t; remainingT > 0; remainingT -= 1) {
                if (segmentIdx >= spline.SegmentCount())
                    break;
                
                var length = spline[segmentIdx].Length(normalizationSamplesPerSegment);
                segmentIdx += 1;
                location += length * Math.Min(1, remainingT);
            }

            // Profiler.EndSample();
            return location;
        }

        /// For a given node on a spline and locations relative to it,
        /// this method will normalize all of these locations and return them in the same order.
        /// However, the provided locations have to be sorted in ascending order,
        /// otherwise an exception will be thrown!
        /// The sorting has to be done by the caller beforehand,
        /// to avoid situations where points are returned in a different
        /// order than they were provided, leading to hard-to-understand bugs.
        /// 
        /// This method is a more performant alternative to <see cref="Normalize{TPos,TDiff}"/>
        /// when normalizing multiple locations at once.
        public static IEnumerable<NormalizedSplineLocation> BulkNormalizeOrdered<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            IEnumerable<SplineLocation> locations,
            int normalizationSamplesPerSegment = DefaultNormalizationSamples
        ) 
            where TPos : struct 
            where TDiff : struct {
            // Explanation of the algorithm:
            // To convert from an (absolute) spline location to a normalized spline location, two things are needed:
            // - The index of the segment the position is in
            // - The progress within this segment
            //
            // To compute both of them, the length of all segments
            // leading up to the contained segment are needed.
            // These are usually re-computed every time when
            // calling <see cref="Normalize"/> with multiple locations.
            //
            // This algorithm is designed so that the length of each
            // segment is only computed once, even for multiple locations.
            // This is possible because the provided locations
            // have to be sorted in ascending order.
            //
            // To calculate a normalized location, this method increments
            // the index of the current segment until the location
            // to be converted is within the current segment.
            // Then the progress within that segment is added to the index,
            // and the result is the Normalized location.

            // Profiler.BeginSample(
            //     $"SplineNormalizationUtility.BulkNormalizeOrdered(" +
            //     $"normalizationSamplesPerSegment = {normalizationSamplesPerSegment})"
            // );

            Length segmentLengthAt(SplineSegmentIndex idx) => spline[idx].Length(normalizationSamplesPerSegment);

            var currentSegmentIndex = SplineSegmentIndex.Zero;
            var segmentAbsoluteBegin = SplineLocation.Zero;
            var segmentAbsoluteEnd = (SplineLocation) segmentLengthAt(currentSegmentIndex);

            foreach (var current in locations) {
                var currentLocation = current;
                while (currentLocation > segmentAbsoluteEnd) {
                    if (currentSegmentIndex < spline.SegmentCount() - 1) {
                        // If we aren't in the last segment yet we advance the segment.
                        currentSegmentIndex += 1;
                        segmentAbsoluteBegin = segmentAbsoluteEnd;
                        segmentAbsoluteEnd += segmentLengthAt(currentSegmentIndex);
                    
                    }else if (currentLocation - segmentAbsoluteEnd < splineLocationOvershootTolerance) {
                        // If it is the last segment, but we are within overshoot tolerance
                        // treat the current location as if it were in the last segment
                        currentLocation = segmentAbsoluteEnd;
                    } else {
                        var overshoot = currentLocation - segmentAbsoluteEnd;
                        // If neither is true, this is an error.
                        throw new ArgumentOutOfRangeException(
                            nameof(locations), 
                            $"The location {currentLocation} is outside of the spline's" +
                            $" length {spline.Length()} and cannot be normalized! Tolerance overshoot: {overshoot.Value.Meters()}"
                        );
                    }
                   
                }

                if (currentLocation < segmentAbsoluteBegin)
                    throw new ArgumentException(
                        $"The locations given to {nameof(BulkNormalizeOrdered)} must " +
                        $"be ordered in ascending order, but were not!"
                    );

                yield return NormalizedSplineLocation.From(
                    Mathd.InverseLerp(segmentAbsoluteBegin, segmentAbsoluteEnd, current)
                ) + currentSegmentIndex;
            }

            // Profiler.EndSample();
        }
    }
}