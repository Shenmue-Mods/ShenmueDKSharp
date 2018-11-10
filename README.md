# Shenmue Developer Kit for C#
Shenmue I & II HD Modding SDK for C#

This library was created to give developers an easy access to all the file formats of Shenmue I & II HD.
Feel free to create pull requests if you want to contribute.
If you found an bug just create an issue on GitHub.
For feature requests just create an issue on GitHub.


## File Formats
For more informations about the file formats see the [wiki](https://github.com/philyeahz/ShenmueDKSharp/wiki).

### Containers

| Name| Read | Write | Description | Notes |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| AFS | :heavy_check_mark: | :heavy_check_mark: | Archive | Needs testing |
| IDX | :heavy_check_mark: | :large_orange_diamond: | AFS Archive Reference Names | HUMANS.IDX can't be created |
| PKF | :heavy_check_mark: | :heavy_check_mark: | Archive (mainly textures for PKS) | Needs testing |
| PKS (IPAC) | :heavy_check_mark: | :heavy_check_mark: | Archive | Needs testing |
| SPR | :heavy_check_mark: | :heavy_check_mark: | Sprite texture Container | Needs testing |
| GZ | :heavy_check_mark: | :heavy_check_mark: | GZip | |

### Images

| Name| Read | Write | Description | Notes |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| PVRT | :large_orange_diamond: | :x: | PowerVR Texture | Not all formats working but enough for MT5 and MT7 |
| DDS | :large_orange_diamond: | :x: | DirectDraw_Surface | Only reads first mipmap |

### Models/Animation

| Name| Read | Write | Description | Notes |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| MT5 | :large_orange_diamond: | :x: | Model Container | Works but still has some unknown stuff |
| MT6 | :x: | :x: | Model Container | |
| MT7 | :large_orange_diamond: | :x: | Model Container | Works but missing rig and skin weights and some unknown stuff |
| MOTN | :x: | :x: | Motion data (Animation sequences) | |

### Audio

| Name| Read | Write | Description | Notes |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| SND | :x: | :x: | Dreamcast sound file | |
| XWMA | :x: | :x: | Xbox WMA (XAudio2) file | |

### Subtitles/Text

| Name| Read | Write | Description | Notes |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| SRF | :x: | :x: | Cinematic subtitles file | |
| FONTDEF | :x: | :x: | Font definition file | |
| SUB | :heavy_check_mark: | :heavy_check_mark: | Subtitles file | |
| GLYPHS | :x: | :x: | Font glyph file | |
| FON | :x: | :x: | Disk font file | |

### Other

| Name| Read | Write | Description | Notes |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| ATH | :x: | :x: | Sequence Data | |
| SRL | :x: | :x: | Scroll Data | |
| IWD | :x: | :x: | LCD Table | |
| WDT | :x: | :x: | Weather Data | |
| UI | :x: | :x: | UI Json | |
| CHR | :x: | :x: | Character | |
| MVS | :x: | :x: | MVS data | |
| DYM | :x: | :x: | Dynamics Info | |
| CRM | :x: | :x: | Character Model | |
| CHT | :x: | :x: | Character Properties | |
| CSV | :x: | :x: | Comma-separated values | |
| EMU | :x: | :x: | Emulator file | |


## Usage
Basic example:
```c-sharp
using ShenmueDKSharp;
using ShenmueDKSharp.Files.Models;
	 
public void ReadMT7(string filename)
{
  MT7 mt7 = new MT7(filename);
  foreach(ModelNode node in mt7.GetAllNodes())
  {
    foreach(MeshFace face in node.Faces)
    {
      face.GetFloatArray(node, Vertex.VertexFormat.VertexNormalUV)
    }
  }
}
```

## Credits

- [SHENTRAD Team](http://shenmuesubs.sourceforge.net/) - Shenmue I & II DC ground work
- [Shenmue-Mods](https://github.com/Shenmue-Mods/Shenmue-Mods) - Modding knowledge database
- [Fishbiter](https://github.com/Fishbiter/Shenmunity_plugin) - MT5, MOTN and PVRT starting code
- [hellwig](https://github.com/hellwig/shencon) - MT5 incompleted code
- [yazgoo](https://github.com/yazgoo/mt5_extraction_tools) - MT5 and MT7 rough starting point
- [nickbabcock](https://github.com/nickbabcock/Pfim) - DDS reader base code
