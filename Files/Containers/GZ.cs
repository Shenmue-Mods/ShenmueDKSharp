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
                if (Helper.CompareSignature(Identifiers[i], identifier)) return true;
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

        public BaseFile File;

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


        public override void Read(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                Read(reader);
            }
        }

        public override void Write(Stream stream)
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                Write(writer);
            }
        }

        public void Read(BinaryReader reader)
        {
            byte[] identifier = reader.ReadBytes(2);
            if (!IsValid(identifier)) return;

            //TODO read header correctly
            reader.BaseStream.Seek(10, SeekOrigin.Begin);

            //reading filename from header
            byte ch = reader.ReadByte();
            string fName = "";
            while (ch != 0)
            {
                fName += (char)ch;
                ch = reader.ReadByte();
            }

            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            using (MemoryStream streamOut = new MemoryStream())
            {
                using (GZipStream streamGZip = new GZipStream(reader.BaseStream, CompressionMode.Decompress))
                {
                    streamGZip.CopyTo(streamOut);
                }
                Buffer = streamOut.GetBuffer();
            }

        }

        public void Write(BinaryWriter writer)
        {

        }

        public static void Unpack(string folder)
        {
            throw new NotImplementedException();
        }

        public static void Pack(string[] filenames)
        {
            throw new NotImplementedException();
        }
    }
}
