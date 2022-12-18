using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BII.WasaBii.Analyzers;
using BII.WasaBii.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace RoslynAnalyzerTemplate.Test;

[TestFixture]
public class RoslynAnalyzerTemplateTest {
    
    private MustBeImmutableAnalyzer _analyzer = null!;
    
    [SetUp]
    public void SetUp() { _analyzer = new(); }
    
    private static Compilation CreateCompilation(string source) => 
        CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(
                source.Replace("[MustBeImmutable]", "[BII.WasaBii.Core.MustBeImmutable]"), 
                new CSharpParseOptions(LanguageVersion.Preview)) 
            },
            new[] {
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location),
                MetadataReference.CreateFromFile(typeof(System.Object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MustBeImmutableAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

    private async Task AssertDiagnostics(int expectedDiagnostics, string code) {
        var comp = CreateCompilation(code).WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(_analyzer));
        var diagnostics = await comp.GetAllDiagnosticsAsync();
        
        Assert.That(
            diagnostics.Where(d => d.Id != MustBeImmutableAnalyzer.DiagnosticId),
            Is.Empty,
            "Compilation failed"
        );
        
        Assert.That(
            diagnostics.Where(d => d.Id == MustBeImmutableAnalyzer.DiagnosticId),
            Has.Exactly(expectedDiagnostics).Items,
            "Incorrect amount of diagnostics"
        );
    }

    [Test]
    public Task EmptySourceCode_NoDiagnostics() => AssertDiagnostics(0, "");

    // TODO CR PREMERGE: more test cases, esp including generics
    
    // Enums
    
    [Test]
    public Task ClassWithEnum_NoDiagnostics() =>
        AssertDiagnostics(0, @"
            public enum ImmutabilityTestEnum { Bagger, Two, EightEight }

            [MustBeImmutable]  
            public readonly struct MustBeImmutableWithEnumField {
                public readonly ImmutabilityTestEnum Enum;
            }");

    // Mutable Simple Types

    [Test]
    public Task MutableStruct_OneDiagnostic() =>
        AssertDiagnostics(1, "[MustBeImmutable] public struct MutableStruct { public int Foo; }");
    
    [Test]
    public Task FieldNotReadonly_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            [MustBeImmutable] 
            public sealed class FieldNotReadonly { 
                public readonly int Foo = 0;
                public int Bar = 0;
            }");
    
    [Test]
    public Task FieldsNotReadonly_TwoDiagnostics() =>
        AssertDiagnostics(2, @"
            [MustBeImmutable] 
            public sealed class FieldNotReadonly { 
                public readonly int Foo = 0;
                public int Bar = 0;
                public int Baz = 1337;
            }");
    
    [Test]
    public Task AutoPropertyFieldWithSet_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            [MustBeImmutable] 
            public sealed class AutoPropertyFieldWithSet { 
                public readonly int Foo = 0;
                public int Bar { get; set; }
            }
        ");
    
    [Test]
    public Task AutoPropertyFieldWithInit_NoDiagnostics() =>
        AssertDiagnostics(0, @"
            namespace System.Runtime.CompilerServices { public static class IsExternalInit { } }
            [MustBeImmutable] 
            public sealed class AutoPropertyFieldWithInit { 
                public readonly int Foo = 0;
                public int Bar { get; init; }
            }");
    
    [Test]
    public Task MutableFieldType_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            public sealed class MutableClass { public int Foo; }
            [MustBeImmutable] 
            public sealed class MutableFieldType { 
                public readonly int Foo = 0;
                public readonly MutableClass Bar = default;
            }");
    
    // Mutable Base Types
    
    [Test]
    public Task HasNotReadOnlyField_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            [MustBeImmutable] 
            public abstract class HasNotReadOnlyField { 
                public int Foo = 0;
            }");
    
    [Test]
    public Task HasMutableFieldType_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            public sealed class MutableClass { public int Foo; }

            [MustBeImmutable] 
            public abstract class HasMutableFieldType { 
                public readonly int Foo = 0;
                public readonly MutableClass Bar = default;
            }");
    
    [Test]
    public Task NotReadOnlyFieldInBase_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            public abstract class HasNotReadOnlyField { public int Foo = 0; }
            [MustBeImmutable] 
            public sealed class NotReadOnlyFieldInBase : HasNotReadOnlyField {}");
    
    [Test]
    public Task MutableFieldTypeInBase_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            public sealed class MutableClass { public int Foo; }
            public abstract class HasMutableFieldType { 
                public readonly int Foo = 0;
                public readonly MutableClass Bar = default;
            }

            [MustBeImmutable] 
            public sealed class MutableFieldTypeInBase : HasMutableFieldType {}");

    // Mutable Abstract Field Types
    
    [Test]
    public Task HasNonImmutableInterfaceField_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            public interface CouldBeMutableInterface {}

            [MustBeImmutable] 
            public sealed class HasNonImmutableInterfaceField {
                public readonly CouldBeMutableInterface Foo = default;
            }");
    
    [Test]
    public Task HasNonImmutableAbstractField_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            public abstract class CouldBeMutableAbstractClass {}

            [MustBeImmutable] 
            public sealed class HasNonImmutableAbstractField {
                public readonly CouldBeMutableAbstractClass Foo = default;
            }");
    
    // Mutable structs referenced as readonly
    
    [Test]
    public Task TopLevelMutableStructInReadOnlyField_NoDiagnostics() =>
        AssertDiagnostics(0, @"
            public struct Vec { public float X, Y, Z; }

            [MustBeImmutable] 
            public sealed class TopLevelMutableStructInReadOnlyField {
                public readonly Vec Vector = default;
            }");
    
    [Test]
    public Task NestedMutableStructsInReadOnlyField_NoDiagnostics() =>
        AssertDiagnostics(0, @"
            public struct Float { public float Value; }
            public struct Vec { public Float X, Y, Z; }

            [MustBeImmutable] 
            public sealed class NestedMutableStructsInReadOnlyField {
                public readonly Vec Vector = default;
            }");

    // Immutable Abstract Field Types
    
    [Test]
    public Task MustBeImmutableWithInterfaceField_NoDiagnostics() =>
        AssertDiagnostics(0, @"
            [MustBeImmutable] public interface MustBeImmutableInterface {}

            [MustBeImmutable] 
            public sealed class MustBeImmutableWithInterfaceField {
                public readonly MustBeImmutableInterface Foo = default;
            }");
    
    [Test]
    public Task MustBeImmutableWithAbstractField_NoDiagnostics() =>
        AssertDiagnostics(0, @"
            [MustBeImmutable] public abstract class MustBeImmutableAbstractClass {}

            [MustBeImmutable] 
            public sealed class MustBeImmutableWithAbstractField {
                public readonly MustBeImmutableAbstractClass Foo = default;
            }");
    
    // Mutable with immutable base types
    
    [Test]
    public Task MutableSubtypeOfMustBeImmutableInterface_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            [MustBeImmutable] public interface MustBeImmutableInterface {}

            public sealed class MutableSubtypeOfMustBeImmutableInterface : MustBeImmutableInterface { 
                public int Foo;
            }");
    
    [Test]
    public Task MutableSubtypeOfMustBeImmutableAbstract_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            [MustBeImmutable] public abstract class MustBeImmutableAbstractClass {}

            public sealed class MutableSubtypeOfMustBeImmutableAbstract : MustBeImmutableAbstractClass {
                public int Foo;
            }");
    
    // Immutable Collection Support (a subset of all types)
    
    [Test]
    public Task SupportsImmutableList_NoDiagnostics() =>
        AssertDiagnostics(0, @"
            using System.Collections.Immutable;

            [MustBeImmutable] 
            public sealed class SupportsImmutableList {
                public readonly ImmutableList<int> Foo = default;
            }");
    
    [Test]
    public Task SupportsImmutableArray_NoDiagnostics() =>
        AssertDiagnostics(0, @"
            using System.Collections.Immutable;

            [MustBeImmutable] 
            public sealed class SupportsImmutableArray {
                public readonly ImmutableArray<int> Foo = default;
            }");
    
    [Test]
    public Task SupportsImmutableHashSet_NoDiagnostics() =>
        AssertDiagnostics(0, @"
            using System.Collections.Immutable;

            [MustBeImmutable] 
            public sealed class SupportsImmutableHashSet {
                public readonly ImmutableHashSet<int> Foo = default;
            }");
    
    [Test]
    public Task SupportsImmutableDictionary_NoDiagnostics() =>
        AssertDiagnostics(0, @"
            using System.Collections.Immutable;

            [MustBeImmutable] 
            public sealed class SupportsImmutableDictionary {
                public readonly ImmutableDictionary<int, double> Foo = default;
            }");
    
    // Immutable Collections With Mutable Elements
    
    [Test]
    public Task RejectsImmutableListWithMutable_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            using System.Collections.Immutable;
            public sealed class MutableClass { public int Foo; }

            [MustBeImmutable] 
            public sealed class ImmutableListOfMutable {
                public readonly ImmutableList<MutableClass> Foo = default;
            }");
    
    [Test]
    public Task RejectsImmutableArrayWithMutable_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            using System.Collections.Immutable;
            public sealed class MutableClass { public int Foo; }

            [MustBeImmutable] 
            public sealed class ImmutableArrayOfMutable {
                public readonly ImmutableArray<MutableClass> Foo = default;
            }");
    
    [Test]
    public Task RejectsImmutableHashSetWithMutable_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            using System.Collections.Immutable;
            public sealed class MutableClass { public int Foo; }

            [MustBeImmutable] 
            public sealed class ImmutableHashSetOfMutable {
                public readonly ImmutableHashSet<MutableClass> Foo = default;
            }");
    
    [Test]
    public Task RejectsImmutableDictWithMutableKeys_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            using System.Collections.Immutable;
            public sealed class MutableClass { public int Foo; }

            [MustBeImmutable] 
            public sealed class ImmutableDictionaryOfMutableKeys {
                public readonly ImmutableDictionary<MutableClass, int> Foo = default;
            }");
    
    [Test]
    public Task RejectsImmutableDictWithMutableValues_OneDiagnostic() =>
        AssertDiagnostics(1, @"
            using System.Collections.Immutable;
            public sealed class MutableClass { public int Foo; }

            [MustBeImmutable] 
            public sealed class ImmutableDictionaryOfMutableValues {
                public readonly ImmutableDictionary<char, MutableClass> Foo = default;
            }");
}