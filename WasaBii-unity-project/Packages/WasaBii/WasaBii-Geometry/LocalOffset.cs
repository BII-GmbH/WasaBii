using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// <summary>
    /// A 3D vector that represents a the difference between two positions in the same local space.
    /// Can also be viewed as a <see cref="LocalDirection"/> with a length.
    /// </summary>
    [MustBeImmutable]
    [Serializable]
    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: true, hasOrientation: true)]
    public partial struct LocalOffset : 
        LocalDirectionLike<LocalOffset>, 
        IsLocalVariant<LocalOffset, GlobalOffset> {
        
        public static readonly LocalOffset Zero = new(System.Numerics.Vector3.Zero);

        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        public UnityEngine.Vector3 AsUnityVector { get; private set; }
        public readonly System.Numerics.Vector3 AsNumericsVector => AsUnityVector.ToSystemVector();
        
        public LocalOffset(UnityEngine.Vector3 toWrap) => AsUnityVector = toWrap;
        public LocalOffset(System.Numerics.Vector3 toWrap) => AsUnityVector = toWrap.ToUnityVector();
        public LocalOffset(float x, float y, float z) => AsUnityVector = new(x, y, z);
        #else
        public System.Numerics.Vector3 AsNumericsVector { get; private set; }
        public LocalOffset(System.Numerics.Vector3 toWrap) => AsNumericsVector = toWrap;
        public LocalOffset(float x, float y, float z) => AsNumericsVector = new(x, y, z);
        #endif

        public LocalDirection Normalized => new(AsNumericsVector);

        public LocalOffset(Length x, Length y, Length z) : this((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()) { }

        /// <summary>
        /// The same as <see cref="LocalPosition.Zero"/> + this.
        /// </summary>
        public LocalPosition AsPosition => new(AsNumericsVector);

        [Pure] public static Builder From(LocalPosition origin) => new Builder(origin);

        /// <summary>
        /// <inheritdoc cref="TransformProvider.TransformOffset"/>
        /// This is the inverse of <see cref="GlobalOffset.RelativeTo"/>
        /// </summary>
        /// <example> <code>local.ToGlobalWith(parent).RelativeTo(parent) == local</code> </example>
        [Pure]
        public GlobalOffset ToGlobalWith(TransformProvider parent) => parent.TransformOffset(this);

        public GlobalOffset ToGlobalWithWorldZero => new (AsNumericsVector);

        /// <summary>
        /// Transforms the offset into the local space <paramref name="localParent"/> is defined relative to.
        /// Only applicable if the offset is defined relative to the given <paramref name="localParent"/>!
        /// This is the inverse of itself with the inverse parent.
        /// </summary>
        /// <example> <code>local.TransformBy(parent).TransformBy(parent.Inverse) = local</code> </example>
        [Pure] public LocalOffset TransformBy(LocalPose localParent) => localParent.Rotation * this;
        
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
        
        public LocalOffset Cross(LocalOffset other) => System.Numerics.Vector3.Cross(AsNumericsVector, other.AsNumericsVector).AsLocalOffset();

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

        [Pure]
        public static LocalOffset Sum(this IEnumerable<LocalOffset> offsets) => 
            offsets.Select(o => o.AsNumericsVector).Aggregate(System.Numerics.Vector3.Add).AsLocalOffset();

    }

}
