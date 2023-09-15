namespace ModelConverter.ParameterParser
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Command line parser
    /// </summary>
    /// <typeparam name="ViewModel">View model to use as a base</typeparam>
    internal static class Parser<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] ViewModel> where ViewModel : new()
    {
        /// <summary>
        /// First character of each argument
        /// </summary>
        private const char ArgumentLeadIn = '-';

        /// <summary>
        /// Parse arguments into a <see cref="ViewModel"/> class
        /// </summary>
        /// <param name="arguments">Collection of arguments to parse</param>
        /// <returns></returns>
        /// <exception cref="MissingMethodException">Thrown when property with argument attribute is not writable</exception>
        /// <exception cref="NotSupportedException">Thrown when property values could not be converted</exception>
        /// <exception cref="KeyNotFoundException">Thrown when required argument is not found or argument does not have any values when it should have</exception>
        public static ViewModel Parse(string[] arguments)
        {
            Dictionary<string, string[]> foundArguments = Parser<ViewModel>.GetArguments(arguments);

            // Get view properties
            PropertyInfo[] properties = typeof(ViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Create view instance
            ViewModel model = Activator.CreateInstance<ViewModel>();

            // Get all properties
            foreach (PropertyInfo property in properties)
            {
                CmdArgumentAttribute? argument = property.GetCustomAttribute<CmdArgumentAttribute>();

                // Validate that we want to set properties only with our special attribute
                if (argument != null)
                {
                    if (!property.CanWrite || property.GetSetMethod() == null)
                    {
                        throw new MissingMethodException(string.Format("Property '{0}' does not have set method.", property.Name));
                    }

                    // Check whether argument we want exists and whether it is required
                    if (foundArguments.ContainsKey(argument.Name))
                    {
                        CmdConverterAttribute? converter = property.GetCustomAttribute<CmdConverterAttribute>();

                        // Check whether we will use default conversion or not
                        if (converter != null)
                        {
                            property.SetValue(model, converter.Convert(foundArguments[argument.Name]));
                        }
                        else
                        {
                            property.SetValue(model, Parser<ViewModel>.ParseArgument(property, foundArguments[argument.Name]));
                        }
                    }
                    else if (foundArguments.ContainsKey(argument.Alternative))
                    {
                        CmdConverterAttribute? converter = property.GetCustomAttribute<CmdConverterAttribute>();

                        // Check whether we will use default conversion or not
                        if (converter != null)
                        {
                            property.SetValue(model, converter.Convert(foundArguments[argument.Alternative]));
                        }
                        else
                        {
                            property.SetValue(model, Parser<ViewModel>.ParseArgument(property, foundArguments[argument.Alternative]));
                        }
                    }
                    else if (argument.IsRequired)
                    {
                        throw new KeyNotFoundException(string.Format("Argument '{0}' on property '{1}' is required.", argument.Name, property.Name));
                    }
                }
            }

            return model;
        }

        /// <summary>
        /// Print help screen for the view model
        /// </summary>
        public static void PrintHelp()
        {
            // Get view properties
            PropertyInfo[] properties = typeof(ViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Print help text
            CmdHelpAttribute? vieModelHelp = typeof(ViewModel).GetCustomAttribute<CmdHelpAttribute>();

            if (vieModelHelp != null)
            {
                Console.WriteLine(vieModelHelp.Text);
            }

            Console.WriteLine();

            foreach (PropertyInfo property in properties)
            {
                CmdArgumentAttribute? argument = property.GetCustomAttribute<CmdArgumentAttribute>();
                CmdHelpAttribute? help = property.GetCustomAttribute<CmdHelpAttribute>();

                // Show only properties that have both argument and help attribute set
                if (argument != null && help != null)
                {
                    if (string.IsNullOrEmpty(argument.Alternative))
                    {
                        Console.Write(string.Format("\t-{0}", argument.Name));
                        Console.CursorLeft = 0;

                        Console.WriteLine(string.Format("\t\t\t\t{0}\n", help.Text));
                    }
                    else
                    {
                        Console.Write(string.Format("\t-{0}, -{1}", argument.Name, argument.Alternative));
                        Console.CursorLeft = 0;

                        Console.WriteLine(string.Format("\t\t\t\t{0}\n", help.Text));
                    }
                }
            }
        }

        /// <summary>
        /// Get all arguments from the arguments array
        /// </summary>
        /// <param name="arguments">Command line arguments</param>
        /// <returns>Dictionary of collected arguments</returns>
        private static Dictionary<string, string[]> GetArguments(string[] arguments)
        {
            Dictionary<string, string[]> collection = new Dictionary<string, string[]>();

            string currentArgument = string.Empty;
            List<string> currentArgumentParameters = new List<string>();
            IEnumerator enumerator = arguments.GetEnumerator();
            string? argument;

            do
            {
                argument = enumerator.MoveNext() ? enumerator.Current as string : null;

                if (argument == null || argument.StartsWith(Parser<ViewModel>.ArgumentLeadIn))
                {
                    if (!string.IsNullOrWhiteSpace(currentArgument))
                    {
                        collection.Add(currentArgument, currentArgumentParameters.ToArray());
                    }

                    if (argument != null)
                    {
                        currentArgument = argument[1..].Trim();
                        currentArgumentParameters = new List<string>();
                    }
                }
                else
                {
                    currentArgumentParameters.Add(argument);
                }
            }
            while (argument != null);

            return collection;
        }

        /// <summary>
        /// Parse argument
        /// </summary>
        /// <param name="property">Target property type</param>
        /// <param name="values">Argument values</param>
        /// <returns>Argument object</returns>
        private static object? ParseArgument(PropertyInfo property, string[] values)
        {
            if (values.Length > 1)
            {
                throw new NotSupportedException(
                    string.Format(
                        "Conversion of multiple values is not supported for property '{0}' of type '{1}'",
                        property.Name,
                        property.PropertyType.Name));
            }

            if (property.PropertyType == typeof(bool) && values.Length > 0)
            {
                throw new NotSupportedException(
                    string.Format(
                        "Property '{0}' of type '{1}' does not support multiple values",
                        property.Name,
                        property.PropertyType.Name));
            }
            else if (property.PropertyType != typeof(bool) && values.Length < 1)
            {
                throw new NotSupportedException(
                    string.Format(
                        "Property '{0}' of type '{1}' expected a value.",
                        property.Name,
                        property.PropertyType.Name));
            }

            if (property.PropertyType == typeof(bool) && values.Length < 1)
            {
                return true;
            }
            else if (property.PropertyType == typeof(string))
            {
                return values[0];
            }
            else if (property.PropertyType.IsEnum)
            {
                object? enumValue = Enum.GetValues(property.PropertyType)
                    .Cast<object>()
                    .FirstOrDefault(value => value.ToString() == values[0]);

                if (enumValue != null)
                {
                    return enumValue;
                }
                else
                {
                    throw new NotSupportedException(
                        string.Format(
                            "Value '{0}' was not found in '{1}' for property '{2}'",
                            values[0],
                            property.PropertyType.Name,
                            property.Name));
                }
            }
            else
            {
                TypeConverter converter = new();
                return converter.ConvertFromInvariantString(values[0]);
            }
        }
    }
}