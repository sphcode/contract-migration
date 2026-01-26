using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace ContractScanner.Core;

public sealed class ContractScanner
{
    private const string ServiceContractAttribute = "global::System.ServiceModel.ServiceContractAttribute";
    private const string DataContractAttribute = "global::System.Runtime.Serialization.DataContractAttribute";

    private static readonly SymbolDisplayFormat TypeNameFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

    public async Task ScanAsync(
        string solutionOrProjectPath,
        Func<ScanResult, Task> onResult,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionOrProjectPath))
        {
            throw new InvalidDataException("Solution or project path is required.");
        }

        if (onResult is null)
        {
            throw new InvalidDataException("onResult callback is required.");
        }

        using var workspace = MSBuildWorkspace.Create();
        var seen = new HashSet<string>();

        if (solutionOrProjectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            var solution = await workspace.OpenSolutionAsync(
                solutionOrProjectPath,
                progress: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            foreach (var project in solution.Projects)
            {
                await ScanProjectAsync(project, onResult, seen, cancellationToken).ConfigureAwait(false);
            }
        }
        else if (solutionOrProjectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            var project = await workspace.OpenProjectAsync(
                solutionOrProjectPath,
                progress: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            await ScanProjectAsync(project, onResult, seen, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new InvalidDataException("Input path must be a .sln or .csproj file.");
        }
    }

    private static async Task ScanProjectAsync(
        Project project,
        Func<ScanResult, Task> onResult,
        HashSet<string> seen,
        CancellationToken cancellationToken)
    {
        foreach (var document in project.Documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (document.SourceCodeKind != SourceCodeKind.Regular)
            {
                continue;
            }

            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxRoot is null)
            {
                continue;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
            {
                continue;
            }

            foreach (var typeDecl in syntaxRoot.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                var symbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);
                if (symbol is not INamedTypeSymbol namedType)
                {
                    continue;
                }

                var match = GetMatch(namedType);
                if (match is null)
                {
                    continue;
                }

                var name = namedType.ToDisplayString(TypeNameFormat);
                var key = $"{match.Value.Type}|{name}";
                if (!seen.Add(key))
                {
                    continue;
                }

                await onResult(new ScanResult(match.Value.Type, name)).ConfigureAwait(false);
            }
        }
    }

    private static (string Type, string Name)? GetMatch(INamedTypeSymbol typeSymbol)
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

public readonly record struct ScanResult(string Type, string Name);
