using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CoreScanner = ContractScanner.Core.ContractScanner;
using Microsoft.Build.Locator;

namespace ContractScanner.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: scanner <solutionOrProjectPath> <outputJsonlPath>");
            return 1;
        }

        var inputPath = args[0];
        var outputPath = args[1];

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Input file not found: {inputPath}");
            return 2;
        }

        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? ".");

        await using var stream = new FileStream(
            outputPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        var scanner = new CoreScanner();
        await scanner.ScanAsync(inputPath, async result =>
        {
            var json = JsonSerializer.Serialize(new { type = result.Type, name = result.Name });
            await writer.WriteLineAsync(json).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }).ConfigureAwait(false);

        return 0;
    }
}
