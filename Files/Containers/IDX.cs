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
    /// AFS filename indexing file (table of content in some way).
    /// TODO: Writing for all IDX formats
    /// </summary>
    public class IDX : BaseFile
    {
        public static bool EnableBuffering = true;
        public override bool BufferingEnabled => EnableBuffering;


        public readonly static List<string> Extensions = new List<string>()
        {
            "IDX"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[] { 0x49, 0x44, 0x58, 0x30 }, //IDX0
            new byte[] { 0x49, 0x44, 0x58, 0x42 }, //IDXB
            new byte[] { 0x49, 0x44, 0x58, 0x43 }, //IDXC
            new byte[] { 0x49, 0x44, 0x58, 0x44 }  //IDXD
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

        public enum IDXType
        {
            IDX0,
            IDXB,
            IDXC,
            IDXD,
            HUMANS
        }

        public uint Identifier;
        public ushort EntryCountSelf;
        public ushort EntryCount;
        public IDXType Type;
        public uint Unknown;
        public List<IDXEntry> Entries { get; set; } = new List<IDXEntry>();

        public IDX() { }
        public IDX(string filepath)
        {
            Read(filepath);
        }
        public IDX(Stream stream)
        {
            Read(stream);
        }
        public IDX(BinaryReader reader)
        {
            Read(reader);
        }

        protected override void _Read(BinaryReader reader)
        {
            Identifier = reader.ReadUInt32();
            Type = GetIDXType(Identifier);

            if (Type == IDXType.HUMANS)
            {
                reader.BaseStream.Seek(-4, SeekOrigin.Current);
                EntryCount = (ushort)reader.ReadUInt32();
            }
            else if (Type == IDXType.IDXD)
            {
                Unknown = reader.ReadUInt32();
                EntryCountSelf = reader.ReadUInt16();
                EntryCount = reader.ReadUInt16();
                reader.BaseStream.Seek(4, SeekOrigin.Current);
            }
            else if (Type == IDXType.IDXC)
            {
                Unknown = reader.ReadUInt32();
                EntryCountSelf = reader.ReadUInt16();
                EntryCount = reader.ReadUInt16();
                reader.BaseStream.Seek(4, SeekOrigin.Current);
            }
            else if (Type == IDXType.IDXB)
            {
                Unknown = reader.ReadUInt32();
                EntryCountSelf = reader.ReadUInt16();
                EntryCount = reader.ReadUInt16();
                reader.BaseStream.Seek(4, SeekOrigin.Current);
            }
            else
            {
                EntryCountSelf = reader.ReadUInt16();
                EntryCount = reader.ReadUInt16();
                reader.BaseStream.Seek(12, SeekOrigin.Current);
            }


            if (Type == IDXType.HUMANS)
            {
                uint fileCount = EntryCount;
                for (int i = 0; i < fileCount; i++)
                {
                    IDXEntry entryPKF = new IDXEntry(reader, Type);
                    entryPKF.AFSIndex = (ushort)i;
                    Entries.Add(entryPKF);

                    //Add entry two times (for PKF and PKS)
                    IDXEntry entryPKS = new IDXEntry(entryPKF);
                    entryPKS.AFSIndex = (ushort)(i + 1);
                    Entries.Add(entryPKS); 
                }
            }
            else if (Type == IDXType.IDXD)
            {
                reader.BaseStream.Seek(8, SeekOrigin.Current); //SKIP TABL
                uint fileCount = EntryCount;
                for (int i = 0; i < fileCount; i++)
                {
                    IDXEntry entry = new IDXEntry(reader, Type);
                    entry.AFSIndex = (ushort)i;
                    Entries.Add(entry);
                }
            }
            else if (Type == IDXType.IDXC)
            {
                reader.BaseStream.Seek(8, SeekOrigin.Current); //SKIP TABL
                uint fileCount = EntryCount;
                for (int i = 0; i < fileCount; i++)
                {
                    IDXEntry entry = new IDXEntry(reader, Type);
                    entry.AFSIndex = (ushort)i;
                    Entries.Add(entry);
                }
            }
            else if (Type == IDXType.IDXB)
            {
                reader.BaseStream.Seek(8, SeekOrigin.Current); //SKIP TABL
                uint fileCount = EntryCount;
                for (int i = 0; i < fileCount; i++)
                {
                    IDXEntry entry = new IDXEntry(reader, Type);
                    entry.AFSIndex = (ushort)i;
                    Entries.Add(entry);
                }
            }
            else
            {
                for (int i = 0; i < EntryCount; i++)
                {
                    IDXEntry entry = new IDXEntry(reader, Type);
                    Entries.Add(entry);
                }
            }
        }

        protected override void _Write(BinaryWriter writer)
        {
            if (Type == IDXType.HUMANS)
            {
                throw new NotImplementedException();
                writer.Write(EntryCount);
            }
            else
            {
                writer.Write(Identifier);
                if (Type == IDXType.IDXD)
                {
                    writer.Write(Unknown);
                    writer.Write(EntryCountSelf);
                    writer.Write(EntryCount);
                    writer.BaseStream.Seek(4, SeekOrigin.Current);
                }
                else if (Type == IDXType.IDXC)
                {
                    writer.Write(Unknown);
                    writer.Write(EntryCountSelf);
                    writer.Write(EntryCount);
                    writer.BaseStream.Seek(4, SeekOrigin.Current);
                }
                else if (Type == IDXType.IDXB)
                {
                    writer.Write(Unknown);
                    writer.Write(EntryCountSelf);
                    writer.Write(EntryCount);
                    writer.BaseStream.Seek(4, SeekOrigin.Current);
                }
                else
                {
                    writer.Write(EntryCountSelf);
                    writer.Write(EntryCount);
                    writer.BaseStream.Seek(12, SeekOrigin.Current);
                }
            }
            
            if (Type == IDXType.HUMANS)
            {
                for (int i = 0; i < EntryCount; i++)
                {
                    IDXEntry entry = Entries[i];
                    entry.Write(writer);
                }
            }
            else if (Type == IDXType.IDXD)
            {
                throw new NotImplementedException();
                //TODO: Write table
                for (int i = 0; i < EntryCount; i++)
                {
                    IDXEntry entry = Entries[i];
                    entry.Write(writer);
                }
            }
            else if (Type == IDXType.IDXC)
            {
                throw new NotImplementedException();
                //TODO: Write table
                for (int i = 0; i < EntryCount; i++)
                {
                    IDXEntry entry = Entries[i];
                    entry.Write(writer);
                }
            }
            else if (Type == IDXType.IDXB)
            {
                throw new NotImplementedException();
                //TODO: Write table
                for (int i = 0; i < EntryCount; i++)
                {
                    IDXEntry entry = Entries[i];
                    entry.Write(writer);
                }
            }
            else
            {
                for (int i = 0; i < EntryCount; i++)
                {
                    IDXEntry entry = Entries[i];
                    entry.Write(writer);
                }
            }
        }

        private IDXType GetIDXType(uint data)
        {
            return GetIDXType(BitConverter.GetBytes(data));
        }

        private IDXType GetIDXType(byte[] data)
        {
            if (FileHelper.CompareSignature(Identifiers[0], data)) return IDXType.IDX0;
            if (FileHelper.CompareSignature(Identifiers[1], data)) return IDXType.IDXB;
            if (FileHelper.CompareSignature(Identifiers[2], data)) return IDXType.IDXC;
            if (FileHelper.CompareSignature(Identifiers[3], data)) return IDXType.IDXD;
            return IDXType.HUMANS;
        }
    }

    public class IDXEntry
    {
        private readonly static Encoding m_shiftJis = Encoding.GetEncoding("shift_jis");

        public string Filename { get; set; }
        public ushort AFSIndex { get; set; }
        public ushort AFSLastIndex { get; set; }
        public uint Unknown { get; set; }
        public uint Unknown2 { get; set; }
        public uint Unknown3 { get; set; }
        public IDX.IDXType IDXType { get; set; }

        public IDXEntry() { }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public IDXEntry(IDXEntry entry)
        {
            Filename = entry.Filename;
            AFSIndex = entry.AFSIndex;
            Unknown = entry.Unknown;
            Unknown2 = entry.Unknown2;
            Unknown3 = entry.Unknown3;
            IDXType = entry.IDXType;
        }

        public IDXEntry(BinaryReader reader, IDX.IDXType type)
        {
            Read(reader, type);
        }

        public void Read(BinaryReader reader, IDX.IDXType type)
        {
            IDXType = type;
            if (type == IDX.IDXType.HUMANS)
            {
                byte[] buffer = reader.ReadBytes(4);
                Filename = Encoding.ASCII.GetString(buffer).Replace("\0", "");
            }
            else if (type == IDX.IDXType.IDXD)
            {
                Unknown = reader.ReadUInt32();
                byte[] buffer = reader.ReadBytes(4);
                Filename = Encoding.ASCII.GetString(buffer).Replace("\0", "");
            }
            else if (type == IDX.IDXType.IDXC)
            {
                Unknown = reader.ReadUInt32();
                byte[] buffer = reader.ReadBytes(4);
                Filename = Encoding.ASCII.GetString(buffer).Replace("\0", "");
                Unknown2 = reader.ReadUInt32();
            }
            else if (type == IDX.IDXType.IDXB)
            {
                Unknown = reader.ReadUInt32();
                byte[] buffer = reader.ReadBytes(4);
                Filename = Encoding.ASCII.GetString(buffer).Replace("\0", "");
                Unknown2 = reader.ReadUInt32();
                Unknown3 = reader.ReadUInt32();
            }
            else if (type == IDX.IDXType.IDX0)
            {
                byte[] buffer = reader.ReadBytes(12);
                Filename = m_shiftJis.GetString(buffer).Replace("\0", "");
                AFSIndex = reader.ReadUInt16();
                AFSLastIndex = reader.ReadUInt16();
                Unknown = reader.ReadUInt32();
            }
        }

        public void Write(BinaryWriter writer)
        {
            if (IDXType == IDX.IDXType.HUMANS)
            {
                byte[] buffer = new byte[4];
                Array.Copy(Encoding.ASCII.GetBytes(Filename), 0, buffer, 0, buffer.Length);
                writer.Write(buffer);
            }
            else if (IDXType == IDX.IDXType.IDXD)
            {
                writer.Write(Unknown);
                byte[] buffer = new byte[4];
                Array.Copy(Encoding.ASCII.GetBytes(Filename), 0, buffer, 0, buffer.Length);
                writer.Write(buffer);
            }
            else if (IDXType == IDX.IDXType.IDXC)
            {
                writer.Write(Unknown);
                byte[] buffer = new byte[4];
                Array.Copy(Encoding.ASCII.GetBytes(Filename), 0, buffer, 0, buffer.Length);
                writer.Write(buffer);
                writer.Write(Unknown2);
            }
            else if (IDXType == IDX.IDXType.IDXB)
            {
                writer.Write(Unknown);
                byte[] buffer = new byte[4];
                Array.Copy(Encoding.ASCII.GetBytes(Filename), 0, buffer, 0, buffer.Length);
                writer.Write(buffer);
                writer.Write(Unknown2);
                writer.Write(Unknown3);
            }
            else if (IDXType == IDX.IDXType.IDX0)
            {
                byte[] buffer = new byte[12];
                Array.Copy(m_shiftJis.GetBytes(Filename), 0, buffer, 0, buffer.Length);
                writer.Write(AFSIndex);
                writer.Write(AFSLastIndex);
                writer.Write(Unknown);
            }
        }
    }
}
