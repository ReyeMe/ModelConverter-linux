﻿namespace ModelConverter
{
    using ModelConverter.Geometry;
    using ModelConverter.ParameterParser;
    using ModelConverter.PluginLoader;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Main program class
    /// </summary>
    internal sealed class Program
    {
        /// <summary>
        /// Application exit codes
        /// </summary>
        internal enum ExitCodes
        {
            /// <summary>
            /// Everything went according to plan
            /// </summary>
            Ok = 0,

            /// <summary>
            /// Missing or malformed input file paths
            /// </summary>
            NoOrBadInput,

            /// <summary>
            /// Missing or malformed output file path
            /// </summary>
            NoOrBadOutput,

            /// <summary>
            /// No plugin supporting this format exists
            /// </summary>
            NotSupportedFormat,

            /// <summary>
            /// Import has ended in a failure
            /// </summary>
            ImportFailed,

            /// <summary>
            /// Export has ended in a failure
            /// </summary>
            ExportFailed,

            /// <summary>
            /// Specified plugin does not exit
            /// </summary>
            PluginNotFound,
        }

        /// <summary>
        /// Exit program
        /// </summary>
        /// <param name="exitCode">Program exit code</param>
        [DoesNotReturn]
        public static void Exit(ExitCodes exitCode)
        {
            if (exitCode != ExitCodes.Ok)
            {
                string code = exitCode.ToString("X");
                string exitCodeName = (new string(exitCode.ToString().SelectMany(x => char.IsUpper(x) ? new[] { ' ', x } : new[] { x }).ToArray())).Trim();
                Console.WriteLine($"An error has occured. Exited with: {exitCodeName} (0x{code})");
            }

            Environment.Exit((int)exitCode);
        }

        /// <summary>
        /// Main program entry
        /// </summary>
        /// <param name="args">Application arguments</param>
        [RequiresUnreferencedCode("Calls ModelConverter.PluginLoader.Loader.GetPlugins()")]
        public static void Main(string[] args)
        {
            ArgumentSettings settings = Parser<ArgumentSettings>.Parse(args);

            // Show help and exit
            if (settings.ShowHelp != null || args.Length == 0)
            {
                if (!string.IsNullOrWhiteSpace(settings.ShowHelp))
                {
                    // Load plugins
                    Dictionary<string, PluginLoader.Plugin> availablePlugins = PluginLoader.Loader.GetPlugins();

                    if (availablePlugins.ContainsKey(settings.ShowHelp))
                    {
                        availablePlugins[settings.ShowHelp].PrintHelp();
                        Program.Exit(ExitCodes.Ok);
                    }
                    else
                    {
                        Console.WriteLine($"Plugin '{settings.ShowHelp}' not found!");
                        Program.Exit(ExitCodes.PluginNotFound);
                    }
                }

                Parser<ArgumentSettings>.PrintHelp();
                Program.Exit(ExitCodes.Ok);
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

                    Console.Write("\t\t\t\t\t" + plugin.Description);
                    Console.CursorLeft = 0;
                    Console.Write("\t\t" + plugin.Name);
                    Console.CursorLeft = 0;
                    Console.Write(plugin.Extension);
                    Console.CursorLeft = 0;
                    Console.WriteLine();
                }

                Program.Exit(ExitCodes.Ok);
            }

            if ((!settings.InputFile?.Any()) ?? true)
            {
                Console.WriteLine("No input files were specified! Use --help or -h for help.");
                Program.Exit(ExitCodes.NoOrBadInput);
            }

            bool allFilesValid = true;

            foreach (var file in settings.InputFile ?? Array.Empty<string>())
            {
                if (string.IsNullOrEmpty(file) || !File.Exists(file))
                {
                    Console.WriteLine("Missing: '{0}' @ '{1}'", Path.GetFileName(file), file);
                    allFilesValid = false;
                }
            }

            if (!allFilesValid)
            {
                Console.WriteLine("One or more of the input files are missing! Use --help or -h for help.");
                Program.Exit(ExitCodes.NoOrBadInput);
            }

            if (string.IsNullOrEmpty(settings.OuputFile) || !Directory.Exists(Path.GetDirectoryName(settings.OuputFile)))
            {
                Console.WriteLine("Output directory is missing! Use --help or -h for help.");
                Program.Exit(ExitCodes.NoOrBadOutput);
            }

            // Start converting
            Console.WriteLine(string.Format("Input files:\n{0}", string.Join(", ", settings.InputFile?.Select(file => Path.GetFileName(file)) ?? new[] { string.Empty })));
            Console.WriteLine(string.Format("Output file: {0}", settings.OuputFile));

            List<(PluginLoader.Plugin, string)>? importPlugins = settings.InputFile?.Select(file =>
            {
                PluginLoader.Plugin? importPlugin = null;

                if (string.IsNullOrEmpty(settings.ImportPluginName))
                {
                    importPlugin = plugins.Values
                        .Where(plugin => plugin.Supports.HasFlag(PluginLoader.Plugin.Support.Import))
                        .FirstOrDefault(plugin => file.EndsWith(plugin.Extension));

                    if (importPlugin == null)
                    {
                        Console.WriteLine("No available plugin supports '." + Path.GetExtension(file) + "' file extension! Use -plugins to see available plugins.");
                        Program.Exit(ExitCodes.NotSupportedFormat);
                    }
                }
                else
                {
                    importPlugin = plugins.Values
                        .Where(plugin => plugin.Supports.HasFlag(PluginLoader.Plugin.Support.Import))
                        .FirstOrDefault(plugin => plugin.Name == settings.ImportPluginName);

                    if (importPlugin == null)
                    {
                        Console.WriteLine("Selected plugin not found! Use -plugins to see available plugins.");
                        Program.Exit(ExitCodes.PluginNotFound);
                    }
                }

                return (importPlugin, file);
            }).ToList();

            Group? group = null;

            if (importPlugins?.Any() ?? false)
            {
                foreach ((PluginLoader.Plugin plugin, string file) import in importPlugins)
                {
                    Console.WriteLine("Using import plugin '" + import.plugin.Name + "' for '" + Path.GetFileName(import.file) + "'");

                    // Import group
                    Group? importedArtifact = import.plugin.ImportFile(import.file, settings, args);

                    if (importedArtifact == null)
                    {
                        Console.WriteLine("Import failed!");
                        Program.Exit(ExitCodes.ImportFailed);
                    }
                    else
                    {
                        if (group == null)
                        {
                            // Set group
                            group = importedArtifact;
                        }
                        else
                        {
                            // Merge group
                            int vertexStart = group.Vertices.Count;
                            group.Vertices.AddRange(importedArtifact.Vertices);

                            int normalsStart = group.Normals.Count;
                            group.Normals.AddRange(importedArtifact.Normals);

                            int uvsStart = group.Uv.Count;
                            group.Uv.AddRange(importedArtifact.Uv);

                            // Merge materials
                            foreach (KeyValuePair<string, Material> material in importedArtifact.MaterialTextures)
                            {
                                if (!group.MaterialTextures.ContainsKey(material.Key))
                                {
                                    group.MaterialTextures.Add(material.Key, material.Value);
                                }
                                else if (!material.Value.Equals(group.MaterialTextures[material.Key]))
                                {
                                    Console.WriteLine("Warning: Different material with same name '" + material.Key + "' already exists! First occurence is used...");
                                }
                            }

                            // Merge models
                            foreach (Model model in importedArtifact)
                            {
                                foreach (Face face in model.Faces)
                                {
                                    int[] vertices = face.Vertices.Select(id => id + vertexStart).ToArray();
                                    face.Vertices.Clear();
                                    face.Vertices.AddRange(vertices);

                                    int[] uv = face.Uv.Select(id => id + uvsStart).ToArray();
                                    face.Uv.Clear();
                                    face.Uv.AddRange(uv);

                                    int[] normals = face.Normals.Select(id => id + normalsStart).ToArray();
                                    face.Normals.Clear();
                                    face.Normals.AddRange(normals);
                                }

                                group.Add(model);
                            }
                        }
                    }
                }

                // Order things
                switch (settings.Order)
                {
                    case ArgumentSettings.ObjOrder.ByName:
                        List<Model> models = group?.OrderBy(model => model.Name).ToList() ?? new List<Model>();
                        group?.Clear();
                        group?.AddRange(models);
                        break;

                    default:
                        break;
                }
            }

            PluginLoader.Plugin? exportPlugin;

            if (string.IsNullOrEmpty(settings.ExportPluginName))
            {
                exportPlugin = plugins.Values
                    .Where(plugin => plugin.Supports.HasFlag(PluginLoader.Plugin.Support.Export))
                    .FirstOrDefault(plugin => settings.OuputFile.EndsWith(plugin.Extension));

                if (exportPlugin == null)
                {
                    Console.WriteLine("No available plugin supports this file extension! Use -plugins to see available plugins.");
                    Program.Exit(ExitCodes.NotSupportedFormat);
                }
            }
            else
            {
                exportPlugin = plugins.Values
                    .Where(plugin => plugin.Supports.HasFlag(PluginLoader.Plugin.Support.Export))
                    .FirstOrDefault(plugin => plugin.Name == settings.ExportPluginName);

                if (exportPlugin == null)
                {
                    Console.WriteLine("Selected plugin not found! Use -plugins to see available plugins.");
                    Program.Exit(ExitCodes.PluginNotFound);
                }
            }

            Console.WriteLine("Using export plugin: " + exportPlugin.Name);

            if (group != null && exportPlugin.ExportFile(group, settings.OuputFile, settings, args))
            {
                Console.WriteLine("Export done.");
                Program.Exit(ExitCodes.Ok);
            }
            else
            {
                Console.WriteLine("Export failed.");
                Program.Exit(ExitCodes.ExportFailed);
            }
        }
    }
}