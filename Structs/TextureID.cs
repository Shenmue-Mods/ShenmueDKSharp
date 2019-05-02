using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Structs
{
    public class TextureID
    {
        private readonly static Encoding m_shiftJis = Encoding.GetEncoding("shift_jis");

        public UInt64 Data { get; set; } = 4920561122670497614;

        public string Name
        {
            get
            {
                byte[] data = BitConverter.GetBytes(Data);
                return m_shiftJis.GetString(data);
            }
            set
            {
                byte[] data = m_shiftJis.GetBytes(value);
                Data = BitConverter.ToUInt64(data, 0);
            }
        }

        public TextureID() { }
        public TextureID(TextureID id)
        {
            if (id != null)
            {
                Data = id.Data;
            }
        }
        public TextureID(BinaryReader reader)
        {
            Read(reader);
        }

        public static bool operator ==(TextureID id1, TextureID id2)
        {
            if (id1 is null || id2 is null) return false;
            return id1.Data == id2.Data;
        }

        public static bool operator !=(TextureID id1, TextureID id2)
        {
            return !(id1 == id2);
        }

        public void Read(BinaryReader reader)
        {
            Data = reader.ReadUInt64();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Data);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var texID = obj as TextureID;
            return texID != null && Data == texID.Data;
        }

        public override int GetHashCode()
        {
            return -301143667 + Data.GetHashCode();
        }
    }
}
