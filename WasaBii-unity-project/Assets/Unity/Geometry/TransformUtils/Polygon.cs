using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.Units;
using UnityEngine;

namespace BII.WasaBii.Unity.Geometry {
    
    /// A polygon consisting of any finite number of vertices that define its boundaries.
    /// The vertices are given in clockwise order and are expected to be planar. Note
    /// that polygons might be self-intersecting, but some utilities expect them not to be.
    [MustBeImmutable] [MustBeSerializable]
    public sealed class Polygon : IEquatable<Polygon> {

        public readonly SequenceEqualityList<LocalPosition> Vertices;
        
        /// <param name="vertices">Must be planar and in clockwise order.</param>
        public Polygon(IEnumerable<LocalPosition> vertices) => Vertices = vertices.ToSequenceEqualityList();

        public bool Equals(Polygon other) => other != null && Equals(Vertices, other.Vertices);
        public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is Polygon other && Equals(other);
        public override int GetHashCode() => Vertices != null ? Vertices.GetHashCode() : 0;

        public Area Area {
            get {
                // https://math.stackexchange.com/questions/3207981/caculate-area-of-polygon-in-3d
                var v1 = Vertices[0];
                return Mathf.Abs(
                    0.5f * Vertices.Skip(1)
                        .PairwiseSliding()
                        .Select((vj, vk) => (vj - v1).Cross(vk - v1).AsVector)
                        .Sum()
                        .magnitude
                ).SquareMeters();
            }
        }
        
        public Length Circumference => Vertices.IfNotEmpty(
            p => p.Append(Vertices[0])
                .PairwiseSliding()
                .Sum(tuple => tuple.Item1.DistanceTo(tuple.Item2)),
            () => Length.Zero
        );

        public LocalBounds LocalBounds => Vertices.Bounds();
        public GlobalBounds GlobalBoundsFor(GlobalPose parent) => Vertices.Select(v => v.ToGlobalWith(parent)).Bounds();

    }
    
}
