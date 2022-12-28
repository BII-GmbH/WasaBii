namespace BII.WasaBii.Geometry.Shared;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class GeometryHelper : Attribute {

    /// <summary>
    /// Whether the fields are independent from each other, i.e.
    /// you can freely edit one without the object becoming invalid.
    /// </summary>
    public readonly bool AreFieldsIndependent;
    
    /// <summary>
    /// Whether the object represents something that has a magnitude. Only valid if <see cref="FieldType"/> it not <see cref="FieldType.Other"/>.
    /// </summary>
    public readonly bool HasMagnitude;

    /// <summary>
    /// Whether the object represents something that is directional. Only valid if <see cref="FieldType"/> it not <see cref="FieldType.Other"/>.
    /// </summary>
    public readonly bool HasOrientation;

    public GeometryHelper(bool areFieldsIndependent, bool hasMagnitude, bool hasOrientation) {
        AreFieldsIndependent = areFieldsIndependent;
        HasMagnitude = hasMagnitude;
        HasOrientation = hasOrientation;
    }
}
