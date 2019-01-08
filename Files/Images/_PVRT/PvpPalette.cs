using System;
using System.IO;
using System.Text;

namespace ShenmueDKSharp.Files.Images._PVRT
{
    public class PvpPalette
    {
        private bool m_initalized = false; // Is the palette initalized?

        private ushort m_paletteEntries; // Number of palette entries

        private byte[] m_encodedData; // Encoded palette data (VR data)

        private PvrPixelCodec m_pixelCodec; // Pixel codec

        private PvrPixelFormat m_pixelFormat;

        public ushort PaletteEntries
        {
            get
            {
                if (!m_initalized)
                {
                    throw new TextureNotInitalizedException("Cannot access this property as the palette is not initalized.");
                }

                return m_paletteEntries;
            }
        }

        public byte[] EncodedData
        {
            get
            {
                if (!m_initalized)
                {
                    throw new TextureNotInitalizedException("Cannot access this property as the palette is not initalized.");
                }

                return m_encodedData;
            }
        }

        public PvrPixelCodec PixelCodec
        {
            get
            {
                if (!m_initalized)
                {
                    throw new TextureNotInitalizedException("Cannot access this property as the palette is not initalized.");
                }

                return m_pixelCodec;
            }
        }

        /// <summary>
        /// Returns if the texture was loaded successfully.
        /// </summary>
        /// <returns></returns>
        public bool Initalized
        {
            get { return m_initalized; }
        }

        /// <summary>
        /// The palette's pixel format.
        /// </summary>
        public PvrPixelFormat PixelFormat
        {
            get
            {
                if (!m_initalized)
                {
                    throw new TextureNotInitalizedException("Cannot access this property as the texture is not initalized.");
                }

                return m_pixelFormat;
            }
        }

        /// <summary>
        /// Open a PVP palette from a file.
        /// </summary>
        /// <param name="file">Filename of the file that contains the palette data.</param>
        public PvpPalette(string file)
        {
            m_encodedData = File.ReadAllBytes(file);

            if (m_encodedData != null)
            {
                m_initalized = Initalize();
            }
        }

        /// <summary>
        /// Open a PVP palette from a byte array.
        /// </summary>
        /// <param name="source">Byte array that contains the palette data.</param>
        public PvpPalette(byte[] source) : this(source, 0, source.Length) { }

        /// <summary>
        /// Open a PVP palette from a byte array.
        /// </summary>
        /// <param name="source">Byte array that contains the palette data.</param>
        /// <param name="offset">Offset of the palette in the array.</param>
        /// <param name="length">Number of bytes to read.</param>
        public PvpPalette(byte[] source, int offset, int length)
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
                m_initalized = Initalize();
            }
        }

        /// <summary>
        /// Open a PVP palette from a stream.
        /// </summary>
        /// <param name="source">Stream that contains the palette data.</param>
        public PvpPalette(Stream source) : this(source, (int)(source.Length - source.Position)) { }

        /// <summary>
        /// Open a PVP palette from a stream.
        /// </summary>
        /// <param name="source">Stream that contains the palette data.</param>
        /// <param name="length">Number of bytes to read.</param>
        public PvpPalette(Stream source, int length)
        {
            m_encodedData = new byte[length];
            source.Read(m_encodedData, 0, length);

            if (m_encodedData != null)
            {
                m_initalized = Initalize();
            }
        }

        public bool Initalize()
        {
            // Check to see if what we are dealing with is a GVP palette
            if (!Is(m_encodedData))
                return false;

            // Get the pixel format and the codec and make sure we can decode using them
            m_pixelFormat = (PvrPixelFormat)m_encodedData[0x08];
            m_pixelCodec = PvrPixelCodec.GetPixelCodec(m_pixelFormat);
            if (m_pixelCodec == null) return false;

            // Get the number of colors contained in the palette
            m_paletteEntries = BitConverter.ToUInt16(m_encodedData, 0x0E);

            return true;
        }

        /// <summary>
        /// Determines if this is a PVP palette.
        /// </summary>
        /// <param name="source">Byte array containing the data.</param>
        /// <param name="offset">The offset in the byte array to start at.</param>
        /// <param name="length">Length of the data (in bytes).</param>
        /// <returns>True if this is a PVP palette, false otherwise.</returns>
        public static bool Is(byte[] source, int offset, int length)
        {
            if (length >= 16 &&
                PTMethods.Contains(source, offset + 0x00, Encoding.UTF8.GetBytes("PVPL")) &&
                BitConverter.ToUInt32(source, offset + 0x04) == length - 8)
                return true;

            return false;
        }

        /// <summary>
        /// Determines if this is a PVP palette.
        /// </summary>
        /// <param name="source">Byte array containing the data.</param>
        /// <returns>True if this is a PVP palette, false otherwise.</returns>
        public static bool Is(byte[] source)
        {
            return Is(source, 0, source.Length);
        }

        /// <summary>
        /// Determines if this is a PVP palette.
        /// </summary>
        /// <param name="source">The stream to read from. The stream position is not changed.</param>
        /// <param name="length">Number of bytes to read.</param>
        /// <returns>True if this is a PVP palette, false otherwise.</returns>
        public static bool Is(Stream source, int length)
        {
            // If the length is < 16, then there is no way this is a valid palette file.
            if (length < 16)
            {
                return false;
            }

            byte[] buffer = new byte[16];
            source.Read(buffer, 0, 16);
            source.Position -= 16;

            return Is(buffer, 0, length);
        }

        /// <summary>
        /// Determines if this is a PVP palette.
        /// </summary>
        /// <param name="source">The stream to read from. The stream position is not changed.</param>
        /// <returns>True if this is a PVP palette, false otherwise.</returns>
        public static bool Is(Stream source)
        {
            return Is(source, (int)(source.Length - source.Position));
        }

        /// <summary>
        /// Determines if this is a PVP palette.
        /// </summary>
        /// <param name="file">Filename of the file that contains the data.</param>
        /// <returns>True if this is a PVP palette, false otherwise.</returns>
        public static bool Is(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                return Is(stream);
            }
        }
    }
}