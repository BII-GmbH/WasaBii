using BII.Units;
using UnityEngine;

namespace BII.CatmullRomSplines {

    public interface ClosestOnSplineQueryResult {
        
        /// The position that was originally queried to find the closest point on the spline 
        Vector3 QueriedPosition { get; }
        
        /// The position on the spline that is closest to the queried position.
        Vector3 GlobalPosition { get; }
        
        /// The location on the spline whose position is closest to the queried position.
        SplineLocation Location { get; }
        
        /// The normalized location on the spline whose position is closest to the queried position.
        NormalizedSplineLocation NormalizedLocation { get; }
        
        /// The distance between the queried position and <see cref="GlobalPosition"/>.
        Length Distance { get; }
       
        /// The spline where the closest position is on.
        Spline Spline { get; }
    }
    
    public struct ClosestOnSplineQueryResult<TWithSpline> : ClosestOnSplineQueryResult where TWithSpline : WithSpline {
        internal ClosestOnSplineQueryResult(
            Vector3 queriedPosition, TWithSpline withSpline, Vector3 position, NormalizedSplineLocation normalizedLocation
        ) {
            QueriedPosition = queriedPosition;
            WithSpline = withSpline;
            GlobalPosition = position;
            NormalizedLocation = normalizedLocation;
            __cachedLocation = null;
        }

        /// The spline where the closest position is on.
        public TWithSpline WithSpline { get; }

        public Spline Spline => WithSpline.Spline;
        
        public Vector3 QueriedPosition { get; }

        /// The position on the spline that is closest to the queried position.
        public Vector3 GlobalPosition { get; }

        public Vector3 Tangent => Spline[NormalizedLocation].Tangent;

        // Since de-normalizing a location may be an expensive operation on long splines, the value is lazy & cached
        private SplineLocation? __cachedLocation;

        /// The location on the spline whose position is closest to the queried position.
        public SplineLocation Location =>
            __cachedLocation ??
            // intentional assignment
            (__cachedLocation = Spline.DeNormalizedLocation(NormalizedLocation)).Value;

        /// The normalized location on the spline whose position is closest to the queried position.
        public NormalizedSplineLocation NormalizedLocation { get; }

        public Length Distance => Vector3.Distance(QueriedPosition, GlobalPosition).Meters();
    }
}