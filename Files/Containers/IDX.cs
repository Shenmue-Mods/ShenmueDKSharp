using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Containers
{
    public class IDX : BaseFile
    {
        private readonly static Encoding m_shiftJis = Encoding.GetEncoding("shift_jis");

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
                if (Helper.CompareSignature(Identifiers[i], identifier)) return true;
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
            Identifier = reader.ReadUInt32();
            Type = GetIDXType(Identifier);

            if (Type == IDXType.HUMANS)
            {
                throw new NotImplementedException();
                reader.BaseStream.Seek(-4, SeekOrigin.Current);
                EntryCount = reader.ReadUInt16();
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
                    IDXEntry entry = new IDXEntry();
                    entry.AFSIndex = (ushort)i;
                    byte[] buffer = new byte[4];
                    reader.Read(buffer, 0, 4);
                    entry.Filename = Encoding.ASCII.GetString(buffer).Replace("\0", "");
                    Entries.Add(entry);
                }
            }
            else if (Type == IDXType.IDXD)
            {
                reader.BaseStream.Seek(8, SeekOrigin.Current); //SKIP TABL
                uint fileCount = EntryCount;
                for (int i = 0; i < fileCount; i++)
                {
                    IDXEntry entry = new IDXEntry();
                    entry.AFSIndex = (ushort)i;
                    entry.Unknown = reader.ReadUInt32();
                    byte[] buffer = reader.ReadBytes(4);
                    entry.Filename = Encoding.ASCII.GetString(buffer).Replace("\0", "");
                    Entries.Add(entry);
                }
            }
            else if (Type == IDXType.IDXC)
            {
                reader.BaseStream.Seek(8, SeekOrigin.Current); //SKIP TABL
                uint fileCount = EntryCount;
                for (int i = 0; i < fileCount; i++)
                {
                    IDXEntry entry = new IDXEntry();
                    entry.AFSIndex = (ushort)i;
                    entry.Unknown = reader.ReadUInt32();
                    byte[] buffer = reader.ReadBytes(4);
                    entry.Filename = Encoding.ASCII.GetString(buffer).Replace("\0", "");
                    entry.Unknown2 = reader.ReadUInt32();
                    Entries.Add(entry);
                }
            }
            else if (Type == IDXType.IDXB)
            {
                reader.BaseStream.Seek(8, SeekOrigin.Current); //SKIP TABL
                uint fileCount = EntryCount;
                for (int i = 0; i < fileCount; i++)
                {
                    IDXEntry entry = new IDXEntry();
                    entry.AFSIndex = (ushort)i;
                    entry.Unknown = reader.ReadUInt32();
                    byte[] buffer = reader.ReadBytes(4);
                    entry.Filename = Encoding.ASCII.GetString(buffer).Replace("\0", "");
                    entry.Unknown2 = reader.ReadUInt32();
                    entry.Unknown3 = reader.ReadUInt32();
                    Entries.Add(entry);
                }
            }
            else
            {
                for (int i = 0; i < EntryCount; i++)
                {
                    IDXEntry entry = new IDXEntry();
                    byte[] buffer = reader.ReadBytes(12);
                    entry.Filename = m_shiftJis.GetString(buffer).Replace("\0", "");
                    entry.AFSIndex = reader.ReadUInt16();
                    entry.AFSLastIndex = reader.ReadUInt16();
                    entry.Unknown = reader.ReadUInt32();
                    Entries.Add(entry);
                }
            }
        }

        public void Write(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }

        private IDXType GetIDXType(uint data)
        {
            return GetIDXType(BitConverter.GetBytes(data));
        }

        private IDXType GetIDXType(byte[] data)
        {
            if (Helper.CompareSignature(Identifiers[0], data)) return IDXType.IDX0;
            if (Helper.CompareSignature(Identifiers[1], data)) return IDXType.IDXB;
            if (Helper.CompareSignature(Identifiers[2], data)) return IDXType.IDXC;
            if (Helper.CompareSignature(Identifiers[3], data)) return IDXType.IDXD;
            return IDXType.HUMANS;
        }

        public class IDXEntry
        {
            public string Filename { get; set; }
            public ushort AFSIndex { get; set; }
            public ushort AFSLastIndex { get; set; }
            public uint Unknown { get; set; }
            public uint Unknown2 { get; set; }
            public uint Unknown3 { get; set; }
            public IDXType IDXType { get; set; }
        }
    }
}
