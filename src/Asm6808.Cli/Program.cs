using Asm6808.Core.Assembly;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

if (!args[0].Equals("assemble", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine($"Unknown command: {args[0]}");
    PrintUsage();
    return 1;
}

if (args.Length < 4)
{
    Console.Error.WriteLine("Missing required arguments for 'assemble'.");
    PrintUsage();
    return 1;
}

var inputPath = args[1];
var outputPath = GetOutputPath(args);
if (outputPath is null)
{
    Console.Error.WriteLine("Missing output path. Use: assemble <input.asm> -o <output.s19>");
    return 1;
}

Console.WriteLine($"assemble requested: input='{inputPath}', output='{outputPath}'");
if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input file not found: {inputPath}");
    return 1;
}

try
{
    var assembler = new Assembler6800();
    var records = assembler.AssembleToS19Records(File.ReadLines(inputPath), sourceName: inputPath);
    File.WriteAllLines(outputPath, records);
    Console.WriteLine($"Wrote {records.Count} S-record lines to {outputPath}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Assembly failed: {ex.Message}");
    return 2;
}

static string? GetOutputPath(string[] args)
{
    for (var i = 2; i < args.Length - 1; i++)
    {
        if (args[i].Equals("-o", StringComparison.OrdinalIgnoreCase) ||
            args[i].Equals("--output", StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

static void PrintUsage()
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  Asm6808.Cli assemble <input.asm> -o <output.s19>");
}

