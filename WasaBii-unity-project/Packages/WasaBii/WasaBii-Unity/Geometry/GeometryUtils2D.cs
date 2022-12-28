using System.Collections.Generic;
using BII.WasaBii.Geometry;
using JetBrains.Annotations;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry
{
    public static class GeometryUtils2D
    {
        
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
}