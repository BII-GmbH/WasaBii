using System.Text;

namespace BII.WasaBii.UnitSystem; 

public static class UnitCodeGeneration {
    
    private static string GenerateUnit(IUnitDef unit, UnitConversions conversions, string unitBase, string extensions) {
        var name = unit.TypeName;
        return $@"#region {name}

public readonly partial struct {name} : IUnitValue<{name}, {name}.Unit> {{
    public double SiValue {{ init; get; }}
    public Type UnitType => typeof(Unit);
    
    public {name}(double value, Unit unit) => SiValue = value * unit.SiFactor;

    public static {name} Zero => new {name}{{SiValue = 0}};
    public static {name} Epsilon => new {name}{{SiValue = double.Epsilon}};

    public static Unit SiUnit => Unit.{unit.SiUnit.Name}.Instance;

    [UnitMetadata(typeof(Description))]
    public abstract class Unit : IUnit.{unitBase} {{
        public abstract string LongName {{ get; }}
        public abstract string ShortName {{ get; }}
        public abstract double SiFactor {{ get; }}

        private Unit() {{}}

        public sealed class Description : IUnitDescription<Unit> {{
            public Unit SiUnit => {name}.SiUnit;
            public IReadOnlyList<Unit> AllUnits => new Unit[] {{
                {unit.SiUnit.Name}.Instance,
{string.Join(",\n                ", unit.AdditionalUnits.Select(u => u.Name + ".Instance"))}
            }};
        }}

        public sealed class {unit.SiUnit.Name} : Unit {{
            public override string LongName => ""{unit.SiUnit.Name}"";
            public override string ShortName => ""{unit.SiUnit.Short}"";
            public override double SiFactor => 1d;
            
            private {unit.SiUnit.Name}() : base() {{}}

            public static readonly {unit.SiUnit.Name} Instance = new();
        }}

{string.Join("\n\n", unit.AdditionalUnits.Select(au => $@"
        public sealed class {au.Name} : Unit {{
            public override string LongName => ""{au.Name}"";
            public override string ShortName => ""{au.Short}"";
            public override double SiFactor => {au.Factor};
            
            private {au.Name}() : base() {{}}

            public static readonly {au.Name} Instance = new();
        }}"))}

    }}

    public int CompareTo({name} other) => this.SiValue.CompareTo(other.SiValue);
    public bool Equals({name} other) => this.SiValue.Equals(other.SiValue);

    public static bool operator ==({name} left, {name} right) => Equals(left, right);
    public static bool operator !=({name} left, {name} right) => !Equals(left, right);

    public static bool operator >({name} left, {name} right) => left.SiValue > right.SiValue;
    public static bool operator <({name} left, {name} right) => left.SiValue < right.SiValue;

    public static bool operator >=({name} left, {name} right) => left.SiValue >= right.SiValue;
    public static bool operator <=({name} left, {name} right) => left.SiValue <= right.SiValue;

{GenerateConversionsFor(unit, conversions.For(unit))}

    public override bool Equals(object obj) => obj is {name} other && Equals(other);

    // We include this type in case values of different units are hashed in the same collection
    public override int GetHashCode() => HashCode.Combine(this.SiValue, typeof({name}));
    
}}{extensions}

#endregion
";
    }

#region Base Units

    public static string GenerateBaseUnit(BaseUnitDef unit, UnitConversions conversions) => 
        GenerateUnit(unit, conversions, unitBase: "Base", extensions: $"{GenerateToDoubleExtensions(unit)}{GenerateConstructionExtensions(unit)}");

    private static string GenerateToDoubleExtensions(BaseUnitDef unit) {
        if (!unit.GenerateExtensions) return "";
        var name = unit.TypeName;
        return $@"

public static class {name}ToDoubleExtensions {{
    // Multiplies by 1/factor, precalculated during code generation because multiplication is faster
    public static double As{unit.SiUnit.Name}<T>(this T value) where T : IUnitValueOf<{name}.Unit> => value.SiValue;
{string.Join("\n", unit.AdditionalUnits.Select(au =>
    $@"    public static double As{au.Name}<T>(this T value) where T : IUnitValueOf<{name}.Unit> => value.SiValue * {1d/au.Factor};"))}  
}}
";
    }

    private static string GenerateConstructionExtensions(BaseUnitDef unit) {
        if (!unit.GenerateExtensions) return "";
        var name = unit.TypeName;
        return $@"

public static class {name}ConstructionExtensions {{
    public static {name} {unit.SiUnit.Name}(this double value) => new(value, {name}.Unit.{unit.SiUnit.Name}.Instance);
{string.Join("\n", unit.AdditionalUnits.Select(au => 
    $@"    public static {name} {au.Name}(this double value) => new(value, {name}.Unit.{au.Name}.Instance);"))}
    public static {name} {unit.SiUnit.Name}(this float value) => new(value, {name}.Unit.{unit.SiUnit.Name}.Instance);
{string.Join("\n", unit.AdditionalUnits.Select(au => 
    $@"    public static {name} {au.Name}(this float value) => new(value, {name}.Unit.{au.Name}.Instance);"))}
}}
";
    }
    
#endregion

#region Derived Units

    public static string GenerateDerivedUnit(DerivedUnitDef unit, UnitConversions conversions, bool isMul) => 
        GenerateUnit(unit, conversions,
            unitBase: $"{(isMul ? "Mul" : "Div")}<{unit.Primary}.Unit, {unit.Secondary}.Unit>",
            extensions: $"{GenerateDerivedToDoubleExtensions(unit)}{GenerateDerivedConstructionExtensions(unit)}"
        );

    private static string GenerateDerivedToDoubleExtensions(DerivedUnitDef unit) {
        if (!unit.GenerateExtensions) return "";
        var name = unit.TypeName;
        return $@"

    public static class {name}ToDoubleExtensions {{
        // Multiplies by 1/factor, precalculated during code generation because multiplication is faster
        public static double As{unit.SiUnit.Name}<T>(this T value) where T : IUnitValueOf<{name}.Unit> => value.SiValue;
    {string.Join("\n", unit.AdditionalUnits.Select(au =>
        $@"    public static double As{au.Name}<T>(this T value) where T : IUnitValueOf<{name}.Unit> => value.SiValue * {1d/au.Factor};"))}  
    }}
    ";
    }

    private static string GenerateDerivedConstructionExtensions(DerivedUnitDef unit) {
        if (!unit.GenerateExtensions) return "";
        var name = unit.TypeName;
        return $@"

    public static class {name}ConstructionExtensions {{
        public static {name} {unit.SiUnit.Name}(this double value) => new(value, {name}.Unit.{unit.SiUnit.Name}.Instance);
    {string.Join("\n", unit.AdditionalUnits.Select(au => 
        $@"    public static {name} {au.Name}(this double value) => new(value, {name}.Unit.{au.Name}.Instance);"))}
        public static {name} {unit.SiUnit.Name}(this float value) => new(value, {name}.Unit.{unit.SiUnit.Name}.Instance);
    {string.Join("\n", unit.AdditionalUnits.Select(au => 
        $@"    public static {name} {au.Name}(this float value) => new(value, {name}.Unit.{au.Name}.Instance);"))}
    }}
    ";
    }
    
#endregion

#region Conversion Operators

    // Plus and Minus on the same units
    // Mul and Div on composed units

    public static string GenerateConversionsFor(IUnitDef unit, IReadOnlyCollection<UnitConversion> conversions) {
        var res = new StringBuilder();
        
        // Trivial operators that any unit supports

        string GenerateCommonOps() {
            var name = unit.TypeName;
            return $@"    public static {name} operator +({name} self) => self;
    public static {name} operator -({name} self) => new {name}{{SiValue = -self.SiValue}};

    public static {name} operator +({name} first, {name} second) => new {name}{{SiValue = first.SiValue + second.SiValue}};
    public static {name} operator -({name} first, {name} second) => new {name}{{SiValue = first.SiValue - second.SiValue}};

    public static {name} operator *({name} a, double s) => new {name}{{SiValue = a.SiValue * s}};
    public static {name} operator *({name} a, float s) => new {name}{{SiValue = a.SiValue * s}};

    public static {name} operator *(double s, {name} a) => new {name}{{SiValue = a.SiValue * s}};
    public static {name} operator *(float s, {name} a) => new {name}{{SiValue = a.SiValue * s}};

    public static {name} operator /({name} a, double s) => new {name}{{SiValue = a.SiValue / s}};
    public static {name} operator /({name} a, float s) => new {name}{{SiValue = a.SiValue / s}};

    public static double operator /({name} a, {name} b) => a.SiValue / b.SiValue;

    public static {name} operator %({name} left, {name} right) => new {name}{{SiValue = left.SiValue % right.SiValue}};
";
        }

        res.Append(GenerateCommonOps());
        
        // Multiplication and Division

        var a = unit.TypeName;
        
        string GenerateMul(string b, string c) => $@"
    public static {c} operator *({a} a, {b} b) => 
        new {c}{{SiValue = a.SiValue * b.SiValue}};
";

        string GenerateDiv(string b, string c) => $@"
    public static {c} operator /({a} a, {b} b) => 
        new {c}{{SiValue = a.SiValue / b.SiValue}};
";

        foreach (var c in conversions) 
            res.Append(c.IsMul ? GenerateMul(c.B, c.C) : GenerateDiv(c.B, c.C));

        return res.ToString();
    }

#endregion
}