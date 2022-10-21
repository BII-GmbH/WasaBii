namespace WasaBii.Geometry.Shared;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class GeometryHelper : Attribute {

    public enum BaseType {
        Float, Double, Length
    }
    
    /// <summary>
    /// Whether the fields are independent from each other, i.e.
    /// you can freely edit one without the object becoming invalid.
    /// </summary>
    public readonly bool AreFieldsIndependent;
    
    /// <summary>
    /// Whether the object consists pure.
    /// This defines whether functions like `Dot` will be generated.
    /// </summary>
    public readonly bool IsBasic;
    
    /// <summary>
    /// Whether the object represents something that has a magnitude. Only valid if <see cref="IsBasic"/> is true-
    /// </summary>
    public readonly bool HasMagnitude;

    public GeometryHelper(bool areFieldsIndependent, bool isBasic, bool hasMagnitude) {
        AreFieldsIndependent = areFieldsIndependent;
        IsBasic = isBasic;
        HasMagnitude = hasMagnitude;
    }
}
