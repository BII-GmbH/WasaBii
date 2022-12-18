using System;
using System.Collections.Generic;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines.Maths {
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
    internal static class SplineNormalizationUtility {
        /// Normalizing a spline location to calculate the normalized spline location for a given spline
        /// is normally not possible when the location is above the spline's length.
        /// This is the tolerance the location can be above the length and to be considered
        /// to exactly match the length of the spline.
        ///
        /// Such a threshold is necessary since the normalization algorithm is inherently inaccurate
        /// because calculating a spline's length is always an approximation of its actual length.
        public static readonly SplineLocation DefaultSplineLocationOvershootTolerance = 0.1.Meters();

        /// Converts a location on the spline from <see cref="SplineLocation"/>
        /// to <see cref="NormalizedSplineLocation"/>.
        /// Such a conversion is desirable when performance is relevant,
        /// since operations on <see cref="NormalizedSplineLocation"/> are faster.
        public static NormalizedSplineLocation Normalize<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            SplineLocation location,
            SplineLocation? splineLocationOvershootTolerance = null
        ) 
            where TPos : unmanaged 
            where TDiff : unmanaged {

            var currentSegmentIdx = SplineSegmentIndex.Zero;
            var remainingDistanceToLocation = location.Value;

            // Iterate from each node and subtract its length from the location. 
            // Once the location is no longer greater than the node's length, 
            // the remaining location relative to the length is the normalized value t
            while (currentSegmentIdx < spline.SegmentCount) {
                var segment = spline[currentSegmentIdx];
                var segmentLength = segment.Length;
                if (remainingDistanceToLocation > segmentLength) {
                    remainingDistanceToLocation -= segmentLength;
                    currentSegmentIdx += 1;
                } else {
                    var progressToNextHandle = remainingDistanceToLocation switch {
                        var d when d.IsNearly(Length.Zero, threshold: 1E-3.Meters()) => 0d,
                        var d when d.IsNearly(segmentLength, threshold: 1E-3.Meters()) => 1d,
                        var d => segment.Polynomial.LengthToProgress(d, cachedPolynomialLength: segmentLength)
                    };
                    
                    return NormalizedSplineLocation.From(currentSegmentIdx) + progressToNextHandle;
                }
            }
            return remainingDistanceToLocation < (splineLocationOvershootTolerance ?? DefaultSplineLocationOvershootTolerance)
                ? NormalizedSplineLocation.From(currentSegmentIdx)
                // The spline location is outside the spline's length,
                // so an out-of-range normalized spline location is returned 
                : NormalizedSplineLocation.From(currentSegmentIdx + 1);
        }

        /// Converts a location on the spline from <see cref="NormalizedSplineLocation"/>
        /// to <see cref="SplineLocation"/>.
        /// Such a conversion is desirable when the location value needs to be interpreted,
        /// since <see cref="SplineLocation"/> is equal to the distance
        /// from the beginning of the spline to the location, in meters.
        public static SplineLocation DeNormalize<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            NormalizedSplineLocation t
        )  
            where TPos : unmanaged 
            where TDiff : unmanaged {

            var location = SplineLocation.Zero;
            var lastSegmentIndex = MathD.FloorToInt(t.Value);
            for (var i = 0; i < lastSegmentIndex; i++) {
                location += spline[SplineSegmentIndex.At(i)].Length;
            }

            var progressInLastSegment = t - lastSegmentIndex;
            if (progressInLastSegment > double.Epsilon) {
                var lastSegment = spline[SplineSegmentIndex.At(lastSegmentIndex)];
                location += lastSegment.Polynomial.ProgressToLength(progressInLastSegment);
            }

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
            SplineLocation? splineLocationOvershootTolerance = null
        ) 
            where TPos : unmanaged 
            where TDiff : unmanaged {
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

            var currentSegmentIndex = SplineSegmentIndex.Zero;
            var segment = spline[currentSegmentIndex];
            var segmentAbsoluteBegin = SplineLocation.Zero;
            var segmentAbsoluteEnd = (SplineLocation) segment.Length;

            foreach (var current in locations) {
                var currentLocation = current;
                while (currentLocation > segmentAbsoluteEnd) {
                    if (currentSegmentIndex < spline.SegmentCount - 1) {
                        // If we aren't in the last segment yet we advance the segment.
                        currentSegmentIndex += 1;
                        segment = spline[currentSegmentIndex];
                        segmentAbsoluteBegin = segmentAbsoluteEnd;
                        segmentAbsoluteEnd += segment.Length;
                    
                    } else if (currentLocation - segmentAbsoluteEnd < (splineLocationOvershootTolerance ?? DefaultSplineLocationOvershootTolerance)) {
                        // If it is the last segment, but we are within overshoot tolerance
                        // treat the current location as if it were in the last segment
                        currentLocation = segmentAbsoluteEnd;
                    } else {
                        var overshoot = currentLocation - segmentAbsoluteEnd;
                        // If neither is true, this is an error.
                        throw new ArgumentOutOfRangeException(
                            nameof(locations), 
                            $"The location {currentLocation} is outside of the spline's" +
                            $" length ({spline.Length()}) and cannot be normalized! Tolerance overshoot: {overshoot.Value}"
                        );
                    }
                   
                }

                if (currentLocation < segmentAbsoluteBegin)
                    throw new ArgumentException(
                        $"The locations given to {nameof(BulkNormalizeOrdered)} must " +
                        $"be ordered in ascending order, but were not!"
                    );
                yield return NormalizedSplineLocation.From(
                    segment.Polynomial.LengthToProgress(current - segmentAbsoluteBegin, cachedPolynomialLength: segment.Length)
                ) + currentSegmentIndex;
            }
        }
    }
}