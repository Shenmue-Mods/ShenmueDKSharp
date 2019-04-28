using System;

namespace ShenmueDKSharp.Files.Images._PVRT
{
    // Pvr Pixel Formats
    public enum PvrPixelFormat : byte
    {
        ARGB1555    = 0x00, //Format consisting of one bit of alpha value and five bits of RGB values. The alpha value indicates transparent when it is 0 and opaque when it is 1.
        RGB565      = 0x01, //Format without alpha value and consisting of five bits of RB values and six bits of G value.
        ARGB4444    = 0x02, //Format consisting of four bits of alpha value and four bits of RGB values. The alpha value indicates completely transparent when it is 0x0 and completely opaque when it is 0xF.
        YUV422      = 0x03, //YUV422 format
        BUMP88      = 0x04, //Bump map with positiv only normal vectors (S and R direction angles)
        RGB555      = 0x05, //for PCX compatible only
        ARGB8888    = 0x06, //Palettize only!
        DDS_DXT1_RGB24   = 0x80, //DDS DXT1 RGB, no transparency
        DDS_DXT3_RGBA32  = 0x81, //DDS DXT3 RGBA, transparency
        UNKNOWN     = 0xFF,
    }

    // Pvr Data Formats
    public enum PvrDataFormat : byte
    {
        RAW                                 = 0x00, //Supported
        SQUARE_TWIDDLED                     = 0x01, //Supported
        SQUARE_TWIDDLED_MIPMAP              = 0x02, //Supported
        VECTOR_QUANTIZATION                 = 0x03, //Supported
        VECTOR_QUANTIZATION_MIPMAP          = 0x04, //Supported
        PALETTIZE_4BIT                      = 0x05, //Supported
        PALETTIZE_4BIT_MIPMAP               = 0x06, //Supported
        PALETTIZE_8BIT                      = 0x07, //Supported
        PALETTIZE_8BIT_MIPMAP               = 0x08, //Supported
        RECTANGLE                           = 0x09, //Supported
        RECTANGLE_MIPMAP                    = 0x0A, //Reserved: Can't use.
        RECTANGLE_STRIDE                    = 0x0B,
        RECTANGLE_STRIDE_MIPMAP             = 0x0C, //Reserved: Can't use.
        RECTANGLE_TWIDDLED                  = 0x0D, //Supported
        BMP                                 = 0x0E, //Should not be supported
        BMP_MIPMAP                          = 0x0F, //Should not be supported
        VECTOR_QUANTIZATION_SMALL           = 0x10, //Supported
        VECTOR_QUANTIZATION_SMALL_MIPMAP    = 0x11, //Supported
        SQUARE_TWIDDLED_MIPMAP_ALT          = 0x12, //What?
        DDS                                 = 0x80, //Supported
        DDS_2                               = 0x87, //Supported
        UNKNOWN                             = 0xFF,
    }

    // Pvr Compression Formats
    public enum PvrCompressionFormat
    {
        NONE,
        RLE,
    }
}