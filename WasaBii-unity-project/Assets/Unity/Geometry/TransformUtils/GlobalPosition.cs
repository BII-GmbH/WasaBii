using System;
using BII.WasaBii.Core;
using BII.WasaBii.Units;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    
    /// This struct provides implicit conversions for all supported
    /// types that can be used to obtain a global position vector.
    /// Currently, it is exclusively used by the `Offset.From().To()` pattern,
    /// but it is designed to be future proof, following the same principles as `TransformProvider`.
    public readonly struct PositionProvider {
        public readonly GlobalPosition Wrapped;
        public PositionProvider(GlobalPosition wrapped) => this.Wrapped = wrapped;
        public static implicit operator PositionProvider(GlobalPosition position) 
            => new (position);
        public static implicit operator PositionProvider(Vector3 position)
            => new (position.AsGlobalPosition());
        public static implicit operator PositionProvider(Component component)
            => new (component.GlobalPosition());
        public static implicit operator PositionProvider(GameObject gameObject)
            => new (gameObject.GlobalPosition());
    }

    /// A wrapper for a <see cref="Vector3"/> that represents a world-space position.
    [MustBeImmutable]
    [MustBeSerializable]
    public readonly struct GlobalPosition : 
        VectorLike<GlobalPosition>, HasMagnitude<GlobalPosition>, 
        IsGlobalVariant<GlobalPosition, LocalPosition>,
        IEquatable<GlobalPosition> {
        
        public static readonly GlobalPosition Zero = FromGlobal(Vector3.zero);

        public Vector3 AsVector { get; }

        private GlobalPosition(Vector3 global) => this.AsVector = global;

        [Pure] public static GlobalPosition FromGlobal(Vector3 global) => new GlobalPosition(global);

        [Pure] public static GlobalPosition FromGlobal(Length x, Length y, Length z) 
            => FromGlobal(new Vector3((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()));

        [Pure] public static GlobalPosition FromGlobal(float x, float y, float z) 
            => FromGlobal(new Vector3(x, y, z));

        [Pure] public static GlobalPosition FromLocal(TransformProvider parent, Vector3 local) 
            => FromGlobal(parent.TransformPoint(local));

        [Pure] public static GlobalPosition FromTransform(Transform parent) => new GlobalPosition(parent.position);

        /// Transforms the global position into local space, relative to the <see cref="parent"/>.
        /// This is the inverse of <see cref="LocalPosition.ToGlobalWith"/>
        [Pure] public LocalPosition RelativeTo(TransformProvider parent) 
            => LocalPosition.FromGlobal(parent, AsVector);

        [Pure] public GlobalPosition AsLocalPositionOf(TransformProvider oldParent, TransformProvider newParent) 
            => this.RelativeTo(oldParent).ToGlobalWith(newParent);
        
        [Pure] public static GlobalPosition operator +(GlobalPosition left, GlobalOffset right) => new GlobalPosition(left.AsVector + right.AsVector);
        [Pure] public static GlobalPosition operator -(GlobalPosition left, GlobalOffset right) => new GlobalPosition(left.AsVector - right.AsVector);
        [Pure] public static GlobalOffset operator -(GlobalPosition left, GlobalPosition right) => GlobalOffset.FromGlobal(left.AsVector - right.AsVector);
        [Pure] public static GlobalPosition operator -(GlobalPosition pos) => GlobalPosition.FromGlobal(-pos.AsVector);
        [Pure] public static bool operator ==(GlobalPosition a, GlobalPosition b) => a.AsVector == b.AsVector;
        [Pure] public static bool operator !=(GlobalPosition a, GlobalPosition b) => a.AsVector != b.AsVector;
        [Pure] public override string ToString() => AsVector.ToString();
        [Pure] public bool Equals(GlobalPosition other) => this == other;
        [Pure] public override bool Equals(object obj) => obj is GlobalPosition pos && this == pos;
        [Pure] public override int GetHashCode() => AsVector.GetHashCode();
        
        [Pure] public static GlobalPosition Lerp(
            GlobalPosition start, GlobalPosition end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public static GlobalPosition Slerp(
            GlobalPosition start, GlobalPosition end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

        [Pure] public GlobalPosition CopyWithDifferentValue(Vector3 newValue) => FromGlobal(newValue);
    }
    
    public static partial class PositionExtensions {
        
        [Pure] public static GlobalPosition AsGlobalPosition(this Vector3 globalPosition) 
            => Geometry.GlobalPosition.FromGlobal(globalPosition);

        [Pure] public static GlobalPosition AsGlobalPosition(this System.Numerics.Vector3 globalPosition)
            => globalPosition.ToUnityVector().AsGlobalPosition();
        
        [Pure] public static GlobalPosition GlobalPosition(this Component component) 
            => Geometry.GlobalPosition.FromGlobal(component.transform.position);
        [Pure] public static GlobalPosition GlobalPosition(this GameObject gameObject) 
            => Geometry.GlobalPosition.FromGlobal(gameObject.transform.position);

        [Pure] public static Length DistanceTo(this GlobalPosition p1, GlobalPosition p2) => (p2 - p1).Length;
        
        /// <inheritdoc cref="GeometryUtils.PointReflect(Vector3, Vector3)"/>
        [Pure] public static GlobalPosition Reflect(this GlobalPosition self, GlobalPosition on)
            => self.AsVector.PointReflect(on: on.AsVector).AsGlobalPosition();

        /// <inheritdoc cref="GeometryUtils.Reflect(Vector3, Vector3, Vector3)"/>
        [Pure] public static GlobalPosition Reflect(
            this GlobalPosition self, GlobalPosition pointOnPlane, GlobalDirection planeNormal
        ) => self.AsVector.Reflect(pointOnPlane.AsVector, planeNormal.AsVector).AsGlobalPosition();
        
        [Pure] public static GlobalPosition Rotate(
            this GlobalPosition self, GlobalPosition pivot, GlobalRotation rotation    
        ) => pivot + (self - pivot) * rotation;

    }
    
}