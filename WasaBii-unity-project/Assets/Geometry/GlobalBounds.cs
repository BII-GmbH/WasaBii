using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    /// A Unity-Independent data structure representing an AABB (axis-aligned bounding-box) in global space.
    [MustBeImmutable]
    [MustBeSerializable]
    public readonly struct GlobalBounds : IsGlobalVariant<GlobalBounds, LocalBounds>, GeometryHelper<GlobalBounds> {
        
        public readonly GlobalPosition Center;
        
        public readonly GlobalOffset Size;
        public GlobalOffset Extends => Size / 2.0f;

        public GlobalPosition BottomCenter => Center.WithY(Center.Y() - Size.Y() / 2.0);
        public GlobalPosition TopCenter => Center.WithY(Center.Y() + Size.Y() / 2.0);

        public GlobalPosition Min => Center - Extends;
        public GlobalPosition Max => Center + Extends;

        public Volume Volume => Size.X() * Size.Y() * Size.Z();

        [Pure] public static GlobalBounds FromMinMax(GlobalPosition min, GlobalPosition max) => 
            new GlobalBounds(max.LerpTo(min, 0.5f), (max - min));

        [Pure] public static GlobalBounds FromCenterSize(GlobalPosition center, GlobalOffset size) => 
            new GlobalBounds(center, size);

        private GlobalBounds(GlobalPosition center, GlobalOffset size) {
            Center = center;
            Size = size.Map(vector => vector.Map(Mathf.Abs));
        }

        /// Returns the smallest possible bounds in local space relative to <see cref="parent"/> that completely
        /// wraps this set of bounds. Will most likely be larger than the original if rotations of any angles
        /// other than 90°-multiples are involved. 
        [Pure] public LocalBounds RelativeTo(TransformProvider parent)
            => this.Vertices().Select(p => p.RelativeTo(parent)).Bounds();

        [Pure] public bool Contains(GlobalPosition point) 
            => point.X().IsInsideInterval(Min.X(), Max.X(), inclusive: true)
            && point.Y().IsInsideInterval(Min.Y(), Max.Y(), inclusive: true)
            && point.Z().IsInsideInterval(Min.Z(), Max.Z(), inclusive: true);

        [Pure] public GlobalBounds LerpTo(GlobalBounds target, double progress, bool shouldClamp) => FromCenterSize(
            Center.LerpTo(target.Center, progress, shouldClamp),
            Size.LerpTo(target.Size, progress, shouldClamp)
        );

        [Pure] public GlobalBounds SlerpTo(GlobalBounds target, double progress, bool shouldClamp) => FromCenterSize(
            Center.SlerpTo(target.Center, progress, shouldClamp),
            Size.SlerpTo(target.Size, progress, shouldClamp)
        );
    }
    

    public static class GlobalBoundsExtensions {
        
        [Pure] public static GlobalBounds AsGlobalBounds(this UnityEngine.Bounds b) => 
            GlobalBounds.FromCenterSize(b.center.AsGlobalPosition(), b.size.AsGlobalOffset());

        [Pure] public static Bounds AsUnityBounds(this GlobalBounds bounds) => new Bounds(
            center: bounds.Center.AsVector,
            size: bounds.Size.AsVector
        );
        
        [Pure] public static GlobalBounds Encapsulating(this GlobalBounds a, GlobalBounds b) =>
            GlobalBounds.FromMinMax(
                a.Min.Min(b.Min),
                a.Max.Max(b.Max)
            );

        [Pure] public static GlobalBounds Encapsulating(this GlobalBounds bounds, GlobalPosition point) => 
            GlobalBounds.FromMinMax(bounds.Min.Min(point), bounds.Max.Max(point));

        [Pure] public static GlobalBounds Bounds(this IEnumerable<GlobalPosition> vertices) {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue).AsGlobalPosition();
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue).AsGlobalPosition();
            foreach (var vertex in vertices) {
                min = min.Min(vertex);
                max = max.Max(vertex);
            }
            return GlobalBounds.FromMinMax(min, max);
        }

        [Pure] public static GlobalPosition[] Vertices(this GlobalBounds globalBounds) {
            var min = globalBounds.Min;
            var max = globalBounds.Max;
            return new[] {
                min,
                min.WithX(max.X()),
                min.WithY(max.Y()),
                min.WithZ(max.Z()),
                max.WithX(min.X()),
                max.WithY(min.Y()),
                max.WithZ(min.Z()),
                max
            };
        }
        
        [Pure] public static GlobalBounds WithCenter(this GlobalBounds globalBounds, GlobalPosition center) =>
            GlobalBounds.FromCenterSize(center, globalBounds.Size);

        [Pure] public static GlobalBounds WithCenter(this GlobalBounds globalBounds, Func<GlobalPosition, GlobalPosition> centerGetter) =>
            globalBounds.WithCenter(centerGetter(globalBounds.Center));

        [Pure] public static GlobalBounds WithSize(this GlobalBounds globalBounds, GlobalOffset size) =>
            GlobalBounds.FromCenterSize(globalBounds.Center, size);

        [Pure] public static GlobalBounds WithSize(this GlobalBounds globalBounds, Func<GlobalOffset, GlobalOffset> sizeGetter) =>
            globalBounds.WithSize(sizeGetter(globalBounds.Size));
        
    }
}