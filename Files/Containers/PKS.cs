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
    public class PKS : BaseFile
    {
        public readonly static List<string> Extensions = new List<string>()
        {
            "PKS"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x50, 0x41, 0x4B, 0x53 } //PAKF
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
        

        public uint Signature { get; set; }
        public uint IPACOffset { get; set; }
        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        public IPAC IPAC { get; set; }

        public bool Compress;

        public PKS() { }
        public PKS(string filename)
        {
            Read(filename);
        }
        public PKS(Stream stream)
        {
            Read(stream);
        }
        public PKS(BinaryReader reader)
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
            byte[] gzipSignature = reader.ReadBytes(2);
            reader.BaseStream.Seek(-2, SeekOrigin.Current);

            Compress = false;
            if (GZ.IsValid(gzipSignature))
            {
                MemoryStream streamOut = new MemoryStream();
                using (GZipStream streamGZip = new GZipStream(reader.BaseStream, CompressionMode.Decompress))
                {
                    streamGZip.CopyTo(streamOut);
                }
                reader = new BinaryReader(streamOut);
                Compress = true;
            }

            Signature = reader.ReadUInt32();
            IPACOffset = reader.ReadUInt32();
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            IPAC = new IPAC(reader);

            if (Compress)
            {
                reader.Close();
            }
        }

        public void Write(BinaryWriter writer)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter memoryWriter = new BinaryWriter(memoryStream))
                {
                    writer.Write(Signature);
                    writer.Write(IPACOffset);
                    writer.Write(Unknown1);
                    writer.Write(Unknown2);
                    IPAC.Write(memoryWriter);
                }
                if (Compress)
                {
                    using (MemoryStream compressedStream = new MemoryStream())
                    {
                        using (GZipStream compressionStream = new GZipStream(compressedStream, CompressionMode.Compress, false))
                        {
                            memoryStream.CopyTo(compressionStream);
                            compressionStream.CopyTo(writer.BaseStream);
                        }
                    }
                }
                else
                {
                    memoryStream.CopyTo(writer.BaseStream);
                }
            }
        }
    }

}
