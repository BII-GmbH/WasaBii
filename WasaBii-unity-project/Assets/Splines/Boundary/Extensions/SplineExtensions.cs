using System;
using System.Linq;
using BII.Utilities.Independent.Maths;
using BII.Utilities.Unity;
using UnityEngine;

namespace BII.CatmullRomSplines {

    public class InvalidSplineException : Exception {
        public InvalidSplineException(Spline spline, string reason)
            : base($"The Spline {spline} is not valid because of {reason}") { }
        public InvalidSplineException(string context, Spline spline, string reason)
            : base($"{context}: The Spline {spline} is not valid because of {reason}") { }
    }

    // TODO JM for DG: this has been around since June 2020, it seems like this should have already happened -> #2152
    [Obsolete("Will be migrated by Daniel Götz after the first spline PR has been merged")]
    public static class SplineExtensions {

#region LastLocation

        public static NormalizedSplineLocation LastLocation(this Spline spline)
            => NormalizedSplineLocation.From(spline.HandleCount() - 1);
        
        public static SplineLocation LastDenormalizedLocation(this Spline spline)
            => SplineLocation.From(spline.Length());

#endregion
        
#region Position

        public static Vector3? PositionAt(this Spline spline, SplineLocation location) =>
            spline.TryQuery(location, out var res) ? res.Position : default(Vector3?);

        public static Vector3 PositionAtOrThrow(this Spline spline, SplineLocation location) =>
            spline[location].Position;

        public static Vector3? PositionAtNormalized(this Spline spline, NormalizedSplineLocation t) =>
            spline.TryQuery(t, out var res) ? res.Position : default(Vector3?);

        public static Vector3 PositionAtNormalizedOrThrow(this Spline spline, NormalizedSplineLocation t) =>
            spline[t].Position;
        
        public static Vector3? PositionAtPercentage(this Spline spline, double p) =>
            PositionAtNormalized(spline, NormalizedSplineLocation.From(Mathd.Lerp(0, spline.HandleCount() - 1, p)));

        #endregion

#region Tangent

        public static Vector3? TangentAt(this Spline spline, SplineLocation location) =>
            spline.TryQuery(location, out var res) ? res.Tangent : default(Vector3?);

        public static Vector3 TangentAtOrThrow(this Spline spline, SplineLocation location) =>
            spline[location].Tangent;

        public static Vector3? TangentAtNormalized(this Spline spline, NormalizedSplineLocation t) =>
            spline.TryQuery(t, out var res) ? res.Tangent : default(Vector3?);

        public static Vector3 TangentAtNormalizedOrThrow(this Spline spline, NormalizedSplineLocation t) =>
            spline[t].Tangent;

#endregion

#region Curvature

        public static Vector3? CurvatureAt(this Spline spline, SplineLocation location) =>
            spline.TryQuery(location, out var res) ? res.Curvature : default(Vector3?);

        public static Vector3 CurvatureAtOrThrow(this Spline spline, SplineLocation location) =>
            spline[location].Curvature;

        public static Vector3? CurvatureAtNormalized(this Spline spline, NormalizedSplineLocation t) =>
            spline.TryQuery(t, out var res) ? res.Curvature : default(Vector3?);

        public static Vector3 CurvatureAtNormalizedOrThrow(this Spline spline, NormalizedSplineLocation t) =>
            spline[t].Curvature;

#endregion

#region Position and Tangent

        public static (Vector3 Position, Vector3 Tangent)? PositionAndTangentAt(
            this Spline spline, SplineLocation location
        ) => spline.TryQuery(location, out var res) ? res.PositionAndTangent : default((Vector3, Vector3)?);

        public static (Vector3 Position, Vector3 Tangent) PositionAndTangentAtOrThrow(
            this Spline spline, SplineLocation location
        ) => spline[location].PositionAndTangent;
        
        public static (Vector3 Position, Vector3 Tangent)? PositionAndTangentAtNormalized(
            this Spline spline, NormalizedSplineLocation t
        ) =>
            spline.TryQuery(t, out var res) ? res.PositionAndTangent : default((Vector3, Vector3)?);
        
#endregion

#region Transformation of all Handles

        /// <summary> applies the transformation to all handles of the spline </summary>
        public static Spline Transformed(this Spline spline, TransformProvider transformation)
            => Splines.FromHandlesIncludingMargin(
                spline.HandlesIncludingMargin().Select(transformation.TransformPoint)
            );

        public static Spline RelativeTo(this Spline spline, Vector3 newOrigin)
            => spline.Transformed(TransformProvider.From(-newOrigin));

#endregion

#region SubSpline

        public static Spline SubSpline(this Spline spline, NormalizedSplineLocation? from = null, NormalizedSplineLocation? to = null) {
            from ??= NormalizedSplineLocation.Zero;
            to ??= spline.LastLocation();
            return spline.SubSpline(spline.DeNormalizedLocation(from.Value), spline.DeNormalizedLocation(to.Value));
        }

        public static Spline SubSpline(this Spline spline, SplineLocation? from = null, SplineLocation? to = null) {
            from ??= SplineLocation.Zero;
            to ??= spline.LastDenormalizedLocation();
            return Splines.FromInterpolating(spline.HandlesBetween(@from.Value, to.Value), TODO);
        }

#endregion
    }
}