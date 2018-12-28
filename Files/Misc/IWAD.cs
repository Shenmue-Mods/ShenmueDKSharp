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
    /// LCD something
    /// </summary>
    public class IWAD : BaseFile
    {
        public static bool EnableBuffering = true;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x49, 0x57, 0x41, 0x44 } //IWAD
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
        public uint Count;
        public uint StringOffset;

        public IWAD() { }

        protected override void _Read(BinaryReader reader)
        {
            Identifier = reader.ReadUInt32();
            Count = reader.ReadUInt32();

            StringOffset = reader.ReadUInt32();

        }

        protected override void _Write(BinaryWriter writer)
        {

        }
    }
}
