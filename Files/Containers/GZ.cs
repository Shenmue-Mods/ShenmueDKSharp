using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Containers
{
    /// <summary>
    /// Gzip file
    /// </summary>
    public class GZ : BaseFile
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<string> Extensions = new List<string>()
        {
            "GZ"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[2] { 0x1f, 0x8b } //GZip Signature
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


        public enum CompressionMethod
        {
            Store = 0,
            Compress = 1,
            Pack = 2,
            LZH = 3,
            Deflate = 8
        }

        [Flags]
        public enum GzipFlags
        {
            Text = 1, //file probably ascii text
            MultiPart = 2, //continuation of multi-part gzip file, part number present
            Extra = 4, //extra field present
            Filename = 8, //original file name present
            Comment = 16, //file comment present
            Encrypted = 32 //file is encrypted, encryption header present
        }

        public string ContentFileName { get; set; }
        public byte[] ContentBuffer { get; set; }

        public GZ() { }
        public GZ(string filename)
        {
            Read(filename);
        }
        public GZ(Stream stream)
        {
            Read(stream);
        }
        public GZ(BinaryReader reader)
        {
            Read(reader);
        }

        protected override void _Read(BinaryReader reader)
        {
            byte[] identifier = reader.ReadBytes(2);
            if (!IsValid(identifier)) return;

            //TODO: read header correctly
            reader.BaseStream.Seek(10, SeekOrigin.Begin);

            //Reading filename from header
            byte ch = reader.ReadByte();
            ContentFileName = "";
            while (ch != 0)
            {
                ContentFileName += (char)ch;
                ch = reader.ReadByte();
            }

            //Decompress GZip into buffer
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            MemoryStream streamOut = new MemoryStream();
            GZipStream streamGZip = new GZipStream(reader.BaseStream, CompressionMode.Decompress);
            streamGZip.CopyTo(streamOut);
            ContentBuffer = streamOut.GetBuffer();
        }

        protected override void _Write(BinaryWriter writer)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(ContentBuffer, 0, ContentBuffer.Length);
                using (MemoryStream compressedStream = new MemoryStream())
                {
                    using (GZipStream compressionStream = new GZipStream(compressedStream, CompressionMode.Compress, false))
                    {
                        memoryStream.CopyTo(compressionStream);
                        compressionStream.CopyTo(writer.BaseStream);
                    }
                }
            }
        }

        /// <summary>
        /// Unpacks the file into the given folder or, when empty, in an folder next to the GZ file.
        /// </summary>
        public void Unpack(string folder = "")
        {
            if (String.IsNullOrEmpty(folder))
            {
                folder = Path.GetDirectoryName(FilePath) + "\\_" + FileName + "_";
            }
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            using (FileStream stream = new FileStream(String.Format(folder + "\\{0}", ContentFileName), FileMode.Create))
            {
                stream.Write(ContentBuffer, 0, ContentBuffer.Length);
            }
        }

        /// <summary>
        /// Packs the given file into the GZ object.
        /// </summary>
        public void Pack(string filepath)
        {
            using (FileStream stream = new FileStream(filepath, FileMode.Open))
            {
                stream.Read(ContentBuffer, 0, (int)stream.Length);
            }
        }
    }
}
