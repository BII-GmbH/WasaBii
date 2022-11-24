using BII.WasaBii.UnitSystem;
using BII.WasaBii.Geometry.Shared;

namespace BII.WasaBii.Geometry
{
    
    [GeometryHelper(areFieldsIndependent: true, fieldType: FieldType.Length, hasMagnitude: true, hasDirection: true)]
    public partial struct GlobalOffset3 {
        public Length X { init; get; }
        public Length Y { init; get; }
        public Length Z { init; get; }
        public Length test => this.Magnitude;
    }

}
