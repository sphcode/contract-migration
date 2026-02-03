using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ContractScanner.Core;

internal static class DataMemberCollector
{
    private const string DataMemberAttribute = "global::System.Runtime.Serialization.DataMemberAttribute";

    public static string[]? CollectMembers(string contractType, INamedTypeSymbol typeSymbol)
    {
        if (!string.Equals(contractType, "DataContract", StringComparison.Ordinal))
        {
            return null;
        }

        var members = new List<string>();
        foreach (var memberSymbol in typeSymbol.GetMembers())
        {
            if (memberSymbol is IFieldSymbol or IPropertySymbol)
            {
                foreach (var attr in memberSymbol.GetAttributes())
                {
                    var attrName = attr.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    if (attrName is null)
                    {
                        continue;
                    }

                    if (string.Equals(attrName, DataMemberAttribute, StringComparison.Ordinal))
                    {
                        members.Add(memberSymbol.Name);
                        break;
                    }
                }
            }
        }

        return members.Count > 0 ? members.ToArray() : null;
    }
}
