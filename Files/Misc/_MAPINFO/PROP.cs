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
    /// Props positions
    /// </summary>
    public class PROP : BaseFile
    {
        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x50, 0x52, 0x4F, 0x50 } //PROP
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
        private uint ContentOffset;

        public uint Identifier;
        public uint Size;
        public uint FooterOffset;

        public uint PositionsOffset;
        public uint FloatsOffset1;
        public uint Offset3;
        public uint Offset4;
        public uint Offset5;
        public uint Offset6;
        public uint Offset7;
        public uint Offset8;

        public PROP() { }

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
            Size = reader.ReadUInt32();

            ContentOffset = (uint)reader.BaseStream.Position;
            FooterOffset = reader.ReadUInt32();

            reader.BaseStream.Seek(FooterOffset + ContentOffset, SeekOrigin.Begin);

            PositionsOffset = reader.ReadUInt32();
            FloatsOffset1 = reader.ReadUInt32();
            Offset3 = reader.ReadUInt32();
            Offset4 = reader.ReadUInt32();
            Offset5 = reader.ReadUInt32();
            Offset6 = reader.ReadUInt32();
            Offset7 = reader.ReadUInt32();
            Offset8 = reader.ReadUInt32();
        }

        public void Write(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
