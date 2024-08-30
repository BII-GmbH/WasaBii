using System;
using BII.WasaBii.Splines.Maths;
using BII.WasaBii.UnitSystem;

namespace BII.WasaBii.Splines {

    public readonly struct ClosestOnSplineQueryResult<TPos, TDiff, TTime, TVel> 
        where TPos : unmanaged 
        where TDiff : unmanaged
        where TTime : unmanaged, IComparable<TTime>
        where TVel : unmanaged
    {
        internal ClosestOnSplineQueryResult(
            TPos queriedPosition, Spline<TPos, TDiff, TTime, TVel> spline, SplineSample<TPos, TDiff, TTime, TVel> sample
        ) {
            QueriedPosition = queriedPosition;
            Spline = spline;
            Sample = sample;
            __cachedLocation = new Lazy<SplineLocation>(() => spline.DeNormalizeOrThrow(sample.NormalizedLocation));
        }
        
        public readonly SplineSample<TPos, TDiff, TTime, TVel> Sample;

        public TTime Time => Sample.GlobalT;

        /// <summary> The normalized location on the spline whose position is closest to the queried position. </summary>
        public NormalizedSplineLocation NormalizedLocation => Sample.NormalizedLocation;

        // Since de-normalizing a location may be an expensive operation on long splines, the value is lazy & cached. 
        private readonly Lazy<SplineLocation> __cachedLocation;

        /// <summary> The location on the spline whose position is closest to the queried position. </summary>
        public SplineLocation Location => __cachedLocation.Value;

        /// <summary> The position on the spline that is closest to the queried position. </summary>
        public readonly TPos ClosestOnSpline => Sample.Position;

        public readonly TPos QueriedPosition;

        /// <summary> The spline where the closest position is on. </summary>
        public Spline<TPos, TDiff, TTime, TVel> Spline { get; }
        
        /// <summary> The distance between the queried position and the spline / the position <see cref="ClosestOnSpline"/>. </summary>
        public Length Distance => Spline.Ops.Distance(QueriedPosition, ClosestOnSpline);
    }
}