using ShenmueDKSharp.Files.Misc;
using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Containers
{
    /// <summary>
    /// PKF file container.
    /// Mostly used for texture files with an paired PKS file.
    /// </summary>
    public class PKF : BaseFile
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<string> Extensions = new List<string>()
        {
            "PKF"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x50, 0x41, 0x4B, 0x46 } //PAKF
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

        public uint Identifier { get; set; } = 1179337040;
        public uint ContentSize { get; set; }
        /// <summary>
        /// Could be checksum
        /// </summary>
        public uint Unknown { get; set; }
        public uint FileCount { get; set; }
        public List<PKFEntry> Entries { get; set; } = new List<PKFEntry>();

        public IPAC IPAC { get; set; } = null;

        /// <summary>
        /// True if the read PKF was compressed and can be set if you want to compress the PKF when writing.
        /// </summary>
        public bool Compress { get; set; }
        

        public PKF() { }
        public PKF(string filename)
        {
            Read(filename);
        }
        public PKF(Stream stream)
        {
            Read(stream);
        }
        public PKF(BinaryReader reader)
        {
            Read(reader);
        }

        protected override void _Read(BinaryReader reader)
        {
            byte[] gzipSignature = reader.ReadBytes(2);
            reader.BaseStream.Seek(-2, SeekOrigin.Current);

            Compress = false;
            MemoryStream streamOut = null;
            if (GZ.IsValid(gzipSignature))
            {
                streamOut = new MemoryStream();
                GZipStream streamGZip = new GZipStream(reader.BaseStream, CompressionMode.Decompress);
                streamGZip.CopyTo(streamOut);
                reader = new BinaryReader(streamOut);
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                Compress = true;
            }

            //Read header
            Identifier = reader.ReadUInt32();
            if (!IsValid(Identifier))
            {
                throw new InvalidFileSignatureException();
            }

            ContentSize = reader.ReadUInt32();
            Unknown = reader.ReadUInt32();
            FileCount = reader.ReadUInt32();

            //Check for DUMY
            if (reader.ReadUInt32() == PKFEntry.DUMY_Entry.Token)
            {
                //Skip DUMY
                int dummySize = reader.ReadInt32();
                reader.BaseStream.Seek(dummySize - 8, SeekOrigin.Current);
            }
            else
            {
                reader.BaseStream.Seek(-4, SeekOrigin.Current);
            }

            //Read files
            for (int i = 0; i < FileCount; i++)
            {
                if (reader.BaseStream.Position == reader.BaseStream.Length) break;

                //Test read token to filter out broken PKF files
                uint token = reader.ReadUInt32();
                if (token == 0) continue;
                reader.BaseStream.Seek(-4, SeekOrigin.Current);

                PKFEntry entry = new PKFEntry();
                entry.Read(reader);
                entry.Index = (uint)i;
                Entries.Add(entry);

                if (TextureDatabase.Automatic)
                {
                    if (entry.TokenString.ToUpper() == "TEXN")
                    {
                        using (MemoryStream stream = new MemoryStream(entry.Buffer))
                        {
                            TextureDatabase.AddTexture(new TEXN(stream));
                        }
                    }
                }
            }

            reader.BaseStream.Seek(ContentSize, SeekOrigin.Begin);
            if (reader.BaseStream.CanRead && reader.BaseStream.Position + 8 < reader.BaseStream.Length)
            {
                try
                {
                    IPAC = new IPAC(reader);
                }
                catch (Exception e)
                {

                }
            }

            if (Compress)
            {
                reader.Close();
            }
        }

        protected override void _Write(BinaryWriter writer)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryWriter memoryWriter = new BinaryWriter(memoryStream);
                long baseOffset = memoryWriter.BaseStream.Position;
                FileCount = (uint)Entries.Count;

                //Write header
                memoryWriter.Write(Identifier);
                memoryWriter.Write(ContentSize);
                memoryWriter.Write(Unknown);
                memoryWriter.Write(FileCount);

                //Write DUMY
                PKFEntry.DUMY_Entry.Write(memoryWriter);

                //Write entries
                for (int i = 0; i < Entries.Count; i++)
                {
                    PKFEntry entry = Entries[i];
                    entry.Index = (uint)i;
                    entry.Write(memoryWriter);
                }

                //Write content size into header
                ContentSize = (uint)memoryWriter.BaseStream.Position - (uint)baseOffset;
                memoryWriter.Seek((int)baseOffset + 4, SeekOrigin.Begin);
                memoryWriter.Write(ContentSize);

                memoryStream.Seek(0, SeekOrigin.Begin);

                if (Compress)
                {
                    using (MemoryStream compressedStream = new MemoryStream())
                    {
                        using (GZipStream gZipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                        {
                            memoryStream.CopyTo(gZipStream);
                        }
                        writer.Write(compressedStream.ToArray());
                    }
                }
                else
                {
                    memoryStream.CopyTo(writer.BaseStream);
                }
            }
        }

        /// <summary>
        /// Unpacks all files into the given folder or, when empty, in an folder next to the PKF file.
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
            for (int i = 0; i < Entries.Count; i++)
            {
                PKFEntry entry = (PKFEntry)Entries[i];
                using (FileStream stream = new FileStream(String.Format(folder + "\\file_{0}.{1}", i, entry.TokenString), FileMode.Create))
                {
                    stream.Write(entry.Buffer, 0, entry.Buffer.Length);
                }
            }
            if (IPAC != null)
            {
                IPAC.Unpack(folder);
            }
        }

        /// <summary>
        /// Packs the given files into the PKF object.
        /// The input files must have the same format as the unpack method
        /// or the file entries have to be added manually.
        /// </summary>
        public void Pack(List<string> filepaths)
        {
            Entries.Clear();
            foreach (string filepath in filepaths)
            {
                PKFEntry entry = new PKFEntry();
                entry.TokenString = Path.GetExtension(filepath).Substring(1, 4).ToUpper();
                using (FileStream stream = new FileStream(filepath, FileMode.Open))
                {
                    entry.Size = (uint)stream.Length;
                    entry.Buffer = new byte[stream.Length];
                    stream.Read(entry.Buffer, 0, entry.Buffer.Length);
                }
                Entries.Add(entry);
            }
        }
    }

    public class PKFEntry
    {
        public static PKFEntry DUMY_Entry = new PKFEntry()
        {
            Token = 0x594D5544,
            Size = 20,
            Buffer = new byte[20]
        };

        public uint Index { get; set; }

        public uint Token { get; set; }
        public uint Size { get; set; }
        public string TokenString
        {
            get
            {
                return Encoding.ASCII.GetString(BitConverter.GetBytes(Token));
            }
            set
            {
                Token = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(value), 0);
            }
        }

        public byte[] Buffer;

        public void Read(BinaryReader reader)
        {
            Token = reader.ReadUInt32();
            Size = reader.ReadUInt32();
            reader.BaseStream.Seek(-8, SeekOrigin.Current);
            Buffer = reader.ReadBytes((int)Size);
        }

        public void Write(BinaryWriter writer)
        {
            Array.Copy(BitConverter.GetBytes(Token), 0, Buffer, 0, 4);
            Array.Copy(BitConverter.GetBytes(Size), 0, Buffer, 4, 4);
            writer.Write(Buffer);
        }
    }
}
