using System;
using System.IO;

namespace ShenmueDKSharp.Files.Images._PVRT
{
    public class PvpPaletteEncoder
    {
        private byte[][] m_decodedPalette; // Decoded palette data (32-bit RGBA)

        private ushort m_paletteEntries; // Number of palette entries

        private PvrPixelCodec m_pixelCodec; // Pixel codec

        private PvrPixelFormat m_pixelFormat; // Pixel format

        public PvpPaletteEncoder(byte[][] palette, ushort numColors, PvrPixelFormat pixelFormat, PvrPixelCodec pixelCodec)
        {
            m_decodedPalette = palette;
            m_paletteEntries = numColors;
            m_pixelCodec = pixelCodec;
            m_pixelFormat = pixelFormat;
        }

        /// <summary>
        /// Returns the encoded palette as a byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            return EncodePalette().ToArray();
        }

        /// <summary>
        /// Returns the encoded palette as a stream.
        /// </summary>
        /// <returns></returns>
        public MemoryStream ToStream()
        {
            MemoryStream paletteStream = EncodePalette();
            paletteStream.Position = 0;
            return paletteStream;
        }

        /// <summary>
        /// Saves the encoded palette to the specified path.
        /// </summary>
        /// <param name="path">Name of the file to save the data to.</param>
        public void Save(string path)
        {
            using (FileStream destination = File.Create(path))
            {
                MemoryStream paletteStream = EncodePalette();
                paletteStream.Position = 0;
                PTStream.CopyTo(paletteStream, destination);
            }
        }

        /// <summary>
        /// Saves the encoded palette to the specified stream.
        /// </summary>
        /// <param name="destination">The stream to save the texture to.</param>
        public void Save(Stream destination)
        {
            MemoryStream paletteStream = EncodePalette();
            paletteStream.Position = 0;
            PTStream.CopyTo(paletteStream, destination);
        }

        public MemoryStream EncodePalette()
        {
            // Calculate what the length of the palette will be
            int paletteLength = 16 + (m_paletteEntries * m_pixelCodec.Bpp / 8);

            MemoryStream destination = new MemoryStream(paletteLength);

            // Write out the PVPL header
            destination.WriteByte((byte)'P');
            destination.WriteByte((byte)'V');
            destination.WriteByte((byte)'P');
            destination.WriteByte((byte)'L');

            PTStream.WriteInt32(destination, paletteLength - 8);

            destination.WriteByte((byte)m_pixelFormat);
            destination.WriteByte(0);

            PTStream.WriteUInt32(destination, 0);

            PTStream.WriteUInt16(destination, m_paletteEntries);

            // Write the palette data
            byte[] palette = m_pixelCodec.EncodePalette(m_decodedPalette, m_paletteEntries);
            destination.Write(palette, 0, palette.Length);

            return destination;
        }
    }
}