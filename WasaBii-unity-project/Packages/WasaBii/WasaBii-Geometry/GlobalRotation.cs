using System;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// <summary>
    /// A quaternion-based representation of a world-space rotation.
    /// </summary>
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent: false, hasMagnitude: false, hasOrientation: true)]
    public partial struct GlobalRotation : IsGlobalVariant<GlobalRotation, LocalRotation> {

        public static readonly GlobalRotation Identity = new(System.Numerics.Quaternion.Identity);

        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        public UnityEngine.Quaternion AsUnityQuaternion { get; private set; }
        public readonly System.Numerics.Quaternion AsNumericsQuaternion => AsUnityQuaternion.ToSystemQuaternion();
        
        public GlobalRotation(UnityEngine.Quaternion toWrap) => AsUnityQuaternion = toWrap;
        public GlobalRotation(System.Numerics.Quaternion toWrap) => AsUnityQuaternion = toWrap.ToUnityQuaternion();
        #else
        public System.Numerics.Quaternion AsNumericsQuaternion { get; private set; }
        public GlobalRotation(System.Numerics.Quaternion toWrap) => AsNumericsQuaternion = toWrap;
        #endif

        public readonly GlobalRotation Inverse => AsNumericsQuaternion.Inverse().AsGlobalRotation();

        [Pure] public static GlobalRotation FromAngleAxis(Angle angle, GlobalDirection axis) => angle.WithAxis(axis.AsNumericsVector).AsGlobalRotation();

        /// <summary>
        /// <inheritdoc cref="TransformProvider.InverseTransformRotation"/>
        /// This is the inverse of <see cref="LocalRotation.ToGlobalWith"/>
        /// </summary>
        /// <example> <code>global.RelativeTo(parent).ToGlobalWith(parent) == global</code> </example>
        [Pure] public LocalRotation RelativeTo(TransformProvider parent) 
            => parent.InverseTransformRotation(this);

        public LocalRotation RelativeToWorldZero => new(AsNumericsQuaternion);

        [Pure] public static GlobalOffset operator *(GlobalRotation rotation, GlobalOffset offset) =>
            System.Numerics.Vector3.Transform(offset.AsNumericsVector, rotation.AsNumericsQuaternion).AsGlobalOffset();
        [Pure] public static GlobalOffset operator *(GlobalOffset offset, GlobalRotation rotation) => rotation * offset;
        [Pure] public static GlobalDirection operator *(GlobalRotation rotation, GlobalDirection direction) =>
            System.Numerics.Vector3.Transform(direction.AsNumericsVector, rotation.AsNumericsQuaternion).AsGlobalDirection();
        [Pure] public static GlobalDirection operator *(GlobalDirection direction, GlobalRotation rotation) => rotation * direction;
        
        [Pure] public static GlobalRotation operator *(GlobalRotation left, GlobalRotation right) => 
            System.Numerics.Quaternion.Concatenate(left.AsNumericsQuaternion, right.AsNumericsQuaternion).AsGlobalRotation();

        [Pure] public static Builder From<T>(T from) where T : struct, GlobalDirectionLike<T> => 
            new(from switch {
                GlobalDirection dir => dir.AsNumericsVector,
                _ => from.AsNumericsVector.Normalized()
            });
        
        public readonly struct Builder {

            private readonly System.Numerics.Vector3 from;
            
            /// <param name="from">Must be normalized</param>
            public Builder(System.Numerics.Vector3 from) => this.from = from;

            [Pure]
            public GlobalRotation To<T>(T to, Func<T, T>? axisIfOpposite = null) where T : struct, GlobalDirectionLike<T> => To(
                to switch {
                    GlobalDirection dir => dir.AsNumericsVector,
                    _ => to.AsNumericsVector.Normalized()
                },
                axisIfOpposite: axisIfOpposite == null ? null : _ => axisIfOpposite(to).AsNumericsVector
            );

            /// <param name="to">Must be normalized</param>
            [Pure]
            public GlobalRotation To(System.Numerics.Vector3 to, Func<System.Numerics.Vector3, System.Numerics.Vector3>? axisIfOpposite = null) => 
                from.RotationTo(to, axisIfOpposite).AsGlobalRotation();
        }
        
        [Pure] public Angle AngleOn(GlobalDirection axis, Handedness handedness = Handedness.Default) 
            => this.AsNumericsQuaternion.AngleOn(axis.AsNumericsVector, handedness);

    }

    public static partial class GlobalRotationExtensions {
        [Pure] public static GlobalRotation AsGlobalRotation(this System.Numerics.Quaternion globalRotation) 
            => new(globalRotation);
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalRotation AsGlobalRotation(this UnityEngine.Quaternion globalRotation) 
            => new(globalRotation);
        [Pure] public static GlobalRotation GlobalRotation(this UnityEngine.Component component) 
            => new(component.transform.rotation);
        [Pure] public static GlobalRotation GlobalRotation(this UnityEngine.GameObject gameObject) 
            => new(gameObject.transform.rotation);
        #endif

    }

}