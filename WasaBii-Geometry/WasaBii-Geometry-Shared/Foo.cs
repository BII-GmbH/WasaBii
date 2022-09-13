namespace WasaBii_Geometry_Shared; 

[GeometryHelper(areFieldsIndependent: false)]
public readonly partial struct GlobalRotation {
    public readonly float X, Y, Z, W;
}

[GeometryHelper(areFieldsIndependent: true)]
public readonly partial struct GlobalPosition {
    public readonly float X, Y, Z;
}