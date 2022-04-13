using System.Linq;
using BII.WasaBii.CatmullRomSplines;
using BII.WasaBii.Core;
using BII.WasaBii.Units;

namespace BII.WasaBii.Unity.Geometry.Splines {
    
    [MustBeImmutable][MustBeSerializable]
    public sealed class LocalSplineOps : PositionOperations<LocalPosition, LocalOffset> {

        public static readonly LocalSplineOps Instance = new();
        
        private LocalSplineOps() { }

        public Length Distance(LocalPosition p0, LocalPosition p1) => p0.DistanceTo(p1);

        public LocalOffset Sub(LocalPosition p0, LocalPosition p1) => p0 - p1;
        public LocalPosition Sub(LocalPosition p, LocalOffset d) => p - d;

        public LocalOffset Sub(LocalOffset d1, LocalOffset d2) => d1 - d2;

        public LocalPosition Add(LocalPosition d1, LocalOffset d2) => d1 + d2;

        public LocalOffset Add(LocalOffset d1, LocalOffset d2) => d1 + d2;

        public LocalOffset Div(LocalOffset diff, double d) => diff / d.Number();

        public LocalOffset Mul(LocalOffset diff, double f) => diff * f.Number();

        public double Dot(LocalOffset a, LocalOffset b) => a.Dot(b);
    }
    
    public static class LocalSplineExtensions {
        
        public static Spline<GlobalPosition, GlobalOffset> ToGlobalWith(
            this Spline<LocalPosition, LocalOffset> local, TransformProvider parent
        ) => local.HandlesIncludingMargin().Select(l => l.ToGlobalWith(parent)).ToSplineWithMarginHandlesOrThrow();
        
    }

}