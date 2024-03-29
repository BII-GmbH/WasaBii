using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;

namespace BII.WasaBii.Splines {

    public static class EnumerableClosestOnSplineExtensions {
        
        /// <summary>
        /// Finds the closest point to a given position on a list of splines.
        /// Greedy algorithm which does not always find the global optimum on heavily curved splines.
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
        [Pure] public static Option<(TWithSpline closestSpline, ClosestOnSplineQueryResult<TPos, TDiff> queryResult)> QueryClosestPositionOnSplinesTo<TWithSpline, TPos, TDiff>(
            this IEnumerable<TWithSpline> splines,
            Func<TWithSpline, Spline<TPos, TDiff>> splineSelector,
            TPos position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) where TPos : unmanaged where TDiff : unmanaged => 
            queryClosestPositionOnSplinesTo(
                splines,
                queryFunction: withSpline => splineSelector(withSpline).QueryClosestPositionOnSplineTo(position, samples)
            );

        /// <summary>
        /// Similar to <see cref="QueryClosestPositionOnSplinesTo{TWithSpline, TPos, TDiff}"/>,
        /// but a non-<see cref="Option"/>al result is returned.
        /// Throws when all provided splines are not valid.
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
        [Pure] public static (TWithSpline closestSpline, ClosestOnSplineQueryResult<TPos, TDiff> queryResult) QueryClosestPositionOnSplinesToOrThrow<TWithSpline, TPos, TDiff>(
            this IEnumerable<TWithSpline> splines,
            Func<TWithSpline, Spline<TPos, TDiff>> splineSelector,
            TPos position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        )where TPos : unmanaged where TDiff : unmanaged 
            => splines.QueryClosestPositionOnSplinesTo(splineSelector, position, samples).GetOrThrow(() => new ArgumentException(
                $"All splines given to {nameof(QueryClosestPositionOnSplinesToOrThrow)} were not valid and a query could therefore not be performed!"
            ));

        private static Option<(TWithSpline closestSpline, ClosestOnSplineQueryResult<TPos, TDiff> queryResult)> queryClosestPositionOnSplinesTo<TWithSpline, TPos, TDiff>(
            IEnumerable<TWithSpline> splines,
            Func<TWithSpline, Option<ClosestOnSplineQueryResult<TPos, TDiff>>> queryFunction
        ) where TPos : unmanaged where TDiff : unmanaged => 
            splines.Collect(spline => queryFunction(spline).Map(queryResult => (spline, queryResult)))
                .MinBy(t => t.queryResult.Distance);
    }
}