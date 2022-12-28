using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Geometry {
    
    public static class GeometryUtils {

        ///<remarks>Only x and z are cirped, y is lerped.</remarks>
        public static GlobalPosition Cirp(
            GlobalPosition from, GlobalPosition to, GlobalPosition pivot, double progress, bool shouldClamp = true
        ) {
            var a = (from - pivot).WithY(0);
            var b = (to - pivot).WithY(0);
            var diff = a.SlerpTo(b, progress, shouldClamp);
            return (pivot + diff)
                .WithY((float) MathD.Lerp(from.Y, to.Y, progress, shouldClamp))
                .AsGlobalPosition();
        }

        ///<remarks>Only x and z are cirped, y is lerped.</remarks>
        public static GlobalPose Cirp(
            GlobalPose from, GlobalPose to, GlobalPosition pivot, double progress, bool shouldClamp = true
        ) {
            var position = Cirp(from.Position, to.Position, pivot, progress, shouldClamp);
            var rotation = from.Rotation.SlerpTo(to.Rotation, progress, shouldClamp);
            return new GlobalPose(position.AsVector, rotation);
        }

        // Note DS: This used to be calculated properly, but the algorithm was incorrect
        // and in some cases completely off. We can try to reimplement it if necessary,
        // but until then, we use sampling.
        public static Length CalculateCirpingMovementLength(
            GlobalPosition from, GlobalPosition to, GlobalPosition pivot, int sampleCount = 6
        ) => SampleRange.Sample(t => Cirp(from, to, pivot, t), sampleCount, includeFrom: true, includeTo: true)
            .Select(loc => loc.AsVector)
            .TotalPathLength();

        /// <summary>
        /// This function solves the angles of a triangle from its sides lengths.
        ///
        /// This is done via the Law of cosines:
        /// https://www.mathsisfun.com/algebra/trig-solving-sss-triangles.html
        /// </summary>
        /// <param name="a"> length of the first side</param>
        /// <param name="b"> length of the second side</param>
        /// <param name="c"> length of the third side</param>
        /// <returns> returns the angles of the triangle (A is the opposing angle of side a, ect.)</returns>
        [Pure] public static (Angle A, Angle B, Angle C) SolveTriangleFromSideLengths(Length a, Length b, Length c) =>
            SolveTriangleFromSideLengths(a.AsMeters(), b.AsMeters(), c.AsMeters());

        /// <summary>
        /// This function solves the angles of a triangle from its sides lengths.
        ///
        /// This is done via the Law of cosines:
        /// https://www.mathsisfun.com/algebra/trig-solving-sss-triangles.html
        /// </summary>
        /// <param name="a"> length of the first side</param>
        /// <param name="b"> length of the second side</param>
        /// <param name="c"> length of the third side</param>
        /// <returns> returns the angles of the triangle (A is the opposing angle of side a, ect.)</returns>
        [Pure] public static (Angle A, Angle B, Angle C) SolveTriangleFromSideLengths(double a, double b, double c) {
            Debug.Assert(
                a + b > c && a + c > b && b + c > a,
                "Can only solve triangle if lengths can form a valid triangle" +
                "\n Lengths are: \n A: {a}\n B: {b}\n C: {c}"
            );
            var aAngle = Angles.Acos((b * b + c * c - a * a) / (2 * b * c));
            var bAngle = Angles.Acos((a * a + c * c - b * b) / (2 * a * c));
            var cAngle = 180d.Degrees() - (aAngle + bAngle);

            return (A: aAngle, B: bAngle, C: cAngle);
        }

        /// Reflects the point <see cref="self"/> off <see cref="on"/>. Has the same effect
        /// as rotating <see cref="self"/> around <see cref="on"/> by 180° around an axis
        /// perpendicular to the difference between the two.
        [Pure] public static Vector3 PointReflect(this Vector3 self, Vector3 on) => on + on - self;

        /// Reflects the vector <see cref="offset"/> on the plane defined by the <see cref="planeNormal"/>.
        /// <param name="offset">The vector to reflect.</param>
        /// <param name="planeNormal">The normal of the plane. Must be normalized.</param>
        [Pure] public static Vector3 Reflect(this Vector3 offset, Vector3 planeNormal)
            => Vector3.Reflect(offset, planeNormal);
        
        /// Reflects the <see cref="point"/> on a plane.
        /// <param name="point">The point to reflect</param>
        /// <param name="pointOnPlane">Any point on the plane</param>
        /// <param name="planeNormal">The normal of the plane. Must be normalized.</param>
        [Pure] public static Vector3 Reflect(this Vector3 point, Vector3 pointOnPlane, Vector3 planeNormal)
            => pointOnPlane - (point - pointOnPlane).Reflect(planeNormal);
        
    }
    
}