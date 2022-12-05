using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using System.Numerics;

namespace BII.WasaBii.Geometry {

    /// A quaternion-based representation of a world-space rotation.
    [MustBeImmutable]
    [MustBeSerializable]
    [GeometryHelper(areFieldsIndependent: false, fieldType: FieldType.Other, hasMagnitude: false, hasDirection: false)]
    public readonly partial struct GlobalRotation : IsGlobalVariant<GlobalRotation, LocalRotation> {

        public static readonly GlobalRotation Identity = FromGlobal(Quaternion.Identity);

        public readonly Quaternion AsNumericsQuaternion;
        
        public GlobalRotation Inverse => Quaternion.Inverse(AsNumericsQuaternion).AsGlobalRotation();

        private GlobalRotation(Quaternion local) => AsNumericsQuaternion = local;
        
        [Pure] public static GlobalRotation FromGlobal(Quaternion global) => new(global);

        [Pure] public static GlobalRotation FromAngleAxis(Angle angle, GlobalDirection axis) => angle.WithAxis(axis.AsNumericsVector).AsGlobalRotation();
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalRotation FromGlobal(UnityEngine.Quaternion global) => new(global.ToSystemQuaternion());
        #endif

        /// <inheritdoc cref="TransformProvider.InverseTransformRotation"/>
        /// This is the inverse of <see cref="LocalRotation.ToGlobalWith"/>
        [Pure] public LocalRotation RelativeTo(TransformProvider parent) 
            => parent.InverseTransformRotation(this);

        public LocalRotation RelativeToWorldZero => LocalRotation.FromLocal(AsNumericsQuaternion);

        [Pure] public static GlobalOffset operator *(GlobalRotation rotation, GlobalOffset offset) =>
            Vector3.Transform(offset.AsNumericsVector, rotation.AsNumericsQuaternion).AsGlobalOffset();
        [Pure] public static GlobalOffset operator *(GlobalOffset offset, GlobalRotation rotation) =>
            Vector3.Transform(offset.AsNumericsVector, rotation.AsNumericsQuaternion).AsGlobalOffset();
        [Pure] public static GlobalDirection operator *(GlobalRotation rotation, GlobalDirection direction) =>
            Vector3.Transform(direction.AsNumericsVector, rotation.AsNumericsQuaternion).AsGlobalDirection();
        [Pure] public static GlobalDirection operator *(GlobalDirection direction, GlobalRotation rotation) =>
            Vector3.Transform(direction.AsNumericsVector, rotation.AsNumericsQuaternion).AsGlobalDirection();
        [Pure] public static GlobalRotation operator *(GlobalRotation left, GlobalRotation right) => 
            Quaternion.Concatenate(left.AsNumericsQuaternion, right.AsNumericsQuaternion).AsGlobalRotation();

        [Pure] public static GlobalRotation Lerp(
            GlobalRotation start, GlobalRotation end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);

        public GlobalRotation LerpTo(GlobalRotation target, double progress, bool shouldClamp = true) => 
            Quaternion.Lerp(AsNumericsQuaternion, target.AsNumericsQuaternion, (float)(shouldClamp ? progress.Clamp01() : progress)).AsGlobalRotation();

        [Pure] public static GlobalRotation Slerp(
            GlobalRotation start, GlobalRotation end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

        public GlobalRotation SlerpTo(GlobalRotation target, double progress, bool shouldClamp = true) => 
            Quaternion.Slerp(AsNumericsQuaternion, target.AsNumericsQuaternion, (float)(shouldClamp ? progress.Clamp01() : progress)).AsGlobalRotation();

        [Pure] public static Builder From<T>(T from) where T : struct, GlobalDirectionLike<T> => 
            new(from.AsNumericsVector);
        
        public readonly struct Builder {

            private readonly Vector3 from;
            public Builder(Vector3 from) => this.from = from;

            [Pure]
            public GlobalRotation To<T>(T to) where T : struct, GlobalDirectionLike<T> => To(to.AsNumericsVector);
            
            [Pure]
            public GlobalRotation To(Vector3 to) {
                Vector3.
                var axis = Vector3.Cross(from, to).AsGlobalDirection();
            }
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