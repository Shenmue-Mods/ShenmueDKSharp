using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Subtitles
{
    /// <summary>
    /// Subtitle file for all the dialogs in Shenmue I and II.
    /// </summary>
    /// <seealso cref="ShenmueDKSharp.Files.BaseFile" />
    public class SUB : BaseFile
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<string> Extensions = new List<string>()
        {
            "SUB"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x03, 0x00, 0x00, 0x00 }
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

        public SUB() { }
        public SUB(string filename)
        {
            Read(filename);
        }
        public SUB(Stream stream)
        {
            Read(stream);
        }
        public SUB(BinaryReader reader)
        {
            Read(reader);
        }

        public uint Identifier = 3;
        public uint EntryCount;
        public List<SUBEntry> Entries = new List<SUBEntry>();

        protected override void _Read(BinaryReader reader)
        {
            long baseOffset = reader.BaseStream.Length;

            Identifier = reader.ReadUInt32();
            EntryCount = reader.ReadUInt32();
            reader.BaseStream.Seek(8, SeekOrigin.Current);

            //Read entries
            for(int i = 0; i < EntryCount; i++)
            {
                Entries.Add(new SUBEntry(reader));
            }

            long textOffset = reader.BaseStream.Position;

            //Read text for entries
            foreach(SUBEntry entry in Entries)
            {
                reader.BaseStream.Seek(textOffset + entry.Offset, SeekOrigin.Begin);
                entry.ReadText(reader);
            }
        }

        protected override void _Write(BinaryWriter writer)
        {
            long baseOffset = writer.BaseStream.Length;

            EntryCount = (uint)Entries.Count;

            writer.Write(Identifier);
            writer.Write(EntryCount);
            writer.BaseStream.Seek(8, SeekOrigin.Current);

            //Calculate offsets
            uint offset = 0;
            foreach(SUBEntry entry in Entries)
            {
                entry.Offset = offset;
                offset += (uint)entry.TextBuffer.Length + 1;
            }

            //Write entries
            foreach(SUBEntry entry in Entries)
            {
                entry.Write(writer);
            }

            //Write text for entries
            foreach(SUBEntry entry in Entries)
            {
                entry.WriteText(writer);
            }
        }

        private byte[] ReadText(BinaryReader reader)
        {
            List<byte> data = new List<byte>();
            byte c = reader.ReadByte();
            while(c != 0x00)
            {
                data.Add(c);
                c = reader.ReadByte();
            }
            return data.ToArray();
        }
    }


    public class SUBEntry
    {
        public uint Offset;
        public string Name;
        public byte[] TextBuffer;

        public SUBEntry() { }

        public SUBEntry(BinaryReader reader)
        {
            Read(reader);
        }

        public string GetText(Encoding encoding)
        {
            return encoding.GetString(TextBuffer);
        }

        public void SetText(Encoding encoding, string value)
        {
            TextBuffer = encoding.GetBytes(value);
        }

        public void Read(BinaryReader reader)
        {
            byte[] buffer = reader.ReadBytes(24);
            Name = Encoding.ASCII.GetString(buffer).Replace("\0", "");
            Offset = reader.ReadUInt32();
        }

        public void Write(BinaryWriter writer)
        {
            byte[] buffer = new byte[24];
            byte[] fbuffer = Encoding.ASCII.GetBytes(Name);
            fbuffer.CopyTo(buffer, 0);
            writer.Write(buffer);
            writer.Write(Offset);
        }

        public void ReadText(BinaryReader reader)
        {
            List<byte> data = new List<byte>();
            byte c = reader.ReadByte();
            while (c != 0x00)
            {
                data.Add(c);
                c = reader.ReadByte();
            }
            TextBuffer = data.ToArray();
        }

        public void WriteText(BinaryWriter writer)
        {
            writer.Write(TextBuffer);
            writer.Write('\0');
        }
    }
}
