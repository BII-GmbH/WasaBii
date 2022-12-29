using System;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A 3D vector that represents a world-space direction.
    /// Can also be viewed as a normalized <see cref="GlobalOffset"/>.
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent: false, hasMagnitude: false, hasOrientation: true)]
    public partial struct GlobalDirection : 
        GlobalDirectionLike<GlobalDirection>,
        IsGlobalVariant<GlobalDirection, LocalDirection> {

        public static readonly GlobalDirection Up = new(0, 1, 0);
        public static readonly GlobalDirection Down = new(0, -1, 0);
        public static readonly GlobalDirection Left = new(-1, 0, 0);
        public static readonly GlobalDirection Right = new(1, 0, 0);
        public static readonly GlobalDirection Forward = new(0, 0, 1);
        public static readonly GlobalDirection Back = new(0, 0, -1);
        public static readonly GlobalDirection One = new(1, 1, 1);
        public static readonly GlobalDirection Zero = new(0, 0, 0);

#if UNITY_2022_1_OR_NEWER
        [UnityEngine.SerializeField]
#endif
        private System.Numerics.Vector3 _underlying;
        public System.Numerics.Vector3 AsNumericsVector => _underlying;
        
#if UNITY_2022_1_OR_NEWER
        public UnityEngine.Vector3 AsUnityVector => AsNumericsVector.ToUnityVector();
#endif

        public GlobalOffset AsOffsetWithLength1 => new(AsNumericsVector);
        
        public GlobalDirection(float x, float y, float z) {
            var magnitude = MathF.Sqrt(x * x + y * y + z * z);
            _underlying = new(x / magnitude, y / magnitude, z / magnitude);
        }
        
        public GlobalDirection(System.Numerics.Vector3 toWrap) => _underlying = toWrap.Normalized();

        public GlobalDirection(Length x, Length y, Length z) : this(
            (float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()
        ) { }

        #if UNITY_2022_1_OR_NEWER
        public GlobalDirection(UnityEngine.Vector3 local) : this(local.ToSystemVector()) { }
        #endif

        /// <inheritdoc cref="TransformProvider.InverseTransformDirection"/>
        /// This is the inverse of <see cref="LocalDirection.ToGlobalWith"/>
        [Pure] public LocalDirection RelativeTo(TransformProvider parent) => parent.InverseTransformDirection(this);
        
        public LocalDirection RelativeToWorldZero => new(AsNumericsVector);

        /// Projects this direction onto the plane defined by its normal.
        [Pure] public GlobalDirection ProjectOnPlane(GlobalDirection planeNormal) => 
            this.AsOffsetWithLength1.ProjectOnPlane(planeNormal).Normalized;

        /// Reflects this direction off the plane defined by the given normal
        [Pure]
        public GlobalDirection Reflect(GlobalDirection planeNormal) => this.AsOffsetWithLength1.Reflect(planeNormal).Normalized;
        
        public float Dot(GlobalDirection other) => System.Numerics.Vector3.Dot(AsNumericsVector, other.AsNumericsVector);

        [Pure] public static GlobalOffset operator *(Length a, GlobalDirection b) => new((float)a.AsMeters() * b.AsNumericsVector);
        [Pure] public static GlobalOffset operator *(GlobalDirection b, Length a) => a * b;
        [Pure] public static GlobalDirection operator -(GlobalDirection dir) => new(-dir.AsNumericsVector);

    }

    public static partial class GlobalDirectionExtensions {
       
       [Pure] public static GlobalDirection AsGlobalDirection(this System.Numerics.Vector3 globalPosition) 
           => new(globalPosition);

       #if UNITY_2022_1_OR_NEWER
       [Pure] public static GlobalDirection AsGlobalDirection(this UnityEngine.Vector3 globalPosition) 
           => new(globalPosition);

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