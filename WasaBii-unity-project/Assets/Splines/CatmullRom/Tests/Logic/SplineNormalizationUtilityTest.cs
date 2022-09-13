using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static BII.WasaBii.Splines.Maths.SplineNormalizationUtility;
using static BII.WasaBii.Splines.CatmullRom.Tests.SplineTestUtils;

namespace BII.WasaBii.Splines.CatmullRom.Tests {
    public class SplineNormalizationUtilityTest {
        private static readonly Dictionary<SplineLocation, NormalizedSplineLocation> normalizaionSamples = new Dictionary<double, double>
            {{0, 0}, {0.5, 0.113}, {1, 0.227}, {2.6, 0.590}, {3, 0.681}, {4.404, 1}}.ToDictionary(
            kvp => SplineLocation.From(kvp.Key),
            kvp => NormalizedSplineLocation.From(kvp.Value)
        );

        private static readonly Dictionary<NormalizedSplineLocation, SplineLocation> deNormalizaionSamples = new Dictionary<double, double>
            {{0, 0}, {0.1, 0.440}, {0.3, 1.321}, {0.55, 2.422}, {0.7, 3.083}, {1, 4.404}}.ToDictionary(
            kvp => NormalizedSplineLocation.From(kvp.Key),
            kvp => SplineLocation.From(kvp.Value)
        );
        
        [Test]
        public void DeNormalize_BatchTest() {
            var uut = SplineTestUtils.ExampleCurvedSpline.Spline;
        
            foreach (var kvp in deNormalizaionSamples) {
                var location = uut.DeNormalize(kvp.Key);
                Assert.That(location.Value.SiValue, Is.EqualTo(kvp.Value.Value.SiValue).Within(SplineLocationTolerance));
            }
        }
        
        [Test]
        public void DeNormalize_WhenEquidistantNode_ThenTAndLocationEqual() {
            var uut = SplineTestUtils.ExampleEquidistantLinearSpline.Spline;
        
            foreach (var t in new[] {0, 0.1, 0.3, 0.5, 0.77, 1, 1.5, 1.99, 2}.Select(SplineLocation.From)) {
                var location = uut.DeNormalize(NormalizedSplineLocation.From(t));
        
                Assert.That(location, Is.EqualTo(t), $"Equidistant DeNormalization for t={t} did not work");
            }
        }
        
        
        [Test]
        public void Normalize_BatchTest() {
            var uut = SplineTestUtils.ExampleCurvedSpline.Spline;
        
            foreach (var kvp in normalizaionSamples) {
                var t = uut.Normalize(kvp.Key);
                Assert.That(t.Value, Is.EqualTo(kvp.Value.Value).Within(SplineLocationTolerance));
            }
        }
        
        [Test]
        public void Normalize_WhenEquidistantNode_ThenLocationAndTEqual() {
            var uut = SplineTestUtils.ExampleEquidistantLinearSpline.Spline;
        
            foreach (var t in new[] {0, 0.1, 0.3, 0.5, 0.77, 1, 1.5, 1.87, 2}.Select(SplineLocation.From)) {
                var location = uut.Normalize(t);
        
                Assert.That(location, Is.EqualTo(NormalizedSplineLocation.From(t)), $"Equidistant Normalization for t={t} did not work");
            }
        }
        
        
        [Test]
        public void Normalize_WhenSegmentLengthAsLocation_ThenIntegerValueReturned() {
            var spline = SplineTestUtils.ExampleEquidistantLinearSpline.Spline;
            var length = spline[SplineSegmentIndex.Zero].Length;
            
            var uut = spline.Normalize(length);
        
            Assert.That(uut.Value, Is.EqualTo((int) uut));
        }
        
        
        [Test]
        public void BulkNormalizeOrdered_BatchTest() {
            var uut = SplineTestUtils.ExampleCurvedSpline.Spline;
        
            var toNormalize = new SplineLocation[normalizaionSamples.Count];
            var expected = new NormalizedSplineLocation[deNormalizaionSamples.Count];
        
            int index = 0;
            foreach(var kvp in normalizaionSamples) {
                toNormalize[index] = kvp.Key;
                expected[index] = kvp.Value;
            }
            
            var actual = uut.BulkNormalizeOrdered(toNormalize).Select(l => l.Value).ToArray();
        
            for (int i = 0; i < actual.Length; ++i){
                Assert.That(actual[i], Is.EqualTo(expected[i].Value).Within(SplineLocationTolerance));
            }
            
        }
        
        [Test]
        public void BulkNormalizeOrdered_WhenEquidistantNode_ThenLocationAndTEqual() {
            var uut = SplineTestUtils.ExampleEquidistantLinearSpline.Spline;
        
            var expected = new[] {0, 0.1, 0.3, 0.5, 0.77, 1, 1.5, 1.87, 2};
            var actual = uut.BulkNormalizeOrdered(expected.Select(SplineLocation.From))
                .Select(l => l.Value).ToArray();
        
            for (int i = 0; i < expected.Length; ++i) {
                Assert.That(actual[i], Is.EqualTo(expected[i]), $"Equidistant BulkNormalizationOrdered for t={expected[i]} did not work");
            }
        }
    }
}