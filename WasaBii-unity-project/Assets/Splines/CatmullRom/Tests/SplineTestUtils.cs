using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;
using BII.WasaBii.Unity.Geometry.Splines;
using NUnit.Framework;
using UnityEngine;

namespace BII.WasaBii.Splines.CatmullRom.Tests {
    
    using Spline = CatmullRomSpline<Vector3, Vector3>;
    using SplineSegment = SplineSegment<Vector3, Vector3>;
    using SplineSample = SplineSample<Vector3, Vector3>;
    
    internal class SplineTestUtils {

        private const float splineTypeAlphaValue = 0.5f;
        public const double SplineLocationTolerance = 0.01;
        private const float accuracy = 0.01f;

        public static void AssertVectorEquality(Vector3 actual, Vector3 expected) {
            if(Vector3.Distance(actual, expected) >= accuracy)
                throw new AssertionException($"The vectors were not equal.\n Expected: {expected} +/- {accuracy}\n But was: {actual}");
        }

        [Test]
        public void MockedSplineTest() {
            var uut = ExampleLinearSpline.Spline;
        
            Assert.That(uut.Spline, Is.EqualTo(uut));
            
            Assert.That(uut[SplineHandleIndex.At(0)], Is.EqualTo(ExampleLinearSpline.FirstHandle));
            Assert.That(uut[SplineHandleIndex.At(1)], Is.EqualTo(ExampleLinearSpline.SecondHandle));
            Assert.That(uut[SplineHandleIndex.At(2)], Is.EqualTo(ExampleLinearSpline.ThirdHandle));
            Assert.That(uut[SplineHandleIndex.At(3)], Is.EqualTo(ExampleLinearSpline.FourthHandle));
            
            Assert.That(uut.HandleCountIncludingMargin, Is.EqualTo(ExampleLinearSpline.HandleCount));
            Assert.That(uut.Type, Is.EqualTo(SplineType.Centripetal));
        
            Assert.That(() => uut[SplineSegmentIndex.Zero], Throws.Nothing);
            Assert.That(() => uut[SplineLocation.Zero], Throws.Nothing);
            Assert.That(() => uut[NormalizedSplineLocation.Zero], Throws.Nothing);
            
            Assert.That(uut[NormalizedSplineLocation.Zero].Position, Is.EqualTo(ExampleLinearSpline.Expected0Position));
            Assert.That(uut[NormalizedSplineLocation.From(0.5f)].Position, Is.EqualTo(ExampleLinearSpline.Expected05Position));
            Assert.That(uut[NormalizedSplineLocation.From(1)].Position, Is.EqualTo(ExampleLinearSpline.Expected1Position));
        }

        public static class ExampleInvalidSpline {
            public static Vector3 FirstHandle = new Vector3(0, 0, 1);
            public static Vector3 SecondHandle = new Vector3(0, 0, 2);

            public const int HandleCount = 2;
            public static Spline Spline => UnitySpline.FromHandlesIncludingMargin(
                new []{FirstHandle, SecondHandle},
                SplineType.Centripetal
            );
        }
        
        public static class ExampleLinearSpline {
            public const int HandleCount = 4;

            public static Vector3 FirstHandle = new Vector3(0, 0, 1);
            public static Vector3 SecondHandle = new Vector3(0, 0, 2);
            public static Vector3 ThirdHandle = new Vector3(0, 0, 3);
            public static Vector3 FourthHandle = new Vector3(0, 0, 4);
            
            public static Vector3 Expected0Position = SecondHandle;
            public static Vector3 Expected05Position = new Vector3(0, 0, 2.5f);
            public static Vector3 Expected1Position = ThirdHandle;

            public static Vector3 Expected0Tangent = ThirdHandle - SecondHandle;
            public static Vector3 Expected05Tangent = ThirdHandle - SecondHandle;
            public static Vector3 Expected1Tangent = ThirdHandle - SecondHandle;
            
            public static Vector3 Expected0Curvature = Vector3.zero;
            public static Vector3 Expected05Curvature = Vector3.zero;
            public static Vector3 Expected1Curvature = Vector3.zero;
            
            public static CubicPolynomial<Vector3, Vector3> Polynomial => CubicPolynomial.FromCatmullRomSegment(
                new CatmullRomSegment<Vector3, Vector3>(FirstHandle, SecondHandle, ThirdHandle, FourthHandle, UnitySpline.GeometricOperations.Instance),
                splineTypeAlphaValue
            );

            public static Spline Spline => UnitySpline.FromHandlesIncludingMargin(
                new[]{FirstHandle, SecondHandle, ThirdHandle, FourthHandle},
                SplineType.Centripetal
            );
        }
        
        public static class ExampleEquidistantLinearSpline {
            public const int HandleCount = 5;

            public static Vector3 FirstHandle = new Vector3(0, 0, 1);
            public static Vector3 SecondHandle = new Vector3(0, 0, 2);
            public static Vector3 ThirdHandle = new Vector3(0, 0, 3);
            public static Vector3 FourthHandle = new Vector3(0, 0, 4);
            public static Vector3 FifthHandle = new Vector3(0, 0, 5);
            
            public static Spline Spline => UnitySpline.FromHandlesIncludingMargin(
                new[]{FirstHandle, SecondHandle, ThirdHandle, FourthHandle, FifthHandle},
                SplineType.Centripetal
            );
        }
        
        public static class ExampleCurvedSpline {
            
            // These are arbitrary handles chosen to create a spline with curves 
            public static Vector3 FirstHandle = new Vector3(0, 1, 0.2f);
            public static Vector3 SecondHandle = new Vector3(1, 0, 0.5f);
            public static Vector3 ThirdHandle = new Vector3(2, 3.3f, -2);
            public static Vector3 FourthHandle = new Vector3(4, -3.4f, -12.5f);
        
            // All values below (position, tangent and curvature) have been manually
            // confirmed to be the expected values by testing them with the previous spline system.
            public static Vector3 Expected0Position = SecondHandle;
            public static Vector3 Expected05Position = new Vector3(1.5449f, 1.584f, -0.346f);
            public static Vector3 Expected1Position = ThirdHandle;
            
            public static Vector3 Expected0Tangent = new Vector3(1.453f, 0.130f, -0.595f);
            public static Vector3 Expected05Tangent = new Vector3(0.872f, 4.753f, -2.645f);
            public static Vector3 Expected1Tangent = new Vector3(1.060f, 0.657f, -3.823f);

            public static Vector3 Expected0Curvature = new Vector3(-1.929f, 17.965f, -4.973f);
            public static Vector3 Expected05Curvature = new Vector3(-0.393f, 0.526f, -3.228f);
            public static Vector3 Expected1Curvature = new Vector3(1.143f, -16.913f, -1.483f);
            
            public static Length ExpectedSplineLength => 4.413755.Meters();

            public static CubicPolynomial<Vector3, Vector3> Polynomial => CubicPolynomial.FromCatmullRomSegment(
                new CatmullRomSegment<Vector3, Vector3>(FirstHandle, SecondHandle, ThirdHandle, FourthHandle, UnitySpline.GeometricOperations.Instance),
                splineTypeAlphaValue
            );
            
            public static Spline Spline => UnitySpline.FromHandlesIncludingMargin(
                new[]{FirstHandle, SecondHandle, ThirdHandle, FourthHandle},
                SplineType.Centripetal
            );
        }
        
       
    }
}