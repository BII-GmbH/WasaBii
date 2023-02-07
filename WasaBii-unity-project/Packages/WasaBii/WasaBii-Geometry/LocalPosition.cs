using System;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// <summary>
    /// A 3D vector that represents a local position relative to an undefined parent.
    /// Can also be viewed as a <see cref="LocalOffset"/> from the local space origin.
    /// </summary>
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: true, hasOrientation: false)]
    public partial struct LocalPosition : IsLocalVariant<LocalPosition, GlobalPosition> {

        public static readonly LocalPosition Zero = new(System.Numerics.Vector3.Zero);

        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        public UnityEngine.Vector3 AsUnityVector { get; private set; }
        public readonly System.Numerics.Vector3 AsNumericsVector => AsUnityVector.ToSystemVector();
        
        public LocalPosition(UnityEngine.Vector3 toWrap) => AsUnityVector = toWrap;
        public LocalPosition(System.Numerics.Vector3 toWrap) => AsUnityVector = toWrap.ToUnityVector();
        public LocalPosition(float x, float y, float z) => AsUnityVector = new(x, y, z);
        #else
        public System.Numerics.Vector3 AsNumericsVector { get; private set; }
        public LocalPosition(System.Numerics.Vector3 toWrap) => AsNumericsVector = toWrap;
        public LocalPosition(float x, float y, float z) => AsNumericsVector = new(x, y, z);
        #endif

        public LocalPosition(Length x, Length y, Length z) : this((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()) {}

        public LocalOffset AsOffset => new(AsNumericsVector);

        /// <summary>
        /// <inheritdoc cref="TransformProvider.TransformPoint"/>
        /// This is the inverse of <see cref="GlobalPosition.RelativeTo"/>
        /// </summary>
        /// <example> <code>global.RelativeTo(parent).ToGlobalWith(parent) == global</code> </example>
        [Pure] public GlobalPosition ToGlobalWith(TransformProvider parent) 
            => parent.TransformPoint(this);

        public GlobalPosition ToGlobalWithWorldZero => new(AsNumericsVector);

        /// <summary>
        /// Transforms the position into the local space <paramref name="localParent"/> is defined relative to.
        /// Only applicable if the position is defined relative to the given <paramref name="localParent"/>!
        /// This is the inverse of itself with the inverse parent.
        /// </summary>
        /// <example> <code>local.TransformBy(parent).TransformBy(parent.Inverse) = local</code> </example>
        [Pure]
        public LocalPosition TransformBy(LocalPose localParent) =>
            localParent.Position + localParent.Rotation * this.AsOffset;

        [Pure] public static LocalPosition operator +(LocalPosition left, LocalOffset right) => new(left.AsNumericsVector + right.AsNumericsVector);
        [Pure] public static LocalPosition operator -(LocalPosition left, LocalOffset right) => new(left.AsNumericsVector - right.AsNumericsVector);
        [Pure] public static LocalOffset operator -(LocalPosition left, LocalPosition right) => new(left.AsNumericsVector - right.AsNumericsVector);
        [Pure] public static LocalPosition operator -(LocalPosition pos) => new(-pos.AsNumericsVector);

        [Pure] public Length DistanceTo(LocalPosition p2) => (p2 - this).Magnitude;
        
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
        ) => pivot + rotation * (this - pivot);
    }
    
    public static partial class LocalPositionExtensions {
        
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
