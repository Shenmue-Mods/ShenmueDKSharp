using ShenmueDKSharp.Files.Misc;
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
    /// SPR file container.
    /// Mostly containing sprites in the PVR format.
    /// </summary>
    public class SPR : BaseFile
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<string> Extensions = new List<string>()
        {
            "SPR"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x54, 0x45, 0x58, 0x4E } //TEXN
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

        public List<TEXN> Textures = new List<TEXN>();

        public SPR() { }
        public SPR(string filename)
        {
            Read(filename);
        }
        public SPR(Stream stream)
        {
            Read(stream);
        }
        public SPR(BinaryReader reader)
        {
            Read(reader);
        }

        protected override void _Read(BinaryReader reader)
        {
            uint identifier = reader.ReadUInt32();
            reader.BaseStream.Seek(-4, SeekOrigin.Current);
            if (!TEXN.IsValid(identifier)) return;

            while (reader.BaseStream.CanRead)
            {
                if (reader.BaseStream.Position >= reader.BaseStream.Length - 16) break;
                uint token = reader.ReadUInt32();
                if (token == 0) continue;
                reader.BaseStream.Seek(-4, SeekOrigin.Current);

                TEXN texture = new TEXN(reader);
                if (TextureDatabase.Automatic)
                {
                    TextureDatabase.AddTexture(texture);
                }
                Textures.Add(texture);
            }
        }

        protected override void _Write(BinaryWriter writer)
        {
            foreach(TEXN entry in Textures)
            {
                entry.Write(writer);
            }
        }

        /// <summary>
        /// Unpacks all files into the given folder or, when empty, in an folder next to the SPR file.
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
            foreach(TEXN entry in Textures)
            {
                using (FileStream stream = File.Open(folder + "\\" + entry.FileName, FileMode.Create))
                {
                    entry.Write(stream);
                }
            }
        }

        /// <summary>
        /// Packs the given files into the SPR object.
        /// </summary>
        public void Pack(List<string> filepaths)
        {
            Textures.Clear();
            foreach (string filepath in filepaths)
            {
                TEXN entry = new TEXN(filepath);
                Textures.Add(entry);
            }
        }
    }
}
