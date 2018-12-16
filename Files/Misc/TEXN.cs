using ShenmueDKSharp.Files.Images;
using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Misc
{
    /// <summary>
    /// Texture entry
    /// </summary>
    /// <seealso cref="ShenmueDKSharp.Files.BaseFile" />
    public class TEXN : BaseFile
    {
        private static Encoding m_shiftJis = Encoding.GetEncoding("shift_jis");
        private static uint HeaderSize = 16;

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x54, 0x45, 0x58, 0x4E } //TEXN
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

        public uint Identifier { get; set; }
        /// <summary>
        /// Entry size including the texture size
        /// </summary>
        public uint EntrySize { get; set; }

        public byte[] IDNameData { get; set; }
        public uint TextureID { get; set; }
        public byte[] NameData { get; set; }
        public string Name
        {
            get
            {
                return m_shiftJis.GetString(NameData);
            }
        }

        public BaseImage Texture;


        public TEXN() { }

        public TEXN(BinaryReader reader)
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
            Offset = (uint)reader.BaseStream.Position;

            Identifier = reader.ReadUInt32();
            EntrySize = reader.ReadUInt32();

            IDNameData = reader.ReadBytes(8);
            reader.BaseStream.Seek(-8, SeekOrigin.Current);
            TextureID = reader.ReadUInt32();
            NameData = reader.ReadBytes(4);

            FileName = String.Format("{0}.{1}.TEXN", Helper.ByteArrayToString(IDNameData), Name.Replace("\0", "_"));

            Texture = new PVRT(reader);
            Texture.FileName = String.Format("{0}.{1}.PVR", Helper.ByteArrayToString(IDNameData), Name.Replace("\0", "_"));

            reader.BaseStream.Seek(Offset + EntrySize, SeekOrigin.Begin);
        }

        public void Write(BinaryWriter writer)
        {
            EntrySize = (uint)Texture.DataSize + HeaderSize;

            writer.Write(Identifier);
            writer.Write(EntrySize);

            writer.Write(TextureID);
            writer.Write(NameData);

            Texture.Write(writer);
        }

    }
}
