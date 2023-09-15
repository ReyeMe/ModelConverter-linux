namespace ModelConverter
{
    using ModelConverter.Geometry;
    using ModelConverter.ParameterParser;
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Main program class
    /// </summary>
    internal sealed class Program
    {
        /// <summary>
        /// Main program entry
        /// </summary>
        /// <param name="args">Application arguments</param>
        [RequiresUnreferencedCode("Calls ModelConverter.PluginLoader.Loader.GetPlugins()")]
        public static void Main(string[] args)
        {
            ArgumentSettings settings = Parser<ArgumentSettings>.Parse(args);

            // Show help and exit
            if (settings.ShowHelp)
            {
                Parser<ArgumentSettings>.PrintHelp();
                Environment.Exit(0);
            }

            // Load plugins
            Dictionary<string, PluginLoader.Plugin> plugins = PluginLoader.Loader.GetPlugins();

            // Show list of plugins and exit
            if (settings.ShowPlugins)
            {
                Console.WriteLine("Available plugins:");
                Console.WriteLine("EXT\tTYPE\tNAME\t\t\tDESC");

                foreach (PluginLoader.Plugin plugin in plugins.Values)
                {
                    Console.Write(plugin.Extension);
                    Console.CursorLeft = 0;

                    if (plugin.Supports.HasFlag(PluginLoader.Plugin.Support.Import))
                    {
                        Console.Write("\tIn");
                    }
                    else if (plugin.Supports.HasFlag(PluginLoader.Plugin.Support.Export))
                    {
                        Console.Write("\tOut");
                    }
                    else
                    {
                        Console.Write("\tIn/Out");
                    }

                    Console.CursorLeft = 0;

                    Console.Write("\t\t" + plugin.Name);
                    Console.CursorLeft = 0;

                    Console.WriteLine("\t\t\t\t\t" + plugin.Description);
                }

                Environment.Exit(0);
            }

            if (string.IsNullOrEmpty(settings.InputFile) || !File.Exists(settings.InputFile))
            {
                Console.WriteLine("Input file is missing!");
                Environment.Exit(0);
            }

            if (string.IsNullOrEmpty(settings.OuputFile) || !Directory.Exists(Path.GetDirectoryName(settings.OuputFile)))
            {
                Console.WriteLine("Output directory is missing!");
                Environment.Exit(0);
            }

            // Start converting
            Console.WriteLine(string.Format("Input file: {0}", settings.InputFile));
            Console.WriteLine(string.Format("Output file: {0}", settings.OuputFile));

            PluginLoader.Plugin? importPlugin;

            if (string.IsNullOrEmpty(settings.ImportPluginName))
            {
                importPlugin = plugins.Values
                    .Where(plugin => plugin.Supports.HasFlag(PluginLoader.Plugin.Support.Import))
                    .FirstOrDefault(plugin => settings.InputFile.EndsWith(plugin.Extension));

                if (importPlugin == null)
                {
                    Console.WriteLine("No available plugin supports this file extension!");
                    Environment.Exit(0);
                }
            }
            else
            {
                importPlugin = plugins.Values
                    .Where(plugin => plugin.Supports.HasFlag(PluginLoader.Plugin.Support.Import))
                    .FirstOrDefault(plugin => plugin.Name == settings.ImportPluginName);

                if (importPlugin == null)
                {
                    Console.WriteLine("Selected plugin not found!");
                    Environment.Exit(0);
                }
            }

            Console.WriteLine("Using import plugin: " + importPlugin.Name);

            // Import group
            Group? group = importPlugin.ImportFile(settings.InputFile, settings);

            if (group == null)
            {
                Console.WriteLine("Import failed!");
                Environment.Exit(0);
            }

            PluginLoader.Plugin? exportPlugin;

            if (string.IsNullOrEmpty(settings.ExportPluginName))
            {
                exportPlugin = plugins.Values
                    .Where(plugin => plugin.Supports.HasFlag(PluginLoader.Plugin.Support.Export))
                    .FirstOrDefault(plugin => settings.OuputFile.EndsWith(plugin.Extension));

                if (exportPlugin == null)
                {
                    Console.WriteLine("No available plugin supports this file extension!");
                    Environment.Exit(0);
                }
            }
            else
            {
                exportPlugin = plugins.Values
                    .Where(plugin => plugin.Supports.HasFlag(PluginLoader.Plugin.Support.Export))
                    .FirstOrDefault(plugin => plugin.Name == settings.ExportPluginName);

                if (exportPlugin == null)
                {
                    Console.WriteLine("Selected plugin not found!");
                    Environment.Exit(0);
                }
            }

            Console.WriteLine("Using export plugin: " + exportPlugin.Name);

            if (exportPlugin.ExportFile(group, settings.OuputFile, settings))
            {
                Console.WriteLine("Export done.");
            }
            else
            {
                Console.WriteLine("Export failed.");
            }
        }
    }
}