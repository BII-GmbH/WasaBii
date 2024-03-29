#if UNITY_2022_1_OR_NEWER
 #define IsUnity
#endif

using System.Runtime.InteropServices;
using BII.WasaBii.Core;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {
    
    
#if IsUnity
    using Matrix = UnityEngine.Matrix4x4;
    using Vector3 = UnityEngine.Vector3;
    using Quaternion = UnityEngine.Quaternion;
#else
    using System;
    using Matrix = System.Numerics.Matrix4x4;
    using Vector3 = System.Numerics.Vector3;
    using Quaternion = System.Numerics.Quaternion;
#endif

    /// <summary>
    /// This struct provides implicit conversions for all supported types that can be used as parents.
    /// It is designed to reduce the level of complexity in the geometry helpers, eliminating the need
    /// to implement an overload per supported type for all `RelativeTo` and `ToGlobalWith` methods.
    /// Also, it provides a central and maintainable way to manage all supported types.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct TransformProvider {

        [FieldOffset(0)] private readonly Type type;

        public enum Type : byte {
            Invalid,
            Matrix, 
#if IsUnity
            Transform, 
#endif
            Pose
        }

        [FieldOffset(1)] private readonly Matrix localToGlobalMatrix;
#if IsUnity
        [FieldOffset(1)] private readonly UnityEngine.Transform transform;
#endif
        public Matrix GlobalToLocalMatrix =>
#if IsUnity
            localToGlobalMatrix.inverse;
#else
            Matrix.Invert(localToGlobalMatrix, out var inverted)
                ? inverted
                : throw new ArgumentException($"The matrix provided to the {nameof(TransformProvider)} is not invertible");
#endif

        [FieldOffset(1)] private readonly GlobalPose pose;

        private TransformProvider(Matrix localToGlobalMatrix) {
#if IsUnity
            transform = default!;
#endif
            pose = default!;
            this.localToGlobalMatrix = localToGlobalMatrix;
            type = Type.Matrix;
        }
#if IsUnity
        private TransformProvider(Vector3 position, Quaternion rotation, Vector3 scale) 
            : this(Matrix.TRS(position, rotation, scale)) {}

        private TransformProvider(UnityEngine.Transform transform) {
            localToGlobalMatrix = default!;
            pose = default!;
            this.transform = transform;
            type = Type.Transform;
        }
#else
        private TransformProvider(Vector3 position, Quaternion rotation, Vector3 scale) 
            : this(Matrix.CreateTranslation(position) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateScale(scale)) {}
#endif

        private TransformProvider(GlobalPose pose) {
            localToGlobalMatrix = default!;
#if IsUnity
            transform = default!;
#endif
            this.pose = pose;
            type = Type.Pose;
        }
        
        /// <summary>
        /// Transforms a position from local space to global space.
        /// </summary>
        [Pure]
        public GlobalPosition TransformPoint(LocalPosition local) => type switch {
#if IsUnity
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            Type.Matrix => localToGlobalMatrix.MultiplyPoint3x4(local.AsUnityVector).AsGlobalPosition(),
            Type.Transform => transform.TransformPoint(local.AsUnityVector).AsGlobalPosition(),
#else
            Type.Matrix => Vector3.Transform(local.AsNumericsVector, localToGlobalMatrix).AsGlobalPosition(),
#endif
            Type.Pose => pose.Position + local.AsNumericsVector.AsGlobalOffset() * pose.Rotation,
            _ => throw new UnsupportedEnumValueException(type)
        };
            
        /// <summary>
        /// Transforms a position from global space to local space.
        /// </summary>
        [Pure]
        public LocalPosition InverseTransformPoint(GlobalPosition global) => type switch {
            #if IsUnity
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            Type.Matrix => GlobalToLocalMatrix.MultiplyPoint3x4(global.AsUnityVector).AsLocalPosition(),
            Type.Transform => transform.InverseTransformPoint(global.AsUnityVector).AsLocalPosition(),
            #else
            Type.Matrix => Vector3.Transform(global.AsNumericsVector, GlobalToLocalMatrix).AsLocalPosition(),
            #endif
            Type.Pose => ((global - pose.Position) * pose.Rotation.Inverse).AsNumericsVector.AsLocalPosition(),
            _ => throw new UnsupportedEnumValueException(type)
        };

        /// <summary>
        /// Transforms an offset from local space to global space.
        /// </summary>
        [Pure]
        public GlobalOffset TransformOffset(LocalOffset local) => type switch {
            #if IsUnity
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            Type.Matrix => localToGlobalMatrix.MultiplyVector(local.AsUnityVector).AsGlobalOffset(),
            Type.Transform => transform.TransformVector(local.AsUnityVector).AsGlobalOffset(),
            #else
            Type.Matrix => Vector3.TransformNormal(local.AsNumericsVector, localToGlobalMatrix).AsGlobalOffset(),
            #endif
            Type.Pose => local.AsNumericsVector.AsGlobalOffset() * pose.Rotation,
            _ => throw new UnsupportedEnumValueException(type)
        };
        
        /// <summary>
        /// Transforms an offset from global to local space.
        /// </summary>
        [Pure]
        public LocalOffset InverseTransformOffset(GlobalOffset global) => type switch {
            #if IsUnity
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            Type.Matrix => GlobalToLocalMatrix.MultiplyVector(global.AsUnityVector).AsLocalOffset(),
            Type.Transform => transform.InverseTransformVector(global.AsUnityVector).AsLocalOffset(),
            #else
            Type.Matrix => Vector3.TransformNormal(global.AsNumericsVector, GlobalToLocalMatrix).AsLocalOffset(),
            #endif
            Type.Pose => (global * pose.Rotation.Inverse).AsNumericsVector.AsLocalOffset(),
            _ => throw new UnsupportedEnumValueException(type)
        };

        /// <summary>
        /// Transforms a direction from local space to global space.
        /// </summary>
        [Pure]
        public GlobalDirection TransformDirection(LocalDirection local) => type switch {
            #if IsUnity
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            Type.Matrix => localToGlobalMatrix.MultiplyVector(local.AsUnityVector).AsGlobalDirection(),
            Type.Transform => transform.TransformDirection(local.AsUnityVector).AsGlobalDirection(),
            #else
            Type.Matrix => Vector3.TransformNormal(local.AsNumericsVector, localToGlobalMatrix).AsGlobalDirection(),
            #endif
            Type.Pose => local.AsNumericsVector.AsGlobalDirection() * pose.Rotation,
            _ => throw new UnsupportedEnumValueException(type)
        };
        
        /// <summary>
        /// Transforms a direction from global to local space.
        /// </summary>
        [Pure]
        public LocalDirection InverseTransformDirection(GlobalDirection global) => type switch {
            #if IsUnity
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            Type.Matrix => GlobalToLocalMatrix.MultiplyVector(global.AsUnityVector).AsLocalDirection(),
            Type.Transform => transform.InverseTransformDirection(global.AsUnityVector).AsLocalDirection(),
            #else
            Type.Matrix => Vector3.TransformNormal(global.AsNumericsVector, GlobalToLocalMatrix).AsLocalDirection(),
            #endif
            Type.Pose => (global * pose.Rotation.Inverse).AsNumericsVector.AsLocalDirection(),
            _ => throw new UnsupportedEnumValueException(type)
        };

        /// <summary>
        /// Transforms a rotation from local space to global space.
        /// </summary>
        [Pure]
        public GlobalRotation TransformRotation(LocalRotation local) => type switch {
            #if IsUnity
            Type.Matrix => (localToGlobalMatrix.rotation * local.AsUnityQuaternion).AsGlobalRotation(),
            Type.Transform => (transform.rotation * local.AsUnityQuaternion).AsGlobalRotation(),
            #else
            Type.Matrix => Quaternion.Concatenate(Quaternion.CreateFromRotationMatrix(localToGlobalMatrix), local.AsNumericsQuaternion).AsGlobalRotation(),
            #endif
            Type.Pose => pose.Rotation * local.AsNumericsQuaternion.AsGlobalRotation(),
            _ => throw new UnsupportedEnumValueException(type)
        };

        /// <summary>
        /// Transforms a rotation from global to local space.
        /// </summary>
        [Pure]
        public LocalRotation InverseTransformRotation(GlobalRotation global) => type switch {
            #if IsUnity
            Type.Matrix => (GlobalToLocalMatrix.rotation * global.AsUnityQuaternion).AsLocalRotation(),
            Type.Transform => (Quaternion.Inverse(transform.rotation) * global.AsUnityQuaternion).AsLocalRotation(),
            #else
            Type.Matrix => Quaternion.Concatenate(Quaternion.CreateFromRotationMatrix(GlobalToLocalMatrix), global.AsNumericsQuaternion).AsLocalRotation(),
            #endif
            Type.Pose => (pose.Rotation.Inverse * global).AsNumericsQuaternion.AsLocalRotation(),
            _ => throw new UnsupportedEnumValueException(type)
        };

        [Pure]
        public static TransformProvider From(Vector3? pos = null, Quaternion? rotation = null, Vector3? scale = null) =>
#if IsUnity
            new(pos ?? Vector3.zero, rotation ?? Quaternion.identity, scale ?? Vector3.one);
#else
            new(pos ?? Vector3.Zero, rotation ?? Quaternion.Identity, scale ?? Vector3.One);
#endif
        
#if IsUnity
        public static implicit operator TransformProvider(UnityEngine.Component component)
            => new(component.transform);
        public static implicit operator TransformProvider(UnityEngine.GameObject gameObject)
            => new(gameObject.transform);
#endif
        public static implicit operator TransformProvider(Matrix localToGlobalMatrix)
            => new(localToGlobalMatrix);
        public static implicit operator TransformProvider(GlobalPose pose)
            => new(pose);
    }

}