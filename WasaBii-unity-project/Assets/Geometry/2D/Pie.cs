using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Numerics;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Geometry
{
    public readonly struct Pie {
        public readonly Circle Circle;
        public readonly Angle MinAngle;
        public readonly Angle MaxAngle;
        public Pie(Circle circle, Angle minAngle, Angle maxAngle) {
            Circle = circle;
            MinAngle = minAngle.Normalized360();
            MaxAngle = maxAngle.Normalized360();
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
                .Normalized360();
            return MinAngle < MaxAngle
                ? MinAngle <= angle && angle <= MaxAngle
                : MinAngle <= angle || angle <= MaxAngle;
        }
        
    }
}