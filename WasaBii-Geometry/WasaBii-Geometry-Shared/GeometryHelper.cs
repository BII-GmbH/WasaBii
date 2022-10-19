namespace WasaBii.Geometry.Shared;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class GeometryHelper : Attribute {
    /// <summary>
    /// Whether the fields are independent from each other, i.e.
    /// you can freely edit one without the object becoming invalid.
    /// </summary>
    public readonly bool AreFieldsIndependent;
    
    /// <summary>
    /// Whether the object represents something that has a magnitude. Only valid if all fields are `Length`s.
    /// </summary>
    public readonly bool HasMagnitude;
    public GeometryHelper(bool areFieldsIndependent, bool hasMagnitude) {
        AreFieldsIndependent = areFieldsIndependent;
        HasMagnitude = hasMagnitude;
    }
}
