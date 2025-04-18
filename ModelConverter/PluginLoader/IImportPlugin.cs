namespace ModelConverter.PluginLoader
{
    using ModelConverter.Geometry;

    /// <summary>
    /// Import plugin
    /// </summary>
    public interface IImportPlugin
    {
        /// <summary>
        /// Import model from file
        /// </summary>
        /// <param name="inputFile">Input file path</param>
        /// <returns>Imported <see cref="Model"/></returns>
        Group? Import(string inputFile);
    }
}