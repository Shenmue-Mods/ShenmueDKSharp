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
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<string> Extensions = new List<string>()
        {
            "TEXN"
        };

        private static Encoding m_shiftJis = Encoding.GetEncoding("shift_jis");

        /// <summary>
        /// The header size including the TEXN identifier, the entry size and the name/id data.
        /// </summary>
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
                if (FileHelper.CompareSignature(Identifiers[i], identifier)) return true;
            }
            return false;
        }

        public uint Offset;

        public uint Identifier { get; set; }
        /// <summary>
        /// Entry size including the texture size
        /// </summary>
        public uint EntrySize { get; set; }

        /// <summary>
        /// Unique texture ID with name.
        /// Used for identification of textures.
        /// </summary>
        public UInt64 ID_Name { get; set; }

        public uint TextureID { get; set; }
        public byte[] NameData { get; set; }

        public string Name
        {
            get
            {
                return m_shiftJis.GetString(NameData);
            }
        }

        public PVRT Texture { get; set; }


        public TEXN() { }
        public TEXN(string filepath)
        {
            Read(filepath);
        }
        public TEXN(Stream stream)
        {
            Read(stream);
        }
        public TEXN(BinaryReader reader)
        {
            Read(reader);
        }

        protected override void _Read(BinaryReader reader)
        {
            Offset = (uint)reader.BaseStream.Position;

            Identifier = reader.ReadUInt32();
            EntrySize = reader.ReadUInt32();

            ID_Name = reader.ReadUInt64();
            reader.BaseStream.Seek(-8, SeekOrigin.Current);
            TextureID = reader.ReadUInt32();
            NameData = reader.ReadBytes(4);

            FileName = String.Format("{0}.{1}.TEXN", Helper.ByteArrayToString(BitConverter.GetBytes(ID_Name)), Name.Replace("\0", "_"));

            Texture = new PVRT(reader);
            Texture.FileName = String.Format("{0}.{1}.PVR", Helper.ByteArrayToString(BitConverter.GetBytes(ID_Name)), Name.Replace("\0", "_"));

            reader.BaseStream.Seek(Offset + EntrySize, SeekOrigin.Begin);
        }

        protected override void _Write(BinaryWriter writer)
        {
            EntrySize = (uint)Texture.DataSize + HeaderSize;

            writer.Write(Identifier);
            writer.Write(EntrySize);

            writer.Write(TextureID);
            writer.Write(NameData);

            //TODO: Write buffer until we can convert all texture formats back and forth.
            Texture.WriteBuffer(writer); 
        }

    }
}
