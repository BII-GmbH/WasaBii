using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using BII.WasaBii.Unity.Geometry;
using UnityEngine;

namespace BII.WasaBii.Geometry
{
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
}