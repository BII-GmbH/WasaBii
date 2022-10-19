using Microsoft.CodeAnalysis;

namespace WasaBii_Geometry_Generator; 

// Modified copy of https://stackoverflow.com/a/70486194
public static class AttributeSyntaxUtils {
    
    public sealed record NameTypeAndValue(string Name, string TypeFullName, object Value);

    // Converts names like `string` to `System.String`
    private static string GetTypeFullName(this ITypeSymbol typeSymbol) =>
        typeSymbol.SpecialType == SpecialType.None
            ? typeSymbol.ToDisplayString()
            : typeSymbol.SpecialType.ToString().Replace("_", ".");

    public static IEnumerable<NameTypeAndValue> GetArgumentValues(this AttributeData attributeData) {
        if (attributeData.AttributeConstructor is not { } constructor) return Enumerable.Empty<NameTypeAndValue>();
        var constructorParams = constructor.Parameters;

        // Start with an indexed list of names for mandatory args
        var argumentNames = constructorParams.Select(x => x.Name).ToArray();

        return attributeData.ConstructorArguments
            // For unnamed args, we get the name from the array we just made
            .Select((info, index) => (argumentNames[index], info))
            // Then we use name + value from the named values
            .Union(attributeData.NamedArguments.Select(x => (x.Key, x.Value)))
            .Distinct()
            .Select(argument => 
                new NameTypeAndValue(
                    argument.Item1,
                    argument.Item2.Type.GetTypeFullName(),
                    argument.Item2.Value
                )
            );
    }
}