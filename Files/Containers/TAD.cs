using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Containers
{
    public class TAD : BaseFile
    {
        private static readonly ushort TADHeaderSize = 56;

        public readonly static List<string> Extensions = new List<string>()
        {
            "TAD"
        };

        public uint FileType { get; set; } = 1;
        public uint Identifier1 { get; set; } = 5;
        public uint Identifier2 { get; set; } = 2;
        public DateTime UnixTimestamp { get; set; } = DateTime.UtcNow;
        public string RenderType { get; set; } = "dx11";
        public uint HeaderChecksum { get; set; } = 0;
        public uint TacSize { get; set; } = 0;
        public uint FileCount { get; set; } = 0;

        public List<TADEntry> Entries = new List<TADEntry>();

        public void CalculateHeaderChecksum()
        {
            HeaderChecksum = MurmurHash2.Hash(GetHeaderBytes(true), TADHeaderSize);
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
            FileType = reader.ReadUInt32();
            Identifier1 = reader.ReadUInt32();
            Identifier2 = reader.ReadUInt32();
            reader.ReadUInt32();
            UnixTimestamp = new DateTime(1970, 1, 1).AddSeconds(reader.ReadInt32());
            reader.ReadUInt32();
            char[] typeBuffer = new char[4];
            reader.Read(typeBuffer, 0, 4);
            RenderType = new string(typeBuffer);
            reader.ReadUInt32();
            HeaderChecksum = reader.ReadUInt32();
            reader.ReadUInt32();
            TacSize = reader.ReadUInt32();
            reader.ReadUInt32();
            FileCount = reader.ReadUInt32();

            for (int i = 0; i < FileCount; i++)
            {
                TADEntry entry = new TADEntry();
                entry.Read(reader);
                entry.Index = (uint)i;
                Entries.Add(entry);
            }
        }

        public void Write(BinaryWriter writer)
        {
            CalculateHeaderChecksum();
            writer.Write(GetHeaderBytes());

            foreach(TADEntry entry in Entries)
            {
                entry.Write(writer);
            }
        }

        public byte[] GetHeaderBytes(bool nullHash = false)
        {
            byte[] result = new byte[TADHeaderSize];

            using (MemoryStream stream = new MemoryStream(result))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(FileType);
                    writer.Write(Identifier1);
                    writer.Write(Identifier2);
                    writer.Write(0);
                    writer.Write(BitConverter.GetBytes((Int32)(UnixTimestamp.Subtract(new DateTime(1970, 1, 1))).TotalSeconds), 0, 4);
                    writer.Write(0);
                    byte[] renderTypeBytes = Encoding.ASCII.GetBytes(RenderType);
                    writer.Write(renderTypeBytes, 0, 4);
                    writer.Write(0);

                    if (nullHash)
                    {
                        writer.Write(0);
                    }
                    else
                    {
                        writer.Write(HeaderChecksum);
                    }

                    writer.Write(0);
                    writer.Write(TacSize);
                    writer.Write(0);
                    writer.Write(FileCount);
                    writer.Write(0);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// File entry inside the TAD file (32 Bytes)
    /// </summary>
    public class TADEntry
    {
        /// <summary>
        /// The TAD file entry size in bytes.
        /// </summary>
        public static readonly ushort TADEntrySize = 32;

        public uint Index;

        public uint FirstHash;
        public uint SecondHash;
        public uint Unknown;
        public uint FileOffset;
        public uint FileSize;

        public TADEntry() { }
        public TADEntry(byte[] data)
        {
            Read(data);
        }

        /// <summary>
        /// Reads the file entry with binary reader into the current file entry object.
        /// </summary>
        public void Read(BinaryReader reader)
        {
            FirstHash = reader.ReadUInt32();
            SecondHash = reader.ReadUInt32();
            Unknown = reader.ReadUInt32();
            reader.ReadUInt32();
            FileOffset = reader.ReadUInt32();
            reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
            reader.ReadUInt32();
        }

        /// <summary>
        /// Reads the file entry as byte array into the current file entry object.
        /// No Metadata support.
        /// </summary>
        public void Read(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    Read(reader);
                }
            }
        }

        /// <summary>
        /// Writes the current file entry object to the binary writer.
        /// </summary>
        public void Write(BinaryWriter writer)
        {
            byte[] entryBytes = GetBytes();
            writer.Write(entryBytes, 0, entryBytes.Length);
        }

        /// <summary>
        /// Gets the bytes of the current file entry object as inside an TAD file.
        /// </summary>
        public byte[] GetBytes()
        {
            byte[] result = new byte[TADEntrySize];
            using (MemoryStream stream = new MemoryStream(result))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(FirstHash);
                    writer.Write(SecondHash);
                    writer.Write(Unknown);
                    writer.Write(0);
                    writer.Write(FileOffset);
                    writer.Write(0);
                    writer.Write(FileSize);
                    writer.Write(0);
                }
            }
            return result;
        }
    }
}
