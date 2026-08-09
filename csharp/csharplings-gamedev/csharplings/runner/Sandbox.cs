using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Csharplings.Runner;

public sealed record RunResult(bool Ok, string Output);

public static class Paths
{
    public static string Root { get; } = FindRoot();

    public static string Exercises => Path.Combine(Root, "exercises");
    public static string Solutions => Path.Combine(Root, "solutions");
    public static string Support => Path.Combine(Root, "support");
    public static string SandboxDir => Path.Combine(Root, ".sandbox");
    public static string SandboxAssembly => Path.Combine(SandboxDir, "sandbox.dll");
    public static string SandboxRuntimeConfig => Path.Combine(SandboxDir, "sandbox.runtimeconfig.json");

    public static string ExerciseFile(Exercise exercise) =>
        Path.Combine(Exercises, exercise.Section, exercise.ClassName + ".cs");

    public static string SolutionFile(Exercise exercise) =>
        Path.Combine(Solutions, exercise.Section, exercise.ClassName + ".cs");

    public static string Display(string absolutePath) =>
        Path.GetRelativePath(Root, absolutePath);

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "exercises"))
                && Directory.Exists(Path.Combine(directory.FullName, "support")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("dossier csharplings introuvable");
    }
}

public static class Sandbox
{
    private const int TimeoutMilliseconds = 15_000;
    private const int MaxDiagnosticsShown = 8;
    private const string Indent = "      ";

    private const string ImplicitUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Threading;
        global using System.Threading.Tasks;
        """;

    private static readonly string[] SilencedWarnings =
        { "CS0219", "CS0414", "CS0162", "CS0168", "CS1998" };

    private static readonly CSharpParseOptions ParseOptions =
        CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

    private static readonly CSharpCompilationOptions CompilationOptions =
        new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                mainTypeName: "SandboxEntry",
                optimizationLevel: OptimizationLevel.Release,
                nullableContextOptions: NullableContextOptions.Disable,
                allowUnsafe: true)
            .WithSpecificDiagnosticOptions(
                SilencedWarnings.ToDictionary(id => id, _ => ReportDiagnostic.Suppress));

    private static readonly string RuntimeConfig = $$"""
        {
          "runtimeOptions": {
            "tfm": "net{{Environment.Version.Major}}.{{Environment.Version.Minor}}",
            "framework": {
              "name": "Microsoft.NETCore.App",
              "version": "{{Environment.Version.Major}}.{{Environment.Version.Minor}}.0"
            },
            "configProperties": {
              "System.Globalization.Invariant": true
            }
          }
        }
        """;

    private static readonly Lazy<MetadataReference[]> FrameworkReferences = new(LoadFrameworkReferences);

    private static readonly Lazy<SyntaxTree[]> SharedTrees = new(LoadSharedTrees);

    public static RunResult Run(Exercise exercise, string sourceFile)
    {
        PrepareSandboxDirectory();

        EmitResult emit;

        using (var assembly = new FileStream(Paths.SandboxAssembly, FileMode.Create, FileAccess.Write, FileShare.None))
            emit = Compile(exercise, sourceFile).Emit(assembly);

        string diagnostics = Describe(emit.Diagnostics);

        if (!emit.Success)
            return new RunResult(false, diagnostics);

        (int exitCode, string output) = Execute();

        return new RunResult(exitCode == 0, Join(diagnostics, output));
    }

    private static CSharpCompilation Compile(Exercise exercise, string sourceFile)
    {
        SyntaxTree[] shared = SharedTrees.Value;
        var trees = new SyntaxTree[shared.Length + 2];

        Array.Copy(shared, trees, shared.Length);
        trees[shared.Length] = Parse(File.ReadAllText(sourceFile), Paths.Display(sourceFile));
        trees[shared.Length + 1] = Parse(BuildEntry(exercise.ClassName), "Entry.cs");

        return CSharpCompilation.Create("sandbox", trees, FrameworkReferences.Value, CompilationOptions);
    }

    private static SyntaxTree Parse(string source, string path) =>
        CSharpSyntaxTree.ParseText(source, ParseOptions, path);

    private static SyntaxTree[] LoadSharedTrees()
    {
        string[] files = Directory.GetFiles(Paths.Support, "*.cs");
        Array.Sort(files, StringComparer.Ordinal);

        var trees = new SyntaxTree[files.Length + 1];
        trees[0] = Parse(ImplicitUsings, "ImplicitUsings.cs");

        for (int i = 0; i < files.Length; i++)
            trees[i + 1] = Parse(File.ReadAllText(files[i]), Paths.Display(files[i]));

        return trees;
    }

    private static MetadataReference[] LoadFrameworkReferences()
    {
        string frameworkDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
        string platformAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        string[] candidates = platformAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        var references = new List<MetadataReference>(candidates.Length);

        foreach (string candidate in candidates)
        {
            if (string.Equals(Path.GetDirectoryName(candidate), frameworkDirectory, StringComparison.Ordinal))
                references.Add(MetadataReference.CreateFromFile(candidate));
        }

        return references.ToArray();
    }

    private static void PrepareSandboxDirectory()
    {
        Directory.CreateDirectory(Paths.SandboxDir);

        if (!File.Exists(Paths.SandboxRuntimeConfig)
            || File.ReadAllText(Paths.SandboxRuntimeConfig) != RuntimeConfig)
            File.WriteAllText(Paths.SandboxRuntimeConfig, RuntimeConfig);
    }

    private static string BuildEntry(string className) => $$"""
        using Csharplings;

        public static class SandboxEntry
        {
            public static int Main()
            {
                try
                {
                    {{className}}.Run();
                }
                catch (CheckFailedException failure)
                {
                    Console.WriteLine("      RATE  " + failure.Message);
                    return 1;
                }
                catch (Exception crash)
                {
                    Console.WriteLine("      CRASH " + crash.GetType().Name + " : " + crash.Message);
                    return 1;
                }

                return 0;
            }
        }
        """;

    private static (int ExitCode, string Output) Execute()
    {
        var info = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Paths.SandboxDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        info.ArgumentList.Add(Paths.SandboxAssembly);

        using Process process = Process.Start(info);

        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();

        if (process.WaitForExit(TimeoutMilliseconds))
            return (process.ExitCode, Join(stdout.Result, stderr.Result));

        process.Kill(entireProcessTree: true);
        process.WaitForExit();

        return (1, Join(
            stdout.Result,
            stderr.Result,
            $"{Indent}BLOQUE toujours en train de tourner apres {TimeoutMilliseconds / 1000} secondes : boucle infinie ?"));
    }

    private static string Describe(IEnumerable<Diagnostic> diagnostics)
    {
        List<Diagnostic> reported = diagnostics
            .Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
            .OrderByDescending(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Location.SourceTree?.FilePath, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .ToList();

        if (reported.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();

        foreach (Diagnostic diagnostic in reported.Take(MaxDiagnosticsShown))
            builder.Append(Indent).AppendLine(diagnostic.ToString());

        if (reported.Count > MaxDiagnosticsShown)
            builder.Append(Indent)
                .Append("... et ")
                .Append(reported.Count - MaxDiagnosticsShown)
                .AppendLine(" de plus : corrige les premieres d'abord, elles entrainent souvent les autres");

        return builder.ToString().TrimEnd();
    }

    private static string Join(params string[] parts)
    {
        var builder = new StringBuilder();

        foreach (string part in parts)
        {
            if (part.Length == 0)
                continue;

            if (builder.Length > 0)
                builder.AppendLine();

            builder.Append(part.TrimEnd());
        }

        return builder.ToString();
    }
}
