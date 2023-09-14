using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Geometry.Shared;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Geometry {

    /// <summary>
    /// A 3D vector that represents a linear change of position over time in local space.
    /// Can also be viewed as a <see cref="LocalDirection"/> with a <see cref="Speed"/>.
    /// </summary>
    [Serializable]
    [GeometryHelper(areFieldsIndependent: true, hasMagnitude: true, memberType: nameof(Speed), convertToMemberType: nameof(SpeedConstructionExtensions.MetersPerSecond), hasOrientation: true)]
    public partial struct LocalVelocity : 
        LocalDirectionLike<LocalVelocity>, 
        IsLocalVariant<LocalVelocity, GlobalVelocity> {
        
        public static readonly LocalVelocity Zero = new(System.Numerics.Vector3.Zero);

        #if UNITY_2022_1_OR_NEWER
        [field:UnityEngine.SerializeField]
        public UnityEngine.Vector3 AsUnityVector { get; private set; }
        public readonly System.Numerics.Vector3 AsNumericsVector => AsUnityVector.ToSystemVector();
        
        public LocalVelocity(UnityEngine.Vector3 toWrap) => AsUnityVector = toWrap;
        public LocalVelocity(System.Numerics.Vector3 toWrap) => AsUnityVector = toWrap.ToUnityVector();
        public LocalVelocity(float x, float y, float z) => AsUnityVector =  new(x, y, z);
        #else
        public System.Numerics.Vector3 AsNumericsVector { get; private set; }
        public LocalVelocity(System.Numerics.Vector3 toWrap) => AsNumericsVector = toWrap;
        public LocalVelocity(float x, float y, float z) => AsNumericsVector = new(x, y, z);
        #endif

        public LocalDirection Direction => new(AsNumericsVector);
        public Speed Magnitude => AsNumericsVector.Length().MetersPerSecond();

        public LocalVelocity(Speed x, Speed y, Speed z) : this((float)x.AsMetersPerSecond(), (float)y.AsMetersPerSecond(), (float)z.AsMetersPerSecond()) { }

        [Pure] public static Builder From(LocalPosition origin) => new Builder(origin);

        /// <summary>
        /// <inheritdoc cref="TransformProvider.TransformVelocity"/>
        /// This is the inverse of <see cref="GlobalVelocity.RelativeTo"/>
        /// </summary>
        /// <example> <code>local.ToGlobalWith(parent).RelativeTo(parent) == local</code> </example>
        [Pure]
        public GlobalVelocity ToGlobalWith(TransformProvider parent) => new(parent.TransformOffset(new(this.AsNumericsVector)).AsNumericsVector);

        /// <summary>
        /// Transforms the Velocity into the local space <paramref name="localParent"/> is defined relative to.
        /// Only applicable if the Velocity is defined relative to the given <paramref name="localParent"/>!
        /// This is the inverse of itself with the inverse parent.
        /// </summary>
        /// <example> <code>local.TransformBy(parent).TransformBy(parent.Inverse) = local</code> </example>
        [Pure] public LocalVelocity TransformBy(LocalPose localParent) => localParent.Rotation * this;
        
        /// Projects this Velocity onto the given direction.
        [Pure] public LocalVelocity Project(LocalDirection onNormal) => this.Dot(onNormal) * onNormal;

        /// Projects this Velocity onto the plane defined by its normal.
        [Pure] public LocalVelocity ProjectOnPlane(LocalDirection planeNormal) => this - this.Project(planeNormal);

        /// Reflects this Velocity off the plane defined by the given normal
        [Pure]
        public LocalVelocity Reflect(LocalDirection planeNormal) => this - 2 * this.Project(planeNormal);

        public Speed Dot(LocalDirection normal) => System.Numerics.Vector3.Dot(AsNumericsVector, normal.AsNumericsVector).MetersPerSecond();
        
        public LocalVelocity Cross(LocalVelocity other) => System.Numerics.Vector3.Cross(AsNumericsVector, other.AsNumericsVector).AsLocalVelocity();

        public Angle SignedAngleTo(LocalVelocity other, LocalDirection axis, Handedness handedness = Handedness.Default) =>
            AsNumericsVector.SignedAngleTo(other.AsNumericsVector, axis.AsNumericsVector, handedness);

        public Angle SignedAngleOnPlaneTo(LocalVelocity other, LocalDirection axis, Handedness handedness = Handedness.Default) =>
            AsNumericsVector.SignedAngleOnPlaneTo(other.AsNumericsVector, axis.AsNumericsVector, handedness);

        [Pure] public static LocalVelocity operator +(LocalVelocity left, LocalVelocity right) => new(left.AsNumericsVector + right.AsNumericsVector);

        [Pure] public static LocalVelocity operator -(LocalVelocity left, LocalVelocity right) => new(left.AsNumericsVector - right.AsNumericsVector);

        [Pure] public static LocalVelocity operator -(LocalVelocity velocity) => new(-velocity.AsNumericsVector);

        [Pure]
        public static LocalOffset operator *(LocalVelocity velocity, Duration duration) => 
            new(velocity.AsNumericsVector * (float)duration.AsSeconds());

        [Pure] public static LocalOffset operator *(Duration duration, LocalVelocity velocity) => velocity * duration;
        
        public readonly struct Builder {
            private readonly LocalPosition origin;
            public Builder(LocalPosition origin) => this.origin = origin;
            [Pure] public WithDestination To(LocalPosition destination) => new (origin, destination);
            
            public readonly struct WithDestination {
                private readonly LocalPosition origin;
                private readonly LocalPosition destination;
                public WithDestination(LocalPosition origin, LocalPosition destination) {
                    this.origin = origin;
                    this.destination = destination;
                }
                [Pure] public LocalVelocity In(Duration duration) => (destination - origin) / duration;

            }
        }

    }
    
    public static partial class LocalVelocityExtensions {
        
        #if UNITY_2022_1_OR_NEWER
        [Pure] public static LocalVelocity AsLocalVelocity(this UnityEngine.Vector3 localVelocity)
            => new(localVelocity);
        #endif

        [Pure] public static LocalVelocity AsLocalVelocity(this System.Numerics.Vector3 localVelocity)
            => new(localVelocity);

        [Pure]
        public static LocalVelocity Sum(this IEnumerable<LocalVelocity> velocities) => 
            velocities.Select(o => o.AsNumericsVector).Aggregate(System.Numerics.Vector3.Add).AsLocalVelocity();

    }

}
