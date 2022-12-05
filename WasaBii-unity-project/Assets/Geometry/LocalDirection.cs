using System;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A 3D vector that represents a local direction.
    /// Can also be viewed as a normalized <see cref="LocalOffset"/>.
    [MustBeImmutable]
    [MustBeSerializable]
    [GeometryHelper(areFieldsIndependent: false, fieldType: FieldType.Double, hasMagnitude: false, hasDirection: true)]
    public readonly partial struct LocalDirection :
        LocalDirectionLike<LocalDirection>,
        IsLocalVariant<LocalDirection, GlobalDirection> {

        public static readonly LocalDirection Up = FromLocal(0, 1, 0);
        public static readonly LocalDirection Down = FromLocal(0, -1, 0);
        public static readonly LocalDirection Left = FromLocal(-1, 0, 0);
        public static readonly LocalDirection Right = FromLocal(1, 0, 0);
        public static readonly LocalDirection Forward = FromLocal(0, 0, 1);
        public static readonly LocalDirection Back = FromLocal(0, 0, -1);
        public static readonly LocalDirection One = FromLocal(1, 1, 1);
        public static readonly LocalDirection Zero = FromLocal(0, 0, 0);

        public readonly double X, Y, Z;

        public LocalOffset AsOffsetWithLength1 => LocalOffset.FromLocal(X, Y, Z);

        private LocalDirection(double x, double y, double z) {
            var magnitude = Math.Sqrt(x * x + y * y + z * z);
            X = x / magnitude;
            Y = y / magnitude;
            Z = z / magnitude;
        }
        
        [Pure] public static LocalDirection FromLocal(System.Numerics.Vector3 local) => new(local.X, local.Y, local.Z);

        [Pure] public static LocalDirection FromLocal(double x, double y, double z) => new(x, y, z);
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static LocalDirection FromLocal(UnityEngine.Vector3 local) => new(local.x, local.y, local.z);
        #endif

        /// <inheritdoc cref="TransformProvider.TransformDirection"/>
        /// This is the inverse of <see cref="GlobalDirection.RelativeTo"/>
        [Pure] public GlobalDirection ToGlobalWith(TransformProvider parent) => parent.TransformDirection(this);
        
        public GlobalDirection ToGlobalWithWorldZero => GlobalDirection.FromGlobal(X, Y, Z);

        [Pure] public LocalDirection TransformBy(LocalPose offset) => offset.Rotation * this;
        
        /// Projects this direction onto the plane defined by its normal.
        [Pure] public LocalDirection ProjectOnPlane(LocalDirection planeNormal) => 
            this.AsOffsetWithLength1.ProjectOnPlane(planeNormal).Normalized;

        /// Reflects this direction off the plane defined by the given normal
        [Pure]
        public LocalDirection Reflect(LocalDirection planeNormal) => this.AsOffsetWithLength1.Reflect(planeNormal).Normalized;

        [Pure] public static LocalOffset operator *(Length a, LocalDirection b) => LocalOffset.FromLocal(a * b.X, a * b.Y, a * b.Z);
        [Pure] public static LocalOffset operator *(LocalDirection b, Length a) => LocalOffset.FromLocal(a * b.X, a * b.Y, a * b.Z);
        [Pure] public static LocalDirection operator -(LocalDirection dir) => FromLocal(-dir.X, -dir.Y, -dir.Z);

        [Pure] public static LocalDirection Lerp(
            LocalDirection start, LocalDirection end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public static LocalDirection Slerp(
            LocalDirection start, LocalDirection end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

    }

    public static partial class DirectionExtensions {
       
        #if UNITY_2022_1_OR_NEWER
       [Pure] public static LocalDirection AsLocalDirection(this UnityEngine.Vector3 localDirection) 
           => LocalDirection.FromLocal(localDirection);
       #endif
       
       [Pure] public static LocalDirection AsLocalDirection(this System.Numerics.Vector3 localDirection) 
           => LocalDirection.FromLocal(localDirection);

    }

}