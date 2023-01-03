using System;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A 3D vector that represents a local direction.
    /// Can also be viewed as a normalized <see cref="LocalOffset"/>.
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent: false, hasMagnitude: false, hasOrientation: true)]
    public partial struct LocalDirection :
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

        #if UNITY_2022_1_OR_NEWER
        // We cannot normalize values supplied by the user via the unity inspector without
        // writing a custom property drawer and even then, UX would be awful when we change
        // the x value while the user has not yet set the z value. Thus, we simply show this
        // tooltip and trust the user to input a normalized vector.
        [field:UnityEngine.SerializeField][field:UnityEngine.Tooltip("Must be normalized, otherwise calculations might break")]
        public UnityEngine.Vector3 AsUnityVector { get; private set; }
        public readonly System.Numerics.Vector3 AsNumericsVector => AsUnityVector.ToSystemVector();
        
        public LocalDirection(UnityEngine.Vector3 toWrap) => AsUnityVector = toWrap.normalized;
        public LocalDirection(System.Numerics.Vector3 toWrap) => AsUnityVector = toWrap.Normalized().ToUnityVector();
        #else
        public System.Numerics.Vector3 AsNumericsVector { get; private set; }
        public GlobalDirection(System.Numerics.Vector3 toWrap) => AsNumericsVector = toWrap.Normalized();
        #endif

        public LocalDirection(float x, float y, float z) : this(new System.Numerics.Vector3(x, y, z)) { }
        
        public LocalDirection(Length x, Length y, Length z) : this(
            (float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()
        ) { }

        public readonly LocalOffset AsOffsetWithLength1 => new(AsNumericsVector);

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

    }

    public static partial class LocalDirectionExtensions {
       
        #if UNITY_2022_1_OR_NEWER
       [Pure] public static LocalDirection AsLocalDirection(this UnityEngine.Vector3 localDirection) 
           => new(localDirection);
       #endif
       
       [Pure] public static LocalDirection AsLocalDirection(this System.Numerics.Vector3 localDirection) 
           => new(localDirection);

    }

}