using System;
using System.Collections.Generic;
using BII.Units;
using UnityEngine;
using UnityEngine.Profiling;

namespace BII.CatmullRomSplines {

    public static class EnumerableClosestOnSplineExtensions {
        
        /// <summary>
        /// Similar to <see cref="QueryClosestPositionOnSplinesTo{TWithSpline}"/>,
        /// but a non-nullable result is returned.
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
        public static ClosestOnSplineQueryResult<TWithSpline> QueryClosestPositionOnSplinesToOrThrow<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            Vector3 position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) where TWithSpline : class, WithSpline => splines.QueryClosestPositionOnSplinesTo(position, samples) ?? throw new ArgumentException(
            "All splines given to QueryGreedyClosestPositionOnSplinesTo were not valid and a query could therefore not be performed!"
        );

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
        public static ClosestOnSplineQueryResult<TWithSpline>? QueryClosestPositionOnSplinesTo<TWithSpline>(
            this IEnumerable<TWithSpline> splines,
            Vector3 position,
            int samples = ClosestOnSplineExtensions.DefaultClosestOnSplineSamples
        ) where TWithSpline : class, WithSpline => queryClosestPositionOnSplinesTo(
            splines,
            queryFunction: spline => spline.QueryClosestPositionOnSplineTo(position, samples)
        );
        
        private static ClosestOnSplineQueryResult<TWithSpline>? queryClosestPositionOnSplinesTo<TWithSpline>(
            IEnumerable<TWithSpline> splines, Func<TWithSpline, ClosestOnSplineQueryResult<TWithSpline>?> queryFunction
        ) where TWithSpline : WithSpline {
            Profiler.BeginSample("EnumerableClosestOnSplineExtensions.queryClosestPositionOnSplinesTo");
            var result = default(ClosestOnSplineQueryResult<TWithSpline>?);

            foreach (var spline in splines) {
                var queryResult = queryFunction(spline);
                if (queryResult.HasValue) {
                    if (queryResult.Value.Distance < (result?.Distance ?? Length.MaxValue)) {
                        result = queryResult;
                    }
                }
            }

            Profiler.EndSample();
            return result;
        }
    }
}