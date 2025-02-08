namespace ModelConverter.ParameterParser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Wildcard path converter attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal class CmdWildPathConverterAttribute : CmdConverterAttribute
    {
        /// <summary>
        /// Convert set of argument values into a single object
        /// </summary>
        /// <param name="values">Collection of values</param>
        /// <returns>Argument object</returns>
        public override object Convert(string[] values)
        {
            List<string> paths = new List<string>();

            foreach (string path in values.Select(path => Path.GetFullPath(path)))
            {
                if (path.Contains('*'))
                {
                    string[] components = path.Split('\\');
                    paths.AddRange(CmdWildPathConverterAttribute.EvaluatePath(string.Empty, components));
                }
                else
                {
                    paths.Add(path);
                }
            }

            return paths.OrderBy(path => path).ToArray();
        }

        /// <summary>
        /// Evaluate wildcard path
        /// </summary>
        /// <param name="root">Root of the path</param>
        /// <param name="components">Remaining components of the path</param>
        /// <returns>All evaluated paths</returns>
        private static IEnumerable<string> EvaluatePath(string root, string[] components)
        {
            if (components.Any())
            {
                if (components[0].Contains('*'))
                {
                    if (string.IsNullOrWhiteSpace(root))
                    {
                        return Enumerable.Empty<string>();
                    }

                    // Is this a folder or file
                    if (components.Length > 1)
                    {
                        return Directory.EnumerateDirectories(root, components[0])
                            .SelectMany(folder => CmdWildPathConverterAttribute.EvaluatePath(folder, components.Skip(1).ToArray()));
                    }
                    else
                    {
                        return Directory.EnumerateFiles(root, components[0]);
                    }
                }
                else
                {
                    return CmdWildPathConverterAttribute.EvaluatePath(Path.Combine(root, components[0]), components.Skip(1).ToArray());
                }
            }
            else
            {
                return new[] { root };
            }
        }
    }
}
