using ShenmueDKSharp.Files.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Containers
{
    public class SPR : BaseFile
    {
        public readonly static List<string> Extensions = new List<string>()
        {
            "SPR"
        };

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
            uint identifier = reader.ReadUInt32();
            reader.BaseStream.Seek(-4, SeekOrigin.Current);
            if (!TEXN.IsValid(identifier)) return;

            while (reader.BaseStream.CanRead)
            {
                if (reader.BaseStream.Position >= reader.BaseStream.Length - 16) break;
                Textures.Add(new TEXN(reader));
            }
        }

        public void Write(BinaryWriter writer)
        {
            foreach(TEXN entry in Textures)
            {
                entry.Write(writer);
            }
        }

        public void Unpack(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            foreach(TEXN entry in Textures)
            {
                using (FileStream stream = File.Open(folder + "\\" + entry.Texture.FileName, FileMode.Create))
                {
                    entry.Texture.Write(stream);
                }
            }
        }
    }
}
