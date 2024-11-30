#ifndef __MODEL_LOADER_H__
#define __MODEL_LOADER_H__

/*
 * Texture data (image name, diffuse color)
 */
typedef struct
{
    char FileName[13];
    unsigned char color[3];
} TmfTexture;

/*
 * Load Jo-Engine mesh (textures are loaded from the same folder model is in)
 * @param file File name inside models folder
 * @param dir Model file folder
 * @param loaded number of loaded models
 * @return loaded array of meshes
 */
jo_3d_mesh * ML_LoadMesh(const char *file, const char * dir, int *loaded);

/*
 * Load Jo-Engine mesh
 * @param file File name inside models folder
 * @param dir Model file folder
 * @param texture_loader texture loader
 * @param loaded number of loaded models
 * @return loaded array of meshes
 */
jo_3d_mesh * ML_LoadMeshWithCustomTextureLoader(const char * file, const char * dir, int (*texture_loader)(TmfTexture*, const char *, int), int * loaded);

#endif