using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Runtime.Loader;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ru.tlpteam.Debug;

namespace ru.tlpteam.tb.Runtime.Engine
{
    public class ScriptLoader
    {
        public Assembly? CompiledAssembly { get; private set; }

        public void LoadAndCompile(string scriptsPath)
        {
            if (!Directory.Exists(scriptsPath))
            {
                TlpLogging.Warning($"Scripts folder not found: {scriptsPath}");
                return;
            }

            string[] files = Directory.GetFiles(scriptsPath, "*.cs", SearchOption.AllDirectories);

            // Pass each file path to Roslyn so diagnostics include exact source file names.
            var syntaxTrees = files.Select(f =>
                CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f)
            ).ToArray();

            var dotNetDir = Path.GetDirectoryName(typeof(object).Assembly.Location);

            var references = new List<MetadataReference> {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(dotNetDir!, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetDir!, "netstandard.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetDir!, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetDir!, "System.Collections.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetDir!, "System.Console.dll")),
                MetadataReference.CreateFromFile(Path.Combine(dotNetDir!, "System.Linq.dll")),

                // Runtime assembly (contains logging and input APIs).
                MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location),

                // Core API assembly.
                MetadataReference.CreateFromFile(typeof(ru.tlpteam.tb.Core.TestboxedScriptForObject).Assembly.Location),

                // SFML references.
                MetadataReference.CreateFromFile(typeof(SFML.Window.Window).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(SFML.Graphics.Sprite).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(SFML.System.Vector2f).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                "Testboxed.DynamicScripts",
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                foreach (var diagnostic in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                {
                    TlpLogging.Error($"{diagnostic.GetMessage()}");
                }

                throw new Exception("Script compilation failed.");
            }

            ms.Seek(0, SeekOrigin.Begin);
            CompiledAssembly = AssemblyLoadContext.Default.LoadFromStream(ms);
            TlpLogging.Info("Scripts compiled successfully into memory.");
        }
    }
}
