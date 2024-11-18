using System;
using System.Linq;
using BII.WasaBii.Core;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines.CatmullRom {
    public static class CatmullRomSplineCopyUtils {
    
        /// <summary>
        /// Creates a new spline with a similar trajectory as <paramref name="original"/>, but with all handle
        /// positions being moved by a certain offset which depends on the spline's tangent at these points.
        /// </summary>
        public static CatmullRomSpline<TPos, TDiff, TTime, TVel> CopyWithOffset<TPos, TDiff, TTime, TVel>(
            CatmullRomSpline<TPos, TDiff, TTime, TVel> original, Func<TVel, TDiff> tangentToOffset
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            TPos computePosition(
                Spline<TPos, TDiff, TTime, TVel> deriveFrom, TPos originalPosition, NormalizedSplineLocation tangentLocation
            ) => original.Ops.Sub(originalPosition, tangentToOffset(deriveFrom[tangentLocation].Velocity));

            return new CatmullRomSpline<TPos, TDiff, TTime, TVel>(
                computePosition(original, original.BeginMarginHandle(), NormalizedSplineLocation.Zero)
                    .PrependTo(original.Handles.Select((node, idx) => 
                        computePosition(original, node, NormalizedSplineLocation.From(idx))
                    )).Append(computePosition(
                        original,
                        original.EndMarginHandle(),
                        NormalizedSplineLocation.From(original.Handles.Count - 1)
                    )),
                original.TemporalSegmentOffsets,
                original.Ops,
                original.Type
            );
        }

        /// <summary>
        /// Creates a new spline with the same trajectory as
        /// <paramref name="original"/>, but with all handle positions
        /// being moved along a certain <paramref name="offset"/>,
        /// independent of the spline's tangent at these points.
        /// </summary>
        public static CatmullRomSpline<TPos, TDiff, TTime, TVel> CopyWithStaticOffset<TPos, TDiff, TTime, TVel>(
            CatmullRomSpline<TPos, TDiff, TTime, TVel> original, TDiff offset
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged {
            TPos computePosition(TPos node) => original.Ops.Add(node, offset);
            return new CatmullRomSpline<TPos, TDiff, TTime, TVel>(
                computePosition(original.BeginMarginHandle())
                    .PrependTo(original.Handles.Select(computePosition))
                    .Append(computePosition(original.EndMarginHandle())),
                original.TemporalSegmentOffsets,
                original.Ops,
                original.Type
            );
        }
        
        /// <summary>
        /// Creates a new spline with a similar trajectory as <paramref name="original"/>, but
        /// with a uniform spacing of <paramref name="desiredHandleDistance"/> between the handles.
        /// </summary>
        /// <remarks>The velocities at the new handles are preserved, the accelerations are not.</remarks>
        public static CatmullRomSpline<TPos, TDiff, TTime, TVel> CopyWithDifferentHandleDistance<TPos, TDiff, TTime, TVel>(
            CatmullRomSpline<TPos, TDiff, TTime, TVel> original, Length desiredHandleDistance
        ) where TPos : unmanaged where TDiff : unmanaged where TTime : unmanaged, IComparable<TTime> where TVel : unmanaged =>
            new(
                original.Ops.Lerp(original.Handles[0], original.BeginMarginHandle(), desiredHandleDistance / original.Ops.Distance(original.Handles[0], original.BeginMarginHandle())),
                original.SampleSplineEvery(desiredHandleDistance).Select(sample => (sample.Position, sample.GlobalT)),
                original.Ops.Lerp(original.Handles[^1], original.EndMarginHandle(), desiredHandleDistance / original.Ops.Distance(original.Handles[^1], original.EndMarginHandle())),
                original.Ops,
                original.Type
            );
        
    }
}