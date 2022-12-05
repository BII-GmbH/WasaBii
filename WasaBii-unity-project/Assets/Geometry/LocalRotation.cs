using System;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Geometry {

    /// A wrapper for a <see cref="Quaternion"/> that represents a local rotation.
    [MustBeImmutable]
    [MustBeSerializable]
    public readonly struct LocalRotation : QuaternionLike<LocalRotation>, IsLocalVariant<LocalRotation, GlobalRotation> {

        public static readonly LocalRotation Identity = FromLocal(Quaternion.identity);

        public Quaternion AsQuaternion { get; }

        private LocalRotation(Quaternion local) => AsQuaternion = local;
        
        [Pure] public static LocalRotation FromGlobal(TransformProvider parent, Quaternion global) => 
            FromLocal(parent.InverseTransformQuaternion(global));

        [Pure] public static LocalRotation FromLocal(Quaternion local) => new(local);

        [Pure] public static LocalRotation FromAngleAxis(Angle angle, LocalDirection axis) => angle.WithAxis(axis);
        
        [Pure] public static LocalRotation FromTransform(Transform parent) => new(parent.localRotation);

        /// Transforms the local rotation into global space, with <see cref="parent"/> as the parent.
        /// This is the inverse of <see cref="GlobalRotation.RelativeTo"/>
        [Pure] public GlobalRotation ToGlobalWith(TransformProvider parent) 
            => GlobalRotation.FromLocal(parent, AsQuaternion);

        [Pure] public LocalRotation TransformBy(LocalPose offset) => offset.Rotation * this;
        
        [Pure] public static LocalOffset operator *(LocalRotation rotation, LocalOffset offset) => (rotation.AsQuaternion * offset.AsVector).AsLocalOffset();
        [Pure] public static LocalOffset operator *(LocalOffset offset, LocalRotation rotation) => (rotation.AsQuaternion * offset.AsVector).AsLocalOffset();
        [Pure] public static LocalDirection operator *(LocalRotation rotation, LocalDirection direction) => (rotation.AsQuaternion * direction.AsVector).AsLocalDirection();
        [Pure] public static LocalDirection operator *(LocalDirection direction, LocalRotation rotation) => (rotation.AsQuaternion * direction.AsVector).AsLocalDirection();
        [Pure] public static LocalRotation operator *(LocalRotation left, LocalRotation right) => new(left.AsQuaternion * right.AsQuaternion);
        [Pure] public static LocalRotation operator /(LocalRotation left, LocalRotation right) => new(left.AsQuaternion * right.AsQuaternion.Inverse());
        [Pure] public static bool operator ==(LocalRotation a, LocalRotation b) => a.AsQuaternion == b.AsQuaternion;
        [Pure] public static bool operator !=(LocalRotation a, LocalRotation b) => a.AsQuaternion != b.AsQuaternion;

        [Pure] public bool Equals(LocalRotation other) => this == other;
        [Pure] public override bool Equals(object obj) => obj is LocalRotation pos && this == pos;
        [Pure] public override int GetHashCode() => AsQuaternion.GetHashCode();

        [Pure] public static LocalRotation Lerp(
            LocalRotation start, LocalRotation end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public static LocalRotation Slerp(
            LocalRotation start, LocalRotation end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

        [Pure] public LocalRotation Map(Func<Quaternion, Quaternion> f) => LocalRotation.FromLocal(f(AsQuaternion));

        [Pure] public static Builder From(RelativeDirectionLike<IsLocal> from) => new(from);
        public readonly struct Builder {

            private readonly RelativeDirectionLike<IsLocal> from;
            public Builder(RelativeDirectionLike<IsLocal> from) => this.from = from;

            [Pure] public LocalRotation To(RelativeDirectionLike<IsLocal> to) => LocalRotation.FromLocal(
                Quaternion.FromToRotation(from.AsVector, to.AsVector)
            );
        }

        [Pure] public override string ToString() => AsQuaternion.ToString();

        [Pure] public LocalRotation CopyWithDifferentValue(Quaternion newValue) => FromLocal(newValue);

    }

    public static partial class RotationExtensions {
        
        [Pure] public static LocalRotation AsLocalRotation(this Quaternion localRotation) 
            => Geometry.LocalRotation.FromLocal(localRotation);

        [Pure] public static LocalRotation LocalRotation(this Component component) 
            => Geometry.LocalRotation.FromLocal(component.transform.localRotation);
        [Pure] public static LocalRotation LocalRotation(this GameObject gameObject) 
            => Geometry.LocalRotation.FromLocal(gameObject.transform.localRotation);

        [Pure] public static Angle AngleOn(this LocalRotation rot, LocalDirection axis) 
            => rot.AsQuaternion.AngleOn(axis.AsVector);

    }

}
