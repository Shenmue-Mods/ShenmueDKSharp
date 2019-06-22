using ShenmueDKSharp.Files.Images;
using ShenmueDKSharp.Structs;
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
                if (FileHelper.CompareSignature(Identifiers[i], identifier)) return true;
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
                Entries.Add(new TXT7Entry(reader));
            }
            foreach(TXT7Entry entry in Entries)
            {
                entry.ReadTextureID(reader);
            }

            foreach (TXT7Entry entry in Entries)
            {
                reader.BaseStream.Seek(Offset + entry.Offset, SeekOrigin.Begin);
                Texture tex = new Texture();
                tex.Image = new PVRT(reader);
                tex.TextureID = new TextureID(entry.TextureID);
                Textures.Add(tex);
            }
        }

        public Texture GetTexture(TextureID textureID)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                TXT7Entry entry = Entries[i];
                if (entry.TextureID == textureID)
                {
                    return Textures[i];
                }
            }
            Texture tex = new Texture();
            tex.TextureID = textureID;
            return tex;
        }

        public class TXT7Entry
        {
            public uint Offset;
            public TextureID TextureID;

            public TXT7Entry(BinaryReader reader)
            {
                ReadOffset(reader);
            }

            public void ReadOffset(BinaryReader reader)
            {
                Offset = reader.ReadUInt32();
            }

            public void ReadTextureID(BinaryReader reader)
            {
                TextureID = new TextureID(reader);
            }

            public void WriteOffset(BinaryWriter writer)
            {
                writer.Write(Offset);
            }

            public void WriteTextureID(BinaryWriter writer)
            {
                TextureID.Write(writer);
            }
        }
    }
}
