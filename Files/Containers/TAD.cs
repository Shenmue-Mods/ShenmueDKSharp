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

        public void CalculateHeaderChecksum()
        {
            HeaderChecksum = MurmurHash2.Hash(GetHeaderBytes(true), TADHeaderSize);
        }

        protected override void _Read(BinaryReader reader)
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

        protected override void _Write(BinaryWriter writer)
        {
            CalculateHeaderChecksum();
            writer.Write(GetHeaderBytes());

            foreach(TADEntry entry in Entries)
            {
                entry.Write(writer);
            }
        }

        /// <summary>
        /// Unpacks all the files from the given TAC file to the given folder or in a folder next to the TAD file.
        /// </summary>
        public void Unpack(string tacFilepath, string folder = "")
        {
            if (String.IsNullOrEmpty(folder))
            {
                folder = Path.GetDirectoryName(FilePath) + "\\_" + FileName + "_";
            }
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            using (FileStream stream = File.Open(tacFilepath, FileMode.Open))
            {
                int counter = 0;
                foreach (TADEntry entry in Entries)
                {
                    entry.FileName = Wulinshu.GetFilenameFromHash(entry.FirstHash);

                    stream.Seek(entry.FileOffset, SeekOrigin.Begin);
                    byte[] fileBuffer = new byte[entry.FileSize];
                    stream.Read(fileBuffer, 0, fileBuffer.Length);

                    entry.FilePath = "";
                    if (String.IsNullOrEmpty(entry.FileName))
                    {
                        entry.FilePath = String.Format("{0}\\{1}", folder, counter.ToString());
                    }
                    else
                    {
                        entry.FilePath = entry.FileName.Replace('/', '\\');
                        entry.FilePath = folder + "\\" + entry.FilePath;

                        string dir = Path.GetDirectoryName(entry.FilePath);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                    }
                    using (FileStream entryStream = File.Create(entry.FilePath))
                    {
                        entryStream.Write(fileBuffer, 0, fileBuffer.Length);
                    }
                    counter++;
                }
            }
        }

        /// <summary>
        /// Packs the given entries to the given TAC file.
        /// </summary>
        public void Pack(string tacFilepath, List<TADEntry> entries)
        {
            using (FileStream stream = File.Create(tacFilepath))
            {
                FileCount = 0;
                foreach (TADEntry entry in Entries)
                {
                    FileCount++;
                    if (String.IsNullOrEmpty(entry.FilePath))
                    {
                        throw new ArgumentException("TAD entry was missing the source filepath!");
                    }

                    byte[] buffer;
                    using (FileStream entryStream = File.Open(entry.FilePath, FileMode.Open))
                    {
                        buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);

                        entry.FileOffset = (uint)stream.Position;
                        entry.FileSize = (uint)buffer.Length;
                    }
                    stream.Write(buffer, 0, buffer.Length);
                }
                TacSize = (uint)stream.Length;
                UnixTimestamp = DateTime.UtcNow + TimeSpan.FromDays(365 * 5);
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

        /// <summary>
        /// Filepath where the entry was unpacked or the source filepath when packing.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Filename based on raymonf's wulinshu hash database.
        /// </summary>
        public string FileName { get; set; }

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
