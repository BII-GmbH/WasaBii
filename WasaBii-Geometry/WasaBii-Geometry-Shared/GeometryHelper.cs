namespace WasaBii_Geometry_Shared;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class GeometryHelper : Attribute {
    public readonly bool AreFieldsIndependent;
    public GeometryHelper(bool areFieldsIndependent = true) => AreFieldsIndependent = areFieldsIndependent;
}
