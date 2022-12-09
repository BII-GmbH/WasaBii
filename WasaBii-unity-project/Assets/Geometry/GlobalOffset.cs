using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A 3D vector that represents a the difference between two world-space positions.
    /// Can also be viewed as a <see cref="GlobalDirection"/> with a length.
    [MustBeImmutable]
    [MustBeSerializable]
    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: true, hasDirection: true)]
    public readonly partial struct GlobalOffset : 
        GlobalDirectionLike<GlobalOffset>,
        IsGlobalVariant<GlobalOffset, LocalOffset> {
    
        public System.Numerics.Vector3 AsNumericsVector { get; }

        public GlobalDirection Normalized => new(AsNumericsVector);

        public GlobalOffset(System.Numerics.Vector3 toWrap) => AsNumericsVector = toWrap;
        public GlobalOffset(float x, float y, float z) => AsNumericsVector = new(x, y, z);
        public GlobalOffset(Length x, Length y, Length z) => AsNumericsVector = new((float)x.AsMeters(), (float)y.AsMeters(), (float)z.AsMeters());

        #if UNITY_2022_1_OR_NEWER
        public GlobalOffset(UnityEngine.Vector3 global) => AsNumericsVector = global.ToSystemVector();
        #endif

        [Pure] public static Builder From(GlobalPosition origin) => new Builder(origin);

        /// <inheritdoc cref="TransformProvider.InverseTransformOffset"/>
        /// This is the inverse of <see cref="LocalOffset.ToGlobalWith"/>
        [Pure] public LocalOffset RelativeTo(TransformProvider parent)
            => parent.InverseTransformOffset(this);

        public LocalOffset RelativeToWorldZero => new(AsNumericsVector);

        /// Projects this offset onto the other one.
        [Pure] public GlobalOffset Project(GlobalOffset other) => this.Dot(other) / other.SqrMagnitude * other;

        /// Projects this offset onto the given direction.
        [Pure] public GlobalOffset Project(GlobalDirection onNormal) => this.Dot(onNormal) * onNormal;

        /// Projects this offset onto the plane defined by its normal.
        [Pure] public GlobalOffset ProjectOnPlane(GlobalDirection planeNormal) => this - this.Project(planeNormal);

        /// Reflects this offset off the plane defined by the given normal
        [Pure]
        public GlobalOffset Reflect(GlobalDirection planeNormal) => this - 2 * this.Project(planeNormal);

        public Length Dot(GlobalDirection normal) => System.Numerics.Vector3.Dot(AsNumericsVector, normal.AsNumericsVector).Meters();

        public Area Dot(LocalOffset other) => System.Numerics.Vector3.Dot(AsNumericsVector, other.AsNumericsVector).SquareMeters();

        public Area SqrMagnitude => AsNumericsVector.LengthSquared().SquareMeters();
        public Length Magnitude => AsNumericsVector.Length().Meters();

        [Pure] public static GlobalOffset operator +(GlobalOffset left, GlobalOffset right) => new(left.AsNumericsVector + right.AsNumericsVector);
        [Pure] public static GlobalOffset operator -(GlobalOffset left, GlobalOffset right) => new(left.AsNumericsVector - right.AsNumericsVector);

        [Pure] public static GlobalOffset operator -(GlobalOffset offset) => new(-offset.AsNumericsVector);

        public readonly struct Builder {
            private readonly GlobalPosition origin;
            public Builder(GlobalPosition origin) => this.origin = origin;
            [Pure] public GlobalOffset To(GlobalPosition destination) => destination - origin;
        }

        [Pure] public static GlobalOffset Lerp(
            GlobalOffset start, GlobalOffset end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);

        [Pure] public static GlobalOffset Slerp(
            GlobalOffset start, GlobalOffset end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);

    }

    public static partial class OffsetExtensions {
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalOffset AsGlobalOffset(this UnityEngine.Vector3 globalOffset)
            => new(globalOffset);
        #endif
        
        [Pure] public static GlobalOffset AsGlobalOffset(this System.Numerics.Vector3 globalOffset)
            => new(globalOffset);

    }
    
}