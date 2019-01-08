using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ShenmueDKSharp.Extensions;

namespace ShenmueDKSharp.Files.Images._DDS
{
    /// <summary>
    /// Indicates image format.
    /// Use FORMAT struct.
    /// </summary>
    public enum DDSFormat
    {
        /// <summary>
        /// Unknown DDS format. Using this as a save/load format will fail that operation.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// (BC1) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Simple Non Alpha.
        /// </summary>
        DDS_DXT1 = 0x31545844,  // 1TXD i.e. DXT1 backwards

        /// <summary>
        /// (BC2) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Sharp Alpha. Premultiplied alpha. 
        /// </summary>
        DDS_DXT2 = 0x32545844,

        /// <summary>
        /// (BC2) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Sharp Alpha. 
        /// </summary>
        DDS_DXT3 = 0x33545844,

        /// <summary>
        /// (BC3) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Gradient Alpha. Premultiplied alpha.
        /// </summary>
        DDS_DXT4 = 0x34545844,

        /// <summary>
        /// (BC3) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Gradient Alpha. 
        /// </summary>
        DDS_DXT5 = 0x35545844,

        /// <summary>
        /// Fancy new DirectX 10+ format indicator. DX10 Header will contain true format.
        /// </summary>
        DDS_DX10 = 0x30315844,

        /// <summary>
        /// Uncompressed ARGB DDS.
        /// </summary>
        DDS_ABGR_8 = 32,

        /// <summary>
        /// (BC4) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Normal (bump) Maps. 8 bit single channel with alpha.
        /// </summary>
        DDS_ATI1 = 0x31495441,  // ATI1 backwards

        /// <summary>
        /// Uncompressed pair of 8 bit channels.
        /// Used for Normal (bump) maps.
        /// </summary>
        DDS_V8U8 = 60,

        /// <summary>
        /// Single 8 bit channel.
        /// Used for Luminescence.
        /// </summary>
        DDS_G8_L8 = 50,

        /// <summary>
        /// Alpha and single channel luminescence.
        /// Uncompressed.
        /// </summary>
        DDS_A8L8 = 51,

        /// <summary>
        /// RGB. No alpha. 
        /// Uncompressed.
        /// </summary>
        DDS_RGB_8 = 20,

        /// <summary>
        /// (BC5) Block Compressed Texture. Compresses 4x4 texels.
        /// Used for Normal (bump) Maps. Pair of 8 bit channels.
        /// </summary>
        DDS_ATI2_3Dc = 0x32495441,  // ATI2 backwards

        DDS_ARGB_8 = 21,
        DDS_R5G6B5 = 23,
        DDS_ARGB_4 = 24,
        DDS_A8 = 28,
        DDS_G16_R16 = 34,
        DDS_ARGB_32F = 116,

        /// <summary>
        /// Used when the exact format is not present in this enum, but enough information is present to load it. (ARGB16 or something)
        /// </summary>
        DDS_CUSTOM = 255,
    }


    /// <summary>
    /// Provides format functionality
    /// </summary>
    public partial class DDSFormats
    {
        /// <summary>
        /// Contains formats not yet capable of saving.
        /// </summary>
        public static List<DDSFormat> SaveUnsupported = new List<DDSFormat>() { DDSFormat.Unknown, DDSFormat.DDS_CUSTOM };


        /// <summary>
        /// Determines if given format supports mipmapping.
        /// </summary>
        /// <param name="format">Image format to check.</param>
        /// <returns></returns>
        static bool IsFormatMippable(DDSFormat format)
        {
            return format.ToString().Contains("DDS");
        }

        /// <summary>
        /// Determines if format is a block compressed format.
        /// </summary>
        /// <param name="format">DDS Surface Format.</param>
        /// <returns>True if block compressed.</returns>
        static bool IsBlockCompressed(DDSFormat format)
        {
            switch (format)
            {
                case DDSFormat.DDS_ATI1:
                case DDSFormat.DDS_DXT1:
                case DDSFormat.DDS_DXT2:
                case DDSFormat.DDS_DXT3:
                case DDSFormat.DDS_DXT4:
                case DDSFormat.DDS_DXT5:
                case DDSFormat.DDS_ATI2_3Dc:
                case DDSFormat.DDS_DX10:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets block size of DDS format.
        /// Number of channels if not compressed.
        /// 1 if not a DDS format.
        /// </summary>
        /// <param name="format">DDS format to test.</param>
        /// <param name="componentSize">Size of channel components in bytes. e.g. 16bit = 2.</param>
        /// <returns>Number of blocks/channels in format.</returns>
        static int GetBlockSize(DDSFormat format, int componentSize = 1)
        {
            int blocksize = 1;
            switch (format)
            {
                case DDSFormat.DDS_ATI1:
                case DDSFormat.DDS_DXT1:
                    blocksize = 8;
                    break;
                case DDSFormat.DDS_DXT2:
                case DDSFormat.DDS_DXT3:
                case DDSFormat.DDS_DXT4:
                case DDSFormat.DDS_DXT5:
                case DDSFormat.DDS_ATI2_3Dc:
                case DDSFormat.DDS_DX10:
                    blocksize = 16;
                    break;
                case DDSFormat.DDS_V8U8:
                case DDSFormat.DDS_A8L8:
                case DDSFormat.DDS_ARGB_4:
                    blocksize = 2;
                    break;
                case DDSFormat.DDS_ARGB_8:
                case DDSFormat.DDS_ABGR_8:
                case DDSFormat.DDS_G16_R16:
                    blocksize = 4;
                    break;
                case DDSFormat.DDS_RGB_8:
                    blocksize = 3;
                    break;
                case DDSFormat.DDS_ARGB_32F:
                    blocksize = 16;
                    break;
                case DDSFormat.DDS_CUSTOM:
                    blocksize = 4 * componentSize;
                    break;
            }
            return blocksize;
        }


        /// <summary>
        /// Get list of supported extensions in lower case.
        /// </summary>
        /// <param name="addDot">Adds preceeding dot to be same as Path.GetExtension.</param>
        /// <returns>List of supported extensions.</returns>
        public static List<string> GetSupportedExtensions(bool addDot = false)
        {
            if (addDot)
                return Enum.GetNames(typeof(SupportedExtensions)).Where(t => t != "UNKNOWN").Select(g => "." + g).ToList();
            else
                return Enum.GetNames(typeof(SupportedExtensions)).Where(t => t != "UNKNOWN").ToList();
        }

        /// <summary>
        /// File extensions supported. Used to get initial format.
        /// </summary>
        public enum SupportedExtensions
        {
            /// <summary>
            /// Format isn't known...
            /// </summary>
            [Description("Unknown format")]
            UNKNOWN,

            /// <summary>
            /// DirectDrawSurface image. DirectX image, supports mipmapping, fairly poor compression/artifacting. Good for video memory due to mipmapping.
            /// </summary>
            [Description("DirectX Images")]
            DDS,
        }

        /// <summary>
        /// Determines image type via headers.
        /// Keeps stream position.
        /// </summary>
        /// <param name="imgData">Image data, incl header.</param>
        /// <returns>Type of image.</returns>
        public static SupportedExtensions DetermineImageType(Stream imgData)
        {
            SupportedExtensions ext = SupportedExtensions.UNKNOWN;

            // KFreon: Save position and go back to start
            long originalPos = imgData.Position;
            imgData.Seek(0, SeekOrigin.Begin);

            var bits = new byte[8];
            imgData.Read(bits, 0, 8);

            // DDS
            if (DDS_Header.CheckIdentifier(bits))
                ext = SupportedExtensions.DDS;

            // KFreon: Reset stream position
            imgData.Seek(originalPos, SeekOrigin.Begin);

            return ext;
        }


        /// <summary>
        /// Gets file extension from string of extension.
        /// </summary>
        /// <param name="extension">String containing file extension.</param>
        /// <returns>SupportedExtension of extension.</returns>
        public static SupportedExtensions ParseExtension(string extension)
        {
            SupportedExtensions ext = SupportedExtensions.DDS;
            string tempext = extension.Contains('.') ? Path.GetExtension(extension).Replace(".", "") : extension;
            if (!Enum.TryParse(tempext, true, out ext))
                return SupportedExtensions.UNKNOWN;

            return ext;
        }


        /// <summary>
        /// Searches for a format within a string. Good for automatic file naming.
        /// </summary>
        /// <param name="stringWithFormatInIt">String containing format somewhere in it.</param>
        /// <returns>Format in string, or UNKNOWN otherwise.</returns>
        public static DDSFormat FindFormatInString(string stringWithFormatInIt)
        {
            DDSFormat detectedFormat = DDSFormat.Unknown;
            foreach (var formatName in Enum.GetNames(typeof(DDSFormat)))
            {
                string actualFormat = formatName.Replace("DDS_", "");
                bool check = stringWithFormatInIt.Contains(actualFormat, StringComparison.OrdinalIgnoreCase);

                if (actualFormat.Contains("3Dc"))
                    check = stringWithFormatInIt.Contains("3dc", StringComparison.OrdinalIgnoreCase) || stringWithFormatInIt.Contains("ati2", StringComparison.OrdinalIgnoreCase);
                else if (actualFormat == "A8L8")
                    check = stringWithFormatInIt.Contains("L8", StringComparison.OrdinalIgnoreCase) && !stringWithFormatInIt.Contains("G", StringComparison.OrdinalIgnoreCase);
                else if (actualFormat == "G8_L8")
                    check = !stringWithFormatInIt.Contains("A", StringComparison.OrdinalIgnoreCase) && stringWithFormatInIt.Contains("G8", StringComparison.OrdinalIgnoreCase);
                else if (actualFormat.Contains("ARGB"))
                    check = stringWithFormatInIt.Contains("A8R8G8B8", StringComparison.OrdinalIgnoreCase) || stringWithFormatInIt.Contains("ARGB", StringComparison.OrdinalIgnoreCase);

                if (check)
                {
                    detectedFormat = (DDSFormat)Enum.Parse(typeof(DDSFormat), formatName);
                    break;
                }
            }

            return detectedFormat;
        }


        /// <summary>
        /// Gets file extension of supported surface formats.
        /// Doesn't include preceding dot.
        /// </summary>
        /// <param name="format">Format to get file extension for.</param>
        /// <returns>File extension without dot.</returns>
        static string GetExtensionOfFormat(DDSFormat format)
        {
            string formatString = format.ToString().ToLowerInvariant();
            if (formatString.Contains('_'))
                formatString = "dds";

            return formatString;
        }

        /// <summary>
        /// Calculates the compressed size of an image with given parameters.
        /// </summary>
        /// <param name="numMipmaps">Number of mipmaps in image. JPG etc only have 1.</param>
        /// <param name="formatDetails">Detailed information about format.</param>
        /// <param name="width">Width of image (top mip if mip-able)</param>
        /// <param name="height">Height of image (top mip if mip-able)</param>
        /// <returns>Size of compressed image.</returns>
        public static int GetCompressedSize(int numMipmaps, DDSFormatDetails formatDetails, int width, int height)
        {
            return DDSGeneral.GetCompressedSizeOfImage(numMipmaps, formatDetails, width, height);
        }



        /// <summary>
        /// Gets uncompressed size of image with mipmaps given dimensions and number of channels. 
        /// Assume 8 bits per channel.
        /// </summary>
        /// <param name="topWidth">Width of top mipmap.</param>
        /// <param name="topHeight">Height of top mipmap.</param>
        /// <param name="numChannels">Number of channels in image.</param>
        /// <param name="inclMips">Include size of mipmaps.</param>
        /// <returns>Uncompressed size in bytes including mipmaps.</returns>
        public static int GetUncompressedSize(int topWidth, int topHeight, int numChannels, bool inclMips)
        {
            return (int)(numChannels * (topWidth * topHeight) * (inclMips ? 4d / 3d : 1d));
        }


        /// <summary>
        /// Gets maximum number of channels a format can contain.
        /// NOTE: This likely isn't actually the max number. i.e. None exceed four, but some are only one or two channels.
        /// </summary>
        /// <param name="format">Format to channel count.</param>
        /// <returns>Max number of channels supported.</returns>
        static int MaxNumberOfChannels(DDSFormat format)
        {
            int numChannels = 4;
            switch (format)
            {
                case DDSFormat.DDS_A8:
                case DDSFormat.DDS_ATI1:
                case DDSFormat.DDS_G8_L8:
                    numChannels = 1;
                    break;
                case DDSFormat.DDS_A8L8:
                case DDSFormat.DDS_ATI2_3Dc:
                case DDSFormat.DDS_G16_R16:
                case DDSFormat.DDS_V8U8:
                    numChannels = 2;
                    break;
                case DDSFormat.DDS_R5G6B5:
                case DDSFormat.DDS_RGB_8:
                    numChannels = 3;
                    break;
            }

            return numChannels;
        }
    }
}
