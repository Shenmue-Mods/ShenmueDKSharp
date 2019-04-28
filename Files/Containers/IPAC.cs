using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Containers
{
    /// <summary>
    /// IPAC file container
    /// </summary>
    /// <seealso cref="ShenmueDKSharp.Files.BaseFile" />
    public class IPAC : BaseFile
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

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
                if (FileHelper.CompareSignature(Identifiers[i], identifier)) return true;
            }
            return false;
        }

        public uint Signature { get; set; } = 1128353865;
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

        protected override void _Read(BinaryReader reader)
        {
            long baseOffset = reader.BaseStream.Position;

            //Read header
            Signature = reader.ReadUInt32();
            if (!IsValid(Signature))
            {
                throw new InvalidFileSignatureException();
            }

            DictionaryOffset = reader.ReadUInt32();
            FileCount = reader.ReadUInt32();
            ContentSize = reader.ReadUInt32();
            Entries.Clear();

            //Read the table of content
            reader.BaseStream.Seek(baseOffset + DictionaryOffset, SeekOrigin.Begin);
            for (int i = 0; i < FileCount; i++)
            {
                IPACEntry entry = new IPACEntry();
                entry.Read(reader);
                entry.Index = (uint)i;
                Entries.Add(entry);
            }

            //Read the data to the buffer of the table of content entries
            foreach(IPACEntry entry in Entries)
            {
                reader.BaseStream.Seek(baseOffset + entry.Offset, SeekOrigin.Begin);
                entry.Buffer = reader.ReadBytes((int)entry.FileSize);
            }
        }

        protected override void _Write(BinaryWriter writer)
        {
            long baseOffset = writer.BaseStream.Length;
            FileCount = (uint)Entries.Count;

            //Calculate offsets
            uint offset = 16;
            foreach (IPACEntry entry in Entries)
            {
                entry.Offset = offset;
                offset += entry.FileSize;
                offset += 16 - (offset % 16);
                offset += 16; //16 byte padding
            }
            ContentSize = offset;
            DictionaryOffset = ContentSize;

            //Write header
            writer.Write(Signature);
            writer.Write(DictionaryOffset);
            writer.Write(FileCount);
            writer.Write(ContentSize);

            //Write the data of the table of content files
            foreach (IPACEntry entry in Entries)
            {
                writer.BaseStream.Seek(baseOffset + entry.Offset, SeekOrigin.Begin);
                writer.Write(entry.Buffer);
            }

            //Write table of contents
            writer.BaseStream.Seek(baseOffset + DictionaryOffset, SeekOrigin.Begin);
            for (int i = 0; i < Entries.Count; i++)
            {
                IPACEntry entry = Entries[i];
                entry.Index = (uint)i;
                entry.Write(writer);
            }
        }

        /// <summary>
        /// Unpacks all files into the given folder or, when empty, in an folder next to the IPAC file.
        /// </summary>
        public void Unpack(string folder = "")
        {
            if (String.IsNullOrEmpty(folder))
            {
                folder = Path.GetDirectoryName(FilePath) + "\\_" + FileName + "_";
            }
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            foreach (IPACEntry entry in Entries)
            {
                if (String.IsNullOrEmpty(entry.Filename) || String.IsNullOrEmpty(entry.Extension)) continue;
                using (FileStream stream = new FileStream(String.Format(folder + "\\{0}.{1}", entry.Filename, entry.Extension), FileMode.Create))
                {
                    stream.Write(entry.Buffer, 0, entry.Buffer.Length);
                }
            }
        }

        /// <summary>
        /// Packs the given files into the IPAC object.
        /// The input files must have the same format as the unpack method
        /// or the file entries have to be added manually.
        /// </summary>
        public void Pack(List<string> filepaths)
        {
            Entries.Clear();
            foreach (string filepath in filepaths)
            {
                IPACEntry entry = new IPACEntry();
                entry.Extension = Path.GetExtension(filepath).Substring(1, 4).ToUpper();
                entry.Filename = Path.GetFileNameWithoutExtension(filepath).ToUpper();
                using (FileStream stream = new FileStream(filepath, FileMode.Open))
                {
                    entry.FileSize = (uint)stream.Length;
                    entry.Buffer = new byte[stream.Length];
                    stream.Read(entry.Buffer, 0, entry.Buffer.Length);
                }
                Entries.Add(entry);
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
