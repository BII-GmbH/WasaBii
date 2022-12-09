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
    [GeometryHelper(areFieldsIndependent: false, hasMagnitude: false, hasDirection: true)]
    public readonly partial struct LocalDirection :
        LocalDirectionLike<LocalDirection>,
        IsLocalVariant<LocalDirection, GlobalDirection> {

        public static readonly LocalDirection Up = new(0, 1, 0);
        public static readonly LocalDirection Down = new(0, -1, 0);
        public static readonly LocalDirection Left = new(-1, 0, 0);
        public static readonly LocalDirection Right = new(1, 0, 0);
        public static readonly LocalDirection Forward = new(0, 0, 1);
        public static readonly LocalDirection Back = new(0, 0, -1);
        public static readonly LocalDirection One = new(1, 1, 1);
        public static readonly LocalDirection Zero = new(0, 0, 0);

        public System.Numerics.Vector3 AsNumericsVector { get; }

        public LocalOffset AsOffsetWithLength1 => new(AsNumericsVector);

        public LocalDirection(float x, float y, float z) {
            var magnitude = MathF.Sqrt(x * x + y * y + z * z);
            AsNumericsVector = new(x / magnitude, y / magnitude, z / magnitude);
        }
        
        public LocalDirection(System.Numerics.Vector3 toWrap) => AsNumericsVector = toWrap.Normalized();

        public LocalDirection(Length x, Length y, Length z) : this(
            (float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()
        ) { }

        #if UNITY_2022_1_OR_NEWER
        public LocalDirection(UnityEngine.Vector3 local) : this(local.ToSystemVector()) { }
        #endif

        /// <inheritdoc cref="TransformProvider.TransformDirection"/>
        /// This is the inverse of <see cref="GlobalDirection.RelativeTo"/>
        [Pure] public GlobalDirection ToGlobalWith(TransformProvider parent) => parent.TransformDirection(this);
        
        public GlobalDirection ToGlobalWithWorldZero => new(AsNumericsVector);

        [Pure] public LocalDirection TransformBy(LocalPose offset) => offset.Rotation * this;
        
        /// Projects this direction onto the plane defined by its normal.
        [Pure] public LocalDirection ProjectOnPlane(LocalDirection planeNormal) => 
            this.AsOffsetWithLength1.ProjectOnPlane(planeNormal).Normalized;

        /// Reflects this direction off the plane defined by the given normal
        [Pure]
        public LocalDirection Reflect(LocalDirection planeNormal) => this.AsOffsetWithLength1.Reflect(planeNormal).Normalized;

        public float Dot(LocalDirection other) => System.Numerics.Vector3.Dot(AsNumericsVector, other.AsNumericsVector);

        [Pure] public static LocalOffset operator *(Length a, LocalDirection b) => new((float)a.AsMeters() * b.AsNumericsVector);
        [Pure] public static LocalOffset operator *(LocalDirection b, Length a) => a * b;
        [Pure] public static LocalDirection operator -(LocalDirection dir) => new(-dir.AsNumericsVector);

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
           => new(localDirection);
       #endif
       
       [Pure] public static LocalDirection AsLocalDirection(this System.Numerics.Vector3 localDirection) 
           => new(localDirection);

    }

}