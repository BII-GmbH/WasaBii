using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A 3D vector that represents a local position relative to an undefined parent.
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: true, hasOrientation: false)]
    public readonly partial struct LocalPosition : IsLocalVariant<LocalPosition, GlobalPosition> {

        public static readonly LocalPosition Zero = new(Length.Zero, Length.Zero, Length.Zero);
        
        public System.Numerics.Vector3 AsNumericsVector { get; }

        public LocalPosition(System.Numerics.Vector3 asNumericsVector) => AsNumericsVector = asNumericsVector;
        public LocalPosition(float x, float y, float z) => AsNumericsVector = new(x, y, z);
        public LocalPosition(Length x, Length y, Length z) => AsNumericsVector = new((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters());

        #if UNITY_2022_1_OR_NEWER
        public LocalPosition(UnityEngine.Vector3 global) => AsNumericsVector = global.ToSystemVector();
        #endif

        public LocalOffset AsOffset => new(AsNumericsVector);

        /// <inheritdoc cref="TransformProvider.TransformPoint"/>
        /// This is the inverse of <see cref="GlobalPosition.RelativeTo"/>
        [Pure] public GlobalPosition ToGlobalWith(TransformProvider parent) 
            => parent.TransformPoint(this);

        public GlobalPosition ToGlobalWithWorldZero => new(AsNumericsVector);

        [Pure]
        public LocalPosition TransformBy(LocalPose offset) =>
            offset.Position + offset.Rotation * this.AsOffset;

        [Pure] public static LocalPosition operator +(LocalPosition left, LocalOffset right) => new(left.AsNumericsVector + right.AsNumericsVector);
        [Pure] public static LocalPosition operator -(LocalPosition left, LocalOffset right) => new(left.AsNumericsVector - right.AsNumericsVector);
        [Pure] public static LocalOffset operator -(LocalPosition left, LocalPosition right) => new(left.AsNumericsVector - right.AsNumericsVector);
        [Pure] public static LocalPosition operator -(LocalPosition pos) => new(-pos.AsNumericsVector);
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
            => new(localPosition);
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static LocalPosition AsLocalPosition(this UnityEngine.Vector3 localPosition) 
            => new(localPosition);
        
        [Pure] public static LocalPosition LocalPosition(this UnityEngine.Component component) 
            => new(component.transform.localPosition);
        [Pure] public static LocalPosition LocalPosition(this UnityEngine.GameObject gameObject) 
            => new(gameObject.transform.localPosition);
        #endif
        
    }

}
