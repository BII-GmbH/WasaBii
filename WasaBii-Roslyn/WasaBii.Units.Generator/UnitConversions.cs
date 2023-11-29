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
            AddIdentifier(
                baseUnit.TypeName,
                new UnitIdentifier(new BaseUnitFactor(ImmutableSortedDictionary<string, int>.Empty.Add(baseUnit.TypeName, 1)))
            );
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
            foreach (var x in nameToIdentifier) {
                var a = x.Key;
                var ai = x.Value;
                foreach (var y in nameToIdentifier) {
                    var b = y.Key;
                    var bi = y.Value;
                    if (identifierToName.TryGetValue(ai * bi, out var c1))
                        yield return new UnitConversion(a, b, c1, IsMul: true);
                    if (identifierToName.TryGetValue(ai / bi, out var c2))
                        yield return new UnitConversion(a, b, c2, IsMul: false);
                }
            }
            
        }

        return new UnitConversions(FindPossibleConversions());
    }
    
    
    
    // The idea: Any unit in an SI-unit-like system can be identified by a mapping of
    //           base units to their powers. Negative powers indicate divisions.
    //           Empty entries are equal to the power of 0, or the factor 1.
    // With these identifiers, we can then "simulate" operations for any two units, and
    //  check whether that multiplication or addition would result in another existing unit.

    private record UnitIdentifier(BaseUnitFactor Factor) {

        public static UnitIdentifier operator *(UnitIdentifier a, UnitIdentifier b) => 
            new (a.Factor + b.Factor); // multiplication adds exponents

        public static UnitIdentifier operator /(UnitIdentifier a, UnitIdentifier b) =>
            new(a.Factor - b.Factor); // division subtracts exponents
    }
    
    private class BaseUnitFactor {
        // Every unit can occur multiple times, e.g. Area = Length * Length.
        // => the value of the dict is the number of occurrences of that unit.
        // Sorted, so that GetHashCode can properly compare two dictionaries by content.
        private readonly ImmutableSortedDictionary<string, int> baseUnitPowers;

        public BaseUnitFactor(ImmutableSortedDictionary<string, int> baseUnitPowers) => this.baseUnitPowers = baseUnitPowers;

        public override bool Equals(object obj) => obj is BaseUnitFactor other && Equals(other);
        public bool Equals(BaseUnitFactor other) {
            if (baseUnitPowers.Count != other.baseUnitPowers.Count) return false;
            foreach (var entry in baseUnitPowers) {
                if (!other.baseUnitPowers.TryGetValue(entry.Key, out var i)) return false;
                if (i != entry.Value) return false;
            }
            return true;
        }
        
        public override int GetHashCode() {
            const int prime = 31;
            var hash = 1;
            foreach (var entry in baseUnitPowers) {
                hash = hash * prime + entry.Key.GetHashCode();
                hash = hash * prime + entry.Value.GetHashCode();
            }
            return hash;
        }

        public static readonly BaseUnitFactor Empty = new(ImmutableSortedDictionary<string, int>.Empty);

        public static BaseUnitFactor operator +(BaseUnitFactor a, BaseUnitFactor b) =>
            new(b.baseUnitPowers.Aggregate(a.baseUnitPowers, (acc, c) => {
                if (acc.TryGetValue(c.Key, out var n)) {
                    var i = n + c.Value;
                    if (i == 0) return acc.Remove(c.Key);
                    else return acc.SetItem(c.Key, i);
                }
                else return acc.Add(c.Key, c.Value);
            }));
        
        public static BaseUnitFactor operator -(BaseUnitFactor a, BaseUnitFactor b) =>
            new(b.baseUnitPowers.Aggregate(a.baseUnitPowers, (acc, c) => {
                if (acc.TryGetValue(c.Key, out var n)) {
                    var i = n - c.Value;
                    if (i == 0) return acc.Remove(c.Key);
                    else return acc.SetItem(c.Key, i);
                }
                else return acc.Add(c.Key, -c.Value);
            }));

        public override string ToString() => string.Join(", ", baseUnitPowers.Select(c => $"{c.Key}:{c.Value}"));
    }
    
    private record DerivedUnitNode(string Name, string Primary, string Secondary, bool IsMul, HashSet<DerivedUnitNode> NeededToCompute);
}