using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using ShenmueDKSharp.Files.Images._PVRT.WQuantizer;

namespace ShenmueDKSharp.Files.Images._PVRT
{
    public class PvrTextureEncoder
    {
        private byte[] m_decodedData; // Decoded texture data (either 32-bit RGBA or 8-bit indexed)
        private Bitmap m_decodedBitmap; // Decoded bitmap
        private byte[][] m_texturePalette; // The texture's palette
        private QuantizedPalette m_palette;
        private VQCodeBook m_codeBook;

        public PvrDataCodec DataCodec { get; private set; }
        public PvrPixelCodec PixelCodec { get; private set; }

        /// <summary>
        /// Returns if the texture has mipmaps.
        /// </summary>
        public bool HasMipmaps => DataCodec.HasMipmaps;

        /// <summary>
        /// Returns if the texture needs an external palette file.
        /// </summary>
        public bool NeedsExternalPalette => DataCodec.NeedsExternalPalette;

        public PvrCompressionCodec CompressionCodec { get; set; }

        /// <summary>
        /// Returns the palette encoder if this texture uses an external palette file.
        /// </summary>
        public PvpPaletteEncoder PaletteEncoder { get; set; }

        /// <summary>
        /// The texture's compression format. The default value is PvrCompressionFormat.None.
        /// </summary>
        public PvrCompressionFormat CompressionFormat { get; set; }

        /// <summary>
        /// The texture's pixel format.
        /// </summary>
        public PvrPixelFormat PixelFormat { get; set; }

        /// <summary>
        /// The texture's data format.
        /// </summary>
        public PvrDataFormat DataFormat { get; set; }

        /// <summary>
        /// Indicates whether or not this texture has a global index. If false, the texture will not include a GBIX header. The default value is true.
        /// </summary>
        public bool HasGlobalIndex { get; set; } = true;

        /// <summary>
        /// Sets the texture's global index. This only matters if HasGlobalIndex is true. The default value is 0.
        /// </summary>
        public uint GlobalIndex { get; set; } = 0;

        /// <summary>
        /// Width of the texture (in pixels).
        /// </summary>
        public ushort TextureWidth { get; set; }

        /// <summary>
        /// Height of the texture (in pixels).
        /// </summary>
        public ushort TextureHeight { get; set; }

        /// <summary>
        /// Opens a texture to encode from a file.
        /// </summary>
        /// <param name="file">Filename of the file that contains the texture data.</param>
        /// <param name="pixelFormat">Pixel format to encode the texture to.</param>
        /// <param name="dataFormat">Data format to encode the texture to.</param>
        public PvrTextureEncoder(string file, PvrPixelFormat pixelFormat, PvrDataFormat dataFormat)
        {
            Initalize(new Bitmap(file));

            if (m_decodedBitmap != null)
            {
                Initalize(pixelFormat, dataFormat);
            }
        }

        /// <summary>
        /// Opens a texture to encode from a byte array.
        /// </summary>
        /// <param name="source">Byte array that contains the texture data.</param>
        /// <param name="pixelFormat">Pixel format to encode the texture to.</param>
        /// <param name="dataFormat">Data format to encode the texture to.</param>
        public PvrTextureEncoder(byte[] source, PvrPixelFormat pixelFormat, PvrDataFormat dataFormat)
        {
            MemoryStream buffer = new MemoryStream();
            buffer.Write(source, 0, source.Length);
            Initalize(new Bitmap(buffer));

            if (m_decodedBitmap != null)
            {
                Initalize(pixelFormat, dataFormat);
            }
        }

        /// <summary>
        /// Opens a texture to encode from a byte array.
        /// </summary>
        /// <param name="source">Byte array that contains the texture data.</param>
        /// <param name="offset">Offset of the texture in the array.</param>
        /// <param name="length">Number of bytes to read.</param>
        /// <param name="pixelFormat">Pixel format to encode the texture to.</param>
        /// <param name="dataFormat">Data format to encode the texture to.</param>
        public PvrTextureEncoder(byte[] source, int offset, int length, PvrPixelFormat pixelFormat, PvrDataFormat dataFormat)
        {
            MemoryStream buffer = new MemoryStream();
            buffer.Write(source, offset, length);
            Initalize(new Bitmap(buffer));

            if (m_decodedBitmap != null)
            {
                Initalize(pixelFormat, dataFormat);
            }
        }

        /// <summary>
        /// Opens a texture to encode from a stream.
        /// </summary>
        /// <param name="source">Stream that contains the texture data.</param>
        /// <param name="pixelFormat">Pixel format to encode the texture to.</param>
        /// <param name="dataFormat">Data format to encode the texture to.</param>
        public PvrTextureEncoder(Stream source, PvrPixelFormat pixelFormat, PvrDataFormat dataFormat)
        {
            MemoryStream buffer = new MemoryStream();
            PTStream.CopyPartTo(source, buffer, (int)(source.Length - source.Position));
            Initalize(new Bitmap(buffer));

            if (m_decodedBitmap != null)
            {
                Initalize(pixelFormat, dataFormat);
            }
        }

        /// <summary>
        /// Opens a texture to encode from a stream.
        /// </summary>
        /// <param name="source">Stream that contains the texture data.</param>
        /// <param name="length">Number of bytes to read.</param>
        /// <param name="pixelFormat">Pixel format to encode the texture to.</param>
        /// <param name="dataFormat">Data format to encode the texture to.</param>
        public PvrTextureEncoder(Stream source, int length, PvrPixelFormat pixelFormat, PvrDataFormat dataFormat)
        {
            MemoryStream buffer = new MemoryStream();
            PTStream.CopyPartTo(source, buffer, length);
            Initalize(new Bitmap(buffer));

            if (m_decodedBitmap != null)
            {
                Initalize(pixelFormat, dataFormat);
            }
        }

        /// <summary>
        /// Opens a texture to encode from a bitmap.
        /// </summary>
        /// <param name="source">Bitmap to encode.</param>
        /// <param name="pixelFormat">Pixel format to encode the texture to.</param>
        /// <param name="dataFormat">Data format to encode the texture to.</param>
        public PvrTextureEncoder(Bitmap source, PvrPixelFormat pixelFormat, PvrDataFormat dataFormat)
        {
            Initalize(source);

            if (m_decodedBitmap != null)
            {
                Initalize(pixelFormat, dataFormat);
            }
        }

        // Returns if the texture dimensuons are valid
        private bool HasValidDimensions(int width, int height)
        {
            if (width < 8 || height < 8 || width > 1024 || height > 1024)
                return false;

            if ((width & (width - 1)) != 0 || (height & (height - 1)) != 0)
                return false;

            return true;
        }

        /// <summary>
        /// Returns the encoded texture as a byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            return EncodeTexture().ToArray();
        }

        /// <summary>
        /// Returns the encoded texture as a stream.
        /// </summary>
        /// <returns></returns>
        public MemoryStream ToStream()
        {
            MemoryStream textureStream = EncodeTexture();
            textureStream.Position = 0;
            return textureStream;
        }

        /// <summary>
        /// Saves the encoded texture to the specified path.
        /// </summary>
        /// <param name="path">Name of the file to save the data to.</param>
        public void Save(string path)
        {
            using (FileStream destination = File.Create(path))
            {
                MemoryStream textureStream = EncodeTexture();
                textureStream.Position = 0;
                PTStream.CopyTo(textureStream, destination);
            }
        }

        /// <summary>
        /// Saves the encoded texture to the specified stream.
        /// </summary>
        /// <param name="destination">The stream to save the texture to.</param>
        public void Save(Stream destination)
        {
            MemoryStream textureStream = EncodeTexture();
            textureStream.Position = 0;
            PTStream.CopyTo(textureStream, destination);
        }

        private void Initalize(Bitmap source)
        {
            // Make sure this bitmap's dimensions are valid
            if (!HasValidDimensions(source.Width, source.Height))
                return;

            try
            {
                m_decodedBitmap = source;

                TextureWidth = (ushort)source.Width;
                TextureHeight = (ushort)source.Height;
            }
            catch
            {
                m_decodedBitmap = null;

                TextureWidth = 0;
                TextureHeight = 0;
            }
        }

        private bool Initalize(PvrPixelFormat pixelFormat, PvrDataFormat dataFormat)
        {
            // Set the default values
            HasGlobalIndex = true;
            GlobalIndex = 0;
            CompressionFormat = PvrCompressionFormat.NONE;

            // Set the data format and pixel format and load the appropiate codecs
            this.PixelFormat = pixelFormat;
            PixelCodec = PvrPixelCodec.GetPixelCodec(pixelFormat);

            this.DataFormat = dataFormat;
            DataCodec = PvrDataCodec.GetDataCodec(dataFormat);

            // Make sure the pixel and data codecs exists and we can encode to it
            if (PixelCodec == null || !PixelCodec.CanEncode) return false;
            if (DataCodec == null || !DataCodec.CanEncode) return false;
            DataCodec.PixelCodec = PixelCodec;

            if (DataCodec.PaletteEntries != 0)
            {
                if (DataCodec.VQ)
                {
                    m_decodedData = BitmapToRawVQ(m_decodedBitmap, DataCodec.PaletteEntries, out m_texturePalette);
                }
                else
                {
                    // Convert the bitmap to an array containing indicies.
                    m_decodedData = BitmapToRawIndexed(m_decodedBitmap, DataCodec.PaletteEntries, out m_texturePalette);

                    // If this texture has an external palette file, set up the palette encoder
                    if (DataCodec.NeedsExternalPalette)
                    {
                        PaletteEncoder = new PvpPaletteEncoder(m_texturePalette, (ushort)DataCodec.PaletteEntries, pixelFormat, PixelCodec);
                    }
                }
            }
            else
            {
                // Convert the bitmap to an array
                m_decodedData = BitmapToRaw(m_decodedBitmap);
            }

            return true;
        }

        protected byte[] BitmapToRaw(Bitmap source)
        {
            Bitmap img = source;
            byte[] destination = new byte[img.Width * img.Height * 4];

            // If this is not a 32-bit ARGB bitmap, convert it to one
            if (img.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                Bitmap newImage = new Bitmap(img.Width, img.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(newImage))
                {
                    g.DrawImage(img, 0, 0, img.Width, img.Height);
                }
                img = newImage;
            }

            // Copy over the data to the destination. It's ok to do it without utilizing Stride
            // since each pixel takes up 4 bytes (aka Stride will always be equal to Width)
            BitmapData bitmapData = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, img.PixelFormat);
            Marshal.Copy(bitmapData.Scan0, destination, 0, destination.Length);
            img.UnlockBits(bitmapData);

            return destination;
        }

        // Since this method is only used for mipmaps, and mipmaps are square, we can assume that width = height
        protected byte[] BitmapToRawResized(Bitmap source, int size, int minSize)
        {
            if (size > minSize)
                minSize = size;

            byte[] destination = new byte[minSize * minSize * 4];

            // Resize the image
            Bitmap img = new Bitmap(minSize, minSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(img))
            {
                using (ImageAttributes attr = new ImageAttributes())
                {
                    attr.SetWrapMode(WrapMode.TileFlipXY);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(source, new Rectangle(0, 0, size, size), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attr);
                }
            }

            // Copy over the data to the destination. It's ok to do it without utilizing Stride
            // since each pixel takes up 4 bytes (aka Stride will always be equal to Width)
            BitmapData bitmapData = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, img.PixelFormat);
            Marshal.Copy(bitmapData.Scan0, destination, 0, destination.Length);
            img.UnlockBits(bitmapData);

            return destination;
        }


        private byte[] BitmapToRawVQ(Bitmap source, int codeBookSize, out byte[][] palette)
        {
            Bitmap img = source;
            byte[] destination = new byte[img.Width * img.Height];

            // If this is not a 32-bit ARGB bitmap, convert it to one
            if (img.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                Bitmap newImage = new Bitmap(img.Width, img.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(newImage))
                {
                    g.DrawImage(img, 0, 0, img.Width, img.Height);
                }
                img = newImage;
            }

            //Create code book
            m_codeBook = VectorQuantizer.CreateCodebook(img, codeBookSize / 4);

            //Write palette
            palette = new byte[codeBookSize][];
            for (int i = 0; i < m_codeBook.Entries.Length; i++)
            {
                byte[] pixels = m_codeBook.Entries[i].ToArray();
                for (int j = 0; j < 4; j++)
                {
                    int paletteIndex = i * 4 + j;
                    palette[paletteIndex] = new byte[4];
                    palette[paletteIndex][0] = pixels[j * 4 + 3];
                    palette[paletteIndex][1] = pixels[j * 4 + 2];
                    palette[paletteIndex][2] = pixels[j * 4 + 1];
                    palette[paletteIndex][3] = pixels[j * 4];
                }
            }

            //Quantize image
            return VectorQuantizer.QuantizeImage(img, m_codeBook);
        }

        private byte[] BitmapToRawVQResized(Bitmap source, int size, int minSize, VQCodeBook codeBook)
        {
            if (size > minSize)
                minSize = size;

            // Resize the image
            Bitmap img = new Bitmap(minSize, minSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(img))
            {
                using (ImageAttributes attr = new ImageAttributes())
                {
                    attr.SetWrapMode(WrapMode.TileFlipXY);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(source, new Rectangle(0, 0, size, size), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attr);
                }
            }
            return VectorQuantizer.QuantizeImage(img, codeBook);
        }

        protected unsafe byte[] BitmapToRawIndexed(Bitmap source, int maxColors, out byte[][] palette)
        {
            Bitmap img = source;
            byte[] destination = new byte[img.Width * img.Height];

            // If this is not a 32-bit ARGB bitmap, convert it to one
            if (img.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                Bitmap newImage = new Bitmap(img.Width, img.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(newImage))
                {
                    g.DrawImage(img, 0, 0, img.Width, img.Height);
                }
                img = newImage;
            }

            // Quantize the image
            WQuantizer.WuQuantizer quantizer = new WQuantizer.WuQuantizer();
            m_palette = quantizer.CreatePalette(img, maxColors);
            img = (Bitmap)quantizer.QuantizeImage(img, m_palette);

            // Copy over the data to the destination. We need to use Stride in this case, as it may not
            // always be equal to Width.
            BitmapData bitmapData = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, img.PixelFormat);

            byte* pointer = (byte*)bitmapData.Scan0;
            for (int y = 0; y < bitmapData.Height; y++)
            {
                for (int x = 0; x < bitmapData.Width; x++)
                {
                    destination[(y * img.Width) + x] = pointer[(y * bitmapData.Stride) + x];
                }
            }

            img.UnlockBits(bitmapData);

            // Copy over the palette
            palette = new byte[maxColors][];
            for (int i = 0; i < maxColors; i++)
            {
                palette[i] = new byte[4];

                palette[i][3] = img.Palette.Entries[i].A;
                palette[i][2] = img.Palette.Entries[i].R;
                palette[i][1] = img.Palette.Entries[i].G;
                palette[i][0] = img.Palette.Entries[i].B;
            }

            return destination;
        }

        protected unsafe byte[] BitmapToRawIndexedResized(Bitmap source, int size, int minSize, QuantizedPalette palette)
        {
            if (size > minSize)
                minSize = size;

            byte[] destination = new byte[minSize * minSize * 4];

            // Resize the image
            Bitmap img = new Bitmap(minSize, minSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(img))
            {
                using (ImageAttributes attr = new ImageAttributes())
                {
                    attr.SetWrapMode(WrapMode.TileFlipXY);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(source, new Rectangle(0, 0, size, size), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attr);
                }
            }

            // Quantize the image
            WQuantizer.WuQuantizer quantizer = new WQuantizer.WuQuantizer();
            img = (Bitmap)quantizer.QuantizeImage(img, palette);

            // Copy over the data to the destination. We need to use Stride in this case, as it may not
            // always be equal to Width.
            BitmapData bitmapData = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly, img.PixelFormat);

            byte* pointer = (byte*)bitmapData.Scan0;
            for (int y = 0; y < bitmapData.Height; y++)
            {
                for (int x = 0; x < bitmapData.Width; x++)
                {
                    destination[(y * img.Width) + x] = pointer[(y * bitmapData.Stride) + x];
                }
            }

            img.UnlockBits(bitmapData);
            return destination;
        }

        public MemoryStream EncodeTexture()
        {
            // Calculate what the length of the texture will be
            int textureLength = 16 + (TextureWidth * TextureHeight * DataCodec.Bpp / 8);
            if (HasGlobalIndex)
            {
                textureLength += 16;
            }
            if (DataCodec.PaletteEntries != 0 && !DataCodec.NeedsExternalPalette)
            {
                textureLength += (DataCodec.PaletteEntries * PixelCodec.Bpp / 8);
            }

            // Calculate the mipmap padding (if the texture contains mipmaps)
            int mipmapPadding = 0;

            if (DataCodec.HasMipmaps)
            {
                if (DataFormat == PvrDataFormat.SQUARE_TWIDDLED_MIPMAP)
                {
                    // A 1x1 mipmap takes up as much space as a 2x1 mipmap
                    // There are also 4 extra bytes at the end of the file
                    mipmapPadding = (DataCodec.Bpp) >> 3;
                    textureLength += 4;
                }
                else if (DataFormat == PvrDataFormat.SQUARE_TWIDDLED_MIPMAP_ALT)
                {
                    // A 1x1 mipmap takes up as much space as a 2x2 mipmap
                    mipmapPadding = (3 * DataCodec.Bpp) >> 3;
                }

                textureLength += mipmapPadding;

                for (int size = 1; size < TextureWidth; size <<= 1)
                {
                    textureLength += Math.Max((size * size * DataCodec.Bpp) >> 3, 1);
                }
            }

            MemoryStream destination = new MemoryStream(textureLength);

            // Write out the GBIX header (if we are including one)
            if (HasGlobalIndex)
            {
                destination.WriteByte((byte)'G');
                destination.WriteByte((byte)'B');
                destination.WriteByte((byte)'I');
                destination.WriteByte((byte)'X');

                PTStream.WriteUInt32(destination, 8);
                PTStream.WriteUInt32(destination, GlobalIndex);
                PTStream.WriteUInt32(destination, 0);
            }

            // Write out the PVRT header
            destination.WriteByte((byte)'P');
            destination.WriteByte((byte)'V');
            destination.WriteByte((byte)'R');
            destination.WriteByte((byte)'T');

            if (HasGlobalIndex)
            {
                PTStream.WriteInt32(destination, textureLength - 24);
            }
            else
            {
                PTStream.WriteInt32(destination, textureLength - 8);
            }

            destination.WriteByte((byte)PixelFormat);
            destination.WriteByte((byte)DataFormat);
            PTStream.WriteUInt16(destination, 0);

            PTStream.WriteUInt16(destination, TextureWidth);
            PTStream.WriteUInt16(destination, TextureHeight);

            // If we have an internal palette, write it
            if (DataCodec.PaletteEntries != 0 && !DataCodec.NeedsExternalPalette)
            {
                byte[] palette = PixelCodec.EncodePalette(m_texturePalette, DataCodec.PaletteEntries);
                destination.Write(palette, 0, palette.Length);
            }

            // Write out any mipmaps
            if (DataCodec.HasMipmaps)
            {
                // Write out any padding bytes before the 1x1 mipmap
                for (int i = 0; i < mipmapPadding; i++)
                {
                    destination.WriteByte(0);
                }

                for (int size = 1; size < TextureWidth; size <<= 1)
                {
                    byte[] mipmapDecodedData = null; 
                    if (DataCodec.NeedsExternalPalette)
                    {
                        if (DataCodec.VQ)
                        {
                            mipmapDecodedData = BitmapToRawVQResized(m_decodedBitmap, size, 1, m_codeBook);
                        }
                        else
                        {
                            mipmapDecodedData = BitmapToRawIndexedResized(m_decodedBitmap, size, 1, m_palette);
                        }
                    }
                    else
                    {
                        mipmapDecodedData = BitmapToRawResized(m_decodedBitmap, size, 1);
                    }
                    
                    byte[] mipmapTextureData = DataCodec.Encode(mipmapDecodedData, 0, size, size);
                    destination.Write(mipmapTextureData, 0, mipmapTextureData.Length);
                }
            }

            // Write the texture data
            byte[] textureData = DataCodec.Encode(m_decodedData, TextureWidth, TextureHeight, null);
            destination.Write(textureData, 0, textureData.Length);

            // If the data format is square twiddled with mipmaps, write out the extra bytes.
            if (DataFormat == PvrDataFormat.SQUARE_TWIDDLED_MIPMAP)
            {
                destination.Write(new byte[] { 0, 0, 0, 0 }, 0, 4);
            }

            // Compress the texture
            if (CompressionFormat != PvrCompressionFormat.NONE)
            {
                CompressionCodec = PvrCompressionCodec.GetCompressionCodec(CompressionFormat);

                if (CompressionCodec != null)
                {
                    // Ok, we need to convert the current stream to an array, compress it, then write it back to a new stream
                    byte[] buffer = destination.ToArray();
                    buffer = CompressionCodec.Compress(buffer, (HasGlobalIndex ? 0x20 : 0x10), PixelCodec, DataCodec);

                    destination = new MemoryStream();
                    destination.Write(buffer, 0, buffer.Length);
                }
            }

            return destination;
        }
    }
}