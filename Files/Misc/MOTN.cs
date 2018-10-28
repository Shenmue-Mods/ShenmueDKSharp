using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Misc
{
    /// <summary>
    /// Motion file for animations
    /// </summary>
    public class MOTN : BaseFile
    {

        public uint HeaderSize; //can be used as identifier maybe, (also is offset to motion data indices)
        public uint SequenceNameTableOffset;
        public uint MotionDataOffset;

        public uint SequenceCount;
        public uint Size;

        public List<string> SequenceNames = new List<string>();

        public MOTN() { }
        public MOTN(BinaryReader reader)
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
            HeaderSize = reader.ReadUInt32();
            SequenceNameTableOffset = reader.ReadUInt32();
            MotionDataOffset = reader.ReadUInt32();

            SequenceCount = reader.ReadByte();
            reader.BaseStream.Seek(3, SeekOrigin.Current);
            Size = reader.ReadUInt32();

            reader.BaseStream.Seek(SequenceNameTableOffset, SeekOrigin.Begin);
            for (int i = 0; i < SequenceCount; i++)
            {
                uint nameOffset = reader.ReadUInt32();
                long pos = reader.BaseStream.Position;
                reader.BaseStream.Seek(nameOffset, SeekOrigin.Begin);

                char c = reader.ReadChar();
                StringBuilder sb = new StringBuilder();
                while (c != '\0')
                {
                    sb.Append(c);
                    c = reader.ReadChar();
                }
                SequenceNames.Add(sb.ToString());

                reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            }

            reader.BaseStream.Seek(HeaderSize, SeekOrigin.Begin);
            for (int i = 0; i < SequenceCount; i++)
            {
                uint offset1 = reader.ReadUInt32();
                uint offset2 = reader.ReadUInt32();
            }
        }

        public void Write(BinaryWriter writer)
        {
            
        }
    }
}
