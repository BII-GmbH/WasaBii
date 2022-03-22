using System;
using BII.WasaBii.Core;
using BII.WasaBii.Units;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    /// A wrapper for a <see cref="Vector3"/> that represents a local direction.
    /// Can also be viewed as a normalized <see cref="LocalOffset"/>.
    [MustBeImmutable]
    [MustBeSerializable]
    public readonly struct LocalDirection : 
        VectorLike<LocalDirection>, 
        LocalDirectionLike, 
        IsLocalVariant<LocalDirection, GlobalDirection>,
        IEquatable<LocalDirection> {

        [Pure] public static LocalDirection FromGlobal(TransformProvider parent, Vector3 global)
            => FromLocal(parent.InverseTransformVector(global));
        
        [Pure] public static LocalDirection FromLocal(Vector3 local)
            => new LocalDirection(local);

        [Pure] public static LocalDirection FromLocal(Length x, Length y, Length z) 
            => FromLocal(new Vector3((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()));

        [Pure] public static LocalDirection FromLocal(float x, float y, float z) 
            => FromLocal(new Vector3(x, y, z));

        public Vector3 AsVector { get; }

        private LocalDirection(Vector3 local) => this.AsVector = local.normalized;

        /// Transforms the local direction into global space, with <see cref="parent"/> as the parent.
        /// This is the inverse of <see cref="GlobalDirection.RelativeTo"/>
        [Pure] public GlobalDirection ToGlobalWith(TransformProvider parent) => GlobalDirection.FromLocal(parent, AsVector);
        
        /// <inheritdoc cref="Vector3.Project"/>
        [Pure] public LocalDirection Project(LocalDirection onNormal) => this.AsOffset.Project(onNormal).Normalized;

        /// <inheritdoc cref="Vector3.ProjectOnPlane"/>
        [Pure] public LocalDirection ProjectOnPlane(LocalDirection planeNormal) => this.AsOffset.ProjectOnPlane(planeNormal).Normalized;

        public LocalOffset AsOffset => (LocalOffset) this;
        
        [Pure] public static explicit operator LocalDirection(LocalOffset offset) => new LocalDirection(offset.AsVector);
        [Pure] public static explicit operator LocalOffset(LocalDirection direction) => LocalOffset.FromLocal(direction.AsVector);
        [Pure] public static bool operator ==(LocalDirection a, LocalDirection b) => a.AsVector == b.AsVector;
        [Pure] public static bool operator !=(LocalDirection a, LocalDirection b) => a.AsVector != b.AsVector;
        [Pure] public static LocalOffset operator *(Length a, LocalDirection b) => LocalOffset.FromLocal((float)a.AsMeters() * b.AsVector);
        [Pure] public static LocalOffset operator *(LocalDirection b, Length a) => LocalOffset.FromLocal((float)a.AsMeters() * b.AsVector);
        [Pure] public static LocalDirection operator -(LocalDirection offset) => LocalDirection.FromLocal(-offset.AsVector);

        [Pure] public bool Equals(LocalDirection other) => this == other;
        [Pure] public override bool Equals(object obj) => obj is LocalDirection dir && this == dir;
        [Pure] public override int GetHashCode() => AsVector.GetHashCode();
        
        public static LocalDirection Up => Vector3.up.AsLocalDirection();
        public static LocalDirection Down => Vector3.down.AsLocalDirection();
        public static LocalDirection Left => Vector3.left.AsLocalDirection();
        public static LocalDirection Right => Vector3.right.AsLocalDirection();
        public static LocalDirection Forward => Vector3.forward.AsLocalDirection();
        public static LocalDirection Back => Vector3.back.AsLocalDirection();
        public static LocalDirection One => Vector3.one.AsLocalDirection();
        
        [Pure] public static LocalDirection Lerp(
            LocalDirection start, LocalDirection end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public static LocalDirection Slerp(
            LocalDirection start, LocalDirection end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

        [Pure] public LocalDirection CopyWithDifferentValue(Vector3 newValue) => FromLocal(newValue);
    }

    public static partial class DirectionExtensions {
       
       [Pure] public static LocalDirection AsLocalDirection(this Vector3 localDirection) 
           => LocalDirection.FromLocal(localDirection);
       
       [Pure] public static LocalDirection AsLocalDirection(this System.Numerics.Vector3 localDirection) 
           => localDirection.ToUnityVector().AsLocalDirection();

       /// <inheritdoc cref="GeometryUtils.Reflect(Vector3, Vector3)"/>
       [Pure] public static LocalDirection Reflect(
           this LocalDirection self, LocalDirection planeNormal
       ) => self.AsVector.Reflect(planeNormal.AsVector).AsLocalDirection();

    }

}