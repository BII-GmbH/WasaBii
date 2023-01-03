using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// <summary>
    /// An AABB (axis-aligned bounding-box) in some local space.
    /// </summary>
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent:true, hasMagnitude:false, hasOrientation:false)]
    public partial struct LocalBounds : IsLocalVariant<LocalBounds, GlobalBounds> {
        
        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        #endif
        public LocalPosition Center { get; private set; }
        
        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        #endif
        public LocalOffset Size { get; private set; }

        public LocalOffset Extends => Size * 0.5;

        public LocalPosition BottomCenter => Center.WithY(Center.Y - Size.Y * 0.5);
        public LocalPosition TopCenter => Center.WithY(Center.Y + Size.Y * 0.5);

        public LocalPosition Min => Center - Extends;
        public LocalPosition Max => Center + Extends;

        public Volume Volume => Size.X * Size.Y * Size.Z;

        public LocalBounds(LocalPosition min, LocalPosition max) : 
            this(center: max.LerpTo(min, 0.5f), size: max - min) { }

        public LocalBounds(LocalPosition center, LocalOffset size) {
            Center = center;
            Size = size.Map(MathF.Abs);
        }

        /// <summary>
        /// Returns the smallest possible bounds in global space that completely wraps <see cref="this"/>.
        /// Will be larger than the original if rotations of any angles other than 90°-multiples
        /// are involved.
        /// This is the semi-inverse of <see cref="GlobalBounds.RelativeTo"/>.
        /// </summary>
        /// <example> <code>global.RelativeTo(parent).ToGlobalWith(parent) ~= global</code> </example>
        [Pure] public GlobalBounds ToGlobalWith(TransformProvider parent)
            => this.Vertices().Select(p => p.ToGlobalWith(parent)).Bounds();

        public GlobalBounds ToGlobalWithWorldZero => new(Center.ToGlobalWithWorldZero, Size.ToGlobalWithWorldZero);

        [Pure] public LocalBounds TransformBy(LocalPose offset) 
            => this.Vertices().Select(p => p.TransformBy(offset)).Bounds();
        
        [Pure] public bool Contains(LocalPosition point) 
            => point.X.IsInsideInterval(Min.X, Max.X, inclusive: true)
            && point.Y.IsInsideInterval(Min.Y, Max.Y, inclusive: true)
            && point.Z.IsInsideInterval(Min.Z, Max.Z, inclusive: true);

        [Pure] public LocalBounds LerpTo(LocalBounds target, double progress, bool shouldClamp) => new(
            Center.LerpTo(target.Center, progress, shouldClamp),
            Size.LerpTo(target.Size, progress, shouldClamp)
        );

        [Pure] public LocalBounds SlerpTo(LocalBounds target, double progress, bool shouldClamp) => new(
            Center.AsOffset.SlerpTo(target.Center.AsOffset, progress, shouldClamp).AsPosition,
            Size.SlerpTo(target.Size, progress, shouldClamp)
        );

    }
    

    public static partial class LocalBoundsExtensions {
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static LocalBounds AsLocalBounds(this UnityEngine.Bounds b) => 
            new(b.center.AsLocalPosition(), b.size.AsLocalOffset());
        
        [Pure] public static UnityEngine.Bounds AsUnityBounds(this LocalBounds bounds) => new(
            center: bounds.Center.AsUnityVector,
            size: bounds.Size.AsUnityVector
        );
        #endif

        [Pure] public static LocalBounds Encapsulating(this LocalBounds a, LocalBounds b) =>
            new(
                a.Min.Min(b.Min),
                a.Max.Max(b.Max)
            );

        [Pure] public static LocalBounds Encapsulating(this LocalBounds bounds, LocalPosition point) => 
            new(bounds.Min.Min(point), bounds.Max.Max(point));

        [Pure] public static LocalBounds Bounds(this IEnumerable<LocalPosition> vertices) {
            var min = new LocalPosition(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new LocalPosition(float.MinValue, float.MinValue, float.MinValue);
            foreach (var vertex in vertices) {
                min = min.Min(vertex);
                max = max.Max(vertex);
            }
            return new(min, max);
        }

        [Pure] public static IEnumerable<LocalPosition> Vertices(this LocalBounds localBounds) {
            var min = localBounds.Min;
            var max = localBounds.Max;
            yield return min;
            yield return min.WithX(max.X);
            yield return min.WithY(max.Y);
            yield return min.WithZ(max.Z);
            yield return max.WithX(min.X);
            yield return max.WithY(min.Y);
            yield return max.WithZ(min.Z);
            yield return max;
        }

    }
}