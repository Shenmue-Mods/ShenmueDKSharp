using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Tokens
{
    public abstract class BaseToken
    {
        public uint Position { get; set; }

        public string Token { get; set; }
        public UInt32 Size { get; set; }
        public byte[] Content { get; set; }
        public List<BaseToken> Tokens { get; set; } = new List<BaseToken>();

        public void Read(BinaryReader reader)
        {
            Position = (uint)reader.BaseStream.Position;
            Token = new String(reader.ReadChars(4));
            Size = reader.ReadUInt32();
            long pos = reader.BaseStream.Position;
            Content = reader.ReadBytes((int)(Size - 8));
            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            _Read(reader);
        }

        public void Write(BinaryWriter writer)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (BinaryWriter contentWriter = new BinaryWriter(memoryStream))
            {
                _Write(contentWriter);
            }
            Content = memoryStream.ToArray();
            Size = (uint)(Content.Length + 8);
            writer.WriteASCII(Token);
            writer.Write(Size);
            writer.Write(Content);
        }

        protected abstract void _Read(BinaryReader reader);
        protected abstract void _Write(BinaryWriter writer);

        public override string ToString()
        {
            return Token;
        }
    }
}
