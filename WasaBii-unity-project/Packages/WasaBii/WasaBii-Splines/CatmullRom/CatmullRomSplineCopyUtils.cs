using System;
using System.Linq;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines.CatmullRom {
    public static class CatmullRomSplineCopyUtils {
    
        /// Creates a new spline with a similar trajectory as <paramref name="original"/>, but with all handle
        /// positions being moved by a certain offset which depends on the spline's tangent at these points.
        public static CatmullRomSpline<TPos, TDiff> CopyWithOffset<TPos, TDiff>(
            CatmullRomSpline<TPos, TDiff> original, Func<TDiff, TDiff> tangentToOffset
        ) where TPos : unmanaged where TDiff : unmanaged {
            TPos computePosition(
                Spline<TPos, TDiff> deriveFrom, TPos originalPosition, NormalizedSplineLocation tangentLocation
            ) => original.Ops.Sub(originalPosition, tangentToOffset(deriveFrom[tangentLocation].Tangent));

            return new CatmullRomSpline<TPos, TDiff>(
                computePosition(original, original.BeginMarginHandle(), NormalizedSplineLocation.Zero),
                original.Handles.Select((node, idx) => 
                    computePosition(original, node, NormalizedSplineLocation.From(idx))
                ),
                computePosition(
                    original,
                    original.EndMarginHandle(),
                    NormalizedSplineLocation.From(original.Handles.Count - 1)
                ),
                original.Ops,
                original.Type
            );
        }

        /// Creates a new spline with the same trajectory as
        /// <paramref name="original"/>, but with all handle positions
        /// being moved along a certain <paramref name="offset"/>,
        /// independent of the spline's tangent at these points.
        public static CatmullRomSpline<TPos, TDiff> CopyWithStaticOffset<TPos, TDiff>(
            CatmullRomSpline<TPos, TDiff> original, TDiff offset
        ) where TPos : unmanaged where TDiff : unmanaged {
            TPos computePosition(TPos node) => original.Ops.Add(node, offset);
            return new CatmullRomSpline<TPos, TDiff>(
                computePosition(original.BeginMarginHandle()),
                original.Handles.Select(computePosition),
                computePosition(original.EndMarginHandle()),
                original.Ops,
                original.Type
            );
        }
        
        /// Creates a new spline with a similar trajectory as
        /// <paramref name="original"/>, but different spacing
        /// between the non-margin handles.
        public static CatmullRomSpline<TPos, TDiff> CopyWithDifferentHandleDistance<TPos, TDiff>(
            CatmullRomSpline<TPos, TDiff> original, Length desiredHandleDistance
        ) where TPos : unmanaged where TDiff : unmanaged =>
            new(
                original.Ops.Lerp(original.Handles[0], original.BeginMarginHandle(), desiredHandleDistance / original.Ops.Distance(original.Handles[0], original.BeginMarginHandle())),
                original.SampleSplineEvery(desiredHandleDistance).Select(sample => sample.Position),
                original.Ops.Lerp(original.Handles[^1], original.EndMarginHandle(), desiredHandleDistance / original.Ops.Distance(original.Handles[^1], original.EndMarginHandle())),
                original.Ops,
                original.Type
            );
        
    }
}