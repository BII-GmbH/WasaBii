using System;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A 3D vector that represents a world-space direction.
    /// Can also be viewed as a normalized <see cref="GlobalOffset"/>.
    [MustBeImmutable]
    [MustBeSerializable]
    [GeometryHelper(areFieldsIndependent: false, fieldType: FieldType.Double, hasMagnitude: false, hasDirection: true)]
    public readonly partial struct GlobalDirection : 
        GlobalDirectionLike<GlobalDirection>,
        IsGlobalVariant<GlobalDirection, LocalDirection> {

        public static readonly GlobalDirection Up = FromGlobal(0, 1, 0);
        public static readonly GlobalDirection Down = FromGlobal(0, -1, 0);
        public static readonly GlobalDirection Left = FromGlobal(-1, 0, 0);
        public static readonly GlobalDirection Right = FromGlobal(1, 0, 0);
        public static readonly GlobalDirection Forward = FromGlobal(0, 0, 1);
        public static readonly GlobalDirection Back = FromGlobal(0, 0, -1);
        public static readonly GlobalDirection One = FromGlobal(1, 1, 1);
        public static readonly GlobalDirection Zero = FromGlobal(0, 0, 0);

        public readonly double X, Y, Z;

        public GlobalOffset AsOffsetWithLength1 => GlobalOffset.FromGlobal(X, Y, Z);
        
        private GlobalDirection(double x, double y, double z) {
            var magnitude = Math.Sqrt(x * x + y * y + z * z);
            X = x / magnitude;
            Y = y / magnitude;
            Z = z / magnitude;
        }
        
        [Pure] public static GlobalDirection FromGlobal(System.Numerics.Vector3 global) => new(global.X, global.Y, global.Z);

        [Pure] public static GlobalDirection FromGlobal(double x, double y, double z) => new(x, y, z);
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalDirection FromGlobal(UnityEngine.Vector3 global) => new(global.x, global.y, global.z);
        #endif

        /// <inheritdoc cref="TransformProvider.InverseTransformDirection"/>
        /// This is the inverse of <see cref="LocalDirection.ToGlobalWith"/>
        [Pure] public LocalDirection RelativeTo(TransformProvider parent) => parent.InverseTransformDirection(this);
        
        public LocalDirection RelativeToWorldZero => LocalDirection.FromLocal(X, Y, Z);

        /// Projects this direction onto the plane defined by its normal.
        [Pure] public GlobalDirection ProjectOnPlane(GlobalDirection planeNormal) => 
            this.AsOffsetWithLength1.ProjectOnPlane(planeNormal).Normalized;

        /// Reflects this direction off the plane defined by the given normal
        [Pure]
        public GlobalDirection Reflect(GlobalDirection planeNormal) => this.AsOffsetWithLength1.Reflect(planeNormal).Normalized;

        [Pure] public static GlobalOffset operator *(Length a, GlobalDirection b) => GlobalOffset.FromGlobal(a * b.X, a * b.Y, a * b.Z);
        [Pure] public static GlobalDirection operator -(GlobalDirection dir) => FromGlobal(-dir.X, -dir.Y, -dir.Z);

        [Pure] public static GlobalDirection Lerp(
            GlobalDirection start, GlobalDirection end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public static GlobalDirection Slerp(
            GlobalDirection start, GlobalDirection end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

    }

    public static partial class DirectionExtensions {
       
       [Pure] public static GlobalDirection AsGlobalDirection(this System.Numerics.Vector3 globalPosition) 
           => GlobalDirection.FromGlobal(globalPosition);

       #if UNITY_2022_1_OR_NEWER
       [Pure] public static GlobalDirection AsGlobalDirection(this UnityEngine.Vector3 globalPosition) 
           => GlobalDirection.FromGlobal(globalPosition);

       [Pure] public static GlobalDirection Forward(this UnityEngine.Component component) =>
           component.transform.forward.AsGlobalDirection();

       [Pure] public static GlobalDirection Forward(this UnityEngine.GameObject gameObject) =>
           gameObject.transform.forward.AsGlobalDirection();

       [Pure] public static GlobalDirection Right(this UnityEngine.Component component) =>
           component.transform.right.AsGlobalDirection();

       [Pure] public static GlobalDirection Right(this UnityEngine.GameObject gameObject) =>
           gameObject.transform.right.AsGlobalDirection();

       [Pure] public static GlobalDirection Up(this UnityEngine.Component component) =>
           component.transform.up.AsGlobalDirection();

       [Pure] public static GlobalDirection Up(this UnityEngine.GameObject gameObject) =>
           gameObject.transform.up.AsGlobalDirection();
       #endif

    }

}