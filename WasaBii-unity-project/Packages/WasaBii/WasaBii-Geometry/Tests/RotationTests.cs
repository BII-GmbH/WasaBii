using System;
using System.Collections.Generic;
using System.Numerics;
using BII.WasaBii.UnitSystem;
using NUnit.Framework;

namespace BII.WasaBii.Geometry.Tests 
{
    
    // @Cameron these are for validating my math implementations only. The only reason I didn't delete them
    // yet was because I want to keep the equality check foo for when I implement actual tests later.
    // TODO DS: Make actual tests
    public class RotationTests
    {

        private static readonly Vector3 a = new(2, 8, 8);
        private static readonly Vector3 b = new(1, 33, 7);
        
        [Test]
        public void FromToRotation() {
            var from = a.AsGlobalOffset();
            var to = b.AsGlobalOffset();
            var rot = GlobalRotation.From(from).To(to);
            var result = rot * from;
            Assert.That(result.Normalized, Is.EqualTo(to.Normalized).Using(new DirectionComparer()));
        }
        
        [Test]
        public void ParallelFromToRotation() {
            var from = a.AsGlobalOffset();
            var to = 10 * a.AsGlobalOffset();
            var rot = GlobalRotation.From(from).To(to);
            var result = rot * from;
            Assert.That(result, Is.EqualTo(from).Using(new OffsetComparer()));
        }
        
        [Test]
        public void OppositeFromToRotation() {
            var from = a.AsGlobalOffset();
            var to = -a.AsGlobalOffset();
            var rot = GlobalRotation.From(from).To(to);
            var result = rot * from;
            Assert.That(result, Is.EqualTo(to).Using(new OffsetComparer()));
        }

        [Test]
        public void AngleFoo() {
            var from = new GlobalOffset(-1, 1, 0);
            var to = new GlobalOffset(1, 0, 1);
            Assert.That(from.AngleTo(to), Is.EqualTo(from.AsUnityVector.AngleTo(to.AsUnityVector)).Using(new AngleComparer()));
            var axis = new GlobalDirection(1, 0, 0);
            Assert.That(from.SignedAngleTo(to, axis), Is.EqualTo(UnityEngine.Vector3.SignedAngle(from.AsUnityVector, to.AsUnityVector, axis.AsUnityVector).Degrees()).Using(new AngleComparer()));
            Assert.That(to.SignedAngleTo(from, axis), Is.EqualTo(UnityEngine.Vector3.SignedAngle(to.AsUnityVector, from.AsUnityVector, axis.AsUnityVector).Degrees()).Using(new AngleComparer()));
            Assert.That(from.SignedAngleOnPlaneTo(to, axis), Is.EqualTo(90.0.Degrees()).Using(new AngleComparer()));
            Assert.That(to.SignedAngleOnPlaneTo(from, axis), Is.EqualTo(-90.0.Degrees()).Using(new AngleComparer()));
        }

        private sealed class OffsetComparer : IEqualityComparer<GlobalOffset>
        {
            private const double tolerance = 1E-5;

            public bool Equals(GlobalOffset x, GlobalOffset y) => x.IsNearly(y, tolerance);

            public int GetHashCode(GlobalOffset obj) => throw new NotImplementedException();
        }

        private sealed class DirectionComparer : IEqualityComparer<GlobalDirection>
        {
            private const double tolerance = 1E-5;

            public bool Equals(GlobalDirection x, GlobalDirection y) => x.IsNearly(y, tolerance);

            public int GetHashCode(GlobalDirection obj) => throw new NotImplementedException();
        }

        private sealed class AngleComparer : IEqualityComparer<Angle>
        {
            private static readonly Angle tolerance = 1E-5.Radians();

            public bool Equals(Angle x, Angle y) => x.IsNearly(y, tolerance);

            public int GetHashCode(Angle obj) => throw new NotImplementedException();
        }

    }
    
}