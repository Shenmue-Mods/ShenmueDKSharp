using ShenmueDKSharp.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        public Bitmap Bitmap { get; set; }

        public Bitmap CreateBitmap()
        {
            Bitmap bitmap = new Bitmap(Width, Height);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color4 col4 = Pixels[y * Width + x];
                    bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(col4.ToArgb()));
                }
            }
            return bitmap;
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

        public abstract void Read(BinaryReader reader);
        public abstract void Write(BinaryWriter writer);
    }
}
