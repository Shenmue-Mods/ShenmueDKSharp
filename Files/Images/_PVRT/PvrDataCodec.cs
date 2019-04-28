using System;
using System.IO;

namespace ShenmueDKSharp.Files.Images._PVRT
{
    public abstract class PvrDataCodec
    {
        // Palette
        private byte[][] m_palette;

        // The pixel codec to use for this data codec.
        public PvrPixelCodec PixelCodec;

        public virtual bool VQ
        {
            get { return false; }
        }

        // Returns if we can encode using this codec.
        public abstract bool CanEncode { get; }

        // Returns the bits per pixel for this data format.
        public abstract int Bpp { get; }

        // Returns the number of palette entries for this data format.
        // Returns -1 if this is not a palettized data format.
        public virtual int PaletteEntries
        {
            get { return 0; }
        }

        // Returns if an external palette file is necessary for the texture.
        public virtual bool NeedsExternalPalette
        {
            get { return false; }
        }

        // Returns if the texture has mipmaps.
        public virtual bool HasMipmaps
        {
            get { return false; }
        }

        public void SetPalette(BinaryReader reader, int numEntries)
        {
            byte[] data = reader.ReadBytes(Bpp * numEntries);
            SetPalette(data, 0, numEntries);
        }

        public void SetPalette(byte[] palette, int offset, int numEntries)
        {
            m_palette = PixelCodec.DecodePalette(palette, offset, numEntries);
        }

        // Decode & Encode texture data
        public virtual byte[] Decode(byte[] source, int sourceIndex, int width, int height)
        {
            return Decode(source, sourceIndex, width, height, null);
        }
        public virtual byte[] Encode(byte[] source, int sourceIndex, int width, int height)
        {
            return Encode(source, width, height, null);
        }

        public byte[] Decode(BinaryReader reader, int width, int height, PvrPixelCodec pixelCodec)
        {
            double d = pixelCodec.Bpp / 8.0;
            byte[] data = reader.ReadBytes((int)(width * height * d));
            return Decode(data, 0, width, height, pixelCodec);
        }

        // Decode texture data
        public virtual byte[] Decode(byte[] input, int offset, int width, int height, PvrPixelCodec PixelCodec)
        {
            return Decode(input, offset, width, height);
        }

        // Decode a mipmap in the texture data
        //public virtual byte[] DecodeMipmap(byte[] input, int offset, int mipmap, int width, int height, VrPixelCodec PixelCodec)
        //{
        //    return Decode(input, offset, width, height, PixelCodec);
        //}
        // Encode texture data
        public virtual byte[] Encode(byte[] input, int width, int height, PvrPixelCodec PixelCodec)
        {
            return Encode(input, 0, width, height);
        }


        // Square Twiddled
        public class SquareTwiddled : PvrDataCodec
        {
            public override bool CanEncode
            {
                get { return true; }
            }

            public override int Bpp
            {
                get { return PixelCodec.Bpp; }
            }

            public override byte[] Decode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height * 4];
                int destinationIndex = 0;

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(width);
                
                // Decode texture data
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        PixelCodec.DecodePixel(source, sourceIndex + (((twiddleMap[x] << 1) | twiddleMap[y]) << (PixelCodec.Bpp >> 4)), destination, destinationIndex);
                        destinationIndex += 4;
                    }
                }

                return destination;
            }

            public override byte[] Encode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data
                byte[] destination = new byte[width * height * (PixelCodec.Bpp >> 3)];

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(width);

                // Encode texture data
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        PixelCodec.EncodePixel(source, sourceIndex, destination, ((twiddleMap[x] << 1) | twiddleMap[y]) << (PixelCodec.Bpp >> 4));
                        sourceIndex += 4;
                    }
                }

                return destination;
            }
        }

        // Square Twiddled with Mipmaps
        public class SquareTwiddledMipmaps : PvrDataCodec
        {
            public override bool CanEncode
            {
                get { return true; }
            }

            public override int Bpp
            {
                get { return PixelCodec.Bpp; }
            }

            public override bool HasMipmaps
            {
                get { return true; }
            }

            public override byte[] Decode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height * 4];
                int destinationIndex = 0;

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(width);

                // Decode texture data
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        PixelCodec.DecodePixel(source, sourceIndex + (((twiddleMap[x] << 1) | twiddleMap[y]) << (PixelCodec.Bpp >> 4)), destination, destinationIndex);
                        destinationIndex += 4;
                    }
                }

                return destination;
            }

            public override byte[] Encode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data
                byte[] destination = new byte[width * height * (PixelCodec.Bpp >> 3)];

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(width);

                // Encode texture data
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        PixelCodec.EncodePixel(source, sourceIndex, destination, ((twiddleMap[x] << 1) | twiddleMap[y]) << (PixelCodec.Bpp >> 4));
                        sourceIndex += 4;
                    }
                }

                return destination;
            }
        }

        // Vq
        public class Vq : PvrDataCodec
        {
            public override bool CanEncode
            {
                get { return true; }
            }

            public override bool VQ
            {
                get { return true; }
            }

            public override int Bpp
            {
                get { return 2; }
            }

            public override int PaletteEntries
            {
                get { return 1024; } //256 * 4 texels
            }

            public override byte[] Decode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height * 4];
                int destinationIndex;

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(width);

                // Decode texture data
                for (int y = 0; y < height; y += 2)
                {
                    for (int x = 0; x < width; x += 2)
                    {
                        int index = source[sourceIndex + ((twiddleMap[x >> 1] << 1) | twiddleMap[y >> 1])] * 4;

                        for (int x2 = 0; x2 < 2; x2++)
                        {
                            for (int y2 = 0; y2 < 2; y2++)
                            {
                                destinationIndex = ((((y + y2) * width) + (x + x2)) * 4);

                                //Write 2x2 codebook entry
                                for (int i = 0; i < 4; i++)
                                {
                                    destination[destinationIndex] = m_palette[index][i];
                                    destinationIndex++;
                                }

                                index++;
                            }
                        }
                    }
                }

                return destination;
            }

            public override byte[] Encode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                int compressedWidth = (int)(width / 2.0);
                int compressedHeight = (int)(height / 2.0);
                byte[] destination = new byte[compressedWidth * compressedHeight];
                int destinationIndex = 0;

                // Get the size of each block to process.
                int size = Math.Min(compressedWidth, compressedHeight);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Encode texture data
                for (int y = 0; y < compressedHeight; y++)
                {
                    for (int x = 0; x < compressedWidth; x++)
                    {
                        destinationIndex = (twiddleMap[x] << 1) | twiddleMap[y];
                        destination[destinationIndex] = source[sourceIndex];
                        sourceIndex++;
                        //destinationIndex++;
                    }
                }
                return destination;
            }
        }

        // Vq with Mipmaps
        public class VqMipmaps : PvrDataCodec
        {
            public override bool CanEncode
            {
                get { return true; }
            }

            public override bool VQ
            {
                get { return true; }
            }

            public override int Bpp
            {
                get { return 2; }
            }

            public override int PaletteEntries
            {
                get { return 1024; } // Actually 256
            }

            public override bool HasMipmaps
            {
                get { return true; }
            }

            public override byte[] Decode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height * 4];
                int destinationIndex;

                // Decode a 1x1 texture (for mipmaps)
                // No need to make use of a twiddle map in this case
                if (width == 1 && height == 1)
                {
                    int index = source[sourceIndex] * 4;

                    destinationIndex = 0;

                    for (int i = 0; i < 4; i++)
                    {
                        destination[destinationIndex] = m_palette[index][i];
                        destinationIndex++;
                    }

                    return destination;
                }

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(width);

                // Decode texture data
                for (int y = 0; y < height; y += 2)
                {
                    for (int x = 0; x < width; x += 2)
                    {
                        int index = source[sourceIndex + ((twiddleMap[x >> 1] << 1) | twiddleMap[y >> 1])] * 4;

                        for (int x2 = 0; x2 < 2; x2++)
                        {
                            for (int y2 = 0; y2 < 2; y2++)
                            {
                                destinationIndex = ((((y + y2) * width) + (x + x2)) * 4);

                                for (int i = 0; i < 4; i++)
                                {
                                    destination[destinationIndex] = m_palette[index][i];
                                    destinationIndex++;
                                }

                                index++;
                            }
                        }
                    }
                }

                return destination;
            }

            public override byte[] Encode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                int compressedWidth = (int)(width * Bpp / 8.0);
                int compressedHeight = (int)(height * Bpp / 8.0);
                byte[] destination = new byte[compressedWidth * compressedHeight];
                int destinationIndex = 0;

                // Get the size of each block to process.
                int size = Math.Min(compressedWidth, compressedHeight);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Encode texture data
                for (int y = 0; y < compressedHeight; y++)
                {
                    for (int x = 0; x < compressedWidth; x++)
                    {
                        destinationIndex = (twiddleMap[x] << 1) | twiddleMap[y];
                        destination[destinationIndex] = source[sourceIndex];
                        sourceIndex++;
                    }
                }
                return destination;
            }
        }

        // 4-bit Indexed with External Palette
        public class Index4 : PvrDataCodec
        {
            public override bool CanEncode
            {
                get { return true; }
            }

            public override int Bpp
            {
                get { return 4; }
            }

            public override int PaletteEntries
            {
                get { return 16; }
            }

            public override bool NeedsExternalPalette
            {
                get { return true; }
            }

            public override byte[] Decode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height * 4];
                int destinationIndex;

                // Get the size of each block to process.
                int size = Math.Min(width, height);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Decode texture data
                for (int y = 0; y < height; y += size)
                {
                    for (int x = 0; x < width; x += size)
                    {
                        for (int y2 = 0; y2 < size; y2++)
                        {
                            for (int x2 = 0; x2 < size; x2++)
                            {
                                byte index = (byte)((source[sourceIndex + (((twiddleMap[x2] << 1) | twiddleMap[y2]) >> 1)] >> ((y2 & 0x1) * 4)) & 0xF);
                                destinationIndex = ((((y + y2) * width) + (x + x2)) * 4);

                                for (int i = 0; i < 4; i++)
                                {
                                    destination[destinationIndex] = m_palette[index][i];
                                    destinationIndex++;
                                }
                            }
                        }

                        sourceIndex += (size * size) >> 1;
                    }
                }

                return destination;
            }

            public override byte[] Encode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[(width * height) >> 1];
                int destinationIndex = 0;

                // Get the size of each block to process.
                int size = Math.Min(width, height);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Encode texture data
                for (int y = 0; y < height; y += size)
                {
                    for (int x = 0; x < width; x += size)
                    {
                        for (int y2 = 0; y2 < size; y2++)
                        {
                            for (int x2 = 0; x2 < size; x2++)
                            {
                                byte index = destination[destinationIndex + (((twiddleMap[x2] << 1) | twiddleMap[y2]) >> 1)];
                                index |= (byte)((source[sourceIndex + (((y + y2) * width) + (x + x2))] & 0xF) << ((y2 & 0x1) * 4));

                                destination[destinationIndex + (((twiddleMap[x2] << 1) | twiddleMap[y2]) >> 1)] = index;
                            }
                        }

                        destinationIndex += (size * size) >> 1;
                    }
                }

                return destination;
            }
        }

        // 4-bit Indexed with External Palette
        public class Index4Mipmap : PvrDataCodec
        {
            public override bool CanEncode
            {
                get { return true; }
            }

            public override int Bpp
            {
                get { return 4; }
            }

            public override bool HasMipmaps
            {
                get { return true; }
            }

            public override int PaletteEntries
            {
                get { return 16; }
            }

            public override bool NeedsExternalPalette
            {
                get { return true; }
            }

            public override byte[] Decode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height * 4];
                int destinationIndex;

                // Get the size of each block to process.
                int size = Math.Min(width, height);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Decode texture data
                for (int y = 0; y < height; y += size)
                {
                    for (int x = 0; x < width; x += size)
                    {
                        for (int y2 = 0; y2 < size; y2++)
                        {
                            for (int x2 = 0; x2 < size; x2++)
                            {
                                byte index = (byte)((source[sourceIndex + (((twiddleMap[x2] << 1) | twiddleMap[y2]) >> 1)] >> ((y2 & 0x1) * 4)) & 0xF);
                                destinationIndex = ((((y + y2) * width) + (x + x2)) * 4);

                                for (int i = 0; i < 4; i++)
                                {
                                    destination[destinationIndex] = m_palette[index][i];
                                    destinationIndex++;
                                }
                            }
                        }

                        sourceIndex += (size * size) >> 1;
                    }
                }

                return destination;
            }

            public override byte[] Encode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[(width * height) >> 1];
                int destinationIndex = 0;

                // Get the size of each block to process.
                int size = Math.Min(width, height);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Encode texture data
                for (int y = 0; y < height; y += size)
                {
                    for (int x = 0; x < width; x += size)
                    {
                        for (int y2 = 0; y2 < size; y2++)
                        {
                            for (int x2 = 0; x2 < size; x2++)
                            {
                                byte index = destination[destinationIndex + (((twiddleMap[x2] << 1) | twiddleMap[y2]) >> 1)];
                                index |= (byte)((source[sourceIndex + (((y + y2) * width) + (x + x2))] & 0xF) << ((y2 & 0x1) * 4));

                                destination[destinationIndex + (((twiddleMap[x2] << 1) | twiddleMap[y2]) >> 1)] = index;
                            }
                        }

                        destinationIndex += (size * size) >> 1;
                    }
                }

                return destination;
            }
        }

        // 8-bit Indexed with External Palette
        public class Index8 : PvrDataCodec
        {
            public override bool CanEncode
            {
                get { return true; }
            }

            public override int Bpp
            {
                get { return 8; }
            }

            public override int PaletteEntries
            {
                get { return 256; }
            }

            public override bool NeedsExternalPalette
            {
                get { return true; }
            }

            public override byte[] Decode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height * 4];
                int destinationIndex;

                // Get the size of each block to process.
                int size = Math.Min(width, height);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Decode texture data
                for (int y = 0; y < height; y += size)
                {
                    for (int x = 0; x < width; x += size)
                    {
                        for (int y2 = 0; y2 < size; y2++)
                        {
                            for (int x2 = 0; x2 < size; x2++)
                            {
                                byte index = source[sourceIndex + ((twiddleMap[x2] << 1) | twiddleMap[y2])];
                                destinationIndex = ((((y + y2) * width) + (x + x2)) * 4);

                                for (int i = 0; i < 4; i++)
                                {
                                    destination[destinationIndex] = m_palette[index][i];
                                    destinationIndex++;
                                }
                            }
                        }

                        sourceIndex += (size * size);
                    }
                }

                return destination;
            }

            public override byte[] Encode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height];
                int destinationIndex = 0;

                // Get the size of each block to process.
                int size = Math.Min(width, height);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Encode texture data
                for (int x = 0; x < width; x += size)
                {
                    for (int y = 0; y < height; y += size)
                    {
                        for (int y2 = 0; y2 < size; y2++)
                        {
                            for (int x2 = 0; x2 < size; x2++)
                            {
                                destination[destinationIndex + ((twiddleMap[x2] << 1) | twiddleMap[y2])] = source[sourceIndex + (((y + y2) * width) + (x + x2))];
                            }
                        }

                        destinationIndex += (size * size);
                    }
                }

                return destination;
            }
        }

        // 8-bit Indexed with External Palette
        public class Index8Mipmap : PvrDataCodec
        {
            public override bool CanEncode
            {
                get { return true; }
            }

            public override int Bpp
            {
                get { return 8; }
            }

            public override bool HasMipmaps
            {
                get { return true; }
            }

            public override int PaletteEntries
            {
                get { return 256; }
            }

            public override bool NeedsExternalPalette
            {
                get { return true; }
            }

            public override byte[] Decode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height * 4];
                int destinationIndex;

                // Get the size of each block to process.
                int size = Math.Min(width, height);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Decode texture data
                for (int y = 0; y < height; y += size)
                {
                    for (int x = 0; x < width; x += size)
                    {
                        for (int y2 = 0; y2 < size; y2++)
                        {
                            for (int x2 = 0; x2 < size; x2++)
                            {
                                byte index = source[sourceIndex + ((twiddleMap[x2] << 1) | twiddleMap[y2])];
                                destinationIndex = ((((y + y2) * width) + (x + x2)) * 4);

                                for (int i = 0; i < 4; i++)
                                {
                                    destination[destinationIndex] = m_palette[index][i];
                                    destinationIndex++;
                                }
                            }
                        }

                        sourceIndex += (size * size);
                    }
                }

                return destination;
            }

            public override byte[] Encode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height];
                int destinationIndex = 0;

                // Get the size of each block to process.
                int size = Math.Min(width, height);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Encode texture data
                for (int x = 0; x < width; x += size)
                {
                    for (int y = 0; y < height; y += size)
                    {
                        for (int y2 = 0; y2 < size; y2++)
                        {
                            for (int x2 = 0; x2 < size; x2++)
                            {
                                destination[destinationIndex + ((twiddleMap[x2] << 1) | twiddleMap[y2])] = source[sourceIndex + (((y + y2) * width) + (x + x2))];
                            }
                        }

                        destinationIndex += (size * size);
                    }
                }

                return destination;
            }
        }

        // Rectangle
        public class Rectangle : PvrDataCodec
        {
            public override bool CanEncode
            {
                get { return true; }
            }

            public override int Bpp
            {
                get { return PixelCodec.Bpp; }
            }

            public override byte[] Decode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height * 4];
                int destinationIndex = 0;

                // Decode texture data
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        PixelCodec.DecodePixel(source, sourceIndex, destination, destinationIndex);
                        sourceIndex += (PixelCodec.Bpp >> 3);
                        destinationIndex += 4;
                    }
                }

                return destination;
            }

            public override byte[] Encode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height * (PixelCodec.Bpp >> 3)];
                int destinationIndex = 0;

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(width);

                // Encode texture data
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        PixelCodec.EncodePixel(source, sourceIndex, destination, destinationIndex);
                        sourceIndex += 4;
                        destinationIndex += (PixelCodec.Bpp >> 3);
                    }
                }

                return destination;
            }
        }

        // Rectangle Twiddled
        public class RectangleTwiddled : PvrDataCodec
        {
            public override bool CanEncode
            {
                get { return true; }
            }

            public override int Bpp
            {
                get { return PixelCodec.Bpp; }
            }

            public override byte[] Decode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data
                byte[] destination = new byte[width * height * 4];

                // Get the size of each block to process.
                int size = Math.Min(width, height);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Decode texture data
                for (int y = 0; y < height; y += size)
                {
                    for (int x = 0; x < width; x += size)
                    {
                        for (int y2 = 0; y2 < size; y2++)
                        {
                            for (int x2 = 0; x2 < size; x2++)
                            {
                                PixelCodec.DecodePixel(source, sourceIndex + (((twiddleMap[x2] << 1) | twiddleMap[y2]) << (PixelCodec.Bpp >> 4)), destination, ((((y + y2) * width) + (x + x2)) * 4));
                            }
                        }

                        sourceIndex += (size * size) * (PixelCodec.Bpp >> 3);
                    }
                }

                return destination;
            }

            public override byte[] Encode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height * (PixelCodec.Bpp >> 3)];
                int destinationIndex = 0;

                // Get the size of each block to process.
                int size = Math.Min(width, height);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Encode texture data
                for (int y = 0; y < height; y += size)
                {
                    for (int x = 0; x < width; x += size)
                    {
                        for (int y2 = 0; y2 < size; y2++)
                        {
                            for (int x2 = 0; x2 < size; x2++)
                            {
                                PixelCodec.EncodePixel(source, sourceIndex + ((((y + y2) * width) + (x + x2)) * 4), destination, destinationIndex + (((twiddleMap[x2] << 1) | twiddleMap[y2]) << (PixelCodec.Bpp >> 4)));
                            }
                        }

                        destinationIndex += (size * size) * (PixelCodec.Bpp >> 3);
                    }
                }

                return destination;
            }
        }

        // Small Vq
        public class SmallVq : PvrDataCodec
        {
            public override bool CanEncode
            {
                get { return true; }
            }

            public override bool VQ
            {
                get { return true; }
            }

            public override int Bpp
            {
                get { return 2; }
            }

            public override int PaletteEntries
            {
                get { return 1024; } // Varies, 1024 (actually 256) is the largest, the number various based on its size
            }

            public override byte[] Decode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height * 4];
                int destinationIndex;

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(width);

                // Decode texture data
                for (int y = 0; y < height; y += 2)
                {
                    for (int x = 0; x < width; x += 2)
                    {
                        int index = (source[sourceIndex + ((twiddleMap[x >> 1] << 1) | twiddleMap[y >> 1])]) * 4;

                        for (int x2 = 0; x2 < 2; x2++)
                        {
                            for (int y2 = 0; y2 < 2; y2++)
                            {
                                destinationIndex = ((((y + y2) * width) + (x + x2)) * 4);

                                for (int i = 0; i < 4; i++)
                                {
                                    destination[destinationIndex] = m_palette[index][i];
                                    destinationIndex++;
                                }

                                index++;
                            }
                        }
                    }
                }

                return destination;
            }

            public override byte[] Encode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                int compressedWidth = (int)(width * Bpp / 8.0);
                int compressedHeight = (int)(height * Bpp / 8.0);
                byte[] destination = new byte[compressedWidth * compressedHeight];
                int destinationIndex = 0;

                // Get the size of each block to process.
                int size = Math.Min(compressedWidth, compressedHeight);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Encode texture data
                for (int y = 0; y < compressedHeight; y++)
                {
                    for (int x = 0; x < compressedWidth; x++)
                    {
                        destinationIndex = (twiddleMap[x] << 1) | twiddleMap[y];
                        destination[destinationIndex] = source[sourceIndex];
                        sourceIndex++;
                    }
                }
                return destination;
            }
        }

        // Small Vq with Mipmaps
        public class SmallVqMipmaps : PvrDataCodec
        {
            public override bool CanEncode
            {
                get { return true; }
            }

            public override bool VQ
            {
                get { return true; }
            }

            public override int Bpp
            {
                get { return 2; }
            }

            public override int PaletteEntries
            {
                get { return 1024; } // Varies, 1024 (actually 256) is the largest
            }

            public override bool HasMipmaps
            {
                get { return true; }
            }

            public override byte[] Decode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                byte[] destination = new byte[width * height * 4];
                int destinationIndex;

                // Decode a 1x1 texture (for mipmaps)
                // No need to make use of a twiddle map in this case
                if (width == 1 && height == 1)
                {
                    int index = source[sourceIndex] * 4;

                    destinationIndex = 0;

                    for (int i = 0; i < 4; i++)
                    {
                        destination[destinationIndex] = m_palette[index][i];
                        destinationIndex++;
                    }

                    return destination;
                }

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(width);

                // Decode texture data
                for (int y = 0; y < height; y += 2)
                {
                    for (int x = 0; x < width; x += 2)
                    {
                        int index = (source[sourceIndex + ((twiddleMap[x >> 1] << 1) | twiddleMap[y >> 1])]) * 4;

                        for (int x2 = 0; x2 < 2; x2++)
                        {
                            for (int y2 = 0; y2 < 2; y2++)
                            {
                                destinationIndex = ((((y + y2) * width) + (x + x2)) * 4);

                                for (int i = 0; i < 4; i++)
                                {
                                    destination[destinationIndex] = m_palette[index][i];
                                    destinationIndex++;
                                }

                                index++;
                            }
                        }
                    }
                }

                return destination;
            }

            public override byte[] Encode(byte[] source, int sourceIndex, int width, int height)
            {
                // Destination data & index
                int compressedWidth = (int)(width * Bpp / 8.0);
                int compressedHeight = (int)(height * Bpp / 8.0);
                byte[] destination = new byte[compressedWidth * compressedHeight];
                int destinationIndex = 0;

                // Get the size of each block to process.
                int size = Math.Min(compressedWidth, compressedHeight);

                // Twiddle map
                int[] twiddleMap = MakeTwiddleMap(size);

                // Encode texture data
                for (int y = 0; y < compressedHeight; y++)
                {
                    for (int x = 0; x < compressedWidth; x++)
                    {
                        destinationIndex = (twiddleMap[x] << 1) | twiddleMap[y];
                        destination[destinationIndex] = source[sourceIndex];
                        sourceIndex++;
                    }
                }
                return destination;
            }
        }

        // Makes a twiddle map for the specified size texture
        private int[] MakeTwiddleMap(int size)
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

        public static PvrDataCodec GetDataCodec(PvrDataFormat format)
        {
            switch (format)
            {
                case PvrDataFormat.SQUARE_TWIDDLED:
                    return new SquareTwiddled();
                case PvrDataFormat.SQUARE_TWIDDLED_MIPMAP:
                case PvrDataFormat.SQUARE_TWIDDLED_MIPMAP_ALT:
                    return new SquareTwiddledMipmaps();
                case PvrDataFormat.VECTOR_QUANTIZATION:
                    return new Vq();
                case PvrDataFormat.VECTOR_QUANTIZATION_MIPMAP:
                    return new VqMipmaps();
                case PvrDataFormat.PALETTIZE_4BIT:
                    return new Index4();
                case PvrDataFormat.PALETTIZE_4BIT_MIPMAP:
                    return new Index4Mipmap();
                case PvrDataFormat.PALETTIZE_8BIT:
                    return new Index8();
                case PvrDataFormat.PALETTIZE_8BIT_MIPMAP:
                    return new Index8Mipmap();
                case PvrDataFormat.RAW:
                case PvrDataFormat.RECTANGLE:
                case PvrDataFormat.RECTANGLE_STRIDE:
                    return new Rectangle();
                case PvrDataFormat.RECTANGLE_TWIDDLED:
                    return new RectangleTwiddled();
                case PvrDataFormat.VECTOR_QUANTIZATION_SMALL:
                    return new SmallVq();
                case PvrDataFormat.VECTOR_QUANTIZATION_SMALL_MIPMAP:
                    return new SmallVqMipmaps();
            }

            return null;
        }
    }
}