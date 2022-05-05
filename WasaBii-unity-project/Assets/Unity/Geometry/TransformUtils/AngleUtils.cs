using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    public static class AngleUtils {

        [Pure] 
        public static Quaternion WithAxis(this Angle angle, Vector3 axis) => 
            Quaternion.AngleAxis((float)angle.AsDegrees(), axis);

        [Pure]
        public static GlobalRotation WithAxis(this Angle angle, GlobalDirection axis) =>
            angle.WithAxis(axis.AsVector).AsGlobalRotation();

        [Pure]
        public static LocalRotation WithAxis(this Angle angle, LocalDirection axis) =>
            angle.WithAxis(axis.AsVector).AsLocalRotation();

        public static GlobalAngleBuilder From(GlobalDirection from) => new(from);

        public static RelativeAngleBuilder From(LocalDirection from) => new(from);

        public readonly struct GlobalAngleBuilder {
            private readonly GlobalDirection from;
            public GlobalAngleBuilder(GlobalDirection from) => this.from = from;
            public Angle UnsignedTo(GlobalDirection to) => ((double) Vector3.Angle(from.AsVector, to.AsVector)).Degrees();

            public Angle To(GlobalDirection to, GlobalDirection axis, bool projectOffsetsToPlane = true) =>
                ((double) Vector3.SignedAngle(
                    from.If(projectOffsetsToPlane, f => f.ProjectOnPlane(axis)).AsVector,
                    to.If(projectOffsetsToPlane, t => t.ProjectOnPlane(axis)).AsVector,
                    axis.AsVector
                )).Degrees();
        }
        
        public readonly struct RelativeAngleBuilder {
            private readonly LocalDirection from;
            public RelativeAngleBuilder(LocalDirection from) => this.from = from;
            public Angle UnsignedTo(LocalDirection to) => ((double) Vector3.Angle(from.AsVector, to.AsVector)).Degrees();

            public Angle To(LocalDirection to, LocalDirection axis, bool projectOffsetsToPlane = true) =>
                ((double) Vector3.SignedAngle(
                    from.If(projectOffsetsToPlane, f => f.ProjectOnPlane(axis)).AsVector,
                    to.If(projectOffsetsToPlane, t => t.ProjectOnPlane(axis)).AsVector,
                    axis.AsVector
                )).Degrees();
        }
        
    }
}