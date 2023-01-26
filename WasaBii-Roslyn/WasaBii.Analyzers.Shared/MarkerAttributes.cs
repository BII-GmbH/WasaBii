namespace BII.WasaBii.Core; 

/// <summary>
/// Ensures that a type and all of its derived types is *immutable*,
/// using custom post-compilation checking.
/// </summary>
/// <remarks>
/// A type is immutable when it is either a collection in System.Collections.Immutable
/// of an immutable type or fulfills all of the following conditions:
/// - All its fields can only be assigned in its constructor (= are readonly).
/// - Each base classes' fields can only be assigned in its constructor.
/// - The types of all fields are either <see cref="MustBeImmutableAttribute"/> or recursively immutable as well.
///
/// This holds true for:
/// - primitive types like <see cref="int"/>, <see cref="char"/> etc
/// - all unmanaged `struct`s when stored in a `readonly` field
/// - <see cref="string"/> and <see cref="Type"/> as a special cases
/// - <see cref="Lazy{T}"/> as long as the wrapped type is also immutable
/// - all collections in the `System.Collections.Immutable` namespace
/// - `struct`s with only true immutable fields
/// - `class`es with only readonly true immutable fields in their class hierarchy
/// - interfaces and abstract classes with an explicit <see cref="MustBeImmutableAttribute"/> attribute
/// </remarks>
/// <devremarks>
/// We allow unsealed classes to count as immutable, even through it could be extended mutably.
/// The reasoning behind this is: We validate all types inheriting from any annotated type.
/// Therefore, when a mutable subclass exists, then the subclass will be validated as well.
/// This ensures that all classes are immutable, even when unsealed.
/// </devremarks>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)] 
public sealed class MustBeImmutableAttribute : Attribute { }

/// <summary>
/// Ignores the <see cref="MustBeImmutableAttribute"/> in a parent class.
/// A type with this annotation is skipped during immutability validation.
/// Type parameters with this are also not required to have a constraint with <see cref="MustBeImmutableAttribute"/>.
/// </summary>
/// <devremarks>
/// This is only for testing purposes and for emergencies or prototypes. Hence the two underscores in the name.
/// Some testing mocks may intentionally include mutability.
/// Not inherited, because we want this to be explicitly present on every non-validated type.
/// </devremarks>
// ReSharper disable InconsistentNaming // intentional, should be used with great caution
[AttributeUsage(
    AttributeTargets.Interface 
    | AttributeTargets.Class 
    | AttributeTargets.Struct 
    | AttributeTargets.GenericParameter, 
    Inherited = false
)]
public sealed class __IgnoreMustBeImmutableAttribute : Attribute { }