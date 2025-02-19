# Model converter
Simple cross-platform model converter written in C# .NET with plugin support.

## Plugins
Model converter supports both import and export plugins. Plugins can either be picked automatically or manually using cmd arguments.

To show help for each specific plugin, type ``-h [plugin name]`` as an argument for the model converter. To get the name of the available plugin ``-plugins`` argument can be used.

### Pre-made plugins
Plugin    | Type   | Ext  | Description
----------|--------|------|--------------
NYA       | Export | .NYA | Export for sega Saturn game [Utenyaa](https://reye.me/?show=Project&Id=utenyaa) and [SRL](https://srl.reye.me/) samples
TMF       | Export | .TMF | Export for sega Saturn game [TankGame](https://reye.me/?show=Project&Id=TankGame)
Cobra     | Export | .CBR | Export for sega Saturn game by Cobra!
Wavefront | Import | .obj | Wavefront importer

#### Wavefront import plugin
##### Description
.obj import plugin reads .mtl file to decide what color (using ``kd`` attribute) or texture (using ``map_kd`` attribute).

Material name is also parsed for special flags. Characters after last ``_`` symbol in material name are treated as special flags (*see in table below*).  
Example of a material name with double sided and mesh transparency flag: ``MyMaterial_DM``
Flag  | Description
------|-------------
M     | Mesh checkerboard transparency
D     | Face is double sided
H     | Half-transparent (50% VDP1 transparency)
F     | Force face to be flat shaded
B     | Half-bright (50% color brightness)
L,-,+ | ``L`` = Same sort as last rendered quad<br/>``-`` = Sort by closest point<br/>``+`` = Sort by furthest point<br/>If not specified, sorting by center point is used

##### Plugin arguments
Argument | Description
---------|------------------------------------------------------
s, scale | Scale the imported object by some multiplier<br/>``-s 1.5`` = Scale 1.5x

#### Nya export plugin
##### Description
Export for sega Saturn game [Utenyaa](https://reye.me/?show=Project&Id=utenyaa) and [SRL](https://srl.reye.me/) samples.  
This format supports UV mapping and smooth shading, also allows for multiple objects to be inside a single file making vertex animations possible as well.

__Note:__ When exporting smooth type, vertex normal table is exported at the end of object mesh data.  
__Note:__ UV mapping generates a new sprite texture for each unique UV region.  

##### Plugin arguments
Argument     | Description
-------------|------------------------------------------------------
w, no-unwrap | Disable UV mapping preprocessor
t, type      | Specify object type<br/>``-t Flat`` = Flat shaded<br/>``-t Smooth`` = Smooth shaded model

### Custom plugins
Custom plugins can be also written by referencing the ModelConverter.dll and implementing the interfaces within. Custom plugin dll with its dependencies can than be put inside ``/plugins/[plugin name]/`` folder.
Each custom plugin can also implement its own command line arguments.

## Base arguments
### Base arguments
Argument          | Description
------------------|------------------------------------------------------
exp&#160;*[name]* | Force export plugin regardless of file extension
imp&#160;*[name]* | Force import plugin regardless of file extension
i&#160;*[paths]*  | Path to the input file. (eg.: -i "work/models/test.obj") or multiple files (eg.: -i "work/models/test1.obj" "work/models/test2.obj")
o&#160;*[path]*   | Path to the output file. (eg.: -o "work/result.tmf")
help, h           | Shows this helps screen. To show specific plugin help add plugin name after command.
plugins           | Show all available plugins.

__Note:__ ``-i`` argument can take multiple paths, this allows the export plugins to create either vertex animated models or store multiple models in a single file.  
Example: ``-i "MyFolder/model_01.obj" "MyFolder/model_02.obj"``

Paths specified for the ``-i`` argument can also use wildcards.  
Example: ``-i "MyFolder/model_*.obj"``

### Argument usage example

#### Windows:
```batch
ModelConverter -i "./my models/model.obj" -o "./converted/MODEL.NYA"
```
#### Linux:
```bash
dotnet ./ModelConverter.dll -i "./my models/model.obj" -o "./converted/MODEL.NYA"
```