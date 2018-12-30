using ShenmueDKSharp.Graphics;
using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Pfim;
using System.Windows.Media;

namespace ShenmueDKSharp.Files.Images
{
    /// <summary>
    /// Sega Dreamcast PVR Texture
    /// </summary>
    public class PVRT : BaseImage
    {
        public static bool EnableBuffering = true;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<string> Extensions = new List<string>()
        {
            "PVR",
            "PVRT"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x47, 0x42, 0x49, 0x58 }, //GBIX
            new byte[4] { 0x50, 0x56, 0x52, 0x54 }  //PVRT
        };

        public static bool IsValid(uint identifier)
        {
            return IsValid(BitConverter.GetBytes(identifier));
        }

        public static bool IsValid(byte[] identifier)
        {
            for (int i = 0; i < Identifiers.Count; i++)
            {
                if (FileHelper.CompareSignature(Identifiers[i], identifier)) return true;
            }
            return false;
        }

        private static Color4 m_previousColor;

        public enum PVRColorFormat
        {
            ARGB1555    = 0x00, //Format consisting of one bit of alpha value and five bits of RGB values The alpha value indicates transparent when it is 0 and opaque when it is 1.
            RGB565      = 0x01, //Format without alpha value and consisting of five bits of RB values and six bits of G value.
            ARGB4444    = 0x02, //Format consisting of four bits of alpha value and four bits of RGB values. The alpha value indicates completely transparent when it is 0x0 and completely opaque when it is 0xF.
            YUV422      = 0x03, //YUV422 format
            BUMP        = 0x04, //Bump map with positiv only normal vectors (S and R direction angles)
            RGB555      = 0x05, //for PCX compatible only
            ARGB8888    = 0x06, //RGB, transparency (TODO: multiple definitions, see which one is correct for dreamcast)
            YUV420      = 0x06, //for YUV converter (TODO: multiple definitions, see which one is correct for dreamcast)
            DDS_RGB24   = 0x80, //DDS RGB, no transparency
            DDS_RGBA32  = 0x81  //DDS RGB, transparency
        }

        public enum PVRCategoryCode
        {
            SQUARE_TWIDDLED                     = 0x01,
            SQUARE_TWIDDLED_MIPMAP              = 0x02,
            VECTOR_QUANTIZATION                 = 0x03,
            VECTOR_QUANTIZATION_MIPMAP          = 0x04,
            PALETTIZE_4BIT                      = 0x05,  //Unsuported because shenmue doesn't use this format.
            PALETTIZE_4BIT_MIPMAP               = 0x06,  //Unsuported because shenmue doesn't use this format.
            PALETTIZE_8BIT                      = 0x07,  //Unsuported because shenmue doesn't use this format.
            PALETTIZE_8BIT_MIPMAP               = 0x08,  //Unsuported because shenmue doesn't use this format.
            RECTANGLE                           = 0x09,
            RECTANGLE_MIPMAP                    = 0x0A, //Reserved: Can't use.
            RECTANGLE_STRIDE                    = 0x0B,
            RECTANGLE_STRIDE_MIPMAP             = 0x0C, //Reserved: Can't use.
            RECTANGLE_TWIDDLED                  = 0x0D, //Should not be supported
            BMP                                 = 0x0E, //Converted to Twiddled
            BMP_MIPMAP                          = 0x0F, //Converted to Twiddled Mipmap
            VECTOR_QUANTIZATION_SMALL           = 0x10,
            VECTOR_QUANTIZATION_SMALL_MIPMAP    = 0x11,
            DDS                                 = 0x80,
            DDS_2                               = 0x87
        }

        public override int DataSize => (int)Size;

        public bool HasGBIX { get; set; } = false;
        public uint GBIXSize { get; set; }
        public byte[] GBIXContent { get; set; }

        public uint Size { get; set; }
        public PVRColorFormat ColorFormat { get; set; }
        public PVRCategoryCode CategoryCode { get; set; }

        public PVRT() { }
        public PVRT(string filename)
        {
            Read(filename);
        }
        public PVRT(Stream stream)
        {
            Read(stream);
        }
        public PVRT(BinaryReader reader)
        {
            Read(reader);
        }
        /// <summary>
        /// Creates an PVRT instance with the given filepath as the image data.
        /// Used for creating an PVRT from an DDS file.
        /// </summary>
        public PVRT(string filepath, PVRCategoryCode categoryCode, PVRColorFormat colorFormat, int width, int height)
        {
            Width = width;
            Height = height;

            CategoryCode = categoryCode;
            ColorFormat = colorFormat;

            //Write header
            //Write DDS raw
        }

        protected override void _Read(BinaryReader reader)
        {
            long baseOffset = reader.BaseStream.Position;

            uint identifier = reader.ReadUInt32();
            if (identifier == 0x58494247) //"GBIX"
            {
                HasGBIX = true;
                GBIXSize = reader.ReadUInt32();
                GBIXContent = reader.ReadBytes((int)GBIXSize);
                reader.BaseStream.Seek(4, SeekOrigin.Current); //Skip "PVRT"
            }

            Size = reader.ReadUInt32();
            ColorFormat = (PVRColorFormat)reader.ReadByte();
            CategoryCode = (PVRCategoryCode)reader.ReadByte();
            reader.BaseStream.Seek(2, SeekOrigin.Current);
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();

            if (ColorFormat == PVRColorFormat.BUMP)
            {
                //throw new Exception("HELLO");
            }

            if (CategoryCode == PVRCategoryCode.DDS || CategoryCode == PVRCategoryCode.DDS_2)
            {
                if (!(ColorFormat == PVRColorFormat.DDS_RGB24 || ColorFormat == PVRColorFormat.DDS_RGBA32))
                {
                    throw new Exception("Expected DDS RGB24 or RGBA32 color format!");
                }

                Dds image = Dds.Create(reader.BaseStream, new PfimConfig());

                byte[] pixels = image.Data;
                Pixels = new Color4[image.Width * image.Height];
                for (int i = 0; i < image.Width * image.Height; i++)
                {
                    int index = i * image.BytesPerPixel;
                    if (image.BytesPerPixel > 3)
                    {
                        if (pixels[index + 3] < 0.8)
                        {
                            HasTransparency = true;
                        }
                        Pixels[i] = new Color4(pixels[index + 2], pixels[index + 1], pixels[index], pixels[index + 3]);
                    }
                    else
                    {
                        HasTransparency = false;
                        Pixels[i] = new Color4(pixels[index + 2], pixels[index + 1], pixels[index], 255);
                    }
                }
            }

            /*
            else
            {
                PVRTParameters param = new PVRTParameters(CategoryCode, Width);

                /*
                if (param.PaletteDepth != 0)
                {
                    int paletteSize = 1 << param.PaletteDepth;
                    string paletteFilepath = Path.ChangeExtension(FilePath, "PVP");
                    //TODO: palette support (PVPL)
                }
                *//*

                int mipMapCount = CalculateMipMapCount();
                if (param.VQ)
                {
                    //read codebook entries
                    VQCodeBookEntry[] codeBook = new VQCodeBookEntry[param.CodeBookSize];
                    for (int i = 0; i < param.CodeBookSize; i++)
                    {
                        codeBook[i] = new VQCodeBookEntry(reader);
                    }

                    int mipMap = param.MipMaps ? mipMapCount - 1 : 0;
                    int tempWidth = param.MipMaps ? 1 : Width;
                    int tempHeight = tempWidth;

                    while (mipMap >= 0)
                    {
                        int write = 0;
                        int max = (tempWidth == 1) ? 1 : (tempWidth / 2) * (tempHeight / 2);
                        int x = 0;
                        int y = 0;

                        while (write < max)
                        {
                            if (tempWidth == 1) //special case: 1x1 vq mipmap is stored as 565 and index 0 is used
                            {

                            }
                            else
                            {
                                //unpack the twiddled 2x2 block
                                int xoff = 0;
                                int yoff = 0;
                                int[] linear = new int[]{ 0, 2, 1, 3 }; //this is needed so that YUV can be unpacked properly
                                for (int iTexel = 0; iTexel < 4; iTexel++)
                                {
                                    ushort index = reader.ReadUInt16();
                                    ushort texel = codeBook[index].Texel[linear[iTexel]];
                                }
                            }

                            write++;
                            x += 2;
                            if (x >= tempWidth)
                            {
                                x = 0;
                                y += 2;
                            }
                        }

                        mipMap--;
                        tempWidth *= 2;
                        tempHeight *= 2;
                    }

                }
                else
                {
                    if (param.MipMaps)
                    {
                        //skip 1x1 placeholder dummy
                        int dummySize = 2;
                        if (param.PaletteDepth == 8)
                        {
                            dummySize = 3;
                        }
                        reader.BaseStream.Seek(dummySize, SeekOrigin.Current);
                    }
                }

                if (param.Twiddled)
                {
                    Twiddle();
                }

            }
            */

            else if (CategoryCode == PVRCategoryCode.SQUARE_TWIDDLED || CategoryCode == PVRCategoryCode.RECTANGLE_TWIDDLED)
            {
                Pixels = new Color4[Width * Height];
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        int index = y * Width + x;
                        Pixels[index] = ReadColor(reader, x, y);
                    }
                }
                Twiddle();
            }
            else if (CategoryCode == PVRCategoryCode.VECTOR_QUANTIZATION)
            {
                var palette = new Color4[1024]; //256 Codebook entries * 4 texels
                for (int i = 0; i < palette.Length; i++)
                {
                    palette[i] = ReadColor(reader, i, 0);
                }
                var bytes = new byte[Width * Height / 4];
                for (int i = 0; i < Width * Height / 4; i++)
                {
                    bytes[i] = reader.ReadByte();
                }
                DecodeVQ(bytes, palette);
            }
            else if (CategoryCode == PVRCategoryCode.RECTANGLE)
            {
                Pixels = new Color4[Width * Height];
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        int index = y * Width + x;
                        Pixels[index] = ReadColor(reader, x, y);
                    }
                }
            }
            else
            {
                throw new NotImplementedException("Unknown category code or unsupported: " + CategoryCode);
            }

            reader.BaseStream.Seek(baseOffset + Size, SeekOrigin.Begin);
        }

        protected override void _Write(BinaryWriter writer)
        {
            long baseOffset = writer.BaseStream.Position;

            if (HasGBIX)
            {
                if (GBIXContent.Length != GBIXSize)
                {
                    throw new Exception("GBIX Size does not match the GBIX content length!");
                }
                writer.Write(GBIXSize);
                writer.Write(GBIXContent);
            }
            writer.Write(0x54525650); //"PVRT"

            long offsetSize = writer.BaseStream.Position;
            writer.Seek(4, SeekOrigin.Current); //Write size later

            writer.Write((byte)ColorFormat);
            writer.Write((byte)CategoryCode);
            writer.Write((short)0);
            writer.Write((ushort)Width);
            writer.Write((ushort)Height);

            //Write pixel data
            if (CategoryCode == PVRCategoryCode.DDS)
            {
                //TODO: How to handle DDS because we can't write DDS yet.
                throw new NotImplementedException("TODO");
            }
            else if (CategoryCode == PVRCategoryCode.SQUARE_TWIDDLED || CategoryCode == PVRCategoryCode.SQUARE_TWIDDLED_MIPMAP)
            {
                Twiddle();
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        int index = y * Width + x;
                        WriteColor(writer, x, y, Pixels[index]);
                    }
                }
                Twiddle(); //Twiddle after writing to reset color array
            }
            else if (CategoryCode == PVRCategoryCode.VECTOR_QUANTIZATION)
            {
                throw new NotImplementedException("TODO");
            }
            else if (CategoryCode == PVRCategoryCode.RECTANGLE)
            {
                throw new NotImplementedException("TODO");
            }
            else
            {
                throw new NotImplementedException("Unknown category code or unsupported.");
            }

            //Write size
            Size = (uint)(writer.BaseStream.Position - baseOffset);
            writer.BaseStream.Seek(offsetSize, SeekOrigin.Begin);
            writer.Write(Size);
            writer.Seek(0, SeekOrigin.End);
        }

        private void DecodeVQ(byte[] source, Color4[] palette)
        {
            int[] twiddleMap = CreateTwiddleMap(Width / 2);

            Pixels = new Color4[Width * Height];
            for (int y = 0; y < Height; y += 2)
            {
                for (int x = 0; x < Width; x += 2)
                {
                    int index = (source[(twiddleMap[x >> 1] << 1) | twiddleMap[y >> 1]]) * 4;
                    for (int x2 = 0; x2 < 2; x2++)
                    {
                        for (int y2 = 0; y2 < 2; y2++)
                        {
                            long destinationIndex = ((y + y2) * Width) + (x + x2);
                            Pixels[destinationIndex] = palette[index];
                            index++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Twiddles the current pixels.
        /// </summary>
        private void Twiddle()
        {
            int size = Width < Height ? Width : Height;
            int[] twiddleMap = CreateTwiddleMap(size);

            Color4[] newPixels = new Color4[Width * Height];
            int squareIndex = 0;
            for (int sqy = 0; sqy < Height; sqy += size)
            {
                for (int sqx = 0; sqx < Width; sqx += size)
                {
                    long baseIndex = sqy * Width + sqx;
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            int index = squareIndex + ((twiddleMap[x] << 1) | twiddleMap[y]);
                            long destinationIndex = baseIndex + (y * Width) + x;
                            newPixels[destinationIndex] = Pixels[index];
                        }
                    }
                    squareIndex += size * size;
                }
            }
            Pixels = newPixels;
        }

        /// <summary>
        /// Reads and returns the color for the current pixel coordinates from the given reader.
        /// </summary>
        /// <exception cref="System.NotImplementedException">Unknown color format!</exception>
        private Color4 ReadColor(BinaryReader reader, int pX, int pY)
        {
            switch (ColorFormat)
            {
                case PVRColorFormat.RGB565:
                    {
                        ushort val = reader.ReadUInt16();
                        byte r = (byte)((val & 0xF800) >> 8);
                        byte g = (byte)((val & 0x07E0) >> 3);
                        byte b = (byte)((val & 0x001F) << 3);
                        return new Color4(r, g, b, 255);
                    }
                case PVRColorFormat.ARGB1555:
                    {
                        ushort val = reader.ReadUInt16();
                        byte a = (byte)((val & 0x8000) == 0x8000 ? 0xFF : 0x00);
                        byte r = (byte)((val & 0x7C00) >> 7);
                        byte g = (byte)((val & 0x03E0) >> 2);
                        byte b = (byte)((val & 0x001F) << 3);
                        return new Color4(r, g, b, a);
                    }
                case PVRColorFormat.ARGB4444:
                    {
                        ushort val = reader.ReadUInt16();
                        byte a = (byte)((val & 0xF000) >> 8);
                        byte r = (byte)((val & 0x0F00) >> 4);
                        byte g = (byte)(val & 0x00F0);
                        byte b = (byte)((val & 0x000F) << 4);
                        return new Color4(r, g, b, a);
                    }
                case PVRColorFormat.YUV422:
                    {
                        ushort val1 = reader.ReadUInt16();
                        ushort val2 = reader.ReadUInt16();

                        int Y0 = (val1 & 0xFF00) >> 8, U = (val1 & 0x00FF);
                        int Y1 = (val2 & 0xFF00) >> 8, V = (val2 & 0x00FF);

                        if ((pX & 1) == 0)
                        {
                            //First pixel
                            byte r = MathExtensions.ClampByte((int)(Y0 + 1.375 * (V - 128)));
                            byte g = MathExtensions.ClampByte((int)(Y0 - 0.6875 * (V - 128) - 0.34375 * (U - 128)));
                            byte b = MathExtensions.ClampByte((int)(Y0 + 1.71875 * (U - 128)));

                            //Go back 4 bytes for the second pixel.
                            reader.BaseStream.Seek(-4, SeekOrigin.Current);

                            return new Color4(r, g, b, 255);
                        }
                        else
                        {
                            //Second pixel
                            byte r = MathExtensions.ClampByte((int)(Y1 + 1.375 * (V - 128)));
                            byte g = MathExtensions.ClampByte((int)(Y1 - 0.6875 * (V - 128) - 0.34375 * (U - 128)));
                            byte b = MathExtensions.ClampByte((int)(Y1 + 1.71875 * (U - 128)));

                            return new Color4(r, g, b, 255);
                        }
                    }
                case PVRColorFormat.BUMP:
                    {
                        byte r = reader.ReadByte();
                        byte s = reader.ReadByte();

                        //Convert to angles
                        double rAngle = r / 255.0 * 360.0;
                        double sAngle = s / 255.0 * 90.0;

                        //To radians
                        double rRadian = MathHelper.DegreesToRadians(rAngle);
                        double sRadian = MathHelper.DegreesToRadians(sAngle);

                        //Calculate normal
                        double x = Math.Cos(sRadian) * Math.Cos(rRadian);
                        double y = Math.Cos(sRadian) * Math.Sin(rRadian);
                        double z = Math.Sin(sRadian);

                        //Normalize to RGB ([-1,1] -> [0,1])
                        double colorR = 0.5 * x + 0.5;
                        double colorG = 0.5 * y + 0.5; //Y/Z flip
                        double colorB = 0.5 * z + 0.5; //Z/Y flip

                        return new Color4((float)colorR, (float)colorG, (float)colorB, 1.0f);
                    }
                case PVRColorFormat.RGB555:
                    {
                        ushort val = reader.ReadUInt16();
                        byte r = (byte)((val & 0x7C00) >> 7);
                        byte g = (byte)((val & 0x03E0) >> 2);
                        byte b = (byte)((val & 0x001F) << 3);
                        return new Color4(r, g, b, 255);
                    }
                case PVRColorFormat.ARGB8888:
                    {
                        byte b = reader.ReadByte();
                        byte g = reader.ReadByte();
                        byte r = reader.ReadByte();
                        byte a = reader.ReadByte();
                        return new Color4(r, g, b, a);
                    }
                default:
                    {
                        throw new NotImplementedException("Unknown color format!");
                    }
            }
        }

        /// <summary>
        /// Writes the given color to the given writer at the given pixel coordinates.
        /// </summary>
        /// <exception cref="System.NotImplementedException">Unknown color format!</exception>
        private void WriteColor(BinaryWriter writer, int pX, int pY, Color4 color)
        {
            switch (ColorFormat)
            {
                case PVRColorFormat.RGB565:
                    {
                        ushort val = (ushort)(((color.R_ << 8) & 0xF800) | ((color.G_ << 3) & 0x07E0) | ((color.B_ >> 3) & 0x001F));
                        writer.Write(val);
                        return;
                    }
                case PVRColorFormat.ARGB1555:
                    {
                        ushort val = (ushort)(((color.A_ << 8) & 0x8000) | ((color.R_ << 7) & 0x7C00) | ((color.G_ << 2) & 0x03E0) | ((color.B_ >> 3) & 0x001F));
                        writer.Write(val);
                        return;
                    }
                case PVRColorFormat.ARGB4444:
                    {
                        ushort val = (ushort)(((color.A_ << 8) & 0xF000) | ((color.R_ << 4) & 0x0F00) | (color.G_ & 0x00F0) | ((color.B_ >> 4) & 0x000F));
                        writer.Write(val);
                        return;
                    }
                case PVRColorFormat.YUV422:
                    {
                        m_previousColor = color;
                        if ((pX & 1) == 1)
                        {
                            byte r1 = m_previousColor.R_;
                            byte g1 = m_previousColor.G_;
                            byte b1 = m_previousColor.B_;

                            byte r2 = color.R_;
                            byte g2 = color.G_;
                            byte b2 = color.B_;

                            //Compute each pixel's Y
                            uint Y0 = (uint)(0.299 * r1 + 0.587 * r2 + 0.114 * b1);
                            uint Y1 = (uint)(0.299 * r2 + 0.587 * g2 + 0.114 * b2);

                            //Average both pixel's rgb values
                            byte r = (byte)((r2 + r1) / 2);
                            byte g = (byte)((g2 + r1) / 2);
                            byte b = (byte)((b2 + r1) / 2);

                            //Compute UV
                            uint U = (uint)(128.0f - 0.14 * r - 0.29 * g + 0.43 * b);
                            uint V = (uint)(128.0f + 0.36 * r - 0.29 * g - 0.07 * b);

                            ushort pixel1 = (ushort)((Y0 << 8) | U);
                            ushort pixel2 = (ushort)((Y1 << 8) | V);

                            writer.Write(pixel1);
                            writer.Write(pixel2);
                        }
                        return;
                    }
                case PVRColorFormat.BUMP:
                    {
                        //Normalize to normal direction vector ([0,1] -> [-1,1])
                        double x = color.R * 2.0 - 1.0;
                        double z = color.G * 2.0 - 1.0; //Y/Z flip
                        double y = color.B * 2.0 - 1.0; //Z/Y flip

                        //Normal to radians
                        double radius = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
                        double rRadian = Math.Atan2(y, x);
                        double sRadian = Math.Asin(z / radius);

                        //Radians to angles
                        double rAngle = MathHelper.RadiansToDegrees(rRadian);
                        rAngle = rAngle < 0.0 ? rAngle + 360.0 : rAngle;
                        double sAngle = MathHelper.RadiansToDegrees(sRadian);

                        //Clamp angles to valid angles
                        rAngle = MathExtensions.Clamp(rAngle, 0.0f, 360.0f);
                        sAngle = MathExtensions.Clamp(sAngle, 0.0f, 90.0f);

                        //Convert to bytes
                        byte r = (byte)Math.Round(rAngle / 360.0f * 255.0f);
                        byte s = (byte)Math.Round(sAngle / 90.0f * 255.0f);

                        writer.Write(r);
                        writer.Write(s);
                        return;
                    }
                case PVRColorFormat.RGB555:
                    {
                        ushort val = (ushort)(((color.A_ << 8) & 0x8000) | ((color.R_ << 7) & 0x7C00) | ((color.G_ << 2) & 0x03E0) | ((color.B_ >> 3) & 0x001F));
                        writer.Write(val);
                        return;
                    }
                case PVRColorFormat.ARGB8888:
                    {
                        int val = color.ToArgb();
                        writer.Write(val);
                        return;
                    }
                default:
                    {
                        throw new NotImplementedException("Unknown color format!");
                    }
            }
        }

        /// <summary>
        /// Creates an twiddle map with the given size.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private int[] CreateTwiddleMap(int size)
        {
            int[] twiddleMap = new int[size];
            for (int i = 0; i < size; i++)
            {
                twiddleMap[i] = 0;
                for (int j = 0, k = 1; k <= i; j++, k <<= 1)
                {
                    twiddleMap[i] |= (i & k) << j;
                }
            }
            return twiddleMap;
        }

        private int CalculateMipMapCount()
        {
            int mipMapCount = 0;
            int tempWidth = Width;
            while (tempWidth > 0)
            {
                mipMapCount++;
                tempWidth /= 2;
            }
            return mipMapCount;
        }

        private class VQCodeBookEntry
        {
            public ushort[] Texel;

            public VQCodeBookEntry()
            {
                Texel = new ushort[4];
            }

            public VQCodeBookEntry(BinaryReader reader)
            {
                Texel = new ushort[4];
                Texel[0] = reader.ReadUInt16();
                Texel[1] = reader.ReadUInt16();
                Texel[2] = reader.ReadUInt16();
                Texel[3] = reader.ReadUInt16();
            }
        };

        private class PVRTParameters
        {
            public bool Rectangle { get; set; } = false;
            public bool Twiddled { get; set; } = false;
            public bool MipMaps { get; set; } = false;
            public bool VQ { get; set; } = false;
            public int CodeBookSize { get; set; } = 0;
            public int PaletteDepth { get; set; } = 0;

            public PVRTParameters(PVRCategoryCode categoryCode, int Width)
            {
                switch (categoryCode)
                {
                    case PVRCategoryCode.SQUARE_TWIDDLED:
                        Twiddled = true;
                        break;
                    case PVRCategoryCode.SQUARE_TWIDDLED_MIPMAP:
                        Twiddled = true; MipMaps = true;
                        break;
                    case PVRCategoryCode.RECTANGLE_TWIDDLED:
                        Twiddled = true; Rectangle = true; 
                        break;
                    case PVRCategoryCode.VECTOR_QUANTIZATION:
                        Twiddled = true; VQ = true; CodeBookSize = 256;
                        break;
                    case PVRCategoryCode.VECTOR_QUANTIZATION_MIPMAP:
                        Twiddled = true; VQ = true; CodeBookSize = 256; MipMaps = true;
                        break;
                    case PVRCategoryCode.VECTOR_QUANTIZATION_SMALL:
                        Twiddled = true; VQ = true;
                        if (Width <= 16)
                            CodeBookSize = 16;
                        else if (Width == 32)
                            CodeBookSize = 32;
                        else if (Width == 64)
                            CodeBookSize = 128;
                        else
                            CodeBookSize = 256;
                        break;
                    case PVRCategoryCode.VECTOR_QUANTIZATION_SMALL_MIPMAP:
                        Twiddled = true; VQ = true; MipMaps = true;
                        if (Width <= 16)
                            CodeBookSize = 16;
                        else if (Width == 32)
                            CodeBookSize = 64;
                        else
                            CodeBookSize = 256;
                        break;
                    case PVRCategoryCode.RECTANGLE_STRIDE:
                    case PVRCategoryCode.RECTANGLE:
                        Rectangle = true;
                        break;
                    case PVRCategoryCode.RECTANGLE_MIPMAP:
                        Rectangle = true; MipMaps = true;
                        break;
                    //Adding support for palettized formats maybe later if anyone requests it, because it isn't used in shenmue.
                    case PVRCategoryCode.PALETTIZE_4BIT:
                        //Twiddled = true; PaletteDepth = 4;
                        //break;
                    case PVRCategoryCode.PALETTIZE_4BIT_MIPMAP:
                        //Twiddled = true; PaletteDepth = 4; MipMaps = true;
                        //break;
                    case PVRCategoryCode.PALETTIZE_8BIT:
                        //Twiddled = true; PaletteDepth = 8;
                        //break;
                    case PVRCategoryCode.PALETTIZE_8BIT_MIPMAP:
                        //Twiddled = true; PaletteDepth = 8; MipMaps = true;
                        //break;
                    default:
                        throw new NotImplementedException("Unknown category code or unsupported.");
                }
            }
        }
    }

}
