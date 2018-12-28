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
    /// PKS file container.
    /// Mostly paired with PKF file.
    /// </summary>
    public class PKS : BaseFile
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<string> Extensions = new List<string>()
        {
            "PKS"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x50, 0x41, 0x4B, 0x53 } //PAKS
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
        

        public uint Signature { get; set; }
        public uint IPACOffset { get; set; }
        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }
        public IPAC IPAC { get; set; }

        /// <summary>
        /// True if the read PKS was compressed and can be set if you want to compress the PKS when writing.
        /// </summary>
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

        protected override void _Read(BinaryReader reader)
        {
            byte[] gzipSignature = reader.ReadBytes(2);
            reader.BaseStream.Seek(-2, SeekOrigin.Current);

            Compress = false;
            MemoryStream streamOut = null;
            if (GZ.IsValid(gzipSignature))
            {
                streamOut = new MemoryStream();
                GZipStream streamGZip = new GZipStream(reader.BaseStream, CompressionMode.Decompress);
                streamGZip.CopyTo(streamOut);
                reader = new BinaryReader(streamOut);
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
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

        protected override void _Write(BinaryWriter writer)
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

        /// <summary>
        /// Unpacks all files into the given folder or, when empty, in an folder next to the PKS file.
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
            IPAC.Unpack(folder);
        }

        /// <summary>
        /// Packs the given files into the PKS object.
        /// The input files must have the same format as the unpack method
        /// or the file entries have to be added manually.
        /// </summary>
        public void Pack(List<string> filepaths)
        {
            IPAC.Pack(filepaths);
        }
    }

}
