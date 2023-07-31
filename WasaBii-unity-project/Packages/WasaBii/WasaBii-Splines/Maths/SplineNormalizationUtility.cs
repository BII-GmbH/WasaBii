using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        
        public sealed class SplineLocationOutOfRangeError : Exception
        {
            public readonly SplineLocation Location;
            public readonly Length SplineLength;
            public SplineLocationOutOfRangeError(SplineLocation location, Length splineLength) 
            : base($"Location must be between {Length.Zero} and {splineLength}, was {location}"){
                Location = location;
                SplineLength = splineLength;
            }

        }

        public sealed class NormalizedSplineLocationOutOfRangeError : Exception
        {
            public readonly NormalizedSplineLocation Location;
            public readonly NormalizedSplineLocation Max;
            public NormalizedSplineLocationOutOfRangeError(NormalizedSplineLocation location, NormalizedSplineLocation max) 
            : base($"Normalized location must be between {0} and {max.Value}, was {location.Value}") {
                Location = location;
                Max = max;
            }
        }

        /// Normalizing a spline location to calculate the normalized spline location for a given spline
        /// is normally not possible when the location is above the spline's length.
        /// This is the tolerance the location can be above the length and to be considered
        /// to exactly match the length of the spline.
        ///
        /// Such a threshold is necessary since the normalization algorithm is inherently inaccurate
        /// because calculating a spline's length is always an approximation of its actual length.
        public static readonly SplineLocation DefaultSplineLocationOvershootTolerance = 0.1.Meters();

        /// <summary>
        /// Converts a location on the spline from <see cref="SplineLocation"/>
        /// to <see cref="NormalizedSplineLocation"/>.
        /// Such a conversion is desirable when performance is relevant,
        /// since operations on <see cref="NormalizedSplineLocation"/> are faster.
        /// </summary>
        public static Result<NormalizedSplineLocation, SplineLocationOutOfRangeError> Normalize<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            SplineLocation location,
            SplineLocation? splineLocationOvershootTolerance = null
        ) 
            where TPos : unmanaged 
            where TDiff : unmanaged {

            var searchResult = spline.SegmentOffsetsFromBegin.BinarySearch(location);
            var segmentIndex = SplineSegmentIndex.At(
                searchResult > 0
                    // location is exactly at intersection of two segments -> result is index of segment that starts here
                    ? Math.Min(searchResult, spline.SegmentCount - 1)
                    // location is in a segment -> result is bitwise complement of next segment index
                    // or outside the spline -> complement of 0 or segment count
                    : Math.Max(0, ~searchResult - 1));
            
            var segment = spline[segmentIndex];
            var segmentLength = segment.Length;
            var remainingDistanceToLocation = location.Value - spline.SegmentOffsetsFromBegin[segmentIndex];

            var res = NormalizedSplineLocation.From(segmentIndex);
            return remainingDistanceToLocation switch {
                var d when d > Length.Zero && d < segmentLength => 
                    res + segment.Polynomial.LengthToProgress(d, cachedPolynomialLength: segmentLength),
                var d when d.IsNearly(Length.Zero, threshold: splineLocationOvershootTolerance ?? 1E-3.Meters()) => res,
                var d when d.IsNearly(segmentLength, threshold: splineLocationOvershootTolerance ?? 1E-3.Meters()) => res + 1,
                _ => new SplineLocationOutOfRangeError(location, spline.Length())
            };
        }

        /// <inheritdoc cref="Normalize{TPos,TDiff}"/>
        /// <exception cref="SplineLocationOutOfRangeError">When the queried <paramref name="location"/>
        /// does not lie on the spline within the <paramref name="splineLocationOvershootTolerance"/>,
        /// i.e. when it is less than <see cref="SplineLocation.Zero"/> or greater than the
        /// <paramref name="spline"/> length.</exception>
        public static NormalizedSplineLocation NormalizeOrThrow<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            SplineLocation location,
            SplineLocation? splineLocationOvershootTolerance = null
        ) where TPos : unmanaged where TDiff : unmanaged =>
            spline.Normalize(location, splineLocationOvershootTolerance).ResultOrThrow();

        /// <summary>
        /// Converts a location on the spline from <see cref="NormalizedSplineLocation"/>
        /// to <see cref="SplineLocation"/>.
        /// Such a conversion is desirable when the location value needs to be interpreted,
        /// since <see cref="SplineLocation"/> is equal to the distance
        /// from the beginning of the spline to the location, in meters.
        /// </summary>
        public static Result<SplineLocation, NormalizedSplineLocationOutOfRangeError> DeNormalize<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            NormalizedSplineLocation t,
            NormalizedSplineLocation? overshootTolerance = null
        )  
            where TPos : unmanaged 
            where TDiff : unmanaged {

            if (t < 0) {
                if (overshootTolerance is { } tolerance && -t <= tolerance)
                    t = NormalizedSplineLocation.Zero;
                else return new NormalizedSplineLocationOutOfRangeError(t, new(spline.SegmentCount));
            }

            if (t > spline.SegmentCount) {
                if (overshootTolerance is { } tolerance && t - spline.SegmentCount <= tolerance)
                    t = new (spline.SegmentCount);
                else return new NormalizedSplineLocationOutOfRangeError(t, new(spline.SegmentCount));
            }

            var segmentIndex = new SplineSegmentIndex(Math.Min((int)t.Value, spline.SegmentCount - 1));
            var location = spline.SegmentOffsetsFromBegin[segmentIndex];
            var progressInLastSegment = t.Value - segmentIndex;
            if (progressInLastSegment > double.Epsilon) {
                var lastSegment = spline[SplineSegmentIndex.At(segmentIndex)];
                location += lastSegment.Polynomial.ProgressToLength(progressInLastSegment);
            }

            return new SplineLocation(location);
        }

        /// <inheritdoc cref="DeNormalize"/>
        /// <exception cref="NormalizedSplineLocationOutOfRangeError">When the queried <paramref name="t"/>
        /// does not lie on the spline within the <paramref name="overshootTolerance"/>, i.e. when it is
        /// less than <see cref="NormalizedSplineLocation.Zero"/> or greater than the <paramref name="spline"/>
        /// <see cref="Spline{TPos,TDiff}.SegmentCount"/>.</exception>
        public static SplineLocation DeNormalizeOrThrow<TPos, TDiff>(
            this Spline<TPos, TDiff> spline,
            NormalizedSplineLocation t,
            NormalizedSplineLocation? overshootTolerance = null
        ) where TPos : unmanaged where TDiff : unmanaged =>
            spline.DeNormalize(t, overshootTolerance).ResultOrThrow();

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
                        // If neither is true, this is an error.
                        throw new SplineLocationOutOfRangeError(
                            currentLocation, spline.Length()
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