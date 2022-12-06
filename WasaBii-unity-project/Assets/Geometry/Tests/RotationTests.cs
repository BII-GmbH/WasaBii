using System.Numerics;
using NUnit.Framework;

namespace BII.WasaBii.Geometry.Tests 
{
    
    public class RotationTests
    {

        private static readonly Vector3 a = new(2, 8, 8);
        private static readonly Vector3 b = new(1, 33, 7);

        [Test]
        public void WhenCreatingFromToRotation_ThenItRotatesFromTo() {
            var from = a.AsGlobalOffset();
            var to = b.AsGlobalOffset();
            var rot = GlobalRotation.From(from).To(to);
            var result = rot * from;
            Assert.That(result, Is.EqualTo(to).Within(1E-7f));
        }
        
        [Test]
        public void WhenCreatingParallelFromToRotation_ThenItRotatesFromTo() {
            var from = a.AsGlobalOffset();
            var to = 10 * a.AsGlobalOffset();
            var rot = GlobalRotation.From(from).To(to);
            var result = rot * from;
            Assert.That(result, Is.EqualTo(to).Within(1E-7f));
        }
        
        [Test]
        public void WhenCreatingOppositeFromToRotation_ThenItRotatesFromTo() {
            var from = a.AsGlobalOffset();
            var to = -a.AsGlobalOffset();
            var rot = GlobalRotation.From(from).To(to);
            var result = rot * from;
            Assert.That(result, Is.EqualTo(to).Within(1E-7f));
        }
        
    }
    
}