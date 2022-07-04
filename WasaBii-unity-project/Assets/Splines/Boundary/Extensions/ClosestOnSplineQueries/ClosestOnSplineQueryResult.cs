using System;
using BII.WasaBii.Splines.Logic;
using BII.WasaBii.Units;

namespace BII.WasaBii.Splines {

    public readonly struct ClosestOnSplineQueryResult<TPos, TDiff> 
        where TPos : struct 
        where TDiff : struct {
        internal ClosestOnSplineQueryResult(
            TPos queriedPosition, Spline<TPos, TDiff> spline, TPos position, NormalizedSplineLocation normalizedLocation
        ) {
            QueriedPosition = queriedPosition;
            Spline = spline;
            ClosestOnSpline = position;
            NormalizedLocation = normalizedLocation;
            cachedLocation = new Lazy<SplineLocation>(() => spline.DeNormalize(normalizedLocation));
        }

        /// The normalized location on the spline whose position is closest to the queried position.
        public NormalizedSplineLocation NormalizedLocation { get; }

        /// The position on the spline that is closest to the queried position.
        public readonly TPos ClosestOnSpline;

        public readonly TPos QueriedPosition;

        /// The spline where the closest position is on.
        public Spline<TPos, TDiff> Spline { get; }

        // Since de-normalizing a location may be an expensive operation on long splines, the value is lazy & cached
        private readonly Lazy<SplineLocation> cachedLocation;

        
        /// The location on the spline whose position is closest to the queried position.
        public SplineLocation Location => cachedLocation.Value;

        /// The spline's tangent at the location that is closest to the queried position
        public TDiff Tangent => Spline[NormalizedLocation].Tangent;
        
        /// The distance between the queried position and the spline / the position <see cref="ClosestOnSpline"/>
        public Length Distance => Spline.Ops.Distance(QueriedPosition, ClosestOnSpline);
    }
}