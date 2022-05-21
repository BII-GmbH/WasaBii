using System.Collections.Immutable;

namespace BII.WasaBii.UnitSystem; 

// TODO: much more helpful error handling

public record UnitConversion(string A, string B, string C, bool IsMul);

public sealed class UnitConversions {
    private readonly IReadOnlyDictionary<string, IReadOnlyCollection<UnitConversion>> conversions;

    public IReadOnlyCollection<UnitConversion> For(IUnitDef unit) =>
        conversions.TryGetValue(unit.TypeName, out var res) ? res : new List<UnitConversion>();

    public UnitConversions(IEnumerable<UnitConversion> conversions) => 
        this.conversions = conversions.GroupBy(c => c.A)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<UnitConversion>) g.ToList());
    
    
    public static UnitConversions AllConversionsFor(UnitDefinitions defs) {
        var nameToIdentifier = new Dictionary<string, UnitIdentifier>();
        var identifierToName = new Dictionary<UnitIdentifier, string>();

        void AddIdentifier(string name, UnitIdentifier identifier) {
            nameToIdentifier.Add(name, identifier);
            identifierToName.Add(identifier, name);
        }
        
        // step 1: register all base units
        foreach (var baseUnit in defs.BaseUnits) {
            AddIdentifier(baseUnit.TypeName,
                new UnitIdentifier(new BaseUnitFactor(ImmutableSortedSet.Create(baseUnit.TypeName)),
                    BaseUnitFactor.Empty));
        }

        // step 2: register all derived units
        // units may be out of order, so we construct a graph and calculate step by step
        
        var nameToNode = new Dictionary<string, DerivedUnitNode>(); // remove entry once unit model done (for validation)
        var canMakeModelQueue = new Queue<DerivedUnitNode>();
        
        // first pass: discover all nodes

        foreach (var (derived, isMul) in defs.MulUnits.Select(u => (u, true)).Concat(defs.DivUnits.Select(u => (u, false)))) {
            var node = new DerivedUnitNode(derived.TypeName, derived.Primary, derived.Secondary, isMul, new HashSet<DerivedUnitNode>());
            nameToNode.Add(derived.TypeName, node);
            if (nameToIdentifier.ContainsKey(node.req1) && nameToIdentifier.ContainsKey(node.req2)) {
                canMakeModelQueue.Enqueue(node);
            }
        }

        // second pass: populate `neededToCompute`
        
        foreach (var node in nameToNode.Values) {
            if (nameToNode.TryGetValue(node.req1, out var rn1)) rn1.neededToCompute.Add(node);
            if (nameToNode.TryGetValue(node.req2, out var rn2)) rn2.neededToCompute.Add(node);
        }
        
        // step 3: dequeue and make model, then enqueue all in `neededToCompute` when their dependencies are done
        
        while (canMakeModelQueue.Any()) {
            var curr = canMakeModelQueue.Dequeue();
            var a = nameToIdentifier[curr.req1];
            var b = nameToIdentifier[curr.req2];

            if (curr.isMul) AddIdentifier(curr.name, a * b);
            else AddIdentifier(curr.name, a / b);

            nameToNode.Remove(curr.name);
            
            foreach (var candidate in curr.neededToCompute) 
                if (nameToIdentifier.ContainsKey(candidate.req1) && nameToIdentifier.ContainsKey(candidate.req2)) 
                    canMakeModelQueue.Enqueue(candidate);
        }

        if (nameToNode.Any()) throw new Exception(
            $"Could not find dependent units for derived units: [{string.Join(", ", nameToNode.Keys)}]. " +
            "Check your unit definition file. Do their dependencies exist?"
        );
        
        IEnumerable<UnitConversion> FindPossibleConversions() {
            // Note CR: We intentionally compare a to b, and then b to a.
            //  This way all we need to do is generate the operator `A op B = C` 
            //   in A, and symmetric operators are yielded as additional conversions.
            foreach (var (a, ai) in nameToIdentifier)
            foreach (var (b, bi) in nameToIdentifier) {
                if (identifierToName.TryGetValue(ai * bi, out var c1))
                    yield return new UnitConversion(a, b, c1, true);
                if (identifierToName.TryGetValue(ai / bi, out var c2))
                    yield return new UnitConversion(a, b, c2, true);
            }
        }

        return new UnitConversions(FindPossibleConversions());
    }
    
    
    
    // The idea: Any unit in an SI-unit-like system can be identified by a division
    //           of two factors of base units. And any set of divisions and multiplications
    //           can be written as a division of two sets of factors. When a set is empty,
    //           then the factor is equal to 1. Therefore a base unit has only itself
    //           in the numerator set, and nothing in the denominator set.
    // With these identifiers, we can then "simulate" operations for any two units, and
    //  check whether that multiplication or addition would result in another existing unit.

    private record UnitIdentifier(BaseUnitFactor Numerator, BaseUnitFactor Denominator) {
        public static UnitIdentifier operator *(UnitIdentifier a, UnitIdentifier b) =>
            new UnitIdentifier(a.Numerator + b.Numerator, a.Denominator + b.Denominator);

        public static UnitIdentifier operator /(UnitIdentifier a, UnitIdentifier b) =>
            new UnitIdentifier(a.Numerator + b.Denominator, a.Denominator + b.Numerator);
    }
    
    private class BaseUnitFactor {
        public readonly ImmutableSortedSet<string> BaseUnits;

        public BaseUnitFactor(ImmutableSortedSet<string> baseUnits) => BaseUnits = baseUnits;

        public override bool Equals(object obj) => obj is BaseUnitFactor other && Equals(other);
        public bool Equals(BaseUnitFactor other) {
            if (BaseUnits.Count != other.BaseUnits.Count) return false;
        
            using var first = BaseUnits.GetEnumerator();
            using var second = other.BaseUnits.GetEnumerator();
        
            while (first.MoveNext()) {
                if (!second.MoveNext()) return false;
                if (!Equals(first.Current, second.Current)) return false;
            }

            if (second.MoveNext()) return false;
            return true;
        } 
    
        public override int GetHashCode() => BaseUnits.GetHashCode();

        public static BaseUnitFactor Empty = new(ImmutableSortedSet<string>.Empty);

        public static BaseUnitFactor operator +(BaseUnitFactor a, BaseUnitFactor b) =>
            new(b.BaseUnits.Aggregate(a.BaseUnits, (acc, c) => acc.Add(c)));
    }
    
    private record DerivedUnitNode(string name, string req1, string req2, bool isMul, HashSet<DerivedUnitNode> neededToCompute);
}