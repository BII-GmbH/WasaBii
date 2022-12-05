using BII.WasaBii.Core;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;

namespace BII.WasaBii.Geometry {

    /// A 3D vector that represents a the difference between two positions in the same local space.
    /// Can also be viewed as a <see cref="LocalDirection"/> with a length.
    [MustBeImmutable]
    [MustBeSerializable]
    [GeometryHelper(areFieldsIndependent: true, fieldType: FieldType.Length, hasMagnitude: true, hasDirection: true)]
    public readonly partial struct LocalOffset : 
        LocalDirectionLike<LocalOffset>, 
        IsLocalVariant<LocalOffset, GlobalOffset> {
        
        public static readonly LocalOffset Up = FromLocal(0, 1, 0);
        public static readonly LocalOffset Down = FromLocal(0, -1, 0);
        public static readonly LocalOffset Left = FromLocal(-1, 0, 0);
        public static readonly LocalOffset Right = FromLocal(1, 0, 0);
        public static readonly LocalOffset Forward = FromLocal(0, 0, 1);
        public static readonly LocalOffset Back = FromLocal(0, 0, -1);
        public static readonly LocalOffset One = FromLocal(1, 1, 1);
        public static readonly LocalOffset Zero = FromLocal(0, 0, 0);

        public Length X { init; get; }
        public Length Y { init; get; }
        public Length Z { init; get; }

        public LocalDirection Normalized => LocalDirection.FromGlobal(X, Y, Z);
        public LocalPosition AsPosition => LocalPosition.FromLocal(X, Y, Z);

        [Pure] public static LocalOffset FromLocal(System.Numerics.Vector3 global)
            => new() {X = global.X.Meters(), Y = global.Y.Meters(), Z = global.Z.Meters()};

        [Pure] public static LocalOffset FromLocal(Length x, Length y, Length z) 
            => new() {X = x, Y = y, Z = z};

        [Pure] public static LocalOffset FromLocal(double x, double y, double z) 
            => new() {X = x.Meters(), Y = y.Meters(), Z = z.Meters()};

        #if UNITY_2022_1_OR_NEWER
        [Pure] public static LocalOffset FromLocal(UnityEngine.Vector3 global)
            => new() {X = global.x.Meters(), Y = global.y.Meters(), Z = global.z.Meters()};
        #endif

        [Pure] public static Builder From(LocalPosition origin) => new Builder(origin);

        /// <inheritdoc cref="TransformProvider.TransformOffset"/>
        /// This is the inverse of <see cref="GlobalOffset.RelativeTo"/>
        [Pure]
        public GlobalOffset ToGlobalWith(TransformProvider parent) => parent.TransformOffset(this);

        public GlobalOffset ToGlobalWithWorldZero => GlobalOffset.FromGlobal(X, Y, Z);

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

        public Length Dot(LocalDirection normal) => X * normal.X + Y * normal.Y + Z * normal.Z;

        [Pure] public static LocalOffset operator +(LocalOffset left, LocalOffset right) => FromLocal(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

        [Pure] public static LocalOffset operator -(LocalOffset left, LocalOffset right) => FromLocal(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

        [Pure] public static LocalOffset operator -(LocalOffset offset) => FromLocal(-offset.X, -offset.Y, -offset.Z);

        public readonly struct Builder {
            private readonly LocalPosition origin;
            public Builder(LocalPosition origin) => this.origin = origin;
            [Pure] public LocalOffset To(LocalPosition destination) => destination - origin;
        }

        [Pure] public static LocalOffset Lerp(
            LocalOffset start, LocalOffset end, double perc, bool shouldClamp = true
        ) => start.LerpTo(end, perc, shouldClamp);
        
        [Pure] public static LocalOffset Slerp(
            LocalOffset start, LocalOffset end, double perc, bool shouldClamp = true
        ) => start.SlerpTo(end, perc, shouldClamp);
        
    }
    
    public static partial class OffsetExtensions {
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static LocalOffset AsLocalOffset(this UnityEngine.Vector3 localOffset)
            => LocalOffset.FromLocal(localOffset);
        #endif

        [Pure] public static LocalOffset AsLocalOffset(this System.Numerics.Vector3 localOffset)
            => LocalOffset.FromLocal(localOffset);

    }

}
