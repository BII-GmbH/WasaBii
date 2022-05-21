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
            if (identifierToName.TryGetValue(identifier, out var existing)) 
                throw new Exception(
                    $"Duplicate unit definitions: {name} is the same as {existing} but with a different name!");
            nameToIdentifier.Add(name, identifier);
            identifierToName.Add(identifier, name);
        }
        
        // step 1: register all base units
        foreach (var baseUnit in defs.BaseUnits) {
            AddIdentifier(baseUnit.TypeName,
                new UnitIdentifier(new BaseUnitFactor(ImmutableSortedDictionary<string, int>.Empty.Add(baseUnit.TypeName, 1)),
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
            if (nameToIdentifier.ContainsKey(node.Primary) && nameToIdentifier.ContainsKey(node.Secondary)) {
                canMakeModelQueue.Enqueue(node);
            }
        }

        // second pass: populate `neededToCompute`
        
        foreach (var node in nameToNode.Values) {
            if (nameToNode.TryGetValue(node.Primary, out var rn1)) rn1.NeededToCompute.Add(node);
            if (nameToNode.TryGetValue(node.Secondary, out var rn2)) rn2.NeededToCompute.Add(node);
        }
        
        // step 3: dequeue and make model, then enqueue all in `neededToCompute` when their dependencies are done
        
        while (canMakeModelQueue.Any()) {
            var curr = canMakeModelQueue.Dequeue();
            var a = nameToIdentifier[curr.Primary];
            var b = nameToIdentifier[curr.Secondary];

            if (curr.IsMul) AddIdentifier(curr.Name, a * b);
            else AddIdentifier(curr.Name, a / b);

            nameToNode.Remove(curr.Name);
            
            foreach (var candidate in curr.NeededToCompute) 
                if (nameToIdentifier.ContainsKey(candidate.Primary) && nameToIdentifier.ContainsKey(candidate.Secondary)) 
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
        // TODO CR PREMERGE: numerator and denominator duplicates should be shortened
        
        public static UnitIdentifier operator *(UnitIdentifier a, UnitIdentifier b) =>
            Minimized(a.Numerator + b.Numerator, a.Denominator + b.Denominator);

        public static UnitIdentifier operator /(UnitIdentifier a, UnitIdentifier b) =>
            Minimized(a.Numerator + b.Denominator, a.Denominator + b.Numerator);

        public static UnitIdentifier Minimized(BaseUnitFactor a, BaseUnitFactor b) {
            var (a2, b2) = BaseUnitFactor.Minimized(a, b);
            return new UnitIdentifier(a2, b2);
        }
    }
    
    private class BaseUnitFactor {
        // Every unit can occur multiple times, e.g. Area = Length * Length.
        // => the value of the dict is the number of occurrences of that unit.
        // Sorted, so that GetHashCode can properly compare two dictionaries by content.
        private readonly ImmutableSortedDictionary<string, int> baseUnits;

        public BaseUnitFactor(ImmutableSortedDictionary<string, int> baseUnits) => this.baseUnits = baseUnits;

        public override bool Equals(object obj) => obj is BaseUnitFactor other && Equals(other);
        public bool Equals(BaseUnitFactor other) {
            if (baseUnits.Count != other.baseUnits.Count) return false;
            foreach (var (au, ai) in baseUnits) {
                if (!other.baseUnits.TryGetValue(au, out var i)) return false;
                if (i != ai) return false;
            }
            return true;
        }
        
        public override int GetHashCode() => baseUnits.Aggregate(19, HashCode.Combine);

        public static readonly BaseUnitFactor Empty = new(ImmutableSortedDictionary<string, int>.Empty);

        public static BaseUnitFactor operator +(BaseUnitFactor a, BaseUnitFactor b) =>
            new(b.baseUnits.Aggregate(a.baseUnits, (acc, c) => {
                if (acc.TryGetValue(c.Key, out var n)) 
                    return acc.SetItem(c.Key, n + c.Value);
                else return acc.Add(c.Key, c.Value);
            }));

        public static (BaseUnitFactor, BaseUnitFactor) Minimized(BaseUnitFactor numerator, BaseUnitFactor denominator) {
            // Goal: appropriately reduce occurrences of units that appear in both numerator and denominator
            var finalNumerator = numerator.baseUnits;
            var finalDenominator = denominator.baseUnits;
            
            // step 1: find all units that both have in common

            foreach (var u in numerator.baseUnits.Keys.Where(denominator.baseUnits.ContainsKey)) {
                var ni = finalNumerator[u];
                var di = finalDenominator[u];
                if (ni < di) {
                    // remove from numerator
                    finalDenominator = finalDenominator.SetItem(u, di - ni);
                    finalNumerator = finalNumerator.Remove(u);
                } 
                else if (ni == di) {
                    // equal, so remove from both
                    finalDenominator = finalDenominator.Remove(u);
                    finalNumerator = finalNumerator.Remove(u);
                } 
                else {
                    // ni > di, so remove from denominator
                    finalNumerator = finalNumerator.SetItem(u, ni - di);
                    finalDenominator = finalDenominator.Remove(u);
                }
            }

            return (new(finalNumerator), new(finalDenominator));
        }
    }
    
    private record DerivedUnitNode(string Name, string Primary, string Secondary, bool IsMul, HashSet<DerivedUnitNode> NeededToCompute);
}