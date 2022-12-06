using System;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A quaternion-based representation of a world-space rotation.
    [MustBeImmutable]
    [MustBeSerializable]
    [GeometryHelper(areFieldsIndependent: false, fieldType: FieldType.Other, hasMagnitude: false, hasDirection: false)]
    public readonly partial struct GlobalRotation : IsGlobalVariant<GlobalRotation, LocalRotation> {

        public static readonly GlobalRotation Identity = FromGlobal(System.Numerics.Quaternion.Identity);

        public readonly System.Numerics.Quaternion AsNumericsQuaternion;
        
        public GlobalRotation Inverse => System.Numerics.Quaternion.Inverse(AsNumericsQuaternion).AsGlobalRotation();

        private GlobalRotation(System.Numerics.Quaternion local) => AsNumericsQuaternion = local;
        
        [Pure] public static GlobalRotation FromGlobal(System.Numerics.Quaternion global) => new(global);

        [Pure] public static GlobalRotation FromAngleAxis(Angle angle, GlobalDirection axis) => angle.WithAxis(axis.AsNumericsVector).AsGlobalRotation();
        
        #if UNITY_2022_1_OR_NEWER
        public UnityEngine.Quaternion AsUnityQuaternion => AsNumericsQuaternion.ToUnityQuaternion();
        
        [Pure] public static GlobalRotation FromGlobal(UnityEngine.Quaternion global) => new(global.ToSystemQuaternion());
        #endif

        /// <inheritdoc cref="TransformProvider.InverseTransformRotation"/>
        /// This is the inverse of <see cref="LocalRotation.ToGlobalWith"/>
        [Pure] public LocalRotation RelativeTo(TransformProvider parent) 
            => parent.InverseTransformRotation(this);

        public LocalRotation RelativeToWorldZero => LocalRotation.FromLocal(AsNumericsQuaternion);

        [Pure] public static GlobalOffset operator *(GlobalRotation rotation, GlobalOffset offset) =>
            System.Numerics.Vector3.Transform(offset.AsNumericsVector, rotation.AsNumericsQuaternion).AsGlobalOffset();
        [Pure] public static GlobalOffset operator *(GlobalOffset offset, GlobalRotation rotation) =>
            System.Numerics.Vector3.Transform(offset.AsNumericsVector, rotation.AsNumericsQuaternion).AsGlobalOffset();
        [Pure] public static GlobalDirection operator *(GlobalRotation rotation, GlobalDirection direction) =>
            System.Numerics.Vector3.Transform(direction.AsNumericsVector, rotation.AsNumericsQuaternion).AsGlobalDirection();
        [Pure] public static GlobalDirection operator *(GlobalDirection direction, GlobalRotation rotation) =>
            System.Numerics.Vector3.Transform(direction.AsNumericsVector, rotation.AsNumericsQuaternion).AsGlobalDirection();
        [Pure] public static GlobalRotation operator *(GlobalRotation left, GlobalRotation right) => 
            System.Numerics.Quaternion.Concatenate(left.AsNumericsQuaternion, right.AsNumericsQuaternion).AsGlobalRotation();

        [Pure] public static GlobalRotation Lerp(
            GlobalRotation start, GlobalRotation end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);

        public GlobalRotation LerpTo(GlobalRotation target, double progress, bool shouldClamp = true) => 
            System.Numerics.Quaternion.Lerp(AsNumericsQuaternion, target.AsNumericsQuaternion, (float)(shouldClamp ? progress.Clamp01() : progress)).AsGlobalRotation();

        [Pure] public static GlobalRotation Slerp(
            GlobalRotation start, GlobalRotation end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

        public GlobalRotation SlerpTo(GlobalRotation target, double progress, bool shouldClamp = true) => 
            System.Numerics.Quaternion.Slerp(AsNumericsQuaternion, target.AsNumericsQuaternion, (float)(shouldClamp ? progress.Clamp01() : progress)).AsGlobalRotation();

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
            public GlobalRotation To<T>(T to, T? axisIfOpposite = null) where T : struct, GlobalDirectionLike<T> => To(
                to switch {
                    GlobalDirection dir => dir.AsNumericsVector,
                    _ => to.AsNumericsVector.Normalized()
                },
                axisIfOpposite: axisIfOpposite?.AsNumericsVector
            );
            
            /// <param name="to">Must be normalized</param>
            [Pure]
            public GlobalRotation To(System.Numerics.Vector3 to, System.Numerics.Vector3? axisIfOpposite = null) {
                // https://stackoverflow.com/a/1171995
                var normal = System.Numerics.Vector3.Cross(from, to);
                var dot = System.Numerics.Vector3.Dot(from, to);
                return new GlobalRotation(dot switch {
                    // vectors are parallel, no rotation
                    >= 0.9999f => System.Numerics.Quaternion.Identity,
                    // vectors are opposite, rotate 180° around any axis
                    <= -0.9999f =>  System.Numerics.Quaternion.CreateFromAxisAngle(axisIfOpposite ?? System.Numerics.Vector3.UnitY, MathF.PI),
                    _ => System.Numerics.Quaternion.Normalize(new System.Numerics.Quaternion(normal, dot))
                });
            }
        }

        #if UNITY_2022_1_OR_NEWER
        [Pure] public Angle AngleOn(GlobalDirection axis) 
            => this.AsUnityQuaternion.AngleOn(axis.AsUnityVector);
        #endif

        [Pure] public override string ToString() => AsNumericsQuaternion.ToString();

    }

    public static class GlobalRotationExtensions {
        [Pure] public static GlobalRotation AsGlobalRotation(this System.Numerics.Quaternion globalRotation) 
            => BII.WasaBii.Geometry.GlobalRotation.FromGlobal(globalRotation);
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalRotation AsGlobalRotation(this UnityEngine.Quaternion globalRotation) 
            => BII.WasaBii.Geometry.GlobalRotation.FromGlobal(globalRotation);
        [Pure] public static GlobalRotation GlobalRotation(this UnityEngine.Component component) 
            => BII.WasaBii.Geometry.GlobalRotation.FromGlobal(component.transform.rotation);
        [Pure] public static GlobalRotation GlobalRotation(this UnityEngine.GameObject gameObject) 
            => BII.WasaBii.Geometry.GlobalRotation.FromGlobal(gameObject.transform.rotation);
        #endif

    }

}