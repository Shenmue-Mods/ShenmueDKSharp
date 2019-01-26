using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using ShenmueDKSharp.Files.Images._DDS;
using static ShenmueDKSharp.Files.Images._DDS.DDSFormats;
using ShenmueDKSharp.Files.Images._PVRT;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using ShenmueDKSharp.Files.Images._PVRT.WQuantizer;
using System.Drawing.Drawing2D;

namespace ShenmueDKSharp.Files.Images
{
    /// <summary>
    /// Sega Dreamcast PVR Texture. 
    /// </summary>
    public class PVRT : BaseImage
    {
        private static uint m_gbix = 0x58494247;
        private static uint m_pvrt = 0x54525650;

        public static bool EnableBuffering = false;
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

        private QuantizedPalette m_palette;
        private VQCodeBook m_codeBook;
        private byte[][] m_texturePalette;

        public override int DataSize => (int)ContentSize;

        public bool HasGlobalIndex { get; set; } = true;
        public uint GlobalIndexSize { get; set; } = 4;
        public uint GlobalIndex { get; set; } = 0;

        /// <summary>
        /// Size of the PVRT, excluding the header.
        /// </summary>
        public uint ContentSize { get; set; }

        public PvrPixelFormat PixelFormat { get; set; } = PvrPixelFormat.ARGB1555;
        public PvrDataFormat DataFormat { get; set; } = PvrDataFormat.SQUARE_TWIDDLED;
        public PvrCompressionFormat CompressionFormat { get; set; } = PvrCompressionFormat.NONE;

        public PvrPixelCodec PixelCodec { get; set; }
        public PvrDataCodec DataCodec { get; set; }
        public PvrCompressionCodec CompressionCodec { get; set; }
        public PvpPaletteEncoder PaletteEncoder { get; set; }


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
        public PVRT(BaseImage image)
        {
            Width = image.Width;
            Height = image.Height;
            foreach (MipMap mipmap in image.MipMaps)
            {
                MipMaps.Add(new MipMap(mipmap));
            }
        }

        /// <summary>
        /// Internal read implementation of the sub classes.
        /// </summary>
        /// <param name="reader"></param>
        /// <exception cref="Exception">Expected DDS RGB24 or RGBA32 color format!</exception>
        /// <exception cref="NotImplementedException">TODO</exception>
        protected override void _Read(BinaryReader reader)
        {
            long baseOffset = reader.BaseStream.Position;

            int gbixOffset = 0;
            int pvrtOffset = 0;

            uint identifier = reader.ReadUInt32();
            if (identifier == m_gbix) //"GBIX"
            {
                HasGlobalIndex = true;
                GlobalIndexSize = reader.ReadUInt32();
                GlobalIndex = reader.ReadUInt32();
                reader.BaseStream.Seek(4, SeekOrigin.Current); //Skip "PVRT"
                gbixOffset = 0x00;
                pvrtOffset = 0x08 + (int)GlobalIndexSize;
            }
            else
            {
                identifier = reader.ReadUInt32();
                if (identifier == m_gbix)
                {
                    HasGlobalIndex = true;
                    GlobalIndexSize = reader.ReadUInt32();
                    GlobalIndex = reader.ReadUInt32();
                    gbixOffset = 0x04;
                    pvrtOffset = 0x0C + (int)GlobalIndexSize;
                }
                else if (identifier == m_pvrt)
                {
                    gbixOffset = -1;
                    pvrtOffset = 0x04;
                }
                else
                {
                    gbixOffset = -1;
                    pvrtOffset = 0x00;
                    reader.BaseStream.Seek(-4, SeekOrigin.Current);
                }
            }

            // Read information about the texture
            ContentSize = reader.ReadUInt32();
            PixelFormat = (PvrPixelFormat)reader.ReadByte();
            DataFormat = (PvrDataFormat)reader.ReadByte();
            reader.BaseStream.Seek(2, SeekOrigin.Current);
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();

            if (DataFormat == PvrDataFormat.DDS || DataFormat == PvrDataFormat.DDS_2)
            {
                if (!(PixelFormat == PvrPixelFormat.DDS_DXT1_RGB24 || PixelFormat == PvrPixelFormat.DDS_DXT3_RGBA32))
                {
                    throw new Exception("Expected DDS RGB24 or RGBA32 color format!");
                }

                long ddsOffset = reader.BaseStream.Position;
                DDS_Header header = new DDS_Header(reader.BaseStream);
                DDSFormatDetails format = new DDSFormatDetails(header.Format, header.DX10_DXGI_AdditionalHeader.dxgiFormat);
                reader.BaseStream.Seek(ddsOffset, SeekOrigin.Begin);

                byte[] ddsBuffer = reader.ReadBytes(header.dwPitchOrLinearSize + header.dwSize + 128);
                
                MemoryStream memoryStream = new MemoryStream(ddsBuffer, 0, ddsBuffer.Length, true, true);
                MipMaps = DDSGeneral.LoadDDS(memoryStream, header, 0, format);
                memoryStream.Close();

                Width = header.Width;
                Height = header.Height;
            }
            else
            {
                // Get the codecs and make sure we can decode using them
                PixelCodec = PvrPixelCodec.GetPixelCodec(PixelFormat);
                DataCodec = PvrDataCodec.GetDataCodec(DataFormat);
                if (DataCodec != null && PixelCodec != null)
                {
                    DataCodec.PixelCodec = PixelCodec;
                }

                // Set the number of palette entries
                int paletteEntries = DataCodec.PaletteEntries;
                if (DataFormat == PvrDataFormat.VECTOR_QUANTIZATION_SMALL || DataFormat == PvrDataFormat.VECTOR_QUANTIZATION_SMALL_MIPMAP)
                {
                    if (Width <= 16)
                    {
                        paletteEntries = 64; // Actually 16
                    }
                    else if (Width <= 32)
                    {
                        paletteEntries = 256; // Actually 64
                    }
                    else if (Width <= 64)
                    {
                        paletteEntries = 512; // Actually 128
                    }
                    else
                    {
                        paletteEntries = 1024; // Actually 256
                    }
                }

                // Set the palette and data offsets
                int paletteOffset = 0;
                int dataOffset = 0;
                if (paletteEntries == 0 || DataCodec.NeedsExternalPalette)
                {
                    paletteOffset = -1;
                    dataOffset = pvrtOffset + 0x10;
                }
                else
                {
                    paletteOffset = pvrtOffset + 0x10;
                    dataOffset = paletteOffset + (paletteEntries * (PixelCodec.Bpp >> 3));
                }

                // Get the compression format and determine if we need to decompress this texture
                reader.BaseStream.Seek(baseOffset, SeekOrigin.Begin);
                uint first = reader.ReadUInt32();
                reader.BaseStream.Seek(baseOffset + pvrtOffset + 4, SeekOrigin.Begin);
                uint second = reader.ReadUInt32();
                if (first == second - pvrtOffset + dataOffset + 8)
                {
                    CompressionFormat = PvrCompressionFormat.RLE;
                }
                else
                {
                    CompressionFormat = PvrCompressionFormat.NONE;
                }
                CompressionCodec = PvrCompressionCodec.GetCompressionCodec(CompressionFormat);

                if (CompressionFormat != PvrCompressionFormat.NONE && CompressionCodec != null)
                {
                    //TODO: Convert to stream compatible code
                    throw new NotImplementedException("TODO");
                    //m_encodedData = CompressionCodec.Decompress(m_encodedData, dataOffset, PixelCodec, DataCodec);

                    // Now place the offsets in the appropiate area
                    if (CompressionFormat == PvrCompressionFormat.RLE)
                    {
                        if (gbixOffset != -1) gbixOffset -= 4;
                        pvrtOffset -= 4;
                        if (paletteOffset != -1) paletteOffset -= 4;
                        dataOffset -= 4;
                    }
                }

                // If the texture contains mipmaps, gets the offsets of them
                int[] mipmapOffsets;
                if (DataCodec.HasMipmaps)
                {
                    int mipmapOffset = 0;
                    mipmapOffsets = new int[(int)Math.Log(Width, 2) + 1];

                    // Calculate the padding for the first mipmap offset
                    if (DataFormat == PvrDataFormat.SQUARE_TWIDDLED_MIPMAP)
                    {
                        mipmapOffset = (DataCodec.Bpp) >> 3; // A 1x1 mipmap takes up as much space as a 2x1 mipmap
                    }
                    else if (DataFormat == PvrDataFormat.SQUARE_TWIDDLED_MIPMAP_ALT)
                    {
                        mipmapOffset = (3 * DataCodec.Bpp) >> 3; // A 1x1 mipmap takes up as much space as a 2x2 mipmap
                    }
                    for (int i = mipmapOffsets.Length - 1, size = 1; i >= 0; i--, size <<= 1)
                    {
                        mipmapOffsets[i] = mipmapOffset;
                        mipmapOffset += Math.Max((size * size * DataCodec.Bpp) >> 3, 1);
                    }
                }
                else
                {
                    mipmapOffsets = new int[1] { 0 };
                }

                //DecodeMipmaps()
                if (paletteOffset != -1) // The texture contains an embedded palette
                {
                    reader.BaseStream.Seek(baseOffset + paletteOffset, SeekOrigin.Begin);
                    DataCodec.SetPalette(reader, paletteEntries);
                }

                MipMaps = new List<MipMap>();
                if (DataCodec.HasMipmaps)
                {
                    for (int i = 0, size = Width; i < mipmapOffsets.Length; i++, size >>= 1)
                    {
                        reader.BaseStream.Seek(baseOffset + dataOffset + mipmapOffsets[i], SeekOrigin.Begin);
                        byte[] pixels = DataCodec.Decode(reader, size, size, PixelCodec);
                        MipMaps.Add(new MipMap(pixels, size, size));
                    }
                }
                else
                {
                    reader.BaseStream.Seek(baseOffset + dataOffset + mipmapOffsets[0], SeekOrigin.Begin);
                    byte[] pixels = DataCodec.Decode(reader, Width, Height, PixelCodec);
                    MipMaps.Add(new MipMap(pixels, Width, Height));
                }
            }
            if (HasGlobalIndex)
            {
                reader.BaseStream.Seek(baseOffset + ContentSize + 0xC, SeekOrigin.Begin);
            }
            else
            {
                reader.BaseStream.Seek(baseOffset + ContentSize, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Internal write implementation of the sub classes.
        /// </summary>
        /// <param name="writer"></param>
        /// <exception cref="Exception">Expected DDS RGB24 or RGBA32 color format!</exception>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        protected override void _Write(BinaryWriter writer)
        {
            long baseOffset = writer.BaseStream.Position;

            Bitmap bmp = CreateBitmap();
            //bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            if (DataFormat == PvrDataFormat.DDS || DataFormat == PvrDataFormat.DDS_2)
            {
                if (!(PixelFormat == PvrPixelFormat.DDS_DXT1_RGB24 || PixelFormat == PvrPixelFormat.DDS_DXT3_RGBA32))
                {
                    throw new Exception("Expected DDS RGB24 or RGBA32 color format!");
                }

                byte[] ddsBuffer = null;
                if (PixelFormat == PvrPixelFormat.DDS_DXT1_RGB24)
                {
                    DDSFormatDetails ddsFormatDetails = new DDSFormatDetails(DDSFormat.DDS_DXT1);
                    ddsBuffer = DDSGeneral.Save(MipMaps, ddsFormatDetails, DDSGeneral.AlphaSettings.KeepAlpha, DDSGeneral.MipHandling.Default);
                }
                else if (PixelFormat == PvrPixelFormat.DDS_DXT3_RGBA32)
                {
                    DDSFormatDetails ddsFormatDetails = new DDSFormatDetails(DDSFormat.DDS_DXT3);
                    ddsBuffer = DDSGeneral.Save(MipMaps, ddsFormatDetails, DDSGeneral.AlphaSettings.KeepAlpha, DDSGeneral.MipHandling.Default);
                }
                if (HasGlobalIndex)
                {
                    writer.Write(m_gbix);
                    writer.Write(4);
                    writer.Write(GlobalIndex);
                }

                writer.Write(m_pvrt);
                writer.Write(ddsBuffer.Length + 16);
                writer.Write((byte)PixelFormat);
                writer.Write((byte)DataFormat);
                writer.Write((ushort)0);
                writer.Write((ushort)Width);
                writer.Write((ushort)Height);
                writer.Write(ddsBuffer);
            }
            else
            {
                // Set the data format and pixel format and load the appropiate codecs
                PixelCodec = PvrPixelCodec.GetPixelCodec(PixelFormat);
                DataCodec = PvrDataCodec.GetDataCodec(DataFormat);

                // Make sure the pixel and data codecs exists and we can encode to it
                if (PixelCodec == null || !PixelCodec.CanEncode) { throw new InvalidOperationException(); }
                if (DataCodec == null || !DataCodec.CanEncode) { throw new InvalidOperationException(); }
                DataCodec.PixelCodec = PixelCodec;

                byte[] decodedData = null;
                if (DataCodec.PaletteEntries != 0)
                {
                    if (DataCodec.VQ)
                    {
                        decodedData = BitmapToRawVQ(bmp, DataCodec.PaletteEntries, out m_texturePalette);
                    }
                    else
                    {
                        // Convert the bitmap to an array containing indicies.
                        decodedData = BitmapToRawIndexed(bmp, DataCodec.PaletteEntries, out m_texturePalette);

                        // If this texture has an external palette file, set up the palette encoder
                        if (DataCodec.NeedsExternalPalette)
                        {
                            PaletteEncoder = new PvpPaletteEncoder(m_texturePalette, (ushort)DataCodec.PaletteEntries, PixelFormat, PixelCodec);
                        }
                    }
                }
                else
                {
                    decodedData = BitmapToRaw(bmp);
                }

                // Calculate what the length of the texture will be
                int textureLength = 16 + (int)(Width * Height * (DataCodec.Bpp / 8.0));
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
                    for (int size = 1; size < Width; size <<= 1)
                    {
                        textureLength += Math.Max((size * size * DataCodec.Bpp) >> 3, 1);
                    }
                }

                MemoryStream output = new MemoryStream(textureLength);
                BinaryWriter outputWriter = new BinaryWriter(output);

                // Write out the GBIX header (if we are including one)
                if (HasGlobalIndex)
                {
                    outputWriter.Write(m_gbix);
                    outputWriter.Write(4);
                    outputWriter.Write(GlobalIndex);
                }

                // Write out the PVRT header
                outputWriter.Write(m_pvrt);
                if (HasGlobalIndex)
                {
                    outputWriter.Write(textureLength - 24);
                }
                else
                {
                    outputWriter.Write(textureLength - 8);
                }
                outputWriter.Write((byte)PixelFormat);
                outputWriter.Write((byte)DataFormat);
                outputWriter.Write((ushort)0);
                outputWriter.Write((ushort)Width);
                outputWriter.Write((ushort)Height);

                // If we have an internal palette, write it
                if (DataCodec.PaletteEntries != 0 && !DataCodec.NeedsExternalPalette)
                {
                    byte[] palette = PixelCodec.EncodePalette(m_texturePalette, DataCodec.PaletteEntries);
                    output.Write(palette, 0, palette.Length);
                }

                // Write out any mipmaps
                if (DataCodec.HasMipmaps)
                {
                    // Write out any padding bytes before the 1x1 mipmap
                    for (int i = 0; i < mipmapPadding; i++)
                    {
                        output.WriteByte(0);
                    }
                    for (int size = 1; size < Width; size <<= 1)
                    {
                        byte[] mipmapDecodedData = null;
                        if (DataCodec.NeedsExternalPalette)
                        {
                            if (DataCodec.VQ)
                            {
                                mipmapDecodedData = BitmapToRawVQResized(bmp, size, 1, m_codeBook);
                            }
                            else
                            {
                                mipmapDecodedData = BitmapToRawIndexedResized(bmp, size, 1, m_palette);
                            }
                        }
                        else
                        {
                            mipmapDecodedData = BitmapToRawResized(bmp, size, 1);
                        }
                        byte[] mipmapTextureData = DataCodec.Encode(mipmapDecodedData, 0, size, size);
                        output.Write(mipmapTextureData, 0, mipmapTextureData.Length);
                    }
                }

                // Write the texture data
                byte[] textureData = DataCodec.Encode(decodedData, Width, Height, null);
                output.Write(textureData, 0, textureData.Length);

                // If the data format is square twiddled with mipmaps, write out the extra bytes.
                if (DataFormat == PvrDataFormat.SQUARE_TWIDDLED_MIPMAP)
                {
                    output.Write(new byte[] { 0, 0, 0, 0 }, 0, 4);
                }

                // Compress the texture
                if (CompressionFormat != PvrCompressionFormat.NONE)
                {
                    CompressionCodec = PvrCompressionCodec.GetCompressionCodec(CompressionFormat);
                    if (CompressionCodec != null)
                    {
                        // Ok, we need to convert the current stream to an array, compress it, then write it back to a new stream
                        byte[] buffer = output.ToArray();
                        buffer = CompressionCodec.Compress(buffer, (HasGlobalIndex ? 0x20 : 0x10), PixelCodec, DataCodec);
                        writer.Write(buffer);
                    }
                }
                else
                {
                    writer.Write(output.GetBuffer());
                }
            }
        }

        public void WriteDDSRaw(BinaryWriter writer, DDS dds)
        {
            long offset = writer.BaseStream.Position;
            writer.Write(m_pvrt);
            writer.Write(dds.Buffer.Length + 16);
            writer.Write((byte)PixelFormat);
            writer.Write((byte)DataFormat);
            writer.Write((ushort)0);
            writer.Write((ushort)Width);
            writer.Write((ushort)Height);
            writer.Write(dds.Buffer);
        }

        private byte[] BitmapToRaw(Bitmap source)
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

        private byte[] BitmapToRawResized(Bitmap source, int size, int minSize)
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
                byte[] pixels = m_codeBook.Entries[i].ToArrayTwiddled();
                for (int j = 0; j < 4; j++)
                {
                    int paletteIndex = i * 4 + j;
                    palette[paletteIndex] = new byte[4];
                    palette[paletteIndex][0] = pixels[j * 4];
                    palette[paletteIndex][1] = pixels[j * 4 + 1];
                    palette[paletteIndex][2] = pixels[j * 4 + 2];
                    palette[paletteIndex][3] = pixels[j * 4 + 3];
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

        private unsafe byte[] BitmapToRawIndexed(Bitmap source, int maxColors, out byte[][] palette)
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
            WuQuantizer quantizer = new WuQuantizer();
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

        private unsafe byte[] BitmapToRawIndexedResized(Bitmap source, int size, int minSize, QuantizedPalette palette)
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
            WuQuantizer quantizer = new WuQuantizer();
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
    }

}
