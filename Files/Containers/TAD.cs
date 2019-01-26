using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Containers
{
    /// <summary>
    /// TAD filename hash table with file offset and file size for the TAC file container.
    /// Created by d3t to pack all their assets and allow updating/overwriting with the timestamp as the priority.
    /// </summary>
    public class TAD : BaseFile
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        private static readonly ushort TADHeaderSize = 56;

        public readonly static List<string> Extensions = new List<string>()
        {
            "TAD"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[8] { 0x01, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00 }
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

        public uint FileType { get; set; } = 1;
        public uint Identifier1 { get; set; } = 5;
        public uint Identifier2 { get; set; } = 2;
        public DateTime UnixTimestamp { get; set; } = DateTime.UtcNow;
        public string RenderType { get; set; } = "dx11";
        public uint HeaderChecksum { get; set; } = 0;
        public uint TacSize { get; set; } = 0;
        public uint FileCount { get; set; } = 0;

        public List<TADEntry> Entries = new List<TADEntry>();

        public TAD() { }
        public TAD(string filepath)
        {
            Read(filepath);
        }
        public TAD(Stream stream)
        {
            Read(stream);
        }
        public TAD(BinaryReader reader)
        {
            Read(reader);
        }

        public void CalculateHeaderChecksum()
        {
            HeaderChecksum = MurmurHash2.Hash(GetHeaderBytes(true), TADHeaderSize);
        }

        protected override void _Read(BinaryReader reader)
        {
            FileType = reader.ReadUInt32();
            Identifier1 = reader.ReadUInt32();
            Identifier2 = reader.ReadUInt32();
            reader.BaseStream.Seek(4, SeekOrigin.Current);
            UnixTimestamp = new DateTime(1970, 1, 1).AddSeconds(reader.ReadInt32());
            reader.BaseStream.Seek(4, SeekOrigin.Current);
            char[] typeBuffer = new char[4];
            reader.Read(typeBuffer, 0, 4);
            RenderType = new string(typeBuffer);
            reader.BaseStream.Seek(4, SeekOrigin.Current);
            HeaderChecksum = reader.ReadUInt32();
            reader.BaseStream.Seek(4, SeekOrigin.Current);
            TacSize = reader.ReadUInt32();
            reader.BaseStream.Seek(4, SeekOrigin.Current);
            FileCount = reader.ReadUInt32();
            reader.BaseStream.Seek(8, SeekOrigin.Current); //Skip file count duplicate

            for (int i = 0; i < FileCount; i++)
            {
                TADEntry entry = new TADEntry();
                entry.Read(reader);
                entry.Index = (uint)i;
                Entries.Add(entry);
            }
        }

        protected override void _Write(BinaryWriter writer)
        {
            CalculateHeaderChecksum();
            writer.Write(GetHeaderBytes());
            writer.Write(FileCount); //Write file count duplicate

            foreach(TADEntry entry in Entries)
            {
                entry.Write(writer);
            }
        }

        /// <summary>
        /// Assigns the filenames to each TAD entry.
        /// </summary>
        /// <param name="raymonf">True for using raymonf's wulinshu database else the cached database is used which is faster.</param>
        public void AssignFileNames(bool raymonf = false)
        {
            if (raymonf)
            {
                foreach (TADEntry entry in Entries)
                {
                    entry.FileName = Wulinshu.GetFilenameFromHash(entry.FirstHash);
                }
            }
            else
            {
                foreach (TADEntry entry in Entries)
                {
                    entry.FileName = FilenameDatabase.GetFilename(entry.FirstHash, entry.SecondHash);
                }
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

        public override string ToString()
        {
            return FilePath;
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

        /// <summary>
        /// Filepath where the entry was unpacked or the source filepath when packing.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Filename used to create the hashes or write the filename from a hash database.
        /// </summary>
        public string FileName { get; set; }

        public uint FirstHash { get; set; }
        public uint SecondHash { get; set; }
        public uint Unknown { get; set; }
        public uint FileOffset { get; set; }
        public uint FileSize { get; set; }

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

        public void CalculateFilenameHashes(bool singleHash = false)
        {
            if (singleHash)
            {
                FirstHash = BitConverter.ToUInt32(MurmurHash2.GetFilenameHash(FileName, false), 0);
                SecondHash = 0;
            }
            else
            {
                SecondHash = MurmurHash2.GetFilenameHashPlain(FileName);
                string fullFilename = MurmurHash2.GetFullFilename(FileName, SecondHash);
                FirstHash = BitConverter.ToUInt32(MurmurHash2.GetFilenameHash(fullFilename), 0);
            }
        }

        public override string ToString()
        {
            return FilePath;
        }
    }
}
