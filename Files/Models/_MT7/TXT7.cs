using ShenmueDKSharp.Files.Images;
using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Models._MT7
{
    /// <summary>
    /// Embedded texture section from MT7 which holds PVRT entries.
    /// </summary>
    public class TXT7
    {
        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x54, 0x58, 0x54, 0x37 } //TXT7
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


        public uint Offset;
        public uint Identifier;
        public uint Size;
        public uint EntryCount;
        public List<TXT7Entry> Entries = new List<TXT7Entry>();
        public List<Texture> Textures = new List<Texture>();

        public TXT7(BinaryReader reader)
        {
            Offset = (uint)reader.BaseStream.Position;
            Identifier = reader.ReadUInt32();
            Size = reader.ReadUInt32();
            EntryCount = reader.ReadUInt32();

            for (uint i = 0; i < EntryCount; i++)
            {
                uint offset = (EntryCount - i - 1) * 4 + i * 8;
                Entries.Add(new TXT7Entry(reader, offset));
            }

            foreach (TXT7Entry entry in Entries)
            {
                reader.BaseStream.Seek(Offset + entry.Offset, SeekOrigin.Begin);
                Texture tex = new Texture();
                tex.Image = new PVRT(reader);
                tex.ID = entry.ID;
                tex.NameData = entry.NameData;
                Textures.Add(tex);
            }
        }

        public Texture GetTexture(uint ID, byte[] nameData)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                TXT7Entry entry = Entries[i];
                if (Helper.CompareArray(entry.NameData, nameData) && entry.ID == ID)
                {
                    return Textures[i];
                }
            }
            return null;
        }

        public class TXT7Entry
        {
            private readonly static Encoding m_shiftJis = Encoding.GetEncoding("shift_jis");

            public uint Offset;

            public uint ID;
            public byte[] NameData;

            public string Name
            {
                get { return m_shiftJis.GetString(NameData); }
            }

            public TXT7Entry(BinaryReader reader, uint offset)
            {
                Offset = reader.ReadUInt32();

                long position = reader.BaseStream.Position;
                reader.BaseStream.Seek(offset, SeekOrigin.Current);
                ID = reader.ReadUInt32();
                NameData = reader.ReadBytes(4);
                reader.BaseStream.Seek(position, SeekOrigin.Begin);
            }
        }
    }
}
