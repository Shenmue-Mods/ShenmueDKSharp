using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Images
{
    /// <summary>
    /// Bitmap image file.
    /// Uses GDI+.
    /// </summary>
    public class BMP : BaseImage
    {
        public readonly static List<string> Extensions = new List<string>()
        {
            "BMP"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[2] { 0x42, 0x4D }, //BM
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

        public override int DataSize => 0;

        public override bool BufferingEnabled => false;

        public BMP() { }
        public BMP(string filepath)
        {
            Read(filepath);
        }
        public BMP(Stream stream)
        {
            Read(stream);
        }
        public BMP(BinaryReader reader)
        {
            Read(reader);
        }
        public BMP(BaseImage image)
        {
            Width = image.Width;
            Height = image.Height;
            foreach (MipMap mipmap in image.MipMaps)
            {
                MipMaps.Add(new MipMap(mipmap));
            }
        }

        protected override void _Read(BinaryReader reader)
        {
            Image image = Image.FromStream(reader.BaseStream);
            Bitmap bmp = new Bitmap(image);
            Width = bmp.Width;
            Height = bmp.Height;

            MipMap mipMap = new MipMap(Width, Height);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color col = bmp.GetPixel(x, y);
                    int index = (y * Width + x) * 4;
                    mipMap.Pixels[index] = col.B;
                    mipMap.Pixels[index + 1] = col.G;
                    mipMap.Pixels[index + 2] = col.R;
                    mipMap.Pixels[index + 3] = col.A;
                }
            }
            MipMaps.Add(mipMap);
        }

        protected override void _Write(BinaryWriter writer)
        {
            Bitmap bitmap = CreateBitmap();
            bitmap.Save(writer.BaseStream, ImageFormat.Bmp);
        }
    }
}
