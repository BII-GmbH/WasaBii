namespace BII.WasaBii.Geometry.Shared;

/// <summary>
/// Enables code generation for a geometry helper struct, which is essentially a wrapper for low-level geometry
/// types like <see cref="System.Numerics.Vector3"/> and <see cref="System.Numerics.Quaternion"/>. Such a wrapper
/// type is designed to provide additional type-safe information like whether a vector value describes a position
/// or a direction. Code generation will add methods like <c>Min</c> / <c>Max</c>, <c>Lerp</c>, <c>Slerp</c> and
/// <c>WithX</c> / <c>WithY</c> / <c>WithZ</c> (because Unity doesn't support record structs atm, so stuff like
/// <c>vector with {x = newX}</c> doesn't work :(
/// </summary>
/// <remarks>The struct needs to be partial! Requires our Units package and
/// certain extension methods from the Geometry package.</remarks>
[AttributeUsage(AttributeTargets.Struct)]
public sealed class GeometryHelper : Attribute {

    /// <summary>
    /// Whether the fields are independent from each other, i.e. you can freely edit one without the object
    /// becoming invalid. In case of a struct directly wrapping a single vector or quaternion, this bool
    /// is instead concerned with their fields, i.e. the individual <c>x</c>, <c>y</c>, <c>z</c> (, <c>w</c>).
    /// </summary>
    /// <example>For a position, its x, y, and z components are completely independent, but for a direction,
    /// the vector should be normalized and providing methods that freely change a value could break that.
    /// If true, methods like <c>position.WithX(newX)</c> and <c>position1.LerpTo(position2, t)</c> are generated.</example>
    public readonly bool AreFieldsIndependent;
    
    /// <summary>
    /// Whether the object represents something that has spatial dimensions. For example, a direction has no
    /// length / magnitude, but a position can be viewed as being a certain distance away from (0,0,0).
    /// </summary>
    /// <example><c>position.X</c> is a <c>Length</c>, but <c>direction.X</c> is a scalar (double).</example>
    public readonly bool HasMagnitude;

    /// <summary>
    /// Whether the object represents something that is directional. For example, a direction has an orientation,
    /// a position does not.
    /// </summary>
    /// <example><c>dir1.AngleTo(dir2)</c>, <c>dir1.SlerpTo(dir2, t)</c></example>
    public readonly bool HasOrientation;

    public GeometryHelper(bool areFieldsIndependent, bool hasMagnitude, bool hasOrientation) {
        AreFieldsIndependent = areFieldsIndependent;
        HasMagnitude = hasMagnitude;
        HasOrientation = hasOrientation;
    }
}
