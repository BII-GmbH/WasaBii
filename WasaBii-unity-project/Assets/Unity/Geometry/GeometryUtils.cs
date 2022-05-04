using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Units;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    
    public static class GeometryUtils {

        ///<remarks>Only x and z are cirped, y is lerped.</remarks>
        public static GlobalPosition Cirp(
            PositionProvider from, PositionProvider to, PositionProvider pivot, double progress, bool shouldClamp = true
        ) {
            var start = from.Wrapped.AsVector;
            var end = to.Wrapped.AsVector;
            var piv = pivot.Wrapped.AsVector;
            var a = (start - piv).WithY(0);
            var b = (end - piv).WithY(0);
            var diff = shouldClamp
                ? Vector3.Slerp(a, b, (float) progress)
                : Vector3.SlerpUnclamped(a, b, (float) progress);
            return (piv + diff)
                .WithY((float) Mathd.Lerp(start.y, end.y, progress, shouldClamp))
                .AsGlobalPosition();
        }

        ///<remarks>Only x and z are cirped, y is lerped.</remarks>
        public static GlobalPose Cirp(
            GlobalPose from, GlobalPose to, GlobalPosition pivot, double progress, bool shouldClamp = true
        ) {
            var position = Cirp(from.GlobalPosition, to.GlobalPosition, pivot, progress, shouldClamp);
            var rotation = shouldClamp
                ? Quaternion.Slerp(from.GlobalRotation, to.GlobalRotation, (float) progress)
                : Quaternion.SlerpUnclamped(from.GlobalRotation, to.GlobalRotation, (float) progress);
            return new GlobalPose(position.AsVector, rotation);
        }

        // Note DS: This used to be calculated properly, but the algorithm was incorrect
        // and in some cases completely off. We can try to reimplement it if necessary,
        // but until then, we use sampling.
        public static Length CalculateCirpingMovementLength(
            PositionProvider from, PositionProvider to, PositionProvider pivot, int sampleCount = 6
        )
            => Enumerable.Range(0, sampleCount)
                .Select(i => Cirp(from, to, pivot, i / (sampleCount - 1d)))
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
            Contract.Assert(
                a + b > c && a + c > b && b + c > a,
                "Can only solve triangle if lengths can form a valid triangle" +
                "\n Lengths are: \n A: {a}\n B: {b}\n C: {c}"
            );
            var aAngle = Angle.Acos((b * b + c * c - a * a) / (2 * b * c));
            var bAngle = Angle.Acos((a * a + c * c - b * b) / (2 * a * c));
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
        
        [Pure] public static IEnumerable<Line2D.Intersection> IntersectionsWith(this Rect rect, Circle circle) => 
            circle.IntersectionsWith(rect);
        [Pure] public static IEnumerable<Line2D.Intersection> IntersectionsWith(this Rect rect, Line2D line) => 
            line.IntersectionsWith(rect);

        /// <summary>
        /// Checks if rect2 is completely enclosed in rect1. Returns false when the rects are equal.
        /// </summary>
        [Pure] public static bool Contains(this Rect rect1, Rect rect2) =>
            rect1.Contains(new Vector2(rect2.xMin, rect2.yMin)) &&
            rect1.Contains(new Vector2(rect2.xMin, rect2.yMax)) &&
            rect1.Contains(new Vector2(rect2.xMax, rect2.yMin)) &&
            rect1.Contains(new Vector2(rect2.xMax, rect2.yMax));
                
        /// <summary>
        /// Checks if the spline intersects with the rect and returns the index of the spline segment
        /// that intersected first along with the local intersection on this spline segment.
        /// </summary>
        [Pure] public static (int segmentIndex, Line2D.Intersection)? ClosestIntersectionWith(this IEnumerable<Vector2> sampledSpline, Rect rect) {
            var spline = sampledSpline as IReadOnlyList<Vector2> ?? sampledSpline.ToArray();
            for (var i = 0; i < spline.Count - 1; i++) {
                if(new Line2D(
                    start: spline[i],
                    end: spline[i + 1])
                .IntersectionsWith(rect)
                .TryGetElementWithMinimalValue(valueProvider: it => it.t, out var tMin))
                    return (i, tMin);
            }
            return null;
        }

        [Pure] public static IEnumerable<Line2D> Edges(this Rect rect) {
            yield return new Line2D(rect.min, new Vector2(rect.xMin, rect.yMax));
            yield return new Line2D(new Vector2(rect.xMin, rect.yMax), rect.max);
            yield return new Line2D(rect.max, new Vector2(rect.xMax, rect.yMin));
            yield return new Line2D(new Vector2(rect.xMax, rect.yMin), rect.min);
        }
        
        /// Tries to fit <see cref="inner"/> into <see cref="outer"/>. Returns the point <see cref="inner"/>'s
        /// center should be moved to so that <see cref="outer.min"/> &lt; <see cref="inner.min"/> and
        /// <see cref="outer.max"/> &gt; <see cref="inner.max"/> for both x and y.
        /// If <see cref="inner"/> is larger than <see cref="outer"/>, the return value will be
        /// <see cref="outer.center"/> in that dimension for minimal overlap in all directions.
        [Pure] public static Vector2 ClampToBeContainedIn(Rect inner, Rect outer) {
            float clamp(float center, float intoCenter, float space) =>
                space <= 0
                    ? intoCenter
                    : Mathf.Clamp(center, intoCenter - space / 2, intoCenter + space / 2);
            return new Vector2(
                clamp(inner.center.x, intoCenter: outer.center.x, space: outer.width - inner.width),
                clamp(inner.center.y, intoCenter: outer.center.y, space: outer.height - inner.height)
            );
        }
        
    }
    
    public struct Line2D {
        public readonly Vector2 Start;
        public readonly Vector2 End;
        public Vector2 Dir => End - Start;
        public Line2D(Vector2 start, Vector2 end) {
            Start = start;
            End = end;
        }
        
        public static Line2D FromStartDir(Vector2 start, Vector2 dir) => new Line2D(start, start + dir);
        
        [Pure] private Intersection? IntersectionAt(float t) => Intersection.At(this, t);
        
        /// <summary>
        /// Checks if a line intersects with a rectangle in 2D space.
        /// </summary>
        [Pure] public IEnumerable<Intersection> IntersectionsWith(Rect rect) {
            var min = Vector3.Min(Start, End);
            var max = Vector3.Max(Start, End);
            if (rect.xMin > max.x || rect.yMin > max.y || rect.xMax < min.x || rect.yMax < min.y) yield break;
            Intersection? i;
            if((i = IntersectionWithVerticalLine((rect.min.x, minY: rect.min.y, maxY: rect.max.y))).HasValue)
                yield return i.Value;
            if((i = IntersectionWithVerticalLine((rect.max.x, minY: rect.min.y, maxY: rect.max.y))).HasValue)
                yield return i.Value;
            if((i = IntersectionWithHorizontalLine((minX: rect.min.x, maxX: rect.max.x, rect.min.y))).HasValue)
                yield return i.Value;
            if((i = IntersectionWithHorizontalLine((minX: rect.min.x, maxX: rect.max.x, rect.max.y))).HasValue)
                yield return i.Value;
        }

        /// <summary>
        /// Checks the line intersects another, which is vertical
        /// </summary>
        [Pure] public Intersection? IntersectionWithVerticalLine((float x, float minY, float maxY) verticalLine) {
            if (verticalLine.x < Mathf.Min(Start.x, End.x) 
                || verticalLine.x > Mathf.Max(Start.x, End.x)) return null;
            // Since the other line is vertical, it only has one x value, so if there is an intersection,
            // it must have this x value. Thus, we can find the intersection by determining where `this`
            // line has the vertical line's x value:
            // Start.x + t * Dir.x = verticalLine.x
            var t = (verticalLine.x - Start.x) / Dir.x;
            // For the intersection to be valid, t must be between 0 and 1.
            if (!t.IsInsideInterval(0, 1)) return null;
            var intersection = IntersectionAt(t);
            return intersection.HasValue && intersection.Value.Point.y.IsInsideInterval(verticalLine.minY, verticalLine.maxY) 
                ? intersection
                : null;
        }

        /// <summary>
        /// Checks the line intersects another, which is horizontal
        /// </summary>
        [Pure] public Intersection? IntersectionWithHorizontalLine((float minX, float maxX, float y) horizontalLine) {
            if (horizontalLine.y < Mathf.Min(Start.y, End.y) 
                || horizontalLine.y > Mathf.Max(Start.y, End.y)) return null;
            // Since the other line is horizontal, it only has one y value, so if there is an intersection,
            // it must have this y value. Thus, we can find the intersection by determining where `this`
            // line has the horizontal line's y value:
            // Start.y + t * Dir.y = verticalLine.y
            var t = (horizontalLine.y - Start.y) / Dir.y;
            // For the intersection to be valid, t must be between 0 and 1.
            if (!t.IsInsideInterval(0, 1)) return null;
            var intersection = IntersectionAt(t);
            return  intersection.HasValue && intersection.Value.Point.x.IsInsideInterval(horizontalLine.minX, horizontalLine.maxX) 
                ? IntersectionAt(t)
                : null;
        }

        [Pure] public IEnumerable<Intersection> IntersectionsWith(Circle circle) {
            if (!circle.Contains(this)) {
                var localLine = new Line2D(Start - circle.Center, End - circle.Center);
                var dir = Dir;

                // Taken from http://mathworld.wolfram.com/Circle-LineIntersection.html
                var D = localLine.Start.x * localLine.End.y - localLine.End.x * localLine.Start.y;
                var dirSqrMag = dir.sqrMagnitude;
                var delta = circle.Radius.Sqr() * dirSqrMag - D.Sqr();
                var intersectionBase = circle.Center +
                    new Vector2(
                        D * dir.y,
                        -D * dir.x
                    ) /
                    dirSqrMag;

                if (delta.IsNearly(0)) { // One intersection
                    var i = Intersection.At(this, intersectionBase);
                    if (i.HasValue) yield return i.Value;
                } else if (delta > 0) { // Two intersections
                    var offset = new Vector2(
                        Mathf.Sign(dir.y) * dir.x * Mathf.Sqrt(delta) / dirSqrMag,
                        Mathf.Abs(dir.y) * Mathf.Sqrt(delta) / dirSqrMag
                    );
                    var i1 = Intersection.At(this, intersectionBase + offset);
                    if (i1.HasValue) yield return i1.Value;
                    var i2 = Intersection.At(this, intersectionBase - offset);
                    if (i2.HasValue) yield return i2.Value;
                }
            }
        }

        [Pure]
        public (Intersection? intersection, bool isOnLines) IntersectionWith(Line2D line) {
            var cross = Dir.Cross(line.Dir);
            if (cross == 0) return (null, false);
            var t = (line.Start - Start).Cross(line.Dir) / cross;
            var s = (Start - line.Start).Cross(Dir) / cross;
            return (
                Intersection.At(this, t), 
                isOnLines: t.IsInsideInterval(0, 1) && s.IsInsideInterval(0, 1)
            );
        }

        public struct Intersection {
            public readonly float t;
            private readonly Line2D line;
            public Vector2 Point => line.Start + t * line.Dir;

            public static Intersection? At(Line2D line, Vector2 point) {
                var xT = (point.x - line.Start.x) / line.Dir.x;
                var yT = (point.y - line.Start.y) / line.Dir.y;
                return xT.IsNearly(yT, threshold: 1E-5f) || 
                    (line.Dir.x.IsNearly(0) && point.x.IsNearly(line.Start.x)) ||
                    (line.Dir.y.IsNearly(0) && point.y.IsNearly(line.Start.y)) 
                        ? At(line, line.Dir.x.IsNearly(0) ? yT : xT) 
                        : null;
            }
            public static Intersection? At(Line2D line, float t) => 
                t < 0 || t > 1 ? (Intersection?) null : new Intersection(t, line);

            private Intersection(float t, Line2D line) {
                this.t = t;
                this.line = line; 
            }
            public Intersection(Line2D line, Vector2 point) {
                var res = At(line, point);
                Contract.Assert(res.HasValue, 
                    $"The intersection point ({point}) does not lie on the line ({line}).");
                
                t = res.Value.t;
                this.line = line;
            }
        }

        public override string ToString() => $"start: {Start}, end: {End}";
    }

    public readonly struct Circle {
        public readonly Vector2 Center;
        public readonly float Radius;
        public Circle(Vector2 center, float radius) {
            Center = center;
            Radius = radius;
        }
        
        [Pure] public bool Contains(Vector2 point) => Vector2.Distance(Center, point) < Radius;
        [Pure] public bool Contains(Line2D line) => Contains(line.Start) && Contains(line.End);
        [Pure] public bool Contains(Rect rect) =>
            Contains(rect.min) &&
            Contains(new Vector2(rect.xMin, rect.yMax)) &&
            Contains(rect.max) &&
            Contains(new Vector2(rect.xMax, rect.yMin));
        
        [Pure] public IEnumerable<Line2D.Intersection> IntersectionsWith(Rect rect) {
            var circle = this;
            return rect.Edges().SelectMany(edge => circle.IntersectionsWith(edge));
        }

        [Pure] public IEnumerable<Line2D.Intersection> IntersectionsWith(Line2D line) => line.IntersectionsWith(this);

        public override string ToString() => $"center: {Center}, radius: {Radius}";
    }

    public readonly struct Pie {
        public readonly Circle Circle;
        public readonly Angle MinAngle;
        public readonly Angle MaxAngle;
        public Pie(Circle circle, Angle minAngle, Angle maxAngle) {
            Circle = circle;
            MinAngle = minAngle.Normalized360;
            MaxAngle = maxAngle.Normalized360;
        }

        [Pure] public IEnumerable<Line2D> Edges() {
            var circle = Circle;
            Vector2 pointFor(Angle angle) => circle.Center 
                + new Vector2((float)Math.Sin(angle.AsRadians()), (float)(Math.Cos(angle.AsRadians())) * circle.Radius);
            yield return new Line2D(circle.Center, pointFor(MinAngle));
            yield return new Line2D(circle.Center, pointFor(MaxAngle));
        }
        
        [Pure] public IEnumerable<Line2D.Intersection> IntersectionsWith(Rect rect) {
            var pie = this;
            return rect.Edges().SelectMany(e => e.IntersectionsWith(pie.Circle)).Where(i => pie.isWithinAngle(i.Point))
                .Concat(Edges().SelectMany(e => e.IntersectionsWith(rect)));
        }

        [Pure] public bool Contains(Rect rect) =>
            Circle.Contains(rect) &&
            isWithinAngle(rect.min) &&
            isWithinAngle(new Vector2(rect.xMin, rect.yMax)) &&
            isWithinAngle(new Vector2(rect.xMax, rect.yMin)) &&
            isWithinAngle(rect.max);

        private bool isWithinAngle(Vector2 point) {
            var angle = AngleUtils
                .From(GlobalDirection.Forward)
                .To(GlobalOffset.From(Circle.Center.xxy()).To(point.xxy()).Normalized, 
                    axis: GlobalDirection.Up)
                .Normalized360;
            return MinAngle < MaxAngle
                ? MinAngle <= angle && angle <= MaxAngle
                : MinAngle <= angle || angle <= MaxAngle;
        }
        
    }
    
}