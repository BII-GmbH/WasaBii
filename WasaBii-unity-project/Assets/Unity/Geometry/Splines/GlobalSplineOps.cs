using BII.WasaBii.CatmullRomSplines;
using BII.WasaBii.Core;
using BII.WasaBii.Units;

namespace BII.WasaBii.Unity.Geometry.Splines {
    
    [MustBeImmutable][MustBeSerializable]
    public sealed class GlobalSplineOps : PositionOperations<GlobalPosition, GlobalOffset> {

        public static readonly GlobalSplineOps Instance = new();
        
        private GlobalSplineOps() { }

        public Length Distance(GlobalPosition p0, GlobalPosition p1) => p0.DistanceTo(p1);

        public GlobalOffset Sub(GlobalPosition p0, GlobalPosition p1) => p0 - p1;
        public GlobalPosition Sub(GlobalPosition p, GlobalOffset d) => p - d;

        public GlobalOffset Sub(GlobalOffset d1, GlobalOffset d2) => d1 - d2;

        public GlobalPosition Add(GlobalPosition d1, GlobalOffset d2) => d1 + d2;

        public GlobalOffset Add(GlobalOffset d1, GlobalOffset d2) => d1 + d2;

        public GlobalOffset Div(GlobalOffset diff, double d) => diff / d.Number();

        public GlobalOffset Mul(GlobalOffset diff, double f) => diff * f.Number();

        public double Dot(GlobalOffset a, GlobalOffset b) => a.Dot(b);
    }
    
}