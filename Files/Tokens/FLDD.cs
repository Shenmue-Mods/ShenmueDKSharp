using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Tokens
{
    /// <summary>
    /// Camera sequence stuff?
    /// </summary>
    public class FLDD : BaseFile
    {
        public static bool EnableBuffering = true;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x46, 0x4C, 0x44, 0x44 } //FLDD
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

        public FLDD() { }

        protected override void _Read(BinaryReader reader)
        {
            Identifier = reader.ReadUInt32();
            Size = reader.ReadUInt32();
            Name = new String(reader.ReadChars(4));

        }

        protected override void _Write(BinaryWriter writer)
        {

        }
    }
}
