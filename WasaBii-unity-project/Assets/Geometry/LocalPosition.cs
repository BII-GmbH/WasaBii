using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using BII.WasaBii.Unity.Geometry;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A 3D vector that represents a local position relative to an undefined parent.
    [MustBeImmutable]
    [MustBeSerializable]
    [GeometryHelper(areFieldsIndependent: true, fieldType: FieldType.Length, hasMagnitude: false, hasDirection: false)]
    public readonly partial struct LocalPosition : IsLocalVariant<LocalPosition, GlobalPosition> {

        public static readonly LocalPosition Zero = FromLocal(Length.Zero, Length.Zero, Length.Zero);
        
        public Length X { init; get; }
        public Length Y { init; get; }
        public Length Z { init; get; }

        [Pure] public static LocalPosition FromLocal(System.Numerics.Vector3 local)
            => new() {X = local.X.Meters(), Y = local.Y.Meters(), Z = local.Z.Meters()};

        [Pure] public static LocalPosition FromLocal(Length x, Length y, Length z) 
            => new() {X = x, Y = y, Z = z};

        [Pure] public static LocalPosition FromLocal(double x, double y, double z)
            => new() {X = x.Meters(), Y = y.Meters(), Z = z.Meters()};
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static LocalPosition FromLocal(UnityEngine.Vector3 local)
            => new() {X = local.x.Meters(), Y = local.y.Meters(), Z = local.z.Meters()};
        #endif

        public LocalOffset AsOffset => LocalOffset.FromLocal(X, Y, Z);

        /// <inheritdoc cref="TransformProvider.TransformPoint"/>
        /// This is the inverse of <see cref="GlobalPosition.RelativeTo"/>
        [Pure] public GlobalPosition ToGlobalWith(TransformProvider parent) 
            => parent.TransformPoint(this);

        public GlobalPosition ToGlobalWithWorldZero => new (){X = X, Y = Y, Z = Z};

        [Pure]
        public LocalPosition TransformBy(LocalPose offset) =>
            offset.Position + offset.Rotation * this.AsOffset;

        [Pure] public static LocalPosition operator +(LocalPosition left, LocalOffset right) => FromLocal(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        [Pure] public static LocalPosition operator -(LocalPosition left, LocalOffset right) => FromLocal(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        [Pure] public static LocalOffset operator -(LocalPosition left, LocalPosition right) => LocalOffset.FromLocal(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        [Pure] public static LocalPosition operator -(LocalPosition pos) => FromLocal(-pos.X, -pos.Y, -pos.Z);
        [Pure] public override string ToString() => AsNumericsVector.ToString();

        [Pure] public static LocalPosition Lerp(
            LocalPosition start, LocalPosition end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public Length DistanceTo(LocalPosition p1, LocalPosition p2) => (p2 - p1).Magnitude;
        
        /// <inheritdoc cref="GeometryUtils.PointReflect(Vector3, Vector3)"/>
        [Pure] public LocalPosition PointReflect(LocalPosition on)
            => on + (on - this);

        /// <inheritdoc cref="GeometryUtils.Reflect(Vector3, Vector3, Vector3)"/>
        [Pure] public LocalPosition Reflect(
            LocalPosition pointOnPlane, LocalDirection planeNormal
        ) => pointOnPlane - (this - pointOnPlane).Reflect(planeNormal);
        
        /// Rotates <see cref="self"/> around <see cref="pivot"/> by <see cref="rotation"/>.
        /// Assumes that all three values are given in the same local space.
        [Pure] public LocalPosition Rotate(
            LocalPosition pivot, LocalRotation rotation    
        ) => pivot + (this - pivot) * rotation;
    }
    
    public static partial class PositionExtensions {
        
        [Pure] public static LocalPosition AsLocalPosition(this System.Numerics.Vector3 localPosition) 
            => Geometry.LocalPosition.FromLocal(localPosition);
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static LocalPosition AsLocalPosition(this UnityEngine.Vector3 localPosition) 
            => Geometry.LocalPosition.FromLocal(localPosition);
        
        [Pure] public static LocalPosition LocalPosition(this UnityEngine.Component component) 
            => Geometry.LocalPosition.FromLocal(component.transform.localPosition);
        [Pure] public static LocalPosition LocalPosition(this UnityEngine.GameObject gameObject) 
            => Geometry.LocalPosition.FromLocal(gameObject.transform.localPosition);
        #endif
        
    }

}
