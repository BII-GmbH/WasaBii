using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Geometry {

    /// A Unity-Independent data structure representing an AABB (axis-aligned bounding-box) in some local space.
    [MustBeImmutable]
    [MustBeSerializable]
    public readonly struct LocalBounds : IsLocalVariant<LocalBounds, GlobalBounds>, GeometryHelper<LocalBounds> {
        
        public readonly LocalPosition Center;
        
        public readonly LocalOffset Size;
        public LocalOffset Extends => Size / 2.0f;

        public LocalPosition BottomCenter => Center.WithY(Center.Y() - Size.Y() / 2.0);
        public LocalPosition TopCenter => Center.WithY(Center.Y() + Size.Y() / 2.0);

        public LocalPosition Min => Center - Extends;
        public LocalPosition Max => Center + Extends;

        public Volume Volume => Size.X() * Size.Y() * Size.Z();
        
        [Pure] public static LocalBounds FromMinMax(LocalPosition min, LocalPosition max) => 
            new LocalBounds(max.LerpTo(min, 0.5f), (max - min));

        [Pure] public static LocalBounds FromCenterSize(LocalPosition center, LocalOffset size) => 
            new LocalBounds(center, size);

        private LocalBounds(LocalPosition center, LocalOffset size) {
            Center = center;
            Size = size.Map(vector => vector.Map(Mathf.Abs));
        }

        /// Returns the smallest possible bounds in global space that completely wraps <see cref="this"/>.
        /// Will most likely be larger than the original if rotations of any angles other than 90°-multiples
        /// are involved. 
        [Pure] public GlobalBounds ToGlobalWith(TransformProvider parent)
            => this.Vertices().Select(p => p.ToGlobalWith(parent)).Bounds();
        
        [Pure] public LocalBounds TransformBy(LocalPose offset) 
            => this.Vertices().Select(p => p.TransformBy(offset)).Bounds();
        
        [Pure] public bool Contains(LocalPosition point) 
            => point.X().IsInsideInterval(Min.X(), Max.X(), inclusive: true)
            && point.Y().IsInsideInterval(Min.Y(), Max.Y(), inclusive: true)
            && point.Z().IsInsideInterval(Min.Z(), Max.Z(), inclusive: true);

        [Pure] public LocalBounds LerpTo(LocalBounds target, double progress, bool shouldClamp) => FromCenterSize(
            Center.LerpTo(target.Center, progress, shouldClamp),
            Size.LerpTo(target.Size, progress, shouldClamp)
        );

        [Pure] public LocalBounds SlerpTo(LocalBounds target, double progress, bool shouldClamp) => FromCenterSize(
            Center.SlerpTo(target.Center, progress, shouldClamp),
            Size.SlerpTo(target.Size, progress, shouldClamp)
        );

    }
    

    public static class LocalBoundsExtensions {
        
        [Pure] public static LocalBounds AsLocalBounds(this UnityEngine.Bounds b) => 
            LocalBounds.FromCenterSize(b.center.AsLocalPosition(), b.size.AsLocalOffset());
        
        [Pure] public static Bounds AsUnityBounds(this LocalBounds bounds) => new Bounds(
            center: bounds.Center.AsVector,
            size: bounds.Size.AsVector
        );

        [Pure] public static LocalBounds Encapsulating(this LocalBounds a, LocalBounds b) =>
            LocalBounds.FromMinMax(
                a.Min.Min(b.Min),
                a.Max.Max(b.Max)
            );

        [Pure] public static LocalBounds Encapsulating(this LocalBounds bounds, LocalPosition point) => 
            LocalBounds.FromMinMax(bounds.Min.Min(point), bounds.Max.Max(point));

        [Pure] public static LocalBounds Bounds(this IEnumerable<LocalPosition> vertices) {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue).AsLocalPosition();
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue).AsLocalPosition();
            foreach (var vertex in vertices) {
                min = min.Min(vertex);
                max = max.Max(vertex);
            }
            return LocalBounds.FromMinMax(min, max);
        }

        [Pure] public static IEnumerable<LocalPosition> Vertices(this LocalBounds localBounds) {
            var min = localBounds.Min;
            var max = localBounds.Max;
            yield return min;
            yield return min.WithX(max.X());
            yield return min.WithY(max.Y());
            yield return min.WithZ(max.Z());
            yield return max.WithX(min.X());
            yield return max.WithY(min.Y());
            yield return max.WithZ(min.Z());
            yield return max;
        }

        [Pure] public static LocalBounds WithCenter(this LocalBounds localBounds, LocalPosition center) =>
            LocalBounds.FromCenterSize(center, localBounds.Size);

        [Pure] public static LocalBounds WithCenter(this LocalBounds localBounds, Func<LocalPosition, LocalPosition> centerGetter) =>
            localBounds.WithCenter(centerGetter(localBounds.Center));

        [Pure] public static LocalBounds WithSize(this LocalBounds localBounds, LocalOffset size) =>
            LocalBounds.FromCenterSize(localBounds.Center, size);

        [Pure] public static LocalBounds WithSize(this LocalBounds localBounds, Func<LocalOffset, LocalOffset> sizeGetter) =>
            localBounds.WithSize(sizeGetter(localBounds.Size));
    }
}