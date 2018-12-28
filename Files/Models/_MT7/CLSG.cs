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
    /// TODO
    /// </summary>
    public class CLSG
    {
        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x43, 0x4C, 0x53, 0x47 }, //CLSG
            new byte[4] { 0x43, 0x4C, 0x53, 0x58 }  //CLSX
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

        public float Unknown;

        public uint CLTHOffset;
        public uint OSAGOffset;
        public uint CLCLOffset;

        public CLSG(BinaryReader reader)
        {
            Offset = (uint)reader.BaseStream.Position;
            Identifier = reader.ReadUInt32();
            Size = reader.ReadUInt32();

            Unknown = reader.ReadSingle();

            CLTHOffset = reader.ReadUInt32();
            OSAGOffset = reader.ReadUInt32();
            CLCLOffset = reader.ReadUInt32();

            reader.BaseStream.Seek(Offset + Size, SeekOrigin.Begin);
        }

    }
}
