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
    /// MDPX/MDP7 Node
    /// </summary>
    public class MDPX
    {
        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x4D, 0x44, 0x50, 0x58 }, //MDPX
            new byte[4] { 0x4D, 0x44, 0x50, 0x37 }  //MDP7
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

        public uint Token;
        public uint Size;
        public byte[] NameData;
        public string Name;

        public byte[] Data;

        public MDPX(BinaryReader reader)
        {
            long position = reader.BaseStream.Position;
            Token = reader.ReadUInt32();
            Size = reader.ReadUInt32();
            NameData = reader.ReadBytes(4);
            Name = Encoding.ASCII.GetString(NameData);

            Data = reader.ReadBytes((int)Size - 12);
        }
            

    }
}
