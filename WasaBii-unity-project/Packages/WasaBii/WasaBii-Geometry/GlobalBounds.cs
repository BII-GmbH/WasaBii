using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A Unity-Independent data structure representing an AABB (axis-aligned bounding-box) in global space.
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent:true, hasMagnitude:false, hasOrientation:false)]
    public partial struct GlobalBounds : IsGlobalVariant<GlobalBounds, LocalBounds> {
        
        #if UNITY_2022_1_OR_NEWER
        [UnityEngine.SerializeField]
        #endif
        private GlobalPosition _center;
        public GlobalPosition Center => _center;
        
        #if UNITY_2022_1_OR_NEWER
        [UnityEngine.SerializeField]
        #endif
        private GlobalOffset _size;
        public GlobalOffset Size => _size;

        public GlobalOffset Extends => Size * 0.5;

        public GlobalPosition BottomCenter => Center.WithY(Center.Y - Size.Y * 0.5);
        public GlobalPosition TopCenter => Center.WithY(Center.Y + Size.Y * 0.5);

        public GlobalPosition Min => Center - Extends;
        public GlobalPosition Max => Center + Extends;

        public Volume Volume => Size.X * Size.Y * Size.Z;

        public GlobalBounds(GlobalPosition min, GlobalPosition max) : 
            this(center: max.LerpTo(min, 0.5f), size: max - min) { }

        public GlobalBounds(GlobalPosition center, GlobalOffset size) {
            _center = center;
            _size = size.Map(vector => vector.Map(Math.Abs));
        }

        /// Returns the smallest possible bounds in local space relative to <see cref="parent"/> that completely
        /// wraps this set of bounds. Will most likely be larger than the original if rotations of any angles
        /// other than 90°-multiples are involved. 
        [Pure] public LocalBounds RelativeTo(TransformProvider parent)
            => this.Vertices().Select(p => p.RelativeTo(parent)).Bounds();

        public LocalBounds RelativeToWorldZero => new(
            Center.RelativeToWorldZero,
            Size.RelativeToWorldZero
        );

        [Pure] public bool Contains(GlobalPosition point) 
            => point.X.IsInsideInterval(Min.X, Max.X, inclusive: true)
            && point.Y.IsInsideInterval(Min.Y, Max.Y, inclusive: true)
            && point.Z.IsInsideInterval(Min.Z, Max.Z, inclusive: true);

        [Pure] public GlobalBounds LerpTo(GlobalBounds target, double progress, bool shouldClamp) => new(
            Center.LerpTo(target.Center, progress, shouldClamp),
            Size.LerpTo(target.Size, progress, shouldClamp)
        );

        [Pure] public GlobalBounds SlerpTo(GlobalBounds target, double progress, bool shouldClamp) => new(
            Center.LerpTo(target.Center, progress, shouldClamp),
            Size.SlerpTo(target.Size, progress, shouldClamp)
        );
    }
    

    public static partial class GlobalBoundsExtensions {
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalBounds AsGlobalBounds(this UnityEngine.Bounds b) => 
            new(b.center.AsGlobalPosition(), b.size.AsGlobalOffset());

        [Pure] public static UnityEngine.Bounds AsUnityBounds(this GlobalBounds bounds) => new(
            center: bounds.Center.AsUnityVector,
            size: bounds.Size.AsUnityVector
        );
        #endif
        
        [Pure] public static GlobalBounds Encapsulating(this GlobalBounds a, GlobalBounds b) =>
            new(
                a.Min.Min(b.Min),
                a.Max.Max(b.Max)
            );

        [Pure] public static GlobalBounds Encapsulating(this GlobalBounds bounds, GlobalPosition point) => 
            new(bounds.Min.Min(point), bounds.Max.Max(point));

        [Pure] public static GlobalBounds Bounds(this IEnumerable<GlobalPosition> vertices) {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue).AsGlobalPosition();
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue).AsGlobalPosition();
            foreach (var vertex in vertices) {
                min = min.Min(vertex);
                max = max.Max(vertex);
            }
            return new(min, max);
        }

        [Pure] public static GlobalPosition[] Vertices(this GlobalBounds globalBounds) {
            var min = globalBounds.Min;
            var max = globalBounds.Max;
            return new[] {
                min,
                min.WithX(max.X),
                min.WithY(max.Y),
                min.WithZ(max.Z),
                max.WithX(min.X),
                max.WithY(min.Y),
                max.WithZ(min.Z),
                max
            };
        }
        
    }
}