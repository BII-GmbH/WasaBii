using System.Collections.Generic;
using BII.WasaBii.Splines;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry.Splines {

    public static class UnitySpline {

        /// <inheritdoc cref="GenericSpline.FromInterpolating{TPos,TDiff}"/>
        [Pure]
        public static Spline<Vector3, Vector3> FromInterpolating(
            IEnumerable<Vector3> handles, SplineType? type = null
        ) => GenericSpline.FromInterpolating(handles, PositionOperations.Instance, type);
        
        /// <inheritdoc cref="GenericSpline.FromHandles{TPos,TDiff}"/>
        [Pure]
        public static Spline<Vector3, Vector3> FromHandles(
            Vector3 beginMarginHandle, 
            IEnumerable<Vector3> interpolatedHandles, 
            Vector3 endMarginHandle, 
            SplineType? type = null
        ) => GenericSpline.FromHandles(beginMarginHandle, interpolatedHandles, endMarginHandle, PositionOperations.Instance, type);

        /// <inheritdoc cref="GenericSpline.FromHandlesIncludingMargin{TPos,TDiff}"/>
        [Pure]
        public static Spline<Vector3, Vector3> FromHandlesIncludingMargin(
            IEnumerable<Vector3> allHandlesIncludingMargin,
            SplineType? type = null
        ) => GenericSpline.FromHandlesIncludingMargin(allHandlesIncludingMargin, PositionOperations.Instance, type);
        
#region Extensions
        /// <inheritdoc cref="BII.WasaBii.Splines.EnumerableToSplineExtensions.ToSplineOrThrow{TPos,TDiff}"/>
        [Pure]
        public static Spline<Vector3, Vector3> ToSplineOrThrow(this IEnumerable<Vector3> source, SplineType? splineType = null)
            => source.ToSplineOrThrow(PositionOperations.Instance, splineType);

        /// <inheritdoc cref="BII.WasaBii.Splines.EnumerableToSplineExtensions.ToSpline{TPos,TDiff}"/>
        [Pure]
        public static Option<Spline<Vector3, Vector3>> ToSpline(this IEnumerable<Vector3> source, SplineType? splineType = null)
            => source.ToSpline(PositionOperations.Instance, splineType);

        /// <inheritdoc cref="BII.WasaBii.Splines.EnumerableToSplineExtensions.ToSplineWithMarginHandlesOrThrow{TPos,TDiff}"/>
        [Pure]
        public static Spline<Vector3, Vector3> ToSplineWithMarginHandlesOrThrow(this IEnumerable<Vector3> source, SplineType? splineType = null)
            => source.ToSplineWithMarginHandlesOrThrow(PositionOperations.Instance, splineType);

        /// <inheritdoc cref="BII.WasaBii.Splines.EnumerableToSplineExtensions.CalculateSplineMarginHandles{TPos,TDiff}"/>
        [Pure]
        public static (Vector3 BeginHandle, Vector3 EndHandle) CalculateSplineMarginHandles(
            this IEnumerable<Vector3> handlePositions
        ) => handlePositions.CalculateSplineMarginHandles(PositionOperations.Instance);
#endregion
        
        [MustBeImmutable][MustBeSerializable]
        public sealed class PositionOperations : PositionOperations<Vector3, Vector3> {

            public static readonly PositionOperations Instance = new();
            
            private PositionOperations() { }

            public Length Distance(Vector3 p0, Vector3 p1) => p0.DistanceTo(p1).Meters();

            public Vector3 Sub(Vector3 p0, Vector3 p1) => p0 - p1;

            public Vector3 Add(Vector3 d1, Vector3 d2) => d1 + d2;

            public Vector3 Div(Vector3 diff, double d) => diff / (float)d;

            public Vector3 Mul(Vector3 diff, double f) => diff * (float)f;

            public double Dot(Vector3 a, Vector3 b) => Vector3.Dot(a, b);

        }
        
    }
    
}