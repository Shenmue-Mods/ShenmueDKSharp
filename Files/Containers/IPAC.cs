using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Containers
{
    public class IPAC : BaseFile
    {
        public readonly static List<string> Extensions = new List<string>()
        {
            "IPAC"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x49, 0x50, 0x41, 0x43 } //IPAC
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

        public uint Signature { get; set; }
        public uint DictionaryOffset { get; set; }
        public uint FileCount { get; set; }
        public uint ContentSize { get; set; }

        public List<IPACEntry> Entries = new List<IPACEntry>();


        public IPAC() { }
        public IPAC(string filename)
        {
            Read(filename);
        }
        public IPAC(Stream stream)
        {
            Read(stream);
        }
        public IPAC(BinaryReader reader)
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
            long baseOffset = reader.BaseStream.Length;

            Signature = reader.ReadUInt32();
            DictionaryOffset = reader.ReadUInt32();
            FileCount = reader.ReadUInt32();
            ContentSize = reader.ReadUInt32();
            Entries.Clear();

            reader.BaseStream.Seek(baseOffset + ContentSize, SeekOrigin.Begin);
            for (int i = 0; i < FileCount; i++)
            {
                IPACEntry entry = new IPACEntry();
                entry.Read(reader);
                entry.Index = (uint)i;
                Entries.Add(entry);
            }

            foreach(IPACEntry entry in Entries)
            {
                reader.BaseStream.Seek(baseOffset + entry.Offset, SeekOrigin.Begin);
                entry.Buffer = reader.ReadBytes((int)entry.FileSize);
            }
        }

        public void Write(BinaryWriter writer)
        {
            long baseOffset = writer.BaseStream.Length;

            uint offset = DictionaryOffset;
            foreach (IPACEntry entry in Entries)
            {
                entry.Offset = offset;
                offset += entry.FileSize;
                offset += offset - (offset % 16);
                offset += 16; //16 byte padding
            }
            ContentSize = offset;
            FileCount = (uint)Entries.Count;

            writer.Write(Signature);
            writer.Write(DictionaryOffset);
            writer.Write(FileCount);
            writer.Write(ContentSize);

            writer.BaseStream.Seek(baseOffset + ContentSize, SeekOrigin.Begin);
            foreach (IPACEntry entry in Entries)
            {
                entry.Write(writer);
            }

            foreach (IPACEntry entry in Entries)
            {
                writer.BaseStream.Seek(baseOffset + entry.Offset, SeekOrigin.Begin);
                writer.Write(entry.Buffer);
            }
        }

    }

    public class IPACEntry
    {
        public static readonly uint Length = 20;

        private byte[] m_buffer;

        public uint Index { get; set; }

        public string Filename { get; set; }
        public string Extension { get; set; }
        public uint Offset { get; set; }
        public uint FileSize { get; set; }

        public byte[] Buffer
        {
            get
            {
                return m_buffer;
            }
            set
            {
                m_buffer = value;
                FileSize = (uint)value.Length;
            }
        }

        public void Read(BinaryReader reader)
        {
            byte[] buffer = new byte[8];
            reader.Read(buffer, 0, 8);
            Filename = Encoding.ASCII.GetString(buffer).Replace("\0", "");

            buffer = new byte[4];
            reader.Read(buffer, 0, 4);
            Extension = Encoding.ASCII.GetString(buffer).Replace("\0", "");

            Offset = reader.ReadUInt32();
            FileSize = reader.ReadUInt32();
        }

        public void Write(BinaryWriter writer)
        {
            byte[] buffer = new byte[8];
            byte[] fBuffer = Encoding.ASCII.GetBytes(Filename);
            fBuffer.CopyTo(buffer, 0);
            writer.Write(buffer, 0, 8);

            buffer = new byte[4];
            fBuffer = Encoding.ASCII.GetBytes(Extension);
            fBuffer.CopyTo(buffer, 0);
            writer.Write(buffer, 0, 4);

            writer.Write(Offset);
            writer.Write(FileSize);
        }
    }
}
