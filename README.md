
# Shenmue Developer Kit for C#
Shenmue I & II HD Modding SDK for C#

This library was created to give developers an easy access to all the file formats of Shenmue I & II HD.

How to contribute:
- Feel free to create pull requests if you want to contribute some fixes or features.
- If you found an bug just create an issue on GitHub.
- For feature requests just create an issue on GitHub.

## Usage
Model reading example:
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

## File Formats
For more informations about the file formats see the [wulinshu wiki](https://wulinshu.com/wiki/index.php/Main_Page).

### Containers

| Name| Read | Write | Description | Notes |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| AFS | :heavy_check_mark: | :heavy_check_mark: | Archive | |
| IDX | :heavy_check_mark: | :large_orange_diamond: | AFS Archive Reference Names | Only IDX0 can be created |
| PKF | :heavy_check_mark: | :heavy_check_mark: | Archive (mainly textures for PKS) | |
| PKS (IPAC) | :heavy_check_mark: | :heavy_check_mark: | Archive | |
| SPR | :heavy_check_mark: | :heavy_check_mark: | Sprite texture Container | |
| GZ | :heavy_check_mark: | :heavy_check_mark: | GZip | |
| TAD/TAC | :heavy_check_mark: | :heavy_check_mark: | d3t TAD/TAC container with hash mapping | Filename coverage based on [wulinshu hash database](https://wulinshu.raymonf.me/#/) |

### Textures/Images

| Name| Read | Write | Description | Notes |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| PVRT | :large_orange_diamond: | :large_orange_diamond: | PowerVR Texture | Not all formats working but enough for MT5 and MT7 |
| DDS | :large_orange_diamond: | :large_orange_diamond: | DirectDraw_Surface | Only reads first mipmap |
| JPEG | :heavy_check_mark: | :heavy_check_mark: | JPEG format |  |
| BMP | :heavy_check_mark: | :heavy_check_mark: | Bitmap format |  |
| PNG | :heavy_check_mark: | :heavy_check_mark: | PNG format |  |

#### PVR color formats

| Value | Name | Read | Write | Description | Notes |
| -------------| ------------- | ------------- | ------------- | ------------- | ------------- |
| 0x00 | ARGB1555 | :heavy_check_mark: | :heavy_check_mark: | Format consisting of one bit of alpha value and five bits of RGB values. |  |
| 0x01 | RGB565 | :heavy_check_mark: | :heavy_check_mark: | Format without alpha value and consisting of five bits of RB values and six bits of G value. |  |
| 0x02 | ARGB4444 | :heavy_check_mark: | :heavy_check_mark: | Format consisting of four bits of alpha value and four bits of RGB values. |  |
| 0x03 | YUV422 | :heavy_check_mark: | :heavy_check_mark: | YUV422 format |  |
| 0x04 | BUMP | :large_orange_diamond: | :large_orange_diamond: | Bump map with positiv only normal vectors (S and R direction angles) | Untested |
| 0x05 | RGB555 | :heavy_check_mark: | :heavy_check_mark: | for PCX compatible only | |
| 0x06 | ARGB8888 | :large_orange_diamond: | :large_orange_diamond: | Format consisting of 1 byte of alpha value and 1 byte of RGB values. | Conflicting defines |
| 0x06 | YUV420 | :x: | :x: | YUV420 format :maple_leaf:. For YUV converter | Conflicting defines |
| 0x80 | DDS_RGB24 | :heavy_check_mark: | :x: | RGB24 format |  |
| 0x81 | DDS_RGBA32 | :heavy_check_mark: | :x: | RGBA32 format |  |

#### PVR data formats

| Value | Name | Read | Write | Notes |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| 0x01 | SQUARE_TWIDDLED | :heavy_check_mark: | :heavy_check_mark: |  |
| 0x02 | SQUARE_TWIDDLED_MIPMAP | :x: | :x: |  |
| 0x03 | VECTOR_QUANTIZATION | :heavy_check_mark: | :x: |  |
| 0x04 | VECTOR_QUANTIZATION_MIPMAP | :x: | :x: |  |
| 0x05 | PALETTIZE_4BIT | :x: | :x: | Not needed |
| 0x06 | PALETTIZE_4BIT_MIPMAP | :x: | :x: | Not needed |
| 0x07 | PALETTIZE_8BIT | :x: | :x: | Not needed |
| 0x08 | PALETTIZE_8BIT_MIPMAP | :x: | :x: | Not needed |
| 0x09 | RECTANGLE | :heavy_check_mark: | :x: |  |
| 0x0A | RECTANGLE_MIPMAP | :x: | :x: | Reserved: Can't use. |
| 0x0B | RECTANGLE_STRIDE | :heavy_check_mark: | :x: |  |
| 0x0C | RECTANGLE_STRIDE_MIPMAP | :x: | :x: |Reserved: Can't use. |
| 0x0D | RECTANGLE_TWIDDLED | :heavy_check_mark: | :x: | Should not be supported  |
| 0x0E | BMP | :x: | :x: | Converted to Twiddled |
| 0x0F | BMP_MIPMAP | :x: | :x: | Converted to Twiddled Mipmap |
| 0x10 | VECTOR_QUANTIZATION_SMALL | :x: | :x: |  |
| 0x11 | VECTOR_QUANTIZATION_SMALL_MIPMAP | :x: | :x: |  |
| 0x80 | DDS | :heavy_check_mark: | :x: | DDS format |

### Models/Animation

| Name| Read | Write | Description | Notes |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| MT5 | :large_orange_diamond: | :x: | Model Container | Reading works but still has some unknown stuff. |
| MT6 | :x: | :x: | Model Container | |
| MT7 | :large_orange_diamond: | :x: | Model Container | Reading works but missing rig and skin weights and some unknown stuff |
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

### Mapinfo
| Name| Read | Write | Description | Notes |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| CHRD | :x: | :x: | | |
| CHRM | :x: | :x: | | |
| DOOR | :x: | :x: | Door portals? | |
| ECAM | :x: | :x: | | |
| FLDD | :x: | :x: | | |
| LGHT | :x: | :x: | Lighting data | |
| MAPR | :x: | :x: | | |
| MAPT | :x: | :x: | | |
| SCEX | :x: | :x: | Cutscenes and maybe other stuff| |
| SNDP | :x: | :x: | Sound program | |
| WTHR | :x: | :x: | Weather data | |

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

## Credits

- [SHENTRAD Team](http://shenmuesubs.sourceforge.net/) - Shenmue I & II DC ground work
- [Shenmue-Mods](https://github.com/Shenmue-Mods/Shenmue-Mods) - Modding knowledge database
- [Raymonf](https://wulinshu.raymonf.me/#/) - Wulinshu TAD hash database
- [Fishbiter](https://github.com/Fishbiter/Shenmunity_plugin) - MT5, MOTN and PVRT starting code
- [hellwig](https://github.com/hellwig/shencon) - MT5 incompleted code
- [yazgoo](https://github.com/yazgoo/mt5_extraction_tools) - MT5 and MT7 rough starting point
- [KFreon](https://github.com/KFreon/CSharpImageLibrary) - DDS reader/writer base code

