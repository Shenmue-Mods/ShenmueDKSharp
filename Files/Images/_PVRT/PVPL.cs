using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Images._PVRT
{
    /// <summary>
    /// PVR palette file
    /// </summary>
    public class PVPL : BaseFile
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x50, 0x56, 0x50, 0x4C }  //PVPL
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

        uint Identifier;
        uint PaletteDataSize;
        uint Type;
        ushort Unknown;
        ushort PaletteEntryCount;

        protected override void _Read(BinaryReader reader)
        {
            Identifier = reader.ReadUInt32();
            PaletteDataSize = reader.ReadUInt32();
            Type = reader.ReadUInt32();
            Unknown = reader.ReadUInt16();
            PaletteEntryCount = reader.ReadUInt16();
            throw new NotImplementedException();
        }

        protected override void _Write(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
