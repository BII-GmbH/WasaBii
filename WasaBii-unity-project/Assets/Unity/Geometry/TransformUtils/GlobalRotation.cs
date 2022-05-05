using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {

    /// A wrapper for a <see cref="Quaternion"/> that represents a world-space rotation.
    [MustBeImmutable]
    [MustBeSerializable]
    public readonly struct GlobalRotation : QuaternionLike<GlobalRotation>, IsGlobalVariant<GlobalRotation, LocalRotation> {

        public static readonly GlobalRotation Identity = FromGlobal(Quaternion.identity);

        public Quaternion AsQuaternion { get; }

        private GlobalRotation(Quaternion local) => AsQuaternion = local;
        
        [Pure] public static GlobalRotation FromGlobal(Quaternion global) => new GlobalRotation(global);

        [Pure] public static GlobalRotation FromLocal(TransformProvider parent, Quaternion local) =>
            FromGlobal(parent.TransformQuaternion(local));

        [Pure] public static GlobalRotation FromAngleAxis(Angle angle, GlobalDirection axis) => angle.WithAxis(axis);
        
        [Pure] public static GlobalRotation FromTransform(Transform parent) => new GlobalRotation(parent.rotation);

        /// Transforms the global rotation into local space, relative to the <see cref="parent"/>.
        /// This is the inverse of <see cref="LocalRotation.ToGlobalWith"/>
        [Pure] public LocalRotation RelativeTo(TransformProvider parent) 
            => LocalRotation.FromGlobal(parent, AsQuaternion);

        [Pure] public GlobalRotation AsLocalRotationOf(TransformProvider oldParent, TransformProvider newParent) 
            => this.RelativeTo(oldParent).ToGlobalWith(newParent);

        [Pure] public static GlobalOffset operator *(GlobalRotation rotation, GlobalOffset offset) => (rotation.AsQuaternion * offset.AsVector).AsGlobalOffset();
        [Pure] public static GlobalOffset operator *(GlobalOffset offset, GlobalRotation rotation) => (rotation.AsQuaternion * offset.AsVector).AsGlobalOffset();
        [Pure] public static GlobalDirection operator *(GlobalRotation rotation, GlobalDirection direction) => (rotation.AsQuaternion * direction.AsVector).AsGlobalDirection();
        [Pure] public static GlobalDirection operator *(GlobalDirection direction, GlobalRotation rotation) => (rotation.AsQuaternion * direction.AsVector).AsGlobalDirection();
        [Pure] public static GlobalRotation operator *(GlobalRotation left, GlobalRotation right) => new GlobalRotation(left.AsQuaternion * right.AsQuaternion);
        [Pure] public static GlobalRotation operator /(GlobalRotation left, GlobalRotation right) => new GlobalRotation(left.AsQuaternion * right.AsQuaternion.Inverse());
        [Pure] public static bool operator ==(GlobalRotation a, GlobalRotation b) => a.AsQuaternion == b.AsQuaternion;
        [Pure] public static bool operator !=(GlobalRotation a, GlobalRotation b) => a.AsQuaternion != b.AsQuaternion;

        [Pure] public bool Equals(GlobalRotation other) => this == other;
        [Pure] public override bool Equals(object obj) => obj is GlobalRotation pos && this == pos;
        [Pure] public override int GetHashCode() => AsQuaternion.GetHashCode();

        [Pure] public static GlobalRotation Lerp(
            GlobalRotation start, GlobalRotation end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public static GlobalRotation Slerp(
            GlobalRotation start, GlobalRotation end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

        [Pure] public static Builder From(RelativeDirectionLike<IsGlobal> from) => new Builder(from);
        public readonly struct Builder {

            private readonly RelativeDirectionLike<IsGlobal> from;
            public Builder(RelativeDirectionLike<IsGlobal> from) => this.from = from;

            [Pure] public GlobalRotation To(RelativeDirectionLike<IsGlobal> to) => GlobalRotation.FromGlobal(
                Quaternion.FromToRotation(from.AsVector, to.AsVector)
            );
        }

        [Pure] public override string ToString() => AsQuaternion.ToString();

        public GlobalRotation CopyWithDifferentValue(Quaternion newValue) => FromGlobal(newValue);
    }

    public static partial class RotationExtensions {
        [Pure] public static GlobalRotation AsGlobalRotation(this Quaternion globalRotation) 
            => Geometry.GlobalRotation.FromGlobal(globalRotation);
        
        [Pure] public static GlobalRotation GlobalRotation(this Component component) 
            => Geometry.GlobalRotation.FromGlobal(component.transform.rotation);
        [Pure] public static GlobalRotation GlobalRotation(this GameObject gameObject) 
            => Geometry.GlobalRotation.FromGlobal(gameObject.transform.rotation);

        [Pure] public static Angle AngleOn(this GlobalRotation rot, GlobalDirection axis) 
            => rot.AsQuaternion.AngleOn(axis.AsVector);

    }

}