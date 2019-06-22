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

        public string HexStr
        {
            get {
                StringBuilder sb = new StringBuilder();
                byte[] data = BitConverter.GetBytes(Data);
                for (int i = 0; i < 8; i++)
                {
                    sb.Append(data[i].ToString("X"));
                }
                return sb.ToString();
            }
            set
            {
                byte[] data = new byte[8];
                for (int i = 0; i < value.Length / 2; i++)
                {
                    data[i] = (byte)int.Parse(value.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
                }
                Data = BitConverter.ToUInt64(data, 0);
            }
        }

        public string Name
        {
            get
            {
                byte[] data = BitConverter.GetBytes(Data);
                byte[] trimmed = new byte[7];
                Array.Copy(data, 1, trimmed, 0, 7);
                return m_shiftJis.GetString(trimmed);
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

        public TextureID(string hexStr)
        {
            HexStr = hexStr;
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
