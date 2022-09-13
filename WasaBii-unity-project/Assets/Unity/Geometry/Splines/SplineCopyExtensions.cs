using BII.WasaBii.Splines.CatmullRom;
using BII.WasaBii.UnitSystem;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry.Splines {
    
    /// A non-generic addition to <see cref="CatmullRomSplineCopyExtensions"/> for local,
    /// global and Unity splines.
    public static class SplineCopyExtensions {

        /// Creates a new spline with a similar trajectory as <paramref name="original"/>, but with all handle
        /// positions being moved by a certain offset to the right of the tangent at that handle.
        public static CatmullRomSpline<Vector3, Vector3> CopyWithOffsetToTheRight(
            this CatmullRomSpline<Vector3, Vector3> original, Length diff, Vector3? up = null
        ) => original.CopyWithOffset(tangent =>
            (float)diff.AsMeters() * tangent.Cross(up ?? Vector3.up).normalized);
        
        /// <inheritdoc cref="CopyWithOffsetToTheRight(CatmullRomSpline{Vector3, Vector3},Length,Vector3?)"/>
        public static CatmullRomSpline<LocalPosition, LocalOffset> CopyWithOffsetToTheRight(
            this CatmullRomSpline<LocalPosition, LocalOffset> original, Length diff, LocalDirection? up = null
        ) => original.CopyWithOffset(tangent =>
            diff * tangent.Normalized.Cross(up ?? LocalDirection.Up));
        
        /// <inheritdoc cref="CopyWithOffsetToTheRight(CatmullRomSpline{Vector3, Vector3},Length,Vector3?)"/>
        public static CatmullRomSpline<GlobalPosition, GlobalOffset> CopyWithOffsetToTheRight(
            this CatmullRomSpline<GlobalPosition, GlobalOffset> original, Length diff, GlobalDirection? up = null
        ) => original.CopyWithOffset(tangent =>
            diff * tangent.Normalized.Cross(up ?? GlobalDirection.Up));
        
    }
    
}