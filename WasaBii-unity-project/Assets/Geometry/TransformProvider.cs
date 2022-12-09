// #define IsUnity
// UNITY_2022_1_OR_NEWER

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using BII.WasaBii.Core;

namespace BII.WasaBii.Geometry {
    
    
#if IsUnity
    using Matrix = UnityEngine.Matrix4x4;
    using Vector3 = UnityEngine.Vector3;
    using Vector4 = UnityEngine.Vector4;
    using Quaternion = UnityEngine.Quaternion;
#else
    using Matrix = System.Numerics.Matrix4x4;
    using Vector3 = System.Numerics.Vector3;
    using Vector4 = System.Numerics.Vector4;
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

        public enum Type : byte {
            Invalid,
            Matrix, 
#if IsUnity
            Transform, 
#endif
            Pose
        }

        [FieldOffset(0)] private readonly Type type;
        
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
            transform = default;
#endif
            pose = default;
            this.localToGlobalMatrix = localToGlobalMatrix;
            type = Type.Matrix;
        }
#if IsUnity
        private TransformProvider(Vector3 position, Quaternion rotation, Vector3 scale) 
            : this(Matrix.TRS(position, rotation, scale)) {}

        private TransformProvider(UnityEngine.Transform transform) {
            localToGlobalMatrix = default;
            this.transform = transform;
            type = Type.Transform;
        }
#else
        private TransformProvider(Vector3 position, Quaternion rotation, Vector3 scale) 
            : this(Matrix.CreateTranslation(position) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateScale(scale)) {}
#endif

        private TransformProvider(GlobalPose pose) {
            localToGlobalMatrix = default;
#if IsUnity
            transform = default;
#endif
            this.pose = pose;
            type = Type.Pose;
        }
        
        /// <summary>
        /// Transforms a position from local space to global space.
        /// </summary>
        public GlobalPosition TransformPoint(LocalPosition local) => type switch {
#if IsUnity
            Type.Matrix => (localToGlobalMatrix * local.AsUnityVector.WithW(1)).xyz().AsGlobalPosition(),
            Type.Transform => transform.TransformPoint(local.AsUnityVector).AsGlobalPosition(),
#else
            Type.Matrix => Vector4.Transform(
                local.AsVector.Let(vec3 => new Vector4(vec3.X, vec3.Y, vec3.Z, 1)), 
                localToGlobalMatrix
            ).Let(vec4 => new Vector3(vec4.X, vec4.Y, vec4.Z)).AsGlobalPosition(),
#endif
            Type.Pose => pose.Position + local.AsOffset.ToGlobalWithWorldZero * pose.Rotation,
            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(Type))
        };
            
        /// <summary>
        /// Transforms a position from global space to local space.
        /// </summary>
        public LocalPosition InverseTransformPoint(GlobalPosition global) => type switch {
            #if IsUnity
            Type.Matrix => (GlobalToLocalMatrix * global.AsUnityVector.WithW(1)).xyz().AsLocalPosition(),
            Type.Transform => transform.InverseTransformPoint(global.AsUnityVector).AsLocalPosition(),
            #else
            Type.Matrix => Vector4.Transform(
                global.AsNumericsVector.Let(vec3 => new Vector4(vec3.X, vec3.Y, vec3.Z, 1)), 
                GlobalToLocalMatrix
            ).Let(vec4 => new Vector3(vec4.X, vec4.Y, vec4.Z)).AsLocalPosition(),
            #endif
            Type.Pose => ((global - pose.Position) * pose.Rotation.Inverse).RelativeToWorldZero,
            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(Type))
        };

        /// <summary>
        /// Transforms an offset from local space to global space.
        /// </summary>
        public GlobalOffset TransformOffset(LocalOffset local) => type switch {
            #if IsUnity
            Type.Matrix => (localToGlobalMatrix * local.AsUnityVector.WithW(0)).xyz().AsGlobalOffset(),
            Type.Transform => transform.TransformVector(local.AsUnityVector).AsGlobalOffset(),
            #else
            Type.Matrix => Vector4.Transform(
                local.AsVector.Let(vec3 => new Vector4(vec3.X, vec3.Y, vec3.Z, 0)), 
                localToGlobalMatrix
            ).Let(vec4 => new Vector3(vec4.X, vec4.Y, vec4.Z)).AsGlobalOffset(),
            #endif
            Type.Pose => local.ToGlobalWithWorldZero * pose.Rotation,
            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(Type))
        };
        
        /// <summary>
        /// Transforms an offset from global to local space.
        /// </summary>
        public LocalOffset InverseTransformOffset(GlobalOffset global) => type switch {
            #if IsUnity
            Type.Matrix => (GlobalToLocalMatrix * global.AsUnityVector.WithW(0)).xyz().AsLocalPosition(),
            Type.Transform => transform.InverseTransformVector(global.AsUnityVector).AsLocalPosition(),
            #else
            Type.Matrix => Vector4.Transform(
                global.AsVector.Let(vec3 => new Vector4(vec3.X, vec3.Y, vec3.Z, 0)), 
                GlobalToLocalMatrix
            ).Let(vec4 => new Vector3(vec4.X, vec4.Y, vec4.Z)).AsLocalPosition(),
            #endif
            Type.Pose => (global * pose.Rotation.Inverse).RelativeToWorldZero,
            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(Type))
        };

        /// <summary>
        /// Transforms a direction from local space to global space.
        /// </summary>
        public GlobalDirection TransformDirection(LocalDirection local) => type switch {
            #if IsUnity
            Type.Matrix => (localToGlobalMatrix * local.AsUnityVector.WithW(0)).xyz().AsGlobalDirection(),
            Type.Transform => transform.TransformDirection(local.AsUnityVector).AsGlobalDirection(),
            #else
            Type.Matrix => Vector4.Transform(
                local.AsVector.Let(vec3 => new Vector4(vec3.X, vec3.Y, vec3.Z, 0)), 
                localToGlobalMatrix
            ).Let(vec4 => new Vector3(vec4.X, vec4.Y, vec4.Z)).AsGlobalDirection(),
            #endif
            Type.Pose => local.ToGlobalWithWorldZero * pose.Rotation,
            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(Type))
        };
        
        /// <summary>
        /// Transforms a direction from global to local space.
        /// </summary>
        public LocalDirection InverseTransformDirection(GlobalDirection global) => type switch {
            #if IsUnity
            Type.Matrix => (GlobalToLocalMatrix * global.AsUnityVector.WithW(0)).xyz().AsLocalPosition(),
            Type.Transform => transform.InverseTransformDirection(global.AsUnityVector).AsLocalPosition(),
            #else
            Type.Matrix => Vector4.Transform(
                global.AsVector.Let(vec3 => new Vector4(vec3.X, vec3.Y, vec3.Z, 0)), 
                GlobalToLocalMatrix
            ).Let(vec4 => new Vector3(vec4.X, vec4.Y, vec4.Z)).AsLocalPosition(),
            #endif
            Type.Pose => (global * pose.Rotation.Inverse).RelativeToWorldZero,
            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(Type))
        };

        /// <summary>
        /// Transforms a rotation from local space to global space.
        /// </summary>
        public GlobalRotation TransformRotation(LocalRotation local) => type switch {
            #if IsUnity
            Type.Matrix => (localToGlobalMatrix.rotation * local.AsQuaternion).AsGlobalRotation(),
            Type.Transform => (transform.rotation * local.AsQuaternion).AsGlobalRotation(),
            #else
            Type.Matrix => Quaternion.Concatenate(Quaternion.CreateFromRotationMatrix(localToGlobalMatrix), local.AsQuaternion).AsGlobalRotation(),
            #endif
            Type.Pose => pose.Rotation * local.ToGlobalWithWorldZero,
            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(Type))
        };

        /// <summary>
        /// Transforms a rotation from global to local space.
        /// </summary>
        public LocalRotation InverseTransformRotation(GlobalRotation global) => type switch {
            #if IsUnity
            Type.Matrix => (GlobalToLocalMatrix.rotation * global.AsQuaternion).AsLocalRotation(),
            Type.Transform => (transform.rotation.Inverse() * global.AsQuaternion).AsLocalRotation(),
            #else
            Type.Matrix => Quaternion.Concatenate(Quaternion.CreateFromRotationMatrix(GlobalToLocalMatrix), global.AsQuaternion).AsLocalRotation(),
            #endif
            Type.Pose => (pose.Rotation.Inverse * global).RelativeToWorldZero,
            _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(Type))
        };

        public static TransformProvider From(Vector3? pos = null, Quaternion? rotation = null, Vector3? scale = null) =>
#if IsUnity
            new(pos ?? Vector3.zero, rotation ?? Quaternion.identity, scale ?? Vector3.one);
#else
            new(pos ?? Vector3.Zero, rotation ?? Quaternion.Identity, scale ?? Vector3.One);
#endif
        
#if IsUnity
        public static implicit operator TransformProvider(UnityEngine.Component component)
            => new(component.transform.localToWorldMatrix);
        public static implicit operator TransformProvider(UnityEngine.GameObject gameObject)
            => new(gameObject.transform.localToWorldMatrix);
#endif
        public static implicit operator TransformProvider(Matrix localToGlobalMatrix)
            => new(localToGlobalMatrix);
        public static implicit operator TransformProvider(GlobalPose pose)
            => new(pose);
    }

}