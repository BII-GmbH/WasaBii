using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A 3D vector that represents a the difference between two world-space positions.
    /// Can also be viewed as a <see cref="GlobalDirection"/> with a length.
    [MustBeImmutable]
    [MustBeSerializable]
    [GeometryHelper(areFieldsIndependent: true, fieldType: FieldType.Length, hasMagnitude: true, hasDirection: true)]
    public readonly partial struct GlobalOffset : 
        GlobalDirectionLike<GlobalOffset>,
        IsGlobalVariant<GlobalOffset, LocalOffset> {
    
        public static readonly GlobalOffset Up = FromGlobal(0, 1, 0);
        public static readonly GlobalOffset Down = FromGlobal(0, -1, 0);
        public static readonly GlobalOffset Left = FromGlobal(-1, 0, 0);
        public static readonly GlobalOffset Right = FromGlobal(1, 0, 0);
        public static readonly GlobalOffset Forward = FromGlobal(0, 0, 1);
        public static readonly GlobalOffset Back = FromGlobal(0, 0, -1);
        public static readonly GlobalOffset One = FromGlobal(1, 1, 1);
        public static readonly GlobalOffset Zero = FromGlobal(0, 0, 0);

        public Length X { init; get; }
        public Length Y { init; get; }
        public Length Z { init; get; }

        public GlobalDirection Normalized => GlobalDirection.FromGlobal(X.SiValue, Y.SiValue, Z.SiValue);

        [Pure] public static GlobalOffset FromGlobal(System.Numerics.Vector3 global)
            => new() {X = global.X.Meters(), Y = global.Y.Meters(), Z = global.Z.Meters()};

        [Pure] public static GlobalOffset FromGlobal(Length x, Length y, Length z) 
            => new() {X = x, Y = y, Z = z};

        [Pure] public static GlobalOffset FromGlobal(double x, double y, double z) 
            => new() {X = x.Meters(), Y = y.Meters(), Z = z.Meters()};

        #if UNITY_2022_1_OR_NEWER
        [Pure] public static GlobalOffset FromGlobal(UnityEngine.Vector3 global)
            => new() {X = global.x.Meters(), Y = global.y.Meters(), Z = global.z.Meters()};
        #endif

        [Pure] public static Builder From(GlobalPosition origin) => new Builder(origin);

        /// <inheritdoc cref="TransformProvider.InverseTransformOffset"/>
        /// This is the inverse of <see cref="LocalOffset.ToGlobalWith"/>
        [Pure] public LocalOffset RelativeTo(TransformProvider parent)
            => parent.InverseTransformOffset(this);

        public LocalOffset RelativeToWorldZero => LocalOffset.FromLocal(X, Y, Z);

        /// Projects this offset onto the other one.
        [Pure] public GlobalOffset Project(GlobalOffset other) => this.Dot(other) / other.SqrMagnitude * other;

        /// Projects this offset onto the given direction.
        [Pure] public GlobalOffset Project(GlobalDirection onNormal) => this.Dot(onNormal) * onNormal;

        /// Projects this offset onto the plane defined by its normal.
        [Pure] public GlobalOffset ProjectOnPlane(GlobalDirection planeNormal) => this - this.Project(planeNormal);

        /// Reflects this offset off the plane defined by the given normal
        [Pure]
        public GlobalOffset Reflect(GlobalDirection planeNormal) => this - 2 * this.Project(planeNormal);

        public Length Dot(GlobalDirection normal) => X * normal.X + Y * normal.Y + Z * normal.Z;

        [Pure] public static GlobalOffset operator +(GlobalOffset left, GlobalOffset right) => FromGlobal(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        [Pure] public static GlobalOffset operator -(GlobalOffset left, GlobalOffset right) => FromGlobal(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

        [Pure] public static GlobalOffset operator -(GlobalOffset offset) => FromGlobal(-offset.X, -offset.Y, -offset.Z);

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
            => GlobalOffset.FromGlobal(globalOffset);
        #endif
        
        [Pure] public static GlobalOffset AsGlobalOffset(this System.Numerics.Vector3 globalOffset)
            => GlobalOffset.FromGlobal(globalOffset);

    }
    
}