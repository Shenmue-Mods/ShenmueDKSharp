using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Models
{
    public class FBXD3T : BaseModel
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;
        
        public readonly static List<string> Extensions = new List<string>()
        {
            "FBX"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x12, 0x98, 0xEE, 0x51 }
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
        public uint UnknownEntriesSize;
        public UInt64 ContentSize;
        public UInt64 StringsOffset;
        public UInt64 TextureDefinitionOffset;

        public UInt64 TextureCount_1;
        public UInt64 NodeCount_1;

        public uint TextureCount_2;
        public uint NodeCount_2;

        public uint StringsSize;
        public List<string> Strings = new List<string>();

        public List<uint> UnknownEntries = new List<uint>();

        public FBXD3T(BaseModel model)
        {
            model.CopyTo(this);
            FilePath = Path.ChangeExtension(model.FilePath, "fbx");
        }
        public FBXD3T(string filepath)
        {
            Read(filepath);
        }
        public FBXD3T(Stream stream)
        {
            Read(stream);
        }
        public FBXD3T(BinaryReader reader)
        {
            Read(reader);
        }

        protected override void _Read(BinaryReader reader)
        {
            Identifier = reader.ReadUInt32();
            UnknownEntriesSize = reader.ReadUInt32();
            ContentSize = reader.ReadUInt64();
            StringsOffset = reader.ReadUInt64();
            TextureDefinitionOffset = reader.ReadUInt64();

            TextureCount_1 = reader.ReadUInt64();
            NodeCount_1 = reader.ReadUInt64();

            reader.BaseStream.Seek(0x3C, SeekOrigin.Begin);
            TextureCount_2 = reader.ReadUInt32();
            NodeCount_2 = reader.ReadUInt32();

            for (int i = 0; i < UnknownEntriesSize; i += 4)
            {
                UnknownEntries.Add(reader.ReadUInt32());
            }

            StringsSize = reader.ReadUInt32();
            long stringsEndPos = reader.BaseStream.Position + StringsSize;
            if (stringsEndPos % 4 != 0)
            {
                stringsEndPos += 4 - (stringsEndPos % 4);
            }
            
            string tmpString = "";
            while (reader.BaseStream.Position < stringsEndPos)
            {
                char character = reader.ReadChar();
                if (character == 0x00)
                {
                    if (String.IsNullOrEmpty(tmpString)) continue;
                    Strings.Add(tmpString);
                    tmpString = "";
                }
                else
                {
                    tmpString += character;
                }
            }

            reader.BaseStream.Seek(0x28, SeekOrigin.Begin);
            uint _0x24 = reader.ReadUInt32(); //0x28
            uint _0x0A8 = reader.ReadUInt32(); //0x2C
            uint _0x0B8 = reader.ReadUInt32(); //0x30
            uint _0x40 = reader.ReadUInt32(); //0x34
            uint _r14d = reader.ReadUInt32(); //0x38
            uint _0x58 = reader.ReadUInt32(); //0x3C
            uint _0x78 = reader.ReadUInt32(); //0x40
            uint _0x98 = reader.ReadUInt32(); //0x44
            uint _r15d = reader.ReadUInt32(); //0x48
            uint _0x0C8 = reader.ReadUInt32(); //0x4C
            uint _esi = reader.ReadUInt32(); //0x50
            uint _0x148 = reader.ReadUInt32(); //0x54
            uint _0x168 = reader.ReadUInt32(); //0x58

            uint _rcx = _0x168 + _r15d * 4;
            uint _rdi = _rcx + _rcx * 2;
            _rcx = _0x40 * 0x23;
            _rdi = _rdi + _rcx;
            _rdi = _rdi + _0x148;
            _rdi = _rdi << 2; //Malloc Size
        }

        protected override void _Write(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
