using System.Collections.Generic;
using System.Diagnostics.Contracts;
using BII.WasaBii.Core;
using BII.WasaBii.Unity.Geometry;
using UnityEngine;

namespace BII.WasaBii.Geometry
{
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
}