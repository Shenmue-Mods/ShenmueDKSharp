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
        public static bool EnableBuffering = true;
        public override bool BufferingEnabled => EnableBuffering;

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
                if (FileHelper.CompareSignature(Identifiers[i], identifier)) return true;
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

        protected override void _Read(BinaryReader reader)
        {
            Identifier = reader.ReadUInt32();
            Size = reader.ReadUInt32();
            Name = new String(reader.ReadChars(4));

            Offset1 = reader.ReadUInt32();
            Offset2 = reader.ReadUInt32();

            //this repeats often put in own class
            EntryCount = reader.ReadUInt32(); // * 8 for bytes
            
        }

        protected override void _Write(BinaryWriter writer)
        {

        }
    }
}
