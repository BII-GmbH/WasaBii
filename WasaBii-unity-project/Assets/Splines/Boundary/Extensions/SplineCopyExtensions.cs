using System.Linq;
using BII.CatmullRomSplines.Logic;
using BII.Units;
using BII.Utilities.Unity;
using UnityEngine;

namespace BII.CatmullRomSplines {
    public static class SplineCopyExtensions {

        /// Creates a deep-copy of the provided spline like.
        public static Spline Copy(this Spline spline) =>
            new ImmutableSpline(spline.HandlesIncludingMargin(), spline.Type);
        
        /// Creates a new spline with a similar trajectory as
        /// <paramref name="original"/>, but with all handle positions
        /// being moved a certain distance to the right,
        /// relative to the provided <paramref name="up"/> vector.
        ///
        /// The returned spline is a non-component spline
        /// and may not be of the same type as <paramref name="original"/>.
        public static Spline CopyWithOffsetToTheRight(
            this Spline original, Length offset, Vector3 up
        ) {
            Vector3 computePosition(
                Spline deriveFrom, Vector3 originalPosition, NormalizedSplineLocation tangentLocation
            ) =>
                originalPosition -
                Vector3.Cross(
                        deriveFrom.TangentAtNormalizedOrThrow(tangentLocation),
                        up
                    )
                    .normalized *
                (float)offset.SIValue;

            return new ImmutableSpline(
                computePosition(original, original.BeginMarginHandle(), NormalizedSplineLocation.Zero),
                original.Handles()
                    .Select(
                        (node, idx) => computePosition(original, node, NormalizedSplineLocation.From(idx))
                    ),
                computePosition(
                    original,
                    original.EndMarginHandle(),
                    NormalizedSplineLocation.From(original.HandleCount() - 1)
                )
            );
        }

        /// Creates a new spline with similar trajectory as <paramref name="original"/>, but all
        /// handle positions being moved a certain distance to the right of its tangent.
        ///
        /// The distance is the same as the distance between the provided <paramref name="referencePosition"/>
        /// and <paramref name="closestToReferenceLocation"/>.
        ///
        /// In short, the returned spline has the same trajectory as the original, but is offset by the distance between
        /// referencePosition and closestToReferenceLocation. 
        ///
        /// The returned spline is a non-component spline and may not be of the same type as the
        /// original.
        public static Spline CopyWithOffsetToTheRightFromReferencePosition(this Spline original, Vector3 referencePosition,
            NormalizedSplineLocation closestToReferenceLocation) {

            var startTangent = original[closestToReferenceLocation].Tangent;

            var closestPointToStart = original[closestToReferenceLocation].Position - referencePosition;

            var rightVector = Vector3.Cross( startTangent, Vector3.up).normalized;

            var isRightOf = Vector3.Dot(closestPointToStart.normalized, rightVector) > 0;

            var offset = isRightOf ?
                Vector3.Project(closestPointToStart, rightVector)
                : Vector3.Project(closestPointToStart, -rightVector);

            var offsetLength = isRightOf ?
                offset.magnitude.Meters()
                : -offset.magnitude.Meters();

            return original.CopyWithOffsetToTheRight(offsetLength, Vector3.up);
        }
        
        /// Creates a new spline with the same trajectory as
        /// <paramref name="original"/>, but with all handle positions
        /// being moved along a certain <paramref name="offset"/>,
        /// independent of the spline's tangent at these points.
        ///
        /// The returned spline is a non-component spline
        /// and may not be of the same type as <paramref name="original"/>.
        public static Spline CopyWithStaticOffset(
            this Spline original, GlobalOffset offset
        ) {
            Vector3 computePosition(Vector3 node) => node + offset.AsVector;
            return new ImmutableSpline(
                computePosition(original.BeginMarginHandle()),
                original.Handles().Select(computePosition),
                computePosition(original.EndMarginHandle())
            );
        }
        
        /// Creates a new spline with a similar trajectory as
        /// <paramref name="original"/>, but different spacing
        /// between the non-margin handles.
        ///
        /// The returned spline is a non-component spline
        /// and may not be of the same type as <paramref name="original"/>.
        public static Spline CopyWithDifferentHandleDistance(
            this Spline original, Length desiredHandleDistance
        ) => new ImmutableSpline(
            original.BeginMarginHandle(),
            original.SampleSplineEvery(desiredHandleDistance),
            original.EndMarginHandle()
        );
        
        /// Creates a new spline that is the reverse of the original
        /// but has the same handles and spline type
        public static Spline Reversed(this Spline original) => 
            new ImmutableSpline(original.HandlesIncludingMargin().Reverse(), original.Type);
    }
}