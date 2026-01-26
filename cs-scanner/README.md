# Contract Scanner (Roslyn)

Minimal C# scanner using Roslyn `MSBuildWorkspace` to find types annotated with:

- `System.ServiceModel.ServiceContractAttribute`
- `System.Runtime.Serialization.DataContractAttribute`

## NuGet packages

- `Microsoft.CodeAnalysis.CSharp.Workspaces`
- `Microsoft.Build.Locator`

## Build & Run

```bash
dotnet run --project Cli/Cli.csproj -- <path/to/solution.sln|project.csproj> <output.jsonl>
```
