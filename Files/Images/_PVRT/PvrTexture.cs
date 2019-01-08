using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ShenmueDKSharp.Files.Images._PVRT
{
    public class PvrTexture
    {
        // Size of the entire GBIX header in bytes.
        private const int gbixSizeInBytes = 12;
        // FourCC for GBIX headers.
        private static readonly byte[] gbixFourCC = { (byte)'G', (byte)'B', (byte)'I', (byte)'X' };
        // FourCC for PVRT headers.
        private static readonly byte[] pvrtFourCC = { (byte)'P', (byte)'V', (byte)'R', (byte)'T' };

        private byte[] m_encodedData; // Encoded texture data (VR data)
        private int m_paletteEntries; // Number of palette entries in the palette data
        private int m_paletteOffset;  // Offset of the palette data in the texture (-1 if there is none)
        private int m_dataOffset;     // Offset of the actual data in the texture
        private int[] m_mipmapOffsets; // Mipmap offsets

        public PvrPixelCodec PixelCodec { get; private set; }
        public PvrDataCodec DataCodec { get; private set; }

        public PvrCompressionCodec CompressionCodec { get; private set; }

        /// <summary>
        /// Returns if this texture has a global index.
        /// </summary>
        public bool HasGlobalIndex => GbixOffset != -1;

        /// <summary>
        /// The texture's global index, or 0 if this texture does not have a global index defined.
        /// </summary>
        public uint GlobalIndex { get; set; }

        /// <summary>
        /// Width of the texture (in pixels).
        /// </summary>
        public ushort TextureWidth { get; set; }

        /// <summary>
        /// Height of the texture (in pixels).
        /// </summary>
        public ushort TextureHeight { get; set; }

        /// <summary>
        /// Offset of the GBIX (or GCIX) chunk in the texture file, or -1 if this chunk is not present.
        /// </summary>
        public int GbixOffset { get; set; }

        /// <summary>
        /// Offset of the PVRT (or GVRT) chunk in the texture file.
        /// </summary>
        public int PvrtOffset { get; set; }

        /// <summary>
        /// Returns if the texture can be decoded.
        /// </summary>
        public bool CanDecode
        {
            get { return true; }
        }

        /// <summary>
        /// Returns if the texture has mipmaps.
        /// </summary>
        public bool HasMipmaps => DataCodec.HasMipmaps;

        /// <summary>
        /// Returns if the texture needs an external palette file.
        /// </summary>
        /// <returns></returns>
        public bool NeedsExternalPalette => DataCodec.NeedsExternalPalette;

        /// <summary>
        /// The texture's pixel format.
        /// </summary>
        public PvrPixelFormat PixelFormat { get; set; }

        /// <summary>
        /// The texture's data format.
        /// </summary>
        public PvrDataFormat DataFormat { get; set; }

        /// <summary>
        /// The texture's compression format (if it is compressed).
        /// </summary>
        public PvrCompressionFormat CompressionFormat { get; set; }


        /// <summary>
        /// Open a PVR texture from a file.
        /// </summary>
        /// <param name="file">Filename of the file that contains the texture data.</param>
        public PvrTexture(string file)
        {
            m_encodedData = File.ReadAllBytes(file);

            if (m_encodedData != null)
            {
                Initalize();
            }
        }

        /// <summary>
        /// Open a PVR texture from a byte array.
        /// </summary>
        /// <param name="source">Byte array that contains the texture data.</param>
        public PvrTexture(byte[] source) : this(source, 0, source.Length) { }

        /// <summary>
        /// Open a PVR texture from a byte array.
        /// </summary>
        /// <param name="source">Byte array that contains the texture data.</param>
        /// <param name="offset">Offset of the texture in the array.</param>
        /// <param name="length">Number of bytes to read.</param>
        public PvrTexture(byte[] source, int offset, int length)
        {
            if (source == null || (offset == 0 && source.Length == length))
            {
                m_encodedData = source;
            }
            else if (source != null)
            {
                m_encodedData = new byte[length];
                Array.Copy(source, offset, m_encodedData, 0, length);
            }

            if (m_encodedData != null)
            {
                Initalize();
            }
        }

        /// <summary>
        /// Open a PVR texture from a stream.
        /// </summary>
        /// <param name="source">Stream that contains the texture data.</param>
        public PvrTexture(Stream source) : this(source, (int)(source.Length - source.Position)) { }

        /// <summary>
        /// Open a PVR texture from a stream.
        /// </summary>
        /// <param name="source">Stream that contains the texture data.</param>
        /// <param name="length">Number of bytes to read.</param>
        public PvrTexture(Stream source, int length)
        {
            m_encodedData = new byte[length];
            source.Read(m_encodedData, 0, length);

            if (m_encodedData != null)
            {
                Initalize();
            }
        }

        public void Initalize()
        { 
            // Determine the offsets of the GBIX (if present) and PVRT header chunks.
            if (PTMethods.Contains(m_encodedData, 0x00, gbixFourCC))
            {
                GbixOffset = 0x00;
                PvrtOffset = 0x08 + BitConverter.ToInt32(m_encodedData, GbixOffset + 4);
            }
            else if (PTMethods.Contains(m_encodedData, 0x04, gbixFourCC))
            {
                GbixOffset = 0x04;
                PvrtOffset = 0x0C + BitConverter.ToInt32(m_encodedData, GbixOffset + 4);
            }
            else if (PTMethods.Contains(m_encodedData, 0x04, pvrtFourCC))
            {
                GbixOffset = -1;
                PvrtOffset = 0x04;
            }
            else
            {
                GbixOffset = -1;
                PvrtOffset = 0x00;
            }

            // Read the global index (if it is present). If it is not present, just set it to 0.
            if (GbixOffset != -1)
            {
                GlobalIndex = BitConverter.ToUInt32(m_encodedData, GbixOffset + 0x08);
            }
            else
            {
                GlobalIndex = 0;
            }

            // Read information about the texture
            TextureWidth  = BitConverter.ToUInt16(m_encodedData, PvrtOffset + 0x0C);
            TextureHeight = BitConverter.ToUInt16(m_encodedData, PvrtOffset + 0x0E);

            PixelFormat = (PvrPixelFormat)m_encodedData[PvrtOffset + 0x08];
            DataFormat  = (PvrDataFormat)m_encodedData[PvrtOffset + 0x09];

            // Get the codecs and make sure we can decode using them
            PixelCodec = PvrPixelCodec.GetPixelCodec(PixelFormat);
            DataCodec = PvrDataCodec.GetDataCodec(DataFormat);

            if (DataCodec != null && PixelCodec != null)
            {
                DataCodec.PixelCodec = PixelCodec;
            }

            // Set the number of palette entries
            // The number in a Small Vq encoded texture various based on its size
            m_paletteEntries = DataCodec.PaletteEntries;
            if (DataFormat == PvrDataFormat.VECTOR_QUANTIZATION_SMALL || DataFormat == PvrDataFormat.VECTOR_QUANTIZATION_SMALL_MIPMAP)
            {
                if (TextureWidth <= 16)
                {
                    m_paletteEntries = 64; // Actually 16
                }
                else if (TextureWidth <= 32)
                {
                    m_paletteEntries = 256; // Actually 64
                }
                else if (TextureWidth <= 64)
                {
                    m_paletteEntries = 512; // Actually 128
                }
                else
                {
                    m_paletteEntries = 1024; // Actually 256
                }
            }

            // Set the palette and data offsets
            if (m_paletteEntries == 0 || DataCodec.NeedsExternalPalette)
            {
                m_paletteOffset = -1;
                m_dataOffset = PvrtOffset + 0x10;
            }
            else
            {
                m_paletteOffset = PvrtOffset + 0x10;
                m_dataOffset = m_paletteOffset + (m_paletteEntries * (PixelCodec.Bpp >> 3));
            }

            // Get the compression format and determine if we need to decompress this texture
            CompressionFormat = GetCompressionFormat(m_encodedData, PvrtOffset, m_dataOffset);
            CompressionCodec = PvrCompressionCodec.GetCompressionCodec(CompressionFormat);

            if (CompressionFormat != PvrCompressionFormat.NONE && CompressionCodec != null)
            {
                m_encodedData = CompressionCodec.Decompress(m_encodedData, m_dataOffset, PixelCodec, DataCodec);

                // Now place the offsets in the appropiate area
                if (CompressionFormat == PvrCompressionFormat.RLE)
                {
                    if (GbixOffset != -1) GbixOffset -= 4;
                    PvrtOffset -= 4;
                    if (m_paletteOffset != -1) m_paletteOffset -= 4;
                    m_dataOffset -= 4;
                }
            }

            // If the texture contains mipmaps, gets the offsets of them
            if (DataCodec.HasMipmaps)
            {
                m_mipmapOffsets = new int[(int)Math.Log(TextureWidth, 2) + 1];

                int mipmapOffset = 0;
                
                // Calculate the padding for the first mipmap offset
                if (DataFormat == PvrDataFormat.SQUARE_TWIDDLED_MIPMAP)
                {
                    // A 1x1 mipmap takes up as much space as a 2x1 mipmap
                    mipmapOffset = (DataCodec.Bpp) >> 3;
                }
                else if (DataFormat == PvrDataFormat.SQUARE_TWIDDLED_MIPMAP_ALT)
                {
                    // A 1x1 mipmap takes up as much space as a 2x2 mipmap
                    mipmapOffset = (3 * DataCodec.Bpp) >> 3;
                }

                for (int i = m_mipmapOffsets.Length - 1, size = 1; i >= 0; i--, size <<= 1)
                {
                    m_mipmapOffsets[i] = mipmapOffset;

                    mipmapOffset += Math.Max((size * size * DataCodec.Bpp) >> 3, 1);
                }
            }
        }

        /// <summary>
        /// Returns the decoded texture as an array containg raw 32-bit ARGB data.
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            return DecodeTexture();
        }

        /// <summary>
        /// Returns the decoded texture as a bitmap.
        /// </summary>
        /// <returns></returns>
        public Bitmap ToBitmap()
        {
            byte[] data = DecodeTexture();

            Bitmap img = new Bitmap(TextureWidth, TextureHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData bitmapData = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.WriteOnly, img.PixelFormat);
            Marshal.Copy(data, 0, bitmapData.Scan0, data.Length);
            img.UnlockBits(bitmapData);

            return img;
        }

        /// <summary>
        /// Returns the decoded texture as a stream containg a PNG.
        /// </summary>
        /// <returns></returns>
        public MemoryStream ToStream()
        {
            MemoryStream destination = new MemoryStream();
            ToBitmap().Save(destination, ImageFormat.Png);
            destination.Position = 0;

            return destination;
        }

        /// <summary>
        /// Saves the decoded texture to the specified file.
        /// </summary>
        /// <param name="file">Name of the file to save the data to.</param>
        public void Save(string file)
        {
            ToBitmap().Save(file, ImageFormat.Png);
        }

        /// <summary>
        /// Saves the decoded texture to the specified stream.
        /// </summary>
        /// <param name="destination">The stream to save the texture to.</param>
        public void Save(Stream destination)
        {
            ToBitmap().Save(destination, ImageFormat.Png);
        }

        // Decodes a texture
        private byte[] DecodeTexture()
        {
            if (m_paletteOffset != -1) // The texture contains an embedded palette
            {
                DataCodec.SetPalette(m_encodedData, m_paletteOffset, m_paletteEntries);
            }

            if (HasMipmaps)
            {
                return DataCodec.Decode(m_encodedData, m_dataOffset + m_mipmapOffsets[0], TextureWidth, TextureHeight, PixelCodec);
            }

            return DataCodec.Decode(m_encodedData, m_dataOffset, TextureWidth, TextureHeight, PixelCodec);
        }



        /// <summary>
        /// Returns the mipmaps of a texture as an array of byte arrays. The first index will contain the largest, original sized texture and the last index will contain the smallest texture.
        /// </summary>
        /// <returns></returns>
        public byte[][] MipmapsToArray()
        {
            // If this texture does not contain mipmaps, just return the texture
            if (!HasMipmaps)
            {
                return new byte[][] { ToArray() };
            }

            return DecodeMipmaps();
        }

        /// <summary>
        /// Returns the mipmaps of a texture as an array of bitmaps. The first index will contain the largest, original sized texture and the last index will contain the smallest texture.
        /// </summary>
        /// <returns></returns>
        public Bitmap[] MipmapsToBitmap()
        {
            // If this texture does not contain mipmaps, just return the texture
            if (!HasMipmaps)
            {
                return new Bitmap[] { ToBitmap() };
            }

            byte[][] data = DecodeMipmaps();

            Bitmap[] img = new Bitmap[data.Length];
            for (int i = 0, size = TextureWidth; i < img.Length; i++, size >>= 1)
            {
                img[i] = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                BitmapData bitmapData = img[i].LockBits(new Rectangle(0, 0, img[i].Width, img[i].Height), ImageLockMode.WriteOnly, img[i].PixelFormat);
                Marshal.Copy(data[i], 0, bitmapData.Scan0, data[i].Length);
                img[i].UnlockBits(bitmapData);
            }

            return img;
        }

        /// <summary>
        /// Returns the mipmaps of a texture as an array of streams. The first index will contain the largest, original sized texture and the last index will contain the smallest texture.
        /// </summary>
        /// <returns></returns>
        public MemoryStream[] MipmapsToStream()
        {
            // If this texture does not contain mipmaps, just return the texture
            if (!HasMipmaps)
            {
                return new MemoryStream[] { ToStream() };
            }

            Bitmap[] img = MipmapsToBitmap();

            MemoryStream[] destination = new MemoryStream[img.Length];
            for (int i = 0; i < img.Length; i++)
            {
                img[i].Save(destination[i], ImageFormat.Png);
                destination[i].Position = 0;
            }

            return destination;
        }

        // Decodes mipmaps
        private byte[][] DecodeMipmaps()
        {
            if (m_paletteOffset != -1) // The texture contains an embedded palette
            {
                DataCodec.SetPalette(m_encodedData, m_paletteOffset, m_paletteEntries);
            }

            byte[][] mipmaps = new byte[m_mipmapOffsets.Length][];
            for (int i = 0, size = TextureWidth; i < mipmaps.Length; i++, size >>= 1)
            {
                mipmaps[i] = DataCodec.Decode(m_encodedData, m_dataOffset + m_mipmapOffsets[i], size, size, PixelCodec);
            }

            return mipmaps;
        }

        /// <summary>
        /// Set the palette data from an external palette file.
        /// </summary>
        /// <param name="palette">A VpPalette object</param>
        public void SetPalette(PvpPalette palette)
        {
            // No need to set an external palette if this data format doesn't require one.
            // We can't just call the data codec here as the data format does not determine
            // if a GVR uses an external palette.
            if (!NeedsExternalPalette)
            {
                return;
            }

            // If the palette is not initalized, don't use it
            if (!palette.Initalized)
            {
                return;
            }

            DataCodec.PixelCodec = palette.PixelCodec;
            DataCodec.SetPalette(palette.EncodedData, 0x10, palette.PaletteEntries);
        }

        // Gets the compression format used on the PVR
        private PvrCompressionFormat GetCompressionFormat(byte[] data, int pvrtOffset, int dataOffset)
        {
            // RLE compression
            if (BitConverter.ToUInt32(data, 0x00) == BitConverter.ToUInt32(data, pvrtOffset + 4) - pvrtOffset + dataOffset + 8)
                return PvrCompressionFormat.RLE;

            return PvrCompressionFormat.NONE;
        }

    }
}