using BII.WasaBii.Splines;
using BII.WasaBii.Units;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry.Splines {
    
    /// A non-generic addition to <see cref="Splines.SplineCopyExtensions"/> for local,
    /// global and Unity splines.
    public static class SplineCopyExtensions {

        /// Creates a new spline with a similar trajectory as <paramref name="original"/>, but with all handle
        /// positions being moved by a certain offset to the right of the tangent at that point.
        public static Spline<Vector3, Vector3> CopyWithOffsetToTheRight(
            this Spline<Vector3, Vector3> original, Length diff, Vector3? up = null
        ) => original.CopyWithOffset(tangent =>
            (float)diff.AsMeters() * tangent.Cross(up ?? Vector3.up).normalized);
        
        /// <inheritdoc cref="CopyWithOffsetToTheRight(Spline{Vector3, Vector3},Length,Vector3?)"/>
        public static Spline<LocalPosition, LocalOffset> CopyWithOffsetToTheRight(
            this Spline<LocalPosition, LocalOffset> original, Length diff, LocalDirection? up = null
        ) => original.CopyWithOffset(tangent =>
            diff * tangent.Normalized.Cross(up ?? LocalDirection.Up));
        
        /// <inheritdoc cref="CopyWithOffsetToTheRight(Spline{Vector3, Vector3},Length,Vector3?)"/>
        public static Spline<GlobalPosition, GlobalOffset> CopyWithOffsetToTheRight(
            this Spline<GlobalPosition, GlobalOffset> original, Length diff, GlobalDirection? up = null
        ) => original.CopyWithOffset(tangent =>
            diff * tangent.Normalized.Cross(up ?? GlobalDirection.Up));
        
    }
    
}