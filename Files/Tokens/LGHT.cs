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
    /// Light definitions
    /// </summary>
    /// <seealso cref="ShenmueDKSharp.Files.BaseFile" />
    public class LGHT : BaseFile
    {
        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x4C, 0x47, 0x48, 0x54 } //LGHT
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
        public List<LGTHEntry> Entries = new List<LGTHEntry>();

        public LGHT() { }
        public LGHT(string filename)
        {
            Read(filename);
        }
        public LGHT(Stream stream)
        {
            Read(stream);
        }
        public LGHT(BinaryReader reader)
        {
            Read(reader);
        }

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
            long pos = reader.BaseStream.Position;
            Identifier = reader.ReadUInt32();
            Size = reader.ReadUInt32();

            while (reader.BaseStream.Position < pos + Size)
            {
                Entries.Add(new LGTHEntry(reader));
            }

            reader.BaseStream.Seek(pos + Size, SeekOrigin.Begin);
        }

        public void Write(BinaryWriter writer)
        {

        }
    }

    public class LGTHEntry
    {
        public string Name;
        public uint Size;
        public uint Identifier;
        public uint InternalSize;

        public LGTHEntry(BinaryReader reader)
        {
            long pos = reader.BaseStream.Position;
            Name = Encoding.ASCII.GetString(reader.ReadBytes(4));
            Size = reader.ReadUInt32();
            Identifier = reader.ReadUInt32();
            InternalSize = reader.ReadUInt32();

            Half halfFloat = new Half(reader);

            reader.BaseStream.Seek(pos + Size, SeekOrigin.Begin);
        }



    }
}
