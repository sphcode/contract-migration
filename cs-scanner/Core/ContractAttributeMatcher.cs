using System;
using Microsoft.CodeAnalysis;

namespace ContractScanner.Core;

internal static class ContractAttributeMatcher
{
    private const string ServiceContractAttribute = "global::System.ServiceModel.ServiceContractAttribute";
    private const string DataContractAttribute = "global::System.Runtime.Serialization.DataContractAttribute";

    public static (string Type, string Name)? GetMatch(INamedTypeSymbol typeSymbol)
    {
        foreach (var attribute in typeSymbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (attributeName is null)
            {
                continue;
            }

            if (string.Equals(attributeName, ServiceContractAttribute, StringComparison.Ordinal))
            {
                return ("ServiceContract", typeSymbol.Name);
            }

            if (string.Equals(attributeName, DataContractAttribute, StringComparison.Ordinal))
            {
                return ("DataContract", typeSymbol.Name);
            }
        }

        return null;
    }
}
