using System.Collections.Immutable;

// We don't care about the actual values of the fields in our dummy classes. Let them be null.
#nullable disable

namespace BII.WasaBii.Core.Tests {
    public class ImmutableValidationTest {

        // [Test]
        // public void TestTrueImmutableErrorCases() {
        //     foreach (var shouldFail in new []{
        //         typeof(MutableStruct),
        //         typeof(FieldNotReadonly),
        //         typeof(AutoPropertyFieldNotReadonly),
        //         typeof(MutableFieldType),
        //         typeof(NotReadOnlyFieldInBase),
        //         typeof(MutableFieldTypeInBase),
        //         typeof(HasNonImmutableInterfaceField), 
        //         typeof(HasNonImmutableAbstractField),
        //         typeof(ImmutableArrayOfMutable),
        //         typeof(ImmutableListOfMutable),
        //         typeof(ImmutableHashSetOfMutable),
        //         typeof(ImmutableDictionaryOfMutableKeys),
        //         typeof(ImmutableDictionaryOfMutableValues)
        //     }) {
        //         Assert.That(() => 
        //             ImmutableValidation.ValidateTrueImmutability(shouldFail), 
        //             Is.Not.Empty, 
        //             shouldFail.Name
        //         );
        //     }
        // }
        //
        // [Test]
        // public void TestTrueImmutableSuccessCases() {
        //     foreach (var shouldWork in new []{
        //         typeof(MustBeImmutableWithInterfaceField),
        //         typeof(MustBeImmutableWithAbstractField),
        //         typeof(SupportsImmutableArray),
        //         typeof(SupportsImmutableList),
        //         typeof(SupportsImmutableHashSet),
        //         typeof(SupportsImmutableDictionary),
        //         typeof(MustBeImmutableWithEnumField)
        //     }) {
        //         var failMessages = ImmutableValidation.ValidateTrueImmutability(shouldWork).ToList();
        //         Assert.That(failMessages, Is.Empty, 
        //             "Must but immutable but was not: [" + shouldWork + "]. See next lines for failures.\n" 
        //                 + string.Join("\n\n", failMessages)
        //         );
        //     }
        // }
        
        // Mutable Simple Types

        public struct MutableStruct {
            public int Foo;
        }

        public sealed class FieldNotReadonly {
            public readonly int Foo = 0;
            public int Bar = 0;
        }
        
        public sealed class AutoPropertyFieldNotReadonly {
            public readonly int Foo = 0;
            public int Bar { get; set; }
        }

        public sealed class MutableFieldType {
            public readonly int Foo = 0;
            public readonly MutableStruct Bar = default;
        }
        
        // Mutable Base Types

        public abstract class HasNotReadOnlyField {
            public int Foo = 0;
        }
        
        public abstract class HasMutableFieldType {
            public readonly int Foo = 0;
            public readonly MutableFieldType Bar = default;
        }
        
        public sealed class NotReadOnlyFieldInBase : HasNotReadOnlyField {}
        
        public sealed class MutableFieldTypeInBase : HasMutableFieldType {}
        
        // Mutable Abstract Field Types
        
        public interface CouldBeMutableInterface {}
        public abstract class CouldBeMutableAbstractClass {}
        
        public sealed class HasNonImmutableInterfaceField {
            public readonly CouldBeMutableInterface Foo = default;
        }

        public sealed class HasNonImmutableAbstractField {
            public readonly CouldBeMutableAbstractClass Foo = default;
        }

        // Immutable Abstract Field Types
        
        [MustBeImmutable] public interface MustBeImmutableInterface {}
        [MustBeImmutable] public interface MustBeImmutableAbstractClass {}
        
        public sealed class MustBeImmutableWithInterfaceField {
            public readonly MustBeImmutableInterface Foo = default;
        }

        public sealed class MustBeImmutableWithAbstractField {
            public readonly MustBeImmutableAbstractClass Foo = default;
        }
        
        // Note CR: Creating a mutable class here that implements the interfaces will lead to the
        //          compile time validation always complaining about these cases. So we don't do that.
        
        // Immutable Collection Support (a subset of all types)

        public sealed class SupportsImmutableList {
            public readonly ImmutableList<MustBeImmutableWithInterfaceField> Foo = default;
        }
        
        public sealed class SupportsImmutableArray {
            public readonly ImmutableArray<float> Foo = default;
        }
        
        public sealed class SupportsImmutableHashSet {
            public readonly ImmutableHashSet<string> Foo = default;
        }
        
        public sealed class SupportsImmutableDictionary {
            public readonly ImmutableDictionary<int, MustBeImmutableWithAbstractField> Foo = default;
        }
        
        // Immutable Collections With Mutable Elements
        
        public sealed class ImmutableListOfMutable {
            public readonly ImmutableList<MutableStruct> Foo = default;
        }
        
        public sealed class ImmutableArrayOfMutable {
            public readonly ImmutableArray<MutableFieldType> Foo = default;
        }
        
        public sealed class ImmutableHashSetOfMutable {
            public readonly ImmutableHashSet<HasNonImmutableInterfaceField> Foo = default;
        }
        
        public sealed class ImmutableDictionaryOfMutableKeys {
            public readonly ImmutableDictionary<MutableStruct, int> Foo = default;
        }
        
        public sealed class ImmutableDictionaryOfMutableValues {
            public readonly ImmutableDictionary<char, FieldNotReadonly> Foo = default;
        }
        
        // Enum Tests

        public enum ImmutabilityTestEnum {
            Bagger,
            Two,
            EightEight
        }

        public readonly struct MustBeImmutableWithEnumField {
            public readonly ImmutabilityTestEnum Enum;
        }
    }
}