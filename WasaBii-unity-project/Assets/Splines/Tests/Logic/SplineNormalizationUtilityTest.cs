using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static BII.WasaBii.Splines.Logic.SplineNormalizationUtility;
using static BII.WasaBii.Splines.Tests.SplineTestUtils;

namespace BII.WasaBii.Splines.Tests {
    public class SplineNormalizationUtilityTest {
        private static readonly Dictionary<SplineLocation, NormalizedSplineLocation> normalizaionSamples = new Dictionary<float, float>
            {{0, 0}, {0.5f, 0.113f}, {1, 0.227f}, {2.6f, 0.590f}, {3, 0.681f}, {4.404f, 1f}}.ToDictionary(
            kvp => SplineLocation.From(kvp.Key),
            kvp => NormalizedSplineLocation.From(kvp.Value)
        );

        private static readonly Dictionary<NormalizedSplineLocation, SplineLocation> deNormalizaionSamples = new Dictionary<float, float>
            {{0, 0}, {0.1f, 0.440f}, {0.3f, 1.321f}, {0.55f, 2.422f}, {0.7f, 3.083f}, {1f, 4.404f}}.ToDictionary(
            kvp => NormalizedSplineLocation.From(kvp.Key),
            kvp => SplineLocation.From(kvp.Value)
        );
        
        [Test]
        public void DeNormalize_BatchTest() {
            var uut = SplineTestUtils.ExampleCurvedSpline.Spline;
        
            foreach (var kvp in deNormalizaionSamples) {
                var location = DeNormalize(uut, kvp.Key);
                Assert.That(location.Value, Is.EqualTo(kvp.Value.Value).Within(SplineLocationTolerance));
            }
        }
        
        [Test]
        public void DeNormalize_WhenEquidistantNode_ThenTAndLocationEqual() {
            var uut = SplineTestUtils.ExampleEquidistantLinearSpline.Spline;
        
            foreach (var t in new[] {0, 0.1, 0.3, 0.5, 0.77, 1, 1.5, 1.99, 2}.Select(SplineLocation.From)) {
                var location = DeNormalize(uut, NormalizedSplineLocation.From(t.Value));
        
                Assert.That(location, Is.EqualTo(t), $"Equidistant DeNormalization for t={t} did not work");
            }
        }
        
        
        [Test]
        public void Normalize_BatchTest() {
            var uut = SplineTestUtils.ExampleCurvedSpline.Spline;
        
            foreach (var kvp in normalizaionSamples) {
                var t = Normalize(uut, kvp.Key);
                Assert.That(t.Value, Is.EqualTo(kvp.Value.Value).Within(SplineLocationTolerance));
            }
        }
        
        [Test]
        public void Normalize_WhenEquidistantNode_ThenLocationAndTEqual() {
            var uut = SplineTestUtils.ExampleEquidistantLinearSpline.Spline;
        
            foreach (var t in new[] {0, 0.1, 0.3, 0.5, 0.77, 1, 1.5, 1.87, 2}.Select(SplineLocation.From)) {
                var location = Normalize(uut, t);
        
                Assert.That(location, Is.EqualTo(NormalizedSplineLocation.From(t.Value)), $"Equidistant Normalization for t={t} did not work");
            }
        }
        
        
        [Test]
        public void Normalize_WhenSegmentLengthAsLocation_ThenIntegerValueReturned() {
            var spline = SplineTestUtils.ExampleEquidistantLinearSpline.Spline;
            var length = spline[SplineSegmentIndex.Zero].Length();
            
            var uut = Normalize(spline, length);
        
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
            
            var actual = BulkNormalizeOrdered(uut, toNormalize).Select(l => l.Value).ToArray();
        
            for (int i = 0; i < actual.Length; ++i){
                Assert.That(actual[i], Is.EqualTo(expected[i].Value).Within(SplineLocationTolerance));
            }
            
        }
        
        [Test]
        public void BulkNormalizeOrdered_WhenEquidistantNode_ThenLocationAndTEqual() {
            var uut = SplineTestUtils.ExampleEquidistantLinearSpline.Spline;
        
            var expected = new[] {0, 0.1, 0.3, 0.5, 0.77, 1, 1.5, 1.87, 2};
            var actual = BulkNormalizeOrdered(uut, expected.Select(SplineLocation.From))
                .Select(l => l.Value).ToArray();
        
            for (int i = 0; i < expected.Length; ++i) {
                Assert.That(actual[i], Is.EqualTo(expected[i]), $"Equidistant BulkNormalizationOrdered for t={expected[i]} did not work");
            }
        }
    }
}