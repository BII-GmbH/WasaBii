using System;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    /// A wrapper for a <see cref="Vector3"/> that represents a local position relative to an undefined parent.
    [MustBeImmutable]
    [MustBeSerializable]
    public readonly struct LocalPosition : 
        VectorLike<LocalPosition>, HasMagnitude<LocalPosition>, 
        IsLocalVariant<LocalPosition, GlobalPosition>,
        IEquatable<LocalPosition> {

        public static readonly LocalPosition Zero = FromLocal(Vector3.zero);
        
        public Vector3 AsVector { get; }

        public LocalOffset AsOffset => LocalOffset.FromLocal(AsVector);

        private LocalPosition(Vector3 local) => this.AsVector = local;

        [Pure] public static LocalPosition FromGlobal(TransformProvider parent, Vector3 global) =>
            FromLocal(parent.InverseTransformPoint(global));

        [Pure] public static LocalPosition FromLocal(Vector3 local) 
            => new LocalPosition(local);

        [Pure] public static LocalPosition FromLocal(Length x, Length y, Length z) 
            => FromLocal(new Vector3((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()));

        [Pure] public static LocalPosition FromLocal(float x, float y, float z) 
            => FromLocal(new Vector3(x, y, z));

        [Pure] public static LocalPosition FromTransform(Transform parent) => new LocalPosition(parent.localPosition);

        /// Transforms the local position into global space, with <see cref="parent"/> as the parent.
        /// This is the inverse of <see cref="GlobalPosition.RelativeTo"/>
        [Pure] public GlobalPosition ToGlobalWith(TransformProvider parent) 
            => GlobalPosition.FromLocal(parent, AsVector);

        [Pure] public static LocalPosition operator +(LocalPosition left, LocalOffset right) => new LocalPosition(left.AsVector + right.AsVector);
        [Pure] public static LocalPosition operator -(LocalPosition left, LocalOffset right) => new LocalPosition(left.AsVector - right.AsVector);
        [Pure] public static LocalOffset operator -(LocalPosition left, LocalPosition right) => LocalOffset.FromLocal(left.AsVector - right.AsVector);
        [Pure] public static LocalPosition operator -(LocalPosition pos) => LocalPosition.FromLocal(-pos.AsVector);
        [Pure] public static bool operator ==(LocalPosition a, LocalPosition b) => a.AsVector == b.AsVector;
        [Pure] public static bool operator !=(LocalPosition a, LocalPosition b) => a.AsVector != b.AsVector;
        [Pure] public override string ToString() => AsVector.ToString();
        [Pure] public bool Equals(LocalPosition other) => this == other;
        [Pure] public override bool Equals(object obj) => obj is LocalPosition pos && this == pos;
        [Pure] public override int GetHashCode() => AsVector.GetHashCode();
        
        [Pure] public static LocalPosition Lerp(
            LocalPosition start, LocalPosition end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public static LocalPosition Slerp(
            LocalPosition start, LocalPosition end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

        [Pure] public LocalPosition CopyWithDifferentValue(Vector3 newValue) => FromLocal(newValue);
        
    }
    
    public static partial class PositionExtensions {
        
        [Pure] public static LocalPosition AsLocalPosition(this Vector3 localPosition) 
            => Geometry.LocalPosition.FromLocal(localPosition);
        [Pure] public static LocalPosition AsLocalPosition(this System.Numerics.Vector3 localPosition) 
            => localPosition.ToUnityVector().AsLocalPosition();
        
        [Pure] public static LocalPosition LocalPosition(this Component component) 
            => Geometry.LocalPosition.FromLocal(component.transform.localPosition);
        [Pure] public static LocalPosition LocalPosition(this GameObject gameObject) 
            => Geometry.LocalPosition.FromLocal(gameObject.transform.localPosition);

        [Pure] public static Length DistanceTo(this LocalPosition p1, LocalPosition p2) => (p2 - p1).Length;
        
        /// <inheritdoc cref="GeometryUtils.PointReflect(Vector3, Vector3)"/>
        [Pure] public static LocalPosition Reflect(this LocalPosition self, LocalPosition on)
            => self.AsVector.PointReflect(on: on.AsVector).AsLocalPosition();

        /// <inheritdoc cref="GeometryUtils.Reflect(Vector3, Vector3, Vector3)"/>
        [Pure] public static LocalPosition Reflect(
            this LocalPosition self, LocalPosition pointOnPlane, LocalDirection planeNormal
        ) => self.AsVector.Reflect(pointOnPlane.AsVector, planeNormal.AsVector).AsLocalPosition();
        
        [Pure] public static LocalPosition Rotate(
            this LocalPosition self, LocalPosition pivot, LocalRotation rotation    
        ) => pivot + (self - pivot) * rotation;
    }

}
