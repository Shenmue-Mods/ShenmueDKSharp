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
    public class PKF : BaseFile
    {
        public readonly static List<string> Extensions = new List<string>()
        {
            "PKF"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x50, 0x41, 0x4B, 0x46 } //PAKF
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

        public uint Identifier { get; set; }
        public uint ContentSize { get; set; }
        public uint Unknown { get; set; }
        public uint FileCount { get; set; }

        public List<PKFEntry> Entries = new List<PKFEntry>();

        public bool Compress;

        

        public PKF() { }
        public PKF(string filename)
        {
            Read(filename);
        }
        public PKF(Stream stream)
        {
            Read(stream);
        }
        public PKF(BinaryReader reader)
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

            Identifier = reader.ReadUInt32();
            ContentSize = reader.ReadUInt32();
            Unknown = reader.ReadUInt32();
            FileCount = reader.ReadUInt32();

            if (reader.ReadUInt32() == 0x594D5544)
            {
                //Skip DUMY
                reader.BaseStream.Seek(36, SeekOrigin.Begin);
            }
            else
            {
                reader.BaseStream.Seek(-4, SeekOrigin.Current);
            }

            for (int i = 0; i < FileCount; i++)
            {
                if (reader.BaseStream.Position == reader.BaseStream.Length) break;
                PKFEntry entry = new PKFEntry();
                entry.Read(reader);
                Entries.Add(entry);
            }

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
                    FileCount = (uint)Entries.Count;
                    //ContentSize calculate

                    long baseOffset = memoryWriter.BaseStream.Position;

                    memoryWriter.Write(Identifier);
                    memoryWriter.Write(ContentSize);
                    memoryWriter.Write(Unknown);
                    memoryWriter.Write(FileCount);

                    foreach (PKFEntry entry in Entries)
                    {
                        entry.Write(memoryWriter);
                    }

                    ContentSize = (uint)memoryWriter.BaseStream.Position - (uint)baseOffset;

                    memoryWriter.Seek((int)baseOffset + 4, SeekOrigin.Begin);
                    memoryWriter.Write(ContentSize);
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

    public class PKFEntry
    {
        public uint Token { get; set; }
        public uint Size { get; set; }

        public byte[] Buffer;

        public void Read(BinaryReader reader)
        {
            Token = reader.ReadUInt32();
            Size = reader.ReadUInt32();
            Buffer = reader.ReadBytes((int)Size - 8);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Token);
            writer.Write(Size);
            writer.Write(Buffer);
        }
    }
}
