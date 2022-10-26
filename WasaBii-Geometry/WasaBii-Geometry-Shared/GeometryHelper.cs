namespace WasaBii.Geometry.Shared;

public enum FieldType {
    Float, Double, Length, Other
}

[AttributeUsage(AttributeTargets.Struct)]
public sealed class GeometryHelper : Attribute {

    /// <summary>
    /// Whether the fields are independent from each other, i.e.
    /// you can freely edit one without the object becoming invalid.
    /// </summary>
    public readonly bool AreFieldsIndependent;
    
    /// <summary>
    /// If not <see cref="Other"/>, functions like `Dot` will be generated. Non-null values are only valid if **all** fields
    /// actually have that type.
    /// </summary>
    public readonly FieldType FieldType;
    
    /// <summary>
    /// Whether the object represents something that has a magnitude. Only valid if <see cref="FieldType"/> it not <see cref="FieldType.Other"/>.
    /// </summary>
    public readonly bool HasMagnitude;

    /// <summary>
    /// Whether the object represents something that is directional. Only valid if <see cref="FieldType"/> it not <see cref="FieldType.Other"/>.
    /// </summary>
    public readonly bool HasDirection;

    public GeometryHelper(bool areFieldsIndependent, FieldType fieldType, bool hasMagnitude, bool hasDirection) {
        AreFieldsIndependent = areFieldsIndependent;
        FieldType = fieldType;
        HasMagnitude = hasMagnitude;
        HasDirection = hasDirection;
    }
}
