using System;
using System.Diagnostics.Contracts;

namespace BII.WasaBii.Splines {
    public static class ClosestOnSplineExtensions {
        public const int DefaultClosestOnSplineSamples = 5;

        /// <summary>
        /// Equal to <see cref="QueryClosestPositionOnSplineTo{TPos, TDiff}"/>,
        /// but a non-nullable result is returned.
        /// Throws when the provided spline is invalid.
        /// </summary>
        /// <param name="samples">
        /// Determines the accuracy of the query. Higher values lead to higher accuracy.
        /// However, the default value should be sufficient for all cases.
        /// </param>
        /// <remarks>
        /// This method should only be used on splines where the distance
        /// between pairs of handles is approximately the same.
        /// This is because the errorMarginNormalized is relative to the
        /// length of a segment between spline handles.
        /// Therefore differing distances between handles would lead to different
        /// querying accuracies on different points on the spline.
        /// </remarks>
        [Pure] public static ClosestOnSplineQueryResult<TPos, TDiff> QueryClosestPositionOnSplineToOrThrow<TPos, TDiff>(
            this WithSpline<TPos, TDiff> spline,
            TPos position,
            int samples = DefaultClosestOnSplineSamples
        ) where TPos : struct where TDiff : struct => 
            spline.QueryClosestPositionOnSplineTo(position, samples) ??
                throw new ArgumentException(
                    $"The spline given to {nameof(QueryClosestPositionOnSplineToOrThrow)} was not valid and a query could therefore not be performed!"
                );

        /// <summary>
        /// This function returns the closest location and position (with its distance to the provided position) 
        /// on the spline, relative to the provided position.
        /// It is greedy, which means that on heavily curved splines it will not find the global optimal solution
        /// </summary>
        /// <param name="samples">
        /// Determines the accuracy of the query. Higher values lead to higher accuracy.
        /// However, the default value should be sufficient for all cases.
        /// </param>
        /// <remarks>
        /// This method should only be used on splines where the distance
        /// between pairs of handles is approximately the same.
        /// This is because the errorMarginNormalized is relative to the
        /// length of a segment between spline handles.
        /// Therefore differing distances between handles would lead to different
        /// querying accuracies on different points on the spline.
        /// </remarks> 
        public static ClosestOnSplineQueryResult<TPos, TDiff>? QueryClosestPositionOnSplineTo<TPos, TDiff>(
            this WithSpline<TPos, TDiff> withSpline,
            TPos position,
            int samples = DefaultClosestOnSplineSamples
        ) where TPos : struct where TDiff : struct {

            var spline = withSpline.Spline;

            if (!spline.IsValid()) return null;

            // 0: The position is on the plane,
            // > 0: The position is above the plane (in the direction of the normal)
            // < 0: The position is below the plane (opposite direction of the normal)
            double compareToPlane(TPos planePosition, TDiff planeNormal) =>
                spline.Ops.Dot(spline.Ops.Sub(position, planePosition), planeNormal);

            ClosestOnSplineQueryResult<TPos, TDiff> computeResult(
                TPos closestPosition, NormalizedSplineLocation closestLocation
            ) => new(
                position,
                spline,
                closestPosition,
                closestLocation
            );

            var totalIntervals = spline.HandleCount - 1;
            var lower = 0;
            var upper = totalIntervals;

            // Binary search: We find the normalized spline location segment [lower, upper] where upper = lower + 1
            //                in which the queriedPosition is located.
            while (upper - lower > 1) {
                var currentLocation = (upper + lower) / 2; // Intentional integer result
                var (pos, tan) = spline[NormalizedSplineLocation.From(currentLocation)].PositionAndTangent;
                var comparison = compareToPlane(pos, tan);

                if (Math.Abs(comparison) < float.Epsilon) {
                    // Early exit: The query position is exactly on the plane of the current location,
                    //             therefore that location is the closest result
                    return computeResult(pos, NormalizedSplineLocation.From(currentLocation));
                } else if (comparison > 0) {
                    lower = SplineSegmentIndex.At(currentLocation);
                } else {
                    upper = SplineSegmentIndex.At(currentLocation);
                }
            }

            (TPos position, TDiff tangent) getPositionAndTangentAtNormalized(
                NormalizedSplineLocation location
            ) => spline[location].PositionAndTangent;

            // Edge case: If the queriedPosition is inside the first segment and comes before it, the closest location is 0
            if (lower == 0) {
                var (pos, tan) = getPositionAndTangentAtNormalized(NormalizedSplineLocation.From(lower));
                        

                if (compareToPlane(pos, tan) <= 0)
                    return computeResult(pos, NormalizedSplineLocation.From(lower));
            }

            // Edge case: If the queriedPosition is inside the last segment and comes after it, the closest location is totalSegments
            if (upper == totalIntervals) {
                var (pos, tan) = getPositionAndTangentAtNormalized(NormalizedSplineLocation.From(upper));

                if (compareToPlane(pos, tan) >= 0)
                    return computeResult(pos, NormalizedSplineLocation.From(upper));
            }

            var res = NormalizedSplineLocation.From(
                lower 
                + spline[SplineSegmentIndex.At(lower)].Polynomial.EvaluateClosestPointTo(position, samples)
            );

            return computeResult(spline[res].Position, res);
        }
    }
}