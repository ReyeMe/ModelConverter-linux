namespace ModelConverter.PluginLoader
{
    using ModelConverter.Geometry;

    /// <summary>
    /// Export plugin
    /// </summary>
    public interface IExportPlugin
    {
        /// <summary>
        /// Export group as output file
        /// </summary>
        /// <param name="group">Model group instance</param>
        /// <param name="outputFile">Output file path</param>
        /// <returns></returns>
        bool Export(Group model, string outputFile);
    }
}