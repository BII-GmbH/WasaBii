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

namespace BII.WasaBii.Units;

// https://gist.github.com/chsienki/2955ed9336d7eb22bcb246840bfeb05c

// TODO: Implement Joule = Nm/s²

public class HardCodedText : AdditionalText {
    public HardCodedText(string text, string path) {
        this.Text = text;
        this.path = path;
    }

    public readonly string Text;
    private readonly string path;

    public override SourceText? GetText(CancellationToken cancellationToken = new()) => 
        SourceText.From(Text);

    public override string Path => path;
}

public class GeneratorTests
{
    [Test]
    public void SimpleGeneratorTest() {
        var resourceText = testJson;
        
        Compilation comp = CreateCompilation(/* "namespace Foo { public static class Main { public static void Main(string[] args){} } }" */);
        var newComp = RunGenerators(comp, out var generatorDiags, new [] {
          new HardCodedText(resourceText, "test.units.json")
        }, new UnitGenerator());

        Assert.That(generatorDiags, Is.Empty);

        var sources = newComp.SyntaxTrees.Select(t => t.ToString()).ToArray();
        Console.WriteLine(string.Join("\n\n", sources));

        var diagnostics = newComp.GetDiagnostics();
        Assert.That(diagnostics, Is.Empty);
    }


    private static Compilation CreateCompilation(string? source = null) => 
        CSharpCompilation.Create("compilation",
            source == null 
                ? new CSharpSyntaxTree[] {} 
                : new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
            new[] {
                MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location),
                MetadataReference.CreateFromFile(typeof(System.Object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Units).Assembly.Location) 
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

    private const string testJson = @"{
  ""namespace"": ""BII.WasaBii.Units"",
  ""baseUnits"": [
    
    {
      ""typeName"": ""Angle"",
      ""siUnit"": {
        ""name"": ""Radians"",
        ""short"": ""rad""
      },
      ""additionalUnits"": [
        {
          ""name"": ""Degrees"",
          ""short"": ""°"",
          ""factor"": 57.2958
        }
      ],
      ""generateExtensions"": true
    },

    {
      ""typeName"": ""Duration"",
      ""siUnit"": {
        ""name"": ""Seconds"",
        ""short"": ""s""
      },
      ""additionalUnits"": [
        {
          ""name"": ""Milliseconds"",
          ""short"": ""ms"",
          ""factor"": 0.001
        },
        {
          ""name"": ""Minutes"",
          ""short"": ""min"",
          ""factor"": 60
        },
        {
          ""name"": ""Hours"",
          ""short"": ""h"",
          ""factor"": 3600
        },
        {
          ""name"": ""Days"",
          ""short"": ""d"",
          ""factor"": 86400
        },
        {
          ""name"": ""Weeks"",
          ""short"": ""w"",
          ""factor"": 604800
        }
      ],
      ""generateExtensions"": true
    },

    {
      ""typeName"": ""Force"",
      ""siUnit"": {
        ""name"": ""Newton"",
        ""short"": ""N""
      },
      ""additionalUnits"": [],
      ""generateExtensions"": true
    },
    
    {
      ""typeName"": ""Length"",
      ""siUnit"": {
        ""name"": ""Meters"",
        ""short"": ""m""
      },
      ""additionalUnits"": [
        {
          ""name"": ""Kilometers"",
          ""short"": ""km"",
          ""factor"": 1000
        },
        {
          ""name"": ""Centimeters"",
          ""short"": ""cm"",
          ""factor"": 0.01
        },
        {
          ""name"": ""Millimeters"",
          ""short"": ""cm"",
          ""factor"": 0.01
        }
      ],
      ""generateExtensions"": true
    },

    {
      ""typeName"": ""Mass"",
      ""siUnit"": {
        ""name"": ""Kilograms"",
        ""short"": ""kg""
      },
      ""additionalUnits"": [
        {
          ""name"": ""Tons"",
          ""short"": ""t"",
          ""factor"": 1000
        },
        {
          ""name"": ""Grams"",
          ""short"": ""g"",
          ""factor"": 0.001
        },
        {
          ""name"": ""Milligrams"",
          ""short"": ""mg"",
          ""factor"": 0.000001
        }
      ],
      ""generateExtensions"": true
    }
    
  ],
  
  ""mulUnits"": [

    {
      ""typeName"": ""Area"",
      ""primary"": ""Length"",
      ""secondary"": ""Length"",
      ""siUnit"": {
        ""name"": ""SquareMeters"",
        ""short"": ""m²""
      },
      ""additionalUnits"": [
        {
          ""name"": ""Hectare"",
          ""short"": ""ha"",
          ""factor"": 10000
        }
      ],
      ""generateExtensions"": true
    },

    {
      ""typeName"": ""Volume"",
      ""primary"": ""Area"",
      ""secondary"": ""Length"",
      ""siUnit"": {
        ""name"": ""CubicMeters"",
        ""short"": ""m³""
      },
      ""additionalUnits"": [
        {
          ""name"": ""Liters"",
          ""short"": ""l"",
          ""factor"": 0.001
        }
      ],
      ""generateExtensions"": true
    }
    
  ],
  
  ""divUnits"": [

    {
      ""typeName"": ""AnglePerDuration"",
      ""primary"": ""Angle"",
      ""secondary"": ""Duration"",
      ""siUnit"": {
        ""name"": ""RadiansPerSecond"",
        ""short"": ""rad/s""
      },
      ""additionalUnits"": [
        {
          ""name"": ""DegreesPerSecond"",
          ""short"": ""°/s"",
          ""factor"": 57.2958
        }, 
        {
          ""name"": ""DegreesPerMinute"",
          ""short"": ""°/min"",
          ""factor"": 0.95493
        }
      ],
      ""generateExtensions"": true
    },
    
    {
      ""typeName"": ""Velocity"",
      ""primary"": ""Length"",
      ""secondary"": ""Duration"",
      ""siUnit"": {
        ""name"": ""MetersPerSecond"",
        ""short"": ""m/s""
      },
      ""additionalUnits"": [
        {
          ""name"": ""KilometersPerHour"",
          ""short"": ""km/h"",
          ""factor"": 0.27777777777
        }
      ],
      ""generateExtensions"": true
    },

    {
      ""typeName"": ""MassPerLength"",
      ""primary"": ""Mass"",
      ""secondary"": ""Length"",
      ""siUnit"": {
        ""name"": ""KilogramsPerMeter"",
        ""short"": ""kg/m""
      },
      ""additionalUnits"": [],
      ""generateExtensions"": true
    },

    {
      ""typeName"": ""VolumePerDuration"",
      ""primary"": ""Volume"",
      ""secondary"": ""Duration"",
      ""siUnit"": {
        ""name"": ""CubicMetersPerSecond"",
        ""short"": ""m³/s""
      },
      ""additionalUnits"": [],
      ""generateExtensions"": true
    }
    
  ]
}";
}