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
    public class PVRT : BaseImage
    {
        public enum PVRType
        {
            ARGB1555,// (bilevel translucent alpha 0,255)
            RGB565, //(no translucent)
            ARGB4444, //(translucent alpha 0-255)
            YUV442,
            Bump,
            Bit4,
            Bit8,
            DDS_RGB24 = 128,
            DDS_RGBA32 = 129
        }

        public enum PVRFormat
        {
            SQUARE_TWIDDLED = 1,
            SQUARE_TWIDDLED_MIPMAP = 2,
            VQ = 3,
            VQ_MIPMAP = 4,
            CLUT_TWIDDLED_8BIT = 5,
            CLUT_TWIDDLED_4BIT = 6,
            DIRECT_TWIDDLED_8BIT = 7,
            DIRECT_TWIDDLED_4BIT = 8,
            RECTANGLE = 9,
            RECTANGULAR_STRIDE = 0xb,
            RECTANGULAR_TWIDDLED = 0xd,
            SMALL_VQ = 0x10,
            SMALL_VQ_MIPMAP = 0x11,
            SQUARE_TWIDDLED_MIPMAP_2 = 0x12,
        }

        public Bitmap Bitmap;
        public uint GBIXSize;
        public byte[] GBIXContent;
        public uint Size;
        public PVRType Type;
        public PVRFormat Format;
        public byte[] buffer;

        public PVRT(string filename)
        {
            Read(filename);
        }

        public PVRT(BinaryReader br)
        {
            Read(br);
        }


        public override void Read(BinaryReader br)
        {
            
            long offset = br.BaseStream.Position;

            br.BaseStream.Seek(4, SeekOrigin.Current); //"GBIX"
            GBIXSize = br.ReadUInt32();
            GBIXContent = br.ReadBytes((int)GBIXSize);
            br.BaseStream.Seek(4, SeekOrigin.Current); //"PVRT"

            Size = br.ReadUInt32();
            Type = (PVRType)br.ReadByte();
            Format = (PVRFormat)br.ReadByte();
            br.BaseStream.Seek(2, SeekOrigin.Current);
            Width = br.ReadUInt16();
            Height = br.ReadUInt16();

            if (Format == PVRFormat.VQ)
            {
                var palette = new Color4[1024];
                for (int i = 0; i < palette.Length; i++)
                {
                    palette[i] = ReadColor(br);
                }
                var bytes = new byte[Width * Height / 4];
                for (int i = 0; i < Width * Height / 4; i++)
                {
                    bytes[i] = br.ReadByte();
                }
                DecodeVQ(bytes, palette);
            }
            else if (Type == PVRType.RGB565 || Type == PVRType.ARGB1555 || Type == PVRType.ARGB4444)
            {
                Pixels = new Color4[Width * Height];
                for (int i = 0; i < Width * Height; i++)
                {
                    Pixels[i] = ReadColor(br);
                }
                Unswizzle();
            }
            else if (Type == PVRType.DDS_RGB24 || Type == PVRType.DDS_RGBA32)
            {
                Dds image =  Dds.Create(br.BaseStream, new PfimConfig());
                
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
            else
            {
                throw new NotImplementedException();
            }
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(buffer);
        }

        void DecodeVQ(byte[] source, Color4[] palette)
        {
            int[] swizzleMap = new int[Width / 2];

            for (int i = 0; i < Width / 2; i++)
            {
                swizzleMap[i] = 0;

                for (int j = 0, k = 1; k <= i; j++, k <<= 1)
                {
                    swizzleMap[i] |= (i & k) << j;
                }
            }

            Pixels = new Color4[Width * Height];

            for (int y = 0; y < Height; y += 2)
            {
                for (int x = 0; x < Width; x += 2)
                {
                    int index = (source[(swizzleMap[x >> 1] << 1) | swizzleMap[y >> 1]]) * 4;

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

        private void Unswizzle()
        {
            int twiddleSqr = (int)(Width < Height ? Width : Height);

            int[] swizzleMap = new int[twiddleSqr];

            for (int i = 0; i < twiddleSqr; i++)
            {
                swizzleMap[i] = 0;

                for (int j = 0, k = 1; k <= i; j++, k <<= 1)
                {
                    swizzleMap[i] |= (i & k) << j;
                }
            }

            var newTexels = new Color4[Width * Height];

            int squareIndex = 0;

            for (int sqy = 0; sqy < Height; sqy += twiddleSqr)
            {
                for (int sqx = 0; sqx < Width; sqx += twiddleSqr)
                {
                    long baseIndex = sqy * Width + sqx;

                    for (int y = 0; y < twiddleSqr; y++)
                    {
                        for (int x = 0; x < twiddleSqr; x++)
                        {
                            int index = squareIndex + ((swizzleMap[x] << 1) | swizzleMap[y]);

                            long destinationIndex = baseIndex + (y * Width) + x;

                            newTexels[destinationIndex] = Pixels[index];
                        }
                    }
                    squareIndex += twiddleSqr * twiddleSqr;
                }
            }

            Pixels = newTexels;
        }

        float Comp(ushort val, int shift, int bits)
        {
            return (float)((val >> shift) & ((1 << bits) - 1)) / ((1 << bits) - 1);
        }

        Color4 ReadColor(BinaryReader br)
        {
            switch (Type)
            {
                case PVRType.RGB565:
                    {
                        ushort val = br.ReadUInt16();
                        return new Color4(Comp(val, 11, 5), Comp(val, 5, 6), Comp(val, 0, 5), 1.0f);
                    }
                case PVRType.ARGB1555:
                    {
                        ushort val = br.ReadUInt16();
                        return new Color4(Comp(val, 10, 5), Comp(val, 5, 5), Comp(val, 0, 5), Comp(val, 15, 1));
                    }
                case PVRType.ARGB4444:
                    {
                        ushort val = br.ReadUInt16();
                        return new Color4(Comp(val, 8, 4), Comp(val, 4, 4), Comp(val, 0, 4), Comp(val, 12, 4));
                    }
            }
            return Color4.Magenta;
        }

    }
}
