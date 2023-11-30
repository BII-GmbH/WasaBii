using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// <summary>
    /// A 3D vector that represents a the difference between two world-space positions.
    /// Can also be viewed as a <see cref="GlobalDirection"/> with a length.
    /// </summary>
    [Serializable]
    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: true, memberType: nameof(Length), convertToMemberType: nameof(LengthConstructionExtensions.Meters), hasOrientation: true)]
    public partial struct GlobalOffset : 
        GlobalDirectionLike<GlobalOffset>,
        IsGlobalVariant<GlobalOffset, LocalOffset>
    {

        public static readonly GlobalOffset Zero = new(System.Numerics.Vector3.Zero);
    
        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        public UnityEngine.Vector3 AsUnityVector { get; private set; }
        public readonly System.Numerics.Vector3 AsNumericsVector => AsUnityVector.ToSystemVector();
        
        public GlobalOffset(UnityEngine.Vector3 toWrap) => AsUnityVector = toWrap;
        public GlobalOffset(System.Numerics.Vector3 toWrap) => AsUnityVector = toWrap.ToUnityVector();
        public GlobalOffset(float x, float y, float z) => AsUnityVector = new(x, y, z);
        #else
        public System.Numerics.Vector3 AsNumericsVector { get; private set; }
        public GlobalOffset(System.Numerics.Vector3 toWrap) => AsNumericsVector = toWrap;
        public GlobalOffset(float x, float y, float z) => AsNumericsVector = new(x, y, z);
        #endif

        public GlobalDirection Normalized => new(AsNumericsVector);

        public GlobalOffset(Length x, Length y, Length z) : this((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters()) { }

        [Pure] public static Builder From(GlobalPosition origin) => new Builder(origin);

        /// <summary>
        /// <inheritdoc cref="TransformProvider.InverseTransformOffset"/>
        /// This is the inverse of <see cref="LocalOffset.ToGlobalWith"/>
        /// </summary>
        /// <example> <code>global.RelativeTo(parent).ToGlobalWith(parent) == global</code> </example>
        [Pure] public LocalOffset RelativeTo(TransformProvider parent)
            => parent.InverseTransformOffset(this);

        /// Projects this offset onto the other one.
        [Pure] public GlobalOffset Project(GlobalOffset other) => this.Dot(other) / other.SqrMagnitude * other;

        /// Projects this offset onto the given direction.
        [Pure] public GlobalOffset Project(GlobalDirection onNormal) => this.Dot(onNormal) * onNormal;

        /// Projects this offset onto the plane defined by its normal.
        [Pure] public GlobalOffset ProjectOnPlane(GlobalDirection planeNormal) => this - this.Project(planeNormal);

        /// Reflects this offset off the plane defined by the given normal
        [Pure]
        public GlobalOffset Reflect(GlobalDirection planeNormal) => this - 2 * this.Project(planeNormal);

        [Pure]
        public Length Dot(GlobalDirection normal) => System.Numerics.Vector3.Dot(AsNumericsVector, normal.AsNumericsVector).Meters();

        [Pure]
        public Area Dot(GlobalOffset other) => System.Numerics.Vector3.Dot(AsNumericsVector, other.AsNumericsVector).SquareMeters();
        
        [Pure]
        public GlobalOffset Cross(GlobalOffset other) =>
            new(System.Numerics.Vector3.Cross(AsNumericsVector, other.AsNumericsVector));

        [Pure]
        public Angle SignedAngleTo(GlobalOffset other, GlobalDirection axis, Handedness handedness = Handedness.Default) =>
            AsNumericsVector.SignedAngleTo(other.AsNumericsVector, axis.AsNumericsVector, handedness);

        [Pure]
        public Angle SignedAngleOnPlaneTo(GlobalOffset other, GlobalDirection axis, Handedness handedness = Handedness.Default) =>
            AsNumericsVector.SignedAngleOnPlaneTo(other.AsNumericsVector, axis.AsNumericsVector, handedness);

        public Area SqrMagnitude => AsNumericsVector.LengthSquared().SquareMeters();
        public Length Magnitude => AsNumericsVector.Length().Meters();

        [Pure] public static GlobalOffset operator +(GlobalOffset left, GlobalOffset right) => new(left.AsNumericsVector + right.AsNumericsVector);
        [Pure] public static GlobalOffset operator -(GlobalOffset left, GlobalOffset right) => new(left.AsNumericsVector - right.AsNumericsVector);

        [Pure] public static GlobalOffset operator -(GlobalOffset offset) => new(-offset.AsNumericsVector);

        [Pure]
        public static GlobalVelocity operator/(GlobalOffset offset, Duration duration) => 
            new(offset.AsNumericsVector / (float)duration.AsSeconds());

        public readonly struct Builder {
            private readonly GlobalPosition origin;
            public Builder(GlobalPosition origin) => this.origin = origin;
            [Pure] public GlobalOffset To(GlobalPosition destination) => destination - origin;
        }

    }

    public static partial class GlobalOffsetExtensions {
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalOffset AsGlobalOffset(this UnityEngine.Vector3 globalOffset)
            => new(globalOffset);
        #endif
        
        [Pure] public static GlobalOffset AsGlobalOffset(this System.Numerics.Vector3 globalOffset)
            => new(globalOffset);

        [Pure]
        public static GlobalOffset Sum(this IEnumerable<GlobalOffset> offsets) => 
            offsets.Select(o => o.AsNumericsVector).Aggregate(System.Numerics.Vector3.Add).AsGlobalOffset();

    }
    
}