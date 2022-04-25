using System;

namespace BII.WasaBii.Core {

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum,
        Inherited = false, // must be explicitly present on all subclasses as well
        AllowMultiple = false
    )]
    public class MustBeSerializableAttribute : Attribute { }
    
    /// <summary>
    /// Ignores the <see cref="MustBeSerializableAttribute"/> in a parent class.
    /// A type with this annotation is skipped during serializability validation.
    /// </summary>
    /// <devremarks>
    /// This is only for testing purposes. Some testing mocks may intentionally include non-serializable objects.\
    /// Hence the two underscores in the name.
    /// </devremarks>
    // ReSharper disable once InconsistentNaming // intentional, should be used with great caution
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class __IgnoreMustBeSerializableAttribute : Attribute { }

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = false
    )]
    public class CannotBeSerializedAttribute : __IgnoreMustBeSerializableAttribute {
        public readonly string Reason;
        public CannotBeSerializedAttribute(string reason) => Reason = reason;
    }
    
    [AttributeUsage(
        AttributeTargets.Field,
        AllowMultiple = false
    )]
    public class SerializeInSubclassesAttribute : Attribute { }
        
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
    /// - Primitive types like <see cref="int"/>, <see cref="char"/> etc
    /// - <see cref="string"/> as a special case
    /// - All collections in <see cref="System.Collections.Immutable"/>
    /// - <see cref="SingleLinkedList{T}"/>
    /// - `readonly struct`s with only true immutable fields
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
    /// </summary>
    /// <devremarks>
    /// This is only for testing purposes. Some testing mocks may intentionally include mutability.\
    /// Hence the two underscores in the name.
    /// </devremarks>
    // ReSharper disable once InconsistentNaming // intentional, should be used with great caution
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public sealed class __IgnoreMustBeImmutableAttribute : Attribute { }

}