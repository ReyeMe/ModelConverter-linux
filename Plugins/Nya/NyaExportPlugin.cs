namespace Nya
{
    using ModelConverter.Geometry;
    using ModelConverter.PluginLoader;

    /// <summary>
    /// Export plugin for Tank Game
    /// </summary>
    [Plugin("NyaExport", "Export for sega Saturn game \"Utenyaa\" and SRL samples", ".NYA", typeof(NyaArguments))]
    public class NyaExportPlugin : ModelConverter.PluginLoader.IExportPlugin
    {
        /// <summary>
        /// Export settings
        /// </summary>
        private readonly NyaArguments settigns;

        /// <summary>
        /// Initializes a new instance of the <see cref="NyaExportPlugin"/> class
        /// </summary>
        /// <param name="settings">Application settings</param>
        public NyaExportPlugin(NyaArguments settings)
        {
            this.settigns = settings;
        }

        /// <summary>
        /// Export group as output file
        /// </summary>
        /// <param name="group">Model group instance</param>
        /// <param name="outputFile">Output file path</param>
        /// <returns></returns>
        public bool Export(Group group, string outputFile)
        {
            Console.WriteLine($"Exporting type: '{this.settigns.ModelType}'");
            Console.WriteLine($"UV mapping enabled: '{!this.settigns.NoUV}'");

            List<Texture> uvTextures = new List<Texture>();
            List<Texture> textures = new List<Texture>();
            List<Mesh> meshes = new List<Mesh>();

            foreach (KeyValuePair<string, Material> material in group.MaterialTextures
                .OrderByDescending(material => material.Value is TextureReferenceMaterial || material.Value is TextureMaterial))
            {
                if (material.Value is TextureReferenceMaterial refMat)
                {
                    textures.Add(new Texture(material.Key, Helpers.GetBitmap(refMat.TexturePath)));
                }
                else if (material.Value is TextureMaterial texMat && texMat.Data != null)
                {
                    textures.Add(new Texture(material.Key, (ushort)texMat.Width, (ushort)texMat.Height, texMat.Data.Select(value => value.AsAbgr555()).ToArray()));
                }
            }

            if (this.settigns.NoUV)
            {
                uvTextures = textures;
            }

            foreach (Model model in group)
            {
                if (settigns.ModelType == NyaArguments.ModelTypes.Flat)
                {
                    meshes.Add(new Mesh(group, model, textures, !this.settigns.NoUV, ref uvTextures));
                }
                else
                {
                    meshes.Add(new SmoothMesh(group, model, textures, !this.settigns.NoUV, ref uvTextures));
                }
            }

            if (!this.settigns.NoUV)
            {
                Console.WriteLine($"UV mapping generated {uvTextures.Count} texture{(uvTextures.Count > 1 ? 's' : ' ')}");
            }

            Console.WriteLine($"Texture data size {uvTextures.Sum(texture => texture.DataLength + 8)} bytes");

            object meshData;

            if (settigns.ModelType == NyaArguments.ModelTypes.Flat)
            {
                MeshGroup<Mesh> meshGroup = new MeshGroup<Mesh>();
                meshGroup.Meshes = meshes.ToArray();
                meshGroup.MeshCount = meshGroup.Meshes.Length;
                meshGroup.Textures = uvTextures.ToArray();
                meshGroup.TextureCount = meshGroup.Textures.Length;
                meshData = meshGroup;
            }
            else
            {
                MeshGroup<SmoothMesh> meshGroup = new MeshGroup<SmoothMesh>();
                meshGroup.Meshes = meshes.OfType<SmoothMesh>().ToArray();
                meshGroup.MeshCount = meshGroup.Meshes.Length;
                meshGroup.Textures = uvTextures.ToArray();
                meshGroup.TextureCount = meshGroup.Textures.Length;
                meshData = meshGroup;
            }

            try
            {
                byte[]? data = Serializer.CustomMarshal.MarshalAsBytes(meshData);

                if (data != null)
                {
                    File.WriteAllBytes(outputFile, data);
                    return true;
                }

                Console.WriteLine("Error: Could not export model!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: Could not export model!\n{ex.Message}");
            }

            return false;
        }
    }
}