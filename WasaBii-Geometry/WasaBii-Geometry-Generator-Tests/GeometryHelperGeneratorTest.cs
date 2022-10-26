using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using WasaBii.Geometry.Shared;

namespace BII.WasaBii.UnitSystem;

// https://gist.github.com/chsienki/2955ed9336d7eb22bcb246840bfeb05c

public class HardCodedText : AdditionalText {
    public string Text { get; }
    public override string Path { get; }
  
    public HardCodedText(string text, string path) {
        this.Text = text;
        this.Path = path;
    }

    public override SourceText? GetText(CancellationToken cancellationToken = new()) => 
        SourceText.From(Text);
}

public class GeneratorTests
{
    [Test]
    public void EnsureExampleCompiles() {
        
        Compilation comp = CreateCompilation(@"
using BII.WasaBii.UnitSystem;
using UnityEngine;
using WasaBii.Geometry.Shared;
using static WasaBii.Geometry.Shared.FieldType;

namespace BII.WasaBii.Unity.Geometry {
    [GeometryHelper(areFieldsIndependent: false, fieldType: Other, hasMagnitude: false, hasDirection: false)]
    public readonly partial struct GlobalRotation2 {
        public readonly Quaternion AsQuaternion;
    }

    [GeometryHelper(areFieldsIndependent: true, fieldType: FieldType.Length, hasMagnitude: false, hasDirection: false)]
    public readonly partial struct GlobalPosition2 {
        public readonly Length X, Y, Z;
    }

    [GeometryHelper(areFieldsIndependent: true, fieldType: FieldType.Length, hasMagnitude: true, hasDirection: true)]
    public readonly partial struct GlobalOffset2 {
        public readonly Length X, Y, Z;
        public Length test() => Magnitude;
    }

    [GeometryHelper(areFieldsIndependent: false, FieldType.Double, hasMagnitude: false, hasDirection: true)]
    public readonly partial struct GlobalDirection2 {
        public readonly double X, Y, Z;
    }
}");
        var newComp = RunGenerators(comp, out var generatorDiags, Array.Empty<AdditionalText>(), new GeometryHelperGenerator());

        Assert.That(generatorDiags, Is.Empty);

        var sources = newComp.SyntaxTrees.Select(t => t.ToString()).ToArray();
        Console.WriteLine(string.Join("\n\n", sources));

        var diagnostics = newComp.GetDiagnostics();
        Assert.That(diagnostics, Is.Empty);
    }


    private static Compilation CreateCompilation(string? source = null) => 
        CSharpCompilation.Create("compilation",
            source == null 
                ? Array.Empty<CSharpSyntaxTree>()
                : new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
            new[] {
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location),
                MetadataReference.CreateFromFile(typeof(System.Object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GeometryHelper).Assembly.Location)
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

    private static Compilation RunGenerators(
        Compilation c, 
        out ImmutableArray<Diagnostic> diagnostics, 
        IEnumerable<AdditionalText> additionalFiles, 
        params ISourceGenerator[] generators
    ) {
        CSharpGeneratorDriver.Create(
            generators, 
            additionalFiles, 
            parseOptions: (CSharpParseOptions?) c.SyntaxTrees.FirstOrDefault()?.Options
        ).RunGeneratorsAndUpdateCompilation(c, out var d, out diagnostics);
        
        return d;
    }

}