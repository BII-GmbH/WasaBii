using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// <summary>
    /// An AABB (axis-aligned bounding-box) in global space.
    /// </summary>
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent:true, hasMagnitude:false, hasOrientation:false)]
    public partial struct GlobalBounds : IsGlobalVariant<GlobalBounds, LocalBounds> {
        
        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        #endif
        public GlobalPosition Center { get; private set; }
        
        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        #endif
        public GlobalOffset Size { get; private set; }

        public GlobalOffset Extends => Size * 0.5;

        public GlobalPosition BottomCenter => Center.WithY(Center.Y - Size.Y * 0.5);
        public GlobalPosition TopCenter => Center.WithY(Center.Y + Size.Y * 0.5);

        public GlobalPosition Min => Center - Extends;
        public GlobalPosition Max => Center + Extends;

        public Volume Volume => Size.X * Size.Y * Size.Z;

        public GlobalBounds(GlobalPosition min, GlobalPosition max) : 
            this(center: max.LerpTo(min, 0.5f), size: max - min) { }

        public GlobalBounds(GlobalPosition center, GlobalOffset size) {
            Center = center;
            Size = size.Map(MathF.Abs);
        }

        /// <summary>
        /// Returns the smallest possible bounds in the local space relative to <see cref="parent"/> that completely
        /// wraps <see cref="this"/>. Will be larger than the original if rotations of any angles other than
        /// 90°-multiples are involved. 
        /// This is the semi-inverse of <see cref="LocalBounds.ToGlobalWith"/>.
        /// </summary>
        /// <example> <code>global.RelativeTo(parent).ToGlobalWith(parent) ~= global</code> </example>
        [Pure] public LocalBounds RelativeTo(TransformProvider parent)
            => this.Vertices().Select(p => p.RelativeTo(parent)).Bounds().ValueOrDefault!;

        [Pure] public bool Contains(GlobalPosition point) 
            => point.X.IsInsideInterval(Min.X, Max.X, inclusive: true)
            && point.Y.IsInsideInterval(Min.Y, Max.Y, inclusive: true)
            && point.Z.IsInsideInterval(Min.Z, Max.Z, inclusive: true);
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

        [Pure] public static Option<GlobalBounds> Bounds(this IEnumerable<GlobalPosition> vertices) {
            return vertices.Aggregate(
                Option<(GlobalPosition Min, GlobalPosition Max)>.None, 
                (current, vertex) => current.Match(
                    val => (
                        val.Min.Min(vertex), 
                        val.Max.Max(vertex)
                    ), 
                    () => (vertex, vertex)
                )).Map(val => new GlobalBounds(val.Min, val.Max));
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