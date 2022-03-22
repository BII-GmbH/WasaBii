using System;
using BII.WasaBii.Core;
using BII.WasaBii.Units;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    /// A wrapper for a <see cref="Vector3"/> that represents a world-space direction.
    /// Can also be viewed as a normalized <see cref="GlobalOffset"/>.
    [MustBeImmutable]
    [MustBeSerializable]
    public readonly struct GlobalDirection : 
        VectorLike<GlobalDirection>,
        GlobalDirectionLike,
        IsGlobalVariant<GlobalDirection, LocalDirection>,
        IEquatable<GlobalDirection> {

        [Pure] public static GlobalDirection FromGlobal(Vector3 global) => new GlobalDirection(global);
        
        [Pure] public static GlobalDirection FromGlobal(Length x, Length y, Length z) 
            => FromGlobal(new Vector3((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()));

        [Pure] public static GlobalDirection FromGlobal(float x, float y, float z) 
            => FromGlobal(new Vector3(x, y, z));

        [Pure] public static GlobalDirection FromLocal(TransformProvider parent, Vector3 local)
            => FromGlobal(parent.TransformVector(local));

        public Vector3 AsVector { get; }

        private GlobalDirection(Vector3 global) => this.AsVector = global.normalized;

        /// Transforms the global direction into local space, relative to the <see cref="parent"/>.
        /// This is the inverse of <see cref="LocalDirection.ToGlobalWith"/>
        [Pure] public LocalDirection RelativeTo(TransformProvider parent) => LocalDirection.FromGlobal(parent, AsVector);
        
        /// <inheritdoc cref="Vector3.Project"/>
        [Pure] public GlobalDirection Project(GlobalDirection onNormal) => this.AsOffset.Project(onNormal).Normalized;

        /// <inheritdoc cref="Vector3.ProjectOnPlane"/>
        [Pure] public GlobalDirection ProjectOnPlane(GlobalDirection planeNormal) => this.AsOffset.ProjectOnPlane(planeNormal).Normalized;

        public GlobalOffset AsOffset => (GlobalOffset) this;
        
        [Pure] public static explicit operator GlobalDirection(GlobalOffset offset) => new GlobalDirection(offset.AsVector);
        [Pure] public static explicit operator GlobalOffset(GlobalDirection direction) => GlobalOffset.FromGlobal(direction.AsVector);
        [Pure] public static bool operator ==(GlobalDirection a, GlobalDirection b) => a.AsVector == b.AsVector;
        [Pure] public static bool operator !=(GlobalDirection a, GlobalDirection b) => a.AsVector != b.AsVector;
        [Pure] public static GlobalOffset operator *(Length a, GlobalDirection b) => GlobalOffset.FromGlobal((float)a.AsMeters() * b.AsVector);
        [Pure] public static GlobalDirection operator -(GlobalDirection offset) => GlobalDirection.FromGlobal(-offset.AsVector);

        [Pure] public bool Equals(GlobalDirection other) => this == other;
        [Pure] public override bool Equals(object obj) => obj is GlobalDirection dir && this == dir;
        [Pure] public override int GetHashCode() => AsVector.GetHashCode();
        
        public static GlobalDirection Up => Vector3.up.AsGlobalDirection();
        public static GlobalDirection Down => Vector3.down.AsGlobalDirection();
        public static GlobalDirection Left => Vector3.left.AsGlobalDirection();
        public static GlobalDirection Right => Vector3.right.AsGlobalDirection();
        public static GlobalDirection Forward => Vector3.forward.AsGlobalDirection();
        public static GlobalDirection Back => Vector3.back.AsGlobalDirection();
        public static GlobalDirection One => Vector3.one.AsGlobalDirection();
        
        [Pure] public static GlobalDirection Lerp(
            GlobalDirection start, GlobalDirection end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public static GlobalDirection Slerp(
            GlobalDirection start, GlobalDirection end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

        [Pure] public GlobalDirection CopyWithDifferentValue(Vector3 newValue) => FromGlobal(newValue);

    }

    public static partial class DirectionExtensions {
       
       [Pure] public static GlobalDirection AsGlobalDirection(this Vector3 globalPosition) 
           => GlobalDirection.FromGlobal(globalPosition);

       [Pure] public static GlobalDirection AsGlobalDirection(this System.Numerics.Vector3 globalPosition) 
           => globalPosition.ToUnityVector().AsGlobalDirection();

       [Pure] public static GlobalDirection Forward(this Component component) =>
           component.transform.forward.AsGlobalDirection();

       [Pure] public static GlobalDirection Forward(this GameObject gameObject) =>
           gameObject.transform.forward.AsGlobalDirection();

       [Pure] public static GlobalDirection Right(this Component component) =>
           component.transform.right.AsGlobalDirection();

       [Pure] public static GlobalDirection Right(this GameObject gameObject) =>
           gameObject.transform.right.AsGlobalDirection();

       [Pure] public static GlobalDirection Up(this Component component) =>
           component.transform.up.AsGlobalDirection();

       [Pure] public static GlobalDirection Up(this GameObject gameObject) =>
           gameObject.transform.up.AsGlobalDirection();

       /// <inheritdoc cref="GeometryUtils.Reflect(Vector3, Vector3)"/>
       [Pure] public static GlobalDirection Reflect(
           this GlobalDirection self, GlobalDirection planeNormal
       ) => self.AsVector.Reflect(planeNormal.AsVector).AsGlobalDirection();

    }

}