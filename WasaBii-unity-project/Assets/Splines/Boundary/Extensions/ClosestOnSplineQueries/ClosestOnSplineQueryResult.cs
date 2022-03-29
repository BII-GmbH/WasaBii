using BII.WasaBii.Units;

namespace BII.WasaBii.CatmullRomSplines {

    public interface ClosestOnSplineQueryResult<TPos, TDiff>
        where TPos : struct 
        where TDiff : struct {
        
        /// The position that was originally queried to find the closest point on the spline 
        TPos QueriedPosition { get; }
        
        /// The position on the spline that is closest to the queried position.
        TPos GlobalPosition { get; }
        
        /// The location on the spline whose position is closest to the queried position.
        SplineLocation Location { get; }
        
        /// The normalized location on the spline whose position is closest to the queried position.
        NormalizedSplineLocation NormalizedLocation { get; }
        
        /// The distance between the queried position and <see cref="GlobalPosition"/>.
        Length Distance { get; }
       
        /// The spline where the closest position is on.
        Spline<TPos, TDiff> Spline { get; }
    }
    
    public struct ClosestOnSplineQueryResult<TWithSpline, TPos, TDiff> : ClosestOnSplineQueryResult<TPos, TDiff> 
        where TWithSpline : WithSpline<TPos, TDiff> where TPos : struct where TDiff : struct {
        internal ClosestOnSplineQueryResult(
            TPos queriedPosition, TWithSpline withSpline, TPos position, NormalizedSplineLocation normalizedLocation
        ) {
            QueriedPosition = queriedPosition;
            WithSpline = withSpline;
            GlobalPosition = position;
            NormalizedLocation = normalizedLocation;
            __cachedLocation = null;
        }

        /// The spline where the closest position is on.
        public TWithSpline WithSpline { get; }

        public Spline<TPos, TDiff>  Spline => WithSpline.Spline;
        
        public TPos QueriedPosition { get; }

        /// The position on the spline that is closest to the queried position.
        public TPos GlobalPosition { get; }

        public TDiff Tangent => Spline[NormalizedLocation].Tangent;

        // Since de-normalizing a location may be an expensive operation on long splines, the value is lazy & cached
        private SplineLocation? __cachedLocation;

        /// The location on the spline whose position is closest to the queried position.
        public SplineLocation Location =>
            __cachedLocation ??
            // intentional assignment
            (__cachedLocation = Spline.DeNormalizedLocation(NormalizedLocation)).Value;

        /// The normalized location on the spline whose position is closest to the queried position.
        public NormalizedSplineLocation NormalizedLocation { get; }

        public Length Distance => Spline.Ops.Distance(QueriedPosition, GlobalPosition);
    }
}