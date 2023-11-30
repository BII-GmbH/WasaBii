using System;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// <summary>
    /// A 3D vector that represents a world-space direction.
    /// Can also be viewed as a normalized <see cref="GlobalOffset"/>.
    /// </summary>
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
        // We cannot normalize values supplied by the user via the unity inspector because
        // UX would be awful when we change the x value while the user has not yet set the
        // z value. Thus, we simply show this tooltip and trust the user to input a normalized
        // vector. In addition, the property drawer has a `Normalize` button.
        [field:UnityEngine.SerializeField][field:UnityEngine.Tooltip("Must be normalized, otherwise calculations might break")]
        public UnityEngine.Vector3 AsUnityVector { get; private set; }
        public readonly System.Numerics.Vector3 AsNumericsVector => AsUnityVector.ToSystemVector();
        
        public GlobalDirection(UnityEngine.Vector3 toWrap) => AsUnityVector = toWrap.normalized;
        public GlobalDirection(System.Numerics.Vector3 toWrap) => AsUnityVector = toWrap.Normalized().ToUnityVector();
#else
        public System.Numerics.Vector3 AsNumericsVector { get; private set; }
        public GlobalDirection(System.Numerics.Vector3 toWrap) => AsNumericsVector = toWrap.Normalized();
#endif

        public GlobalDirection(float x, float y, float z) : this(new System.Numerics.Vector3(x, y, z)) { }
        
        public GlobalDirection(Length x, Length y, Length z) : this(
            (float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()
        ) { }

        /// <summary>
        /// <inheritdoc cref="TransformProvider.InverseTransformDirection"/>
        /// This is the inverse of <see cref="LocalDirection.ToGlobalWith"/>
        /// </summary>
        /// <example> <code>global.RelativeTo(parent).ToGlobalWith(parent) == global</code> </example>
        [Pure] public LocalDirection RelativeTo(TransformProvider parent) => parent.InverseTransformDirection(this);
        
        /// <summary> Projects this direction onto the plane defined by its normal. </summary>
        [Pure] public GlobalDirection ProjectOnPlane(GlobalDirection planeNormal) => 
            new GlobalOffset(AsNumericsVector).ProjectOnPlane(planeNormal).Normalized;

        /// <summary> Reflects this direction off the plane defined by the given normal. </summary>
        [Pure]
        public GlobalDirection Reflect(GlobalDirection planeNormal) => 
            new GlobalOffset(AsNumericsVector).Reflect(planeNormal).Normalized;
        
        [Pure]
        public float Dot(GlobalDirection other) => System.Numerics.Vector3.Dot(AsNumericsVector, other.AsNumericsVector);
        
        [Pure]
        public GlobalOffset Cross(GlobalDirection other) => 
            new(System.Numerics.Vector3.Cross(AsNumericsVector, other.AsNumericsVector));

        [Pure] public static GlobalOffset operator *(Length a, GlobalDirection b) => new((float)a.AsMeters() * b.AsNumericsVector);
        [Pure] public static GlobalOffset operator *(GlobalDirection b, Length a) => a * b;
        
        [Pure] public static GlobalVelocity operator *(GlobalDirection velocity, Speed speed) => 
            new(velocity.AsNumericsVector * (float)speed.AsMetersPerSecond());
        [Pure] public static GlobalVelocity operator *(Speed speed, GlobalDirection velocity) => velocity * speed;

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