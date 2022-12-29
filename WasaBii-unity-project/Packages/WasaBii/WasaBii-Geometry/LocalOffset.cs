using System;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A 3D vector that represents a the difference between two positions in the same local space.
    /// Can also be viewed as a <see cref="LocalDirection"/> with a length.
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: true, hasOrientation: true)]
    public partial struct LocalOffset : 
        LocalDirectionLike<LocalOffset>, 
        IsLocalVariant<LocalOffset, GlobalOffset> {
        
        #if UNITY_2022_1_OR_NEWER
        [UnityEngine.SerializeField]
        #endif
        private System.Numerics.Vector3 _underlying;
        public System.Numerics.Vector3 AsNumericsVector => _underlying;
        
        #if UNITY_2022_1_OR_NEWER
        public UnityEngine.Vector3 AsUnityVector => AsNumericsVector.ToUnityVector();
        #endif

        public LocalDirection Normalized => new(AsNumericsVector);
        public LocalPosition AsPosition => new(AsNumericsVector);

        public LocalOffset(System.Numerics.Vector3 asNumericsVector) => _underlying = asNumericsVector;
        public LocalOffset(float x, float y, float z) => _underlying = new(x, y, z);
        public LocalOffset(Length x, Length y, Length z) => _underlying = new((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters());

        #if UNITY_2022_1_OR_NEWER
        public LocalOffset(UnityEngine.Vector3 global) => _underlying = global.ToSystemVector();
        #endif

        [Pure] public static Builder From(LocalPosition origin) => new Builder(origin);

        /// <inheritdoc cref="TransformProvider.TransformOffset"/>
        /// This is the inverse of <see cref="GlobalOffset.RelativeTo"/>
        [Pure]
        public GlobalOffset ToGlobalWith(TransformProvider parent) => parent.TransformOffset(this);

        public GlobalOffset ToGlobalWithWorldZero => new (AsNumericsVector);

        [Pure] public LocalOffset TransformBy(LocalPose offset) => offset.Rotation * this;
        
        /// Projects this offset onto the other one.
        [Pure] public LocalOffset Project(LocalOffset other) => this.Dot(other) / other.SqrMagnitude * other;

        /// Projects this offset onto the given direction.
        [Pure] public LocalOffset Project(LocalDirection onNormal) => this.Dot(onNormal) * onNormal;

        /// Projects this offset onto the plane defined by its normal.
        [Pure] public LocalOffset ProjectOnPlane(LocalDirection planeNormal) => this - this.Project(planeNormal);

        /// Reflects this offset off the plane defined by the given normal
        [Pure]
        public LocalOffset Reflect(LocalDirection planeNormal) => this - 2 * this.Project(planeNormal);

        public Length Dot(LocalDirection normal) => System.Numerics.Vector3.Dot(AsNumericsVector, normal.AsNumericsVector).Meters();
        
        public Area Dot(LocalOffset other) => System.Numerics.Vector3.Dot(AsNumericsVector, other.AsNumericsVector).SquareMeters();

        public Area SqrMagnitude => AsNumericsVector.LengthSquared().SquareMeters();
        public Length Magnitude => AsNumericsVector.Length().Meters();

        [Pure] public static LocalOffset operator +(LocalOffset left, LocalOffset right) => new(left.AsNumericsVector + right.AsNumericsVector);

        [Pure] public static LocalOffset operator -(LocalOffset left, LocalOffset right) => new(left.AsNumericsVector - right.AsNumericsVector);

        [Pure] public static LocalOffset operator -(LocalOffset offset) => new(-offset.AsNumericsVector);

        public readonly struct Builder {
            private readonly LocalPosition origin;
            public Builder(LocalPosition origin) => this.origin = origin;
            [Pure] public LocalOffset To(LocalPosition destination) => destination - origin;
        }

    }
    
    public static partial class LocalOffsetExtensions {
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static LocalOffset AsLocalOffset(this UnityEngine.Vector3 localOffset)
            => new(localOffset);
        #endif

        [Pure] public static LocalOffset AsLocalOffset(this System.Numerics.Vector3 localOffset)
            => new(localOffset);

    }

}
