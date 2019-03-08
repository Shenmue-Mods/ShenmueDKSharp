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
    /// LGHT Token.
    /// Light definitions section.
    /// </summary>
    public class LGHT : BaseToken
    {
        public static string Identifier = "LGHT";

        public List<LGTHEntry> Entries = new List<LGTHEntry>();

        protected override void _Read(BinaryReader reader)
        {
            while (reader.BaseStream.Position < Position + Size)
            {
                Entries.Add(new LGTHEntry(reader));
            }

            reader.BaseStream.Seek(Position + Size, SeekOrigin.Begin);
        }

        protected override void _Write(BinaryWriter writer)
        {
            writer.Write(Content);
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
