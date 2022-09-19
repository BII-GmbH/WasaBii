using System;
using System.Collections.Generic;
using System.Linq;
using BII.WasaBii.Core.Editor;
using NUnit.Framework;
using UnityEngine;

// We don't care about the actual values of the fields in our dummy classes. Let them be null.
#nullable disable

namespace BII.WasaBii.Core.Tests {
    
    public class MustBeSerializableValidationTest {

        [Test]
        public void EnsureAllTypesSerializable() {
            Assert.That(
                CompileTimeMustBeSerializableValidation.ValidateAllTypes(), 
                Is.Empty,
                "MustBeSerializable validation failed for types in project"
            );
        }
        
        [MustBeSerializable]
        private sealed class Class_WithGenericField<T>{
            public T Field;
        }
        
        [Test] 
        public void TestMustBeSerializableErrorCases() {
            foreach (var shouldFail in new[]{
                typeof(EnumWithoutAnnotation),
                typeof(ClassWithoutAnnotation),
                typeof(StructWithoutAnnotation),
                typeof((EnumWithoutAnnotation, ClassWithoutAnnotation, StructWithoutAnnotation)),
                
                typeof(Class_NonSealedWithNonAnnotatedField),
                typeof(Class_WithClassWithoutAnnotation),
                typeof(Class_WithStructWithoutAnnotation),
                typeof(Class_WithEnumWithoutAnnotation),
                
                typeof(Struct_WithClassWithoutAnnotation),
                typeof(Struct_WithStructWithoutAnnotation),
                typeof(Struct_WithEnumWithoutAnnotation),
                
                typeof(ClassWithAnnotation_WithClassWithoutAnnotation),
                typeof(ClassWithAnnotation_WithStructWithoutAnnotation),
                typeof(ClassWithAnnotation_WithEnumWithoutAnnotation),
                
                typeof(StructWithAnnotation_WithClassWithoutAnnotation),
                typeof(StructWithAnnotation_WithStructWithoutAnnotation),
                typeof(StructWithAnnotation_WithEnumWithoutAnnotation),
                
                typeof(NonSealedClass_WithPrivateClassWithAnnotation),
                typeof(Class_WithGenericClass_OfClassWithoutAnnotation),
                
                typeof(List<ClassWithoutAnnotation>),
                typeof(HashSet<ClassWithoutAnnotation>)
            }){
                Assert.That(
                    MustBeSerializableValidation.ValidateMustBeSerializable(shouldFail), 
                    Is.Not.Empty, 
                    "Should not be serializable but was: " + shouldFail
                );
            }
        }

        private enum EnumWithoutAnnotation {
            Todd, Howard
        }
        
        private class ClassWithoutAnnotation { }
        private struct StructWithoutAnnotation { }

        private class Class_NonSealedWithNonAnnotatedField {
            public string Field;
        }
        private sealed class Class_WithClassWithoutAnnotation {
            public ClassWithAnnotation Field;
        }
        private sealed class Class_WithStructWithoutAnnotation {
            public StructWithoutAnnotation Field;
        }
        private sealed class Class_WithEnumWithoutAnnotation {
            public EnumWithoutAnnotation Field;
        }
        
        private struct Struct_WithClassWithoutAnnotation {
            public ClassWithAnnotation Field;
        }
        private struct Struct_WithStructWithoutAnnotation {
            public StructWithoutAnnotation Field;
        }
        private struct Struct_WithEnumWithoutAnnotation {
            public EnumWithoutAnnotation Field;
        }

        [MustBeSerializable]
        private sealed class ClassWithAnnotation_WithClassWithoutAnnotation {
            public ClassWithoutAnnotation Field;
        }
        
        [MustBeSerializable]
        private sealed class ClassWithAnnotation_WithStructWithoutAnnotation {
            public StructWithoutAnnotation Field;
        }

        [MustBeSerializable]
        private sealed class ClassWithAnnotation_WithEnumWithoutAnnotation {
            public EnumWithoutAnnotation Field;
        }
        
        
        [MustBeSerializable]
        private struct StructWithAnnotation_WithClassWithoutAnnotation {
            public ClassWithoutAnnotation Field;
        }
        
        [MustBeSerializable]
        private struct StructWithAnnotation_WithStructWithoutAnnotation {
            public StructWithoutAnnotation Field;
        }

        [MustBeSerializable]
        private struct StructWithAnnotation_WithEnumWithoutAnnotation {
            public EnumWithoutAnnotation Field;
        }
        
        [MustBeSerializable]
        private class NonSealedClass_WithPrivateClassWithAnnotation {
            private ClassWithAnnotation _field;
        }
        
        [MustBeSerializable]
        private sealed class Class_WithGenericClass_OfClassWithoutAnnotation {
            public Class_WithGenericField<ClassWithoutAnnotation> Field;
        }

        [Test]
        public void TestMustBeSerializableSuccessCases() {
            foreach (var shouldWork in new[]{
                // Test that we can serialize primitives
                typeof(char), typeof(string), 
                typeof(int), typeof(uint), 
                typeof(short), typeof(ushort), 
                typeof(long), typeof(ulong),
                typeof(double), typeof(float),
                typeof(byte), typeof(sbyte),
                typeof(Class_WithPrimitiveTypes),
                
                // Test the most basic data types
                typeof(EnumWithAnnotation), typeof(ClassWithAnnotation), typeof(StructWithAnnotation),
                // Tuple test
                typeof((EnumWithAnnotation, ClassWithAnnotation, StructWithAnnotation)),
                
                typeof(Class_WithNonSerializedClassWithoutAnnotation),
                typeof(Class_WithRecusriveSelfreference),
                typeof(Class_WithClassWithAnnotation),
                
                // Unity Types
                typeof(Vector3), typeof(Quaternion),
                
                typeof(NonSealedClass_WithPublicClassWithAnnotation),
                typeof(NonSealedClass_WithPrivateNonSerializedClassWithAnnotation),
                typeof(NonSealedClass_WithPrivateSerializedInSubclassesClassWithAnnotation),
                
                typeof(Class_WithGenericClass_OfClassWithAnnotation)
            }) {
                var failMessages = MustBeSerializableValidation.ValidateMustBeSerializable(shouldWork).ToList();
                Assert.That(failMessages, Is.Empty, 
                    "Should be serializable but failed: [" + shouldWork + "]. See next lines for failures.\n" 
                        + string.Join("\n\n", failMessages)
                );
            }
        }

        [MustBeSerializable]
        private enum EnumWithAnnotation {
            Bob, Martin
        }
        
        [MustBeSerializable]
        private class ClassWithAnnotation { }
        [MustBeSerializable]
        private struct StructWithAnnotation { }

        [MustBeSerializable]
        private class Class_WithNonSerializedClassWithoutAnnotation {
            [NonSerialized] 
            public ClassWithoutAnnotation Field;
        }
        
        [MustBeSerializable]
        private class Class_WithClassWithAnnotation {
            [SerializeInSubclasses]
            public ClassWithAnnotation Field;
        }
        
        [MustBeSerializable]
        private class NonSealedClass_WithPublicClassWithAnnotation {
            [SerializeInSubclasses]
            public ClassWithAnnotation Field;
        }

        [MustBeSerializable]
        private sealed class Class_WithPrimitiveTypes {
            public sbyte Sbyte;
            public byte Byte;
            public int Int;
            public uint Uint;
            public short Short;
            public ushort Ushort;
            public long Long;
            public ulong Ulong;
            public float Float;
            public double Double;
            public char Char;
            public string String;
        }

        [MustBeSerializable]
        private sealed class Class_WithRecusriveSelfreference {
            public Class_WithRecusriveSelfreference Self;
        }
        
        [MustBeSerializable]
        private class NonSealedClass_WithPrivateNonSerializedClassWithAnnotation {
            [NonSerialized]
            private ClassWithAnnotation _field;
        }

        [MustBeSerializable]
        private class NonSealedClass_WithPrivateSerializedInSubclassesClassWithAnnotation {
            [SerializeInSubclasses] private ClassWithAnnotation _field;
        }

        [MustBeSerializable]
        private sealed class Class_WithGenericClass_OfClassWithAnnotation {
            public Class_WithGenericField<ClassWithAnnotation> Field;
        }
    }
}
