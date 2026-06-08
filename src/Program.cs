using Newtonsoft.Json.Linq;
using ru.tlpteam.Debug;
using ru.tlpteam.tb.Runtime.Engine;
using ru.tlpteam.tb.Runtime.Window;
using System;
using System.CommandLine;
using System.Globalization;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        var projectPathArgument = new Argument<string>("project-path")
        {
            Description = "Path to the project folder"
        };

        var initialSceneOption = new Option<string?>("--initial-scene")
        {
            Description = "Initial scene name to load",
            Required = true
        };

        var rootCommand = new RootCommand("TestboxedEngine Runtime")
        {
            Arguments = { projectPathArgument },
            Options = { initialSceneOption }
        };

        rootCommand.SetAction(parseResult => RunEngine(
            parseResult.GetValue(projectPathArgument)!,
            parseResult.GetValue(initialSceneOption)!
        ));

        return rootCommand.Parse(args).Invoke();
    }

    private static void RunEngine(string projectPath, string initialSceneName)
    {
        TlpLogging.Info("Starting TestboxedEngine Runtime.");

        try
        {
            // Validate the project folder layout.
            if (!Directory.Exists(projectPath))
                throw new DirectoryNotFoundException($"Project folder not found: {projectPath}");

            string configPath = Path.Combine(projectPath, "tlpruntimeconfig.json");
            if (!File.Exists(configPath))
                throw new FileNotFoundException("tlpruntimeconfig.json was not found in the project folder. This is not a Testboxed project.");

            var config = JObject.Parse(File.ReadAllText(configPath));
            string title = config["Name"]?.ToString() ?? "ru.tlpteam.tb";
            uint windowWidth = ReadUInt(config, "WindowWidth", 640);
            uint windowHeight = ReadUInt(config, "WindowHeight", 480);
            uint viewportWidth = ReadUInt(config, "ViewportWidth", windowWidth);
            uint viewportHeight = ReadUInt(config, "ViewportHeight", windowHeight);
            float renderScale = ReadFloat(config, "RenderScale", 1f);

            // Compile scripts from the project folder.
            TlpLogging.Info("Compiling scripts...");
            var loader = new ScriptLoader();
            loader.LoadAndCompile(Path.Combine(projectPath, "Scripts"));
            if (loader.CompiledAssembly == null)
                throw new InvalidOperationException("Failed to compile the project scripts.");

            // Create the window provider.
            var windowProvider = new SFMLWindowProvider(
                windowWidth,
                windowHeight,
                title,
                renderScale,
                viewportWidth,
                viewportHeight);

            // Start the engine with the asset path and window provider.
            var engine = new TestboxedEngine(loader.CompiledAssembly, projectPath, windowProvider);
            TlpLogging.Info("Starting TestboxedEngine...");
            engine.Run(initialSceneName);
        }
        catch (Exception ex)
        {
            TlpLogging.Error($"An error occurred: {ex.Message}");
        }
    }

    private static uint ReadUInt(JObject config, string key, uint fallback)
    {
        if (config[key] == null) return fallback;

        if (!uint.TryParse(config[key]!.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out uint parsed))
            return fallback;

        return parsed == 0 ? fallback : parsed;
    }

    private static float ReadFloat(JObject config, string key, float fallback)
    {
        if (config[key] == null) return fallback;

        if (!float.TryParse(config[key]!.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            return fallback;

        return parsed <= 0f ? fallback : parsed;
    }
}