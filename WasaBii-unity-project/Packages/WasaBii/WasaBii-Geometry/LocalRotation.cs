using System;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A quaternion-based representation of a local rotation relative to an undefined parent.
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent: false, hasMagnitude: false, hasOrientation: true)]
    public partial struct LocalRotation : IsLocalVariant<LocalRotation, GlobalRotation> {

        public static readonly LocalRotation Identity = new(System.Numerics.Quaternion.Identity);

        #if UNITY_2022_1_OR_NEWER
        [UnityEngine.SerializeField]
        private UnityEngine.Quaternion _underlying;
        public readonly UnityEngine.Quaternion AsUnityQuaternion => _underlying;
        public readonly System.Numerics.Quaternion AsNumericsQuaternion => _underlying.ToSystemQuaternion();
        
        public LocalRotation(UnityEngine.Quaternion toWrap) => _underlying = toWrap;
        public LocalRotation(System.Numerics.Quaternion toWrap) => _underlying = toWrap.ToUnityQuaternion();
        #else
        private System.Numerics.Quaternion _underlying;
        public System.Numerics.Quaternion AsNumericsQuaternion => _underlying;
        public LocalRotation(System.Numerics.Quaternion toWrap) => _underlying = toWrap;
        #endif

        public readonly LocalRotation Inverse => AsNumericsQuaternion.Inverse().AsLocalRotation();
        
        [Pure] public static LocalRotation FromAngleAxis(Angle angle, LocalDirection axis) => angle.WithAxis(axis.AsNumericsVector).AsLocalRotation();
        
        /// <inheritdoc cref="TransformProvider.TransformRotation"/>
        /// This is the inverse of <see cref="GlobalRotation.RelativeTo"/>
        [Pure] public GlobalRotation ToGlobalWith(TransformProvider parent) 
            => parent.TransformRotation(this);

        public GlobalRotation ToGlobalWithWorldZero => new(AsNumericsQuaternion);

        // TODO DS: this
        [Pure] public LocalRotation TransformBy(LocalPose offset) => offset.Rotation * this;
        
        [Pure] public static LocalOffset operator *(LocalRotation rotation, LocalOffset offset) => 
            System.Numerics.Vector3.Transform(offset.AsNumericsVector, rotation.AsNumericsQuaternion).AsLocalOffset();
        [Pure] public static LocalOffset operator*(LocalOffset offset, LocalRotation rotation) => rotation * offset;
        
        [Pure] public static LocalDirection operator *(LocalRotation rotation, LocalDirection direction) => 
            System.Numerics.Vector3.Transform(direction.AsNumericsVector, rotation.AsNumericsQuaternion).AsLocalDirection();
        [Pure] public static LocalDirection operator *(LocalDirection direction, LocalRotation rotation) => rotation * direction;
        
        [Pure] public static LocalRotation operator *(LocalRotation left, LocalRotation right) => 
            new(System.Numerics.Quaternion.Concatenate(left.AsNumericsQuaternion, right.AsNumericsQuaternion));
        
        [Pure] public static Builder From<T>(T from) where T : struct, LocalDirectionLike<T> => 
            new(from switch {
                LocalDirection dir => dir.AsNumericsVector,
                _ => from.AsNumericsVector.Normalized()
            });
        
        public readonly struct Builder {

            private readonly System.Numerics.Vector3 from;
            
            /// <param name="from">Must be normalized</param>
            public Builder(System.Numerics.Vector3 from) => this.from = from;

            [Pure]
            public LocalRotation To<T>(T to, Func<T, T> axisIfOpposite = null) where T : struct, LocalDirectionLike<T> => To(
                to switch {
                    LocalDirection dir => dir.AsNumericsVector,
                    _ => to.AsNumericsVector.Normalized()
                },
                axisIfOpposite: axisIfOpposite == null ? null : _ => axisIfOpposite(to).AsNumericsVector
            );

            /// <param name="to">Must be normalized</param>
            [Pure]
            public LocalRotation To(System.Numerics.Vector3 to, Func<System.Numerics.Vector3, System.Numerics.Vector3> axisIfOpposite = null) => 
                from.RotationTo(to, axisIfOpposite).AsLocalRotation();
        }

        [Pure] public Angle AngleOn(LocalDirection axis) 
            => this.AsNumericsQuaternion.AngleOn(axis.AsNumericsVector);

        [Pure] public override string ToString() => AsNumericsQuaternion.ToString();

    }

    public static partial class LocalRotationExtensions {
        [Pure] public static LocalRotation AsLocalRotation(this System.Numerics.Quaternion localRotation) 
            => new(localRotation);
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static LocalRotation AsLocalRotation(this UnityEngine.Quaternion localRotation) 
            => new(localRotation);
        [Pure] public static LocalRotation LocalRotation(this UnityEngine.Component component) 
            => new(component.transform.rotation);
        [Pure] public static LocalRotation LocalRotation(this UnityEngine.GameObject gameObject) 
            => new(gameObject.transform.rotation);
        #endif

    }

}
