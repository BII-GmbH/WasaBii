using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Geometry {
    
    /// <summary>
    /// A polygon consisting of any finite number of vertices that define its boundaries.
    /// The vertices are given in clockwise order and are expected to be planar. Note
    /// that polygons might be self-intersecting, but some utilities expect them not to be.
    /// </summary>
    [MustBeImmutable][Serializable]
    public sealed class Polygon : IEquatable<Polygon> {

        public readonly SequenceEqualityList<LocalPosition> Vertices;
        
        /// <param name="vertices">Must be planar and in clockwise order.</param>
        /// <remarks>Passing a <see cref="ImmutableArray{LocalPosition}"/> avoids unnecessary allocations.</remarks>
        public Polygon(IEnumerable<LocalPosition> vertices) => Vertices = vertices.ToSequenceEqualityList();

        public bool Equals(Polygon other) => other != null && Equals(Vertices, other.Vertices);
        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is Polygon other && Equals(other);
        public override int GetHashCode() => Vertices != null ? Vertices.GetHashCode() : 0;

        public Area Area {
            get {
                // https://math.stackexchange.com/questions/3207981/caculate-area-of-polygon-in-3d
                var v1 = Vertices[0];
                return 0.5f.Meters() * Vertices.Skip(1)
                    .PairwiseSliding()
                    .SelectTuple((vj, vk) => (vj - v1).Cross(vk - v1))
                    .Sum()
                    .Magnitude;
            }
        }
        
        public Length Circumference => Vertices.IfNotEmpty(
            p => p.Append(Vertices[0])
                .PairwiseSliding()
                .Sum(tuple => tuple.Item1.DistanceTo(tuple.Item2)),
            () => Length.Zero
        );

        public LocalBounds LocalBounds => Vertices.Bounds().GetOrThrow(() => new Exception("An empty polygon has no bounds"));
        /// <remarks>
        /// More precise than <c>thisLocalBounds.ToGlobalWith(parent)</c> because the vertices are transformed before
        /// calculating the bounding box. However, this also makes it more costly if you have a huge amount of vertices.
        /// </remarks>
        public GlobalBounds GlobalBoundsFor(GlobalPose parent) => 
            Vertices.Select(v => v.ToGlobalWith(parent)).Bounds()
                .GetOrThrow(() => new Exception("An empty polygon has no bounds"));

    }
    
}
