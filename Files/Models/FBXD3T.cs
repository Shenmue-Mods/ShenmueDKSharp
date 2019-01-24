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
        }

        protected override void _Write(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
