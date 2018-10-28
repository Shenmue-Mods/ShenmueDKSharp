using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Misc
{
    public class DYNM : BaseFile
    {
        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x44, 0x59, 0x4E, 0x41 } //DYNA
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

        public uint Identifier;
        public uint Size;
        public string Name;

        public uint Offset1;
        public uint Offset2;
        public uint EntryCount;

        public DYNM() { }

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
            Size = reader.ReadUInt32();
            Name = new String(reader.ReadChars(4));

            Offset1 = reader.ReadUInt32();
            Offset2 = reader.ReadUInt32();

            //this repeats often put in own class
            EntryCount = reader.ReadUInt32(); // * 8 for bytes
            
        }

        public void Write(BinaryWriter writer)
        {

        }
    }
}
