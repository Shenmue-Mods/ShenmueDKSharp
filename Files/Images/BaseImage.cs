using ShenmueDKSharp.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Images
{
    public abstract class BaseImage : BaseFile
    {
        public enum DataFormat
        {
            R,
            RGB,
            RGBA,
            BGR,
            BGRA
        }

        public bool HasTransparency { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public Color4[] Pixels { get; set; }
        public int DataSize { get; set; }

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

        public abstract void Read(BinaryReader reader);
        public abstract void Write(BinaryWriter writer);
    }
}
