using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShenmueDKSharp.Files.Containers
{
    /// <summary>
    /// AFS file container
    /// </summary>
    public class AFS : BaseFile
    {
        public static bool AutomaticLoadIDX = true;

        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<string> Extensions = new List<string>()
        {
            "AFS"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x41, 0x46, 0x53, 0x00 } //AFS
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

        public uint Signature = 5457473;
        public uint FileCount;
        public List<AFSEntry> Entries = new List<AFSEntry>();
        public uint SectorSize = 2048;
        public uint FilenameSectionOffset;
        public uint FilenameSectionSize;

        public AFS() { }
        public AFS(string filepath)
        {
            Read(filepath);
        }
        public AFS(Stream stream)
        {
            Read(stream);
        }
        public AFS(BinaryReader reader)
        {
            Read(reader);
        }

        protected override void _Read(BinaryReader reader)
        {
            long baseOffset = reader.BaseStream.Position;

            Signature = reader.ReadUInt32();
            if (!IsValid(Signature))
            {
                throw new InvalidFileSignatureException();
            }
            FileCount = reader.ReadUInt32();
            Entries.Clear();

            //read file offsets (table of content)
            uint index = 0;
            for (int i = 0; i < FileCount; i++)
            {
                AFSEntry entry = new AFSEntry();
                entry.ReadOffset(reader);
                entry.Index = index;
                Entries.Add(entry);
                index++;
            }
            
            //Read filename table offset
            //This limit is just a guess, needs testing
            if (FileCount > 1016)
            {
                reader.BaseStream.Seek(baseOffset + 0x0008000 - 8, SeekOrigin.Begin);
                FilenameSectionOffset = reader.ReadUInt32();
                FilenameSectionSize = reader.ReadUInt32();
            }
            else
            {
                long offset = reader.BaseStream.Position + 0x0800 - (reader.BaseStream.Position % 0x0800);
                reader.BaseStream.Seek(baseOffset + offset - 8, SeekOrigin.Begin);
                FilenameSectionOffset = reader.ReadUInt32();
                FilenameSectionSize = reader.ReadUInt32();
            }

            if (FilenameSectionOffset == 0)
            {
                //Set incrementing filenames if table of content is missing.
                for (int i = 0; i < FileCount; i++)
                {
                    Entries[i].Filename = "file_" + i;
                }
            }
            else 
            {
                //Read table of contents
                reader.BaseStream.Seek(baseOffset + FilenameSectionOffset, SeekOrigin.Begin);
                for (int i = 0; i < FileCount; i++)
                {
                    Entries[i].ReadFilename(reader);
                }
            }

            //Read file data
            foreach (AFSEntry entry in Entries)
            {
                reader.BaseStream.Seek(baseOffset + entry.Offset, SeekOrigin.Begin);
                entry.ReadData(reader);
            }

            if (AutomaticLoadIDX)
            {
                if (typeof(FileStream).IsAssignableFrom(reader.BaseStream.GetType()))
                {
                    FileStream fs = (FileStream)reader.BaseStream;
                    string idxPath = Path.ChangeExtension(fs.Name, ".IDX");
                    if (File.Exists(idxPath))
                    {
                        IDX idx = new IDX(idxPath);
                        MapIDXFile(idx);
                    }
                }
            }
        }

        protected override void _Write(BinaryWriter writer)
        {
            long baseOffset = writer.BaseStream.Position;

            FileCount = (uint)Entries.Count;
            writer.Write(Signature);
            writer.Write(FileCount);

            //Calculate offsets
            uint startOffset = 8 + FileCount * 16;
            startOffset = startOffset + 0x0800 - (startOffset % 0x0800);

            if (FileCount > 1016)
            {
                startOffset = 0x0008000;
            }

            uint offset = startOffset;
            for (int i = 0; i < Entries.Count; i++)
            {
                AFSEntry entry = Entries[i];
                entry.Index = (uint)i;
                entry.Offset = offset;
                offset += entry.FileSize;
                offset += SectorSize - (offset % SectorSize); //sector padding
            }

            //Write offsets
            foreach (AFSEntry entry in Entries)
            {
                entry.WriteOffset(writer);
            }

            //Calculate and write filename table offset
            FilenameSectionOffset = offset;
            FilenameSectionSize = (uint)Entries.Count * AFSEntry.Length;
            writer.BaseStream.Seek(baseOffset + startOffset - 8, SeekOrigin.Begin);
            writer.Write(FilenameSectionOffset);
            writer.Write(FilenameSectionSize);

            //Write data
            foreach(AFSEntry entry in Entries)
            {
                writer.BaseStream.Seek(baseOffset + entry.Offset, SeekOrigin.Begin);
                entry.WriteData(writer);
            }

            //Write filenames
            writer.BaseStream.Seek(baseOffset + FilenameSectionOffset, SeekOrigin.Begin);
            foreach (AFSEntry entry in Entries)
            {
                entry.WriteFilename(writer);
            }
        }

        /// <summary>
        /// Unpacks all files into the given folder or, when empty, in an folder next to the AFS file.
        /// </summary>
        public void Unpack(string folder = "", bool texOverride = false)
        {
            if (String.IsNullOrEmpty(folder))
            {
                folder = Path.GetDirectoryName(FilePath) + "\\_" + FileName + "_";
            }
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            if (texOverride)
            {
                int counter = 0;
                foreach (AFSEntry entry in Entries)
                {
                    string filename = String.Format("afs{0:00000}_{1}", counter, Path.GetExtension(entry.Filename));
                    if (counter % 2 != 0)
                    {
                        filename = String.Format("afs{0:00000}_{1}", counter - 1, Path.GetExtension(entry.Filename));
                    }
                    using (FileStream stream = File.Open(folder + "\\" + filename, FileMode.Create))
                    {
                        stream.Write(entry.Buffer, 0, (int)entry.FileSize);
                    }
                    counter++;
                }
            }
            else
            {
                foreach (AFSEntry entry in Entries)
                {
                    string filename = entry.Filename;
                    if (!String.IsNullOrEmpty(entry.IDXFilename))
                    {
                        filename = entry.IDXFilename + Path.GetExtension(filename);
                    }
                    using (FileStream stream = File.Open(folder + "\\" + filename, FileMode.Create))
                    {
                        stream.Write(entry.Buffer, 0, (int)entry.FileSize);
                    }
                }
            }
        }

        public void Pack(List<string> filepaths)
        {
            Entries.Clear();
            foreach (string filepath in filepaths)
            {
                FileInfo fileInfo = new FileInfo(filepath);

                AFSEntry entry = new AFSEntry();
                entry.Filename = Path.GetFileNameWithoutExtension(filepath);
                entry.EntryDateTime = fileInfo.LastWriteTimeUtc;
                using (FileStream stream = new FileStream(filepath, FileMode.Open))
                {
                    entry.FileSize = (uint)stream.Length;
                    entry.Buffer = new byte[stream.Length];
                    stream.Read(entry.Buffer, 0, entry.Buffer.Length);
                }
                Entries.Add(entry);
            }
        }

        /// <summary>
        /// Tries to map the filenames of all the AFS file entries with the give IDX file.
        /// </summary>
        public void MapIDXFile(IDX idx)
        {
            for (int i = 0; i < idx.Entries.Count; i++)
            {
                IDXEntry entry = idx.Entries[i];
                if (i >= Entries.Count) break;
                Entries[i].IDXFilename = entry.Filename;
            }
        }

    }

    public class AFSEntry
    {
        public static readonly uint Length = 48;

        private byte[] m_buffer;

        public uint Index { get; set; }

        public uint Offset { get; set; }
        public uint FileSize { get; set; }
        public string Filename { get; set; }
        public string IDXFilename { get; set; }

        public ushort Year { get; set; }
        public ushort Month { get; set; }
        public ushort Day { get; set; }
        public ushort Hour { get; set; }
        public ushort Minute { get; set; }
        public ushort Second { get; set; }

        public byte[] Buffer
        {
            get
            {
                return m_buffer;
            }
            set
            {
                m_buffer = value;
                FileSize = (uint)value.Length;
            }
        }

        public DateTime EntryDateTime
        {
            get
            {
                return new DateTime(Year, Month, Day, Hour, Minute, Second);
            }
            set
            {
                Year = (ushort)value.Year;
                Month = (ushort)value.Month;
                Day = (ushort)value.Day;
                Hour = (ushort)value.Hour;
                Minute = (ushort)value.Minute;
                Second = (ushort)value.Second;
            }
        }

        public AFSEntry() { }
        public AFSEntry(string filename)
        {
            Filename = filename;
            FileInfo fileInfo = new FileInfo(Filename);
            EntryDateTime = fileInfo.LastWriteTime;
            FileSize = (uint)fileInfo.Length;
            Buffer = new byte[FileSize];
            using (FileStream stream = fileInfo.OpenRead())
            {
                stream.Read(Buffer, 0, (int)FileSize);
            }
        }

        public void ReadOffset(BinaryReader reader)
        {
            Offset = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
        }

        public void ReadData(BinaryReader reader)
        {
            Buffer = reader.ReadBytes((int)FileSize);
            Filename = Filename + "." + FileHelper.GetExtensionFromBuffer(Buffer);
        }

        public void ReadFilename(BinaryReader reader)
        {
            Filename = Encoding.ASCII.GetString(reader.ReadBytes(32)).Replace("\0", "");
            Year = reader.ReadUInt16();
            Month = reader.ReadUInt16();
            Day = reader.ReadUInt16();
            Hour = reader.ReadUInt16();
            Minute = reader.ReadUInt16();
            Second = reader.ReadUInt16();
            reader.BaseStream.Seek(4, SeekOrigin.Current); //Ignore file size
        }

        public void WriteOffset(BinaryWriter writer)
        {
            writer.Write(Offset);
            writer.Write(FileSize);
        }

        public void WriteData(BinaryWriter writer)
        {
            writer.Write(Buffer);
        }

        public void WriteFilename(BinaryWriter writer)
        {
            byte[] filenameBuffer = Encoding.ASCII.GetBytes(Filename);
            for (int i = 0; i < 32; i++)
            {
                if (i < filenameBuffer.Length)
                {
                    writer.Write(filenameBuffer[i]);
                }
                else
                {
                    writer.Write('\0');
                }
            }
            writer.Write(Year);
            writer.Write(Month);
            writer.Write(Day);
            writer.Write(Hour);
            writer.Write(Minute);
            writer.Write(Second);
            writer.Write(FileSize);
        }
    }
}
