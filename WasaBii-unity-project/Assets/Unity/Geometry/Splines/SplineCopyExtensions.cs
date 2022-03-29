using BII.WasaBii.CatmullRomSplines;
using BII.WasaBii.Units;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry.Splines {
    
    using UnitySpline = Spline<Vector3, Vector3>;
    using LocalSpline = Spline<LocalPosition, LocalOffset>;
    using GlobalSpline = Spline<GlobalPosition, GlobalOffset>;
    
    /// A non-generic addition to <see cref="CatmullRomSplines.SplineCopyExtensions"/> for local,
    /// global and Unity splines.
    public static class SplineCopyExtensions {

        /// Creates a new spline with a similar trajectory as <paramref name="original"/>, but with all handle
        /// positions being moved by a certain offset to the right of the tangent at that point.
        public static UnitySpline CopyWithOffsetToTheRight(this UnitySpline original, Length diff, Vector3? up = null)
            => original.CopyWithOffset(tangent =>
                (float)diff.AsMeters() * Vector3.Cross(tangent, up ?? Vector3.up).normalized);
        
        /// <inheritdoc cref="CopyWithOffsetToTheRight(UnitySpline,Length,Vector3?)"/>
        public static LocalSpline CopyWithOffsetToTheRight(this LocalSpline original, Length diff, LocalDirection? up = null)
            => original.CopyWithOffset(tangent =>
                diff * tangent.Normalized.Cross(up ?? LocalDirection.Up));
        
        /// <inheritdoc cref="CopyWithOffsetToTheRight(UnitySpline,Length,Vector3?)"/>
        public static GlobalSpline CopyWithOffsetToTheRight(this GlobalSpline original, Length diff, GlobalDirection? up = null)
            => original.CopyWithOffset(tangent =>
                diff * tangent.Normalized.Cross(up ?? GlobalDirection.Up));
        
    }
    
}