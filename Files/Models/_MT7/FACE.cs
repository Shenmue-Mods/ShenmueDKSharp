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
    public class FACE
    {
        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x46, 0x41, 0x43, 0x45 }, //FACE
            new byte[4] { 0x46, 0x41, 0x43, 0x58 }  //FACX (could be wrong)
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

        public uint Offset1;
        public uint EntryCount;

        public float[] Floats;

        public FACE(BinaryReader reader)
        {
            Offset = (uint)reader.BaseStream.Position;
            Identifier = reader.ReadUInt32();
            Size = reader.ReadUInt32();

            Offset1 = reader.ReadUInt32();

            reader.BaseStream.Seek(Offset1 * 4, SeekOrigin.Current);

            EntryCount = reader.ReadUInt32(); //each entry 52 bytes

            reader.BaseStream.Seek(68, SeekOrigin.Current);

            Floats = new float[12]; //Some matrix?
            for (int i = 0; i < 12; i++)
            {
                Floats[i] = reader.ReadSingle();
            }



            reader.BaseStream.Seek(Offset + Size, SeekOrigin.Begin);
        }
    }
}
