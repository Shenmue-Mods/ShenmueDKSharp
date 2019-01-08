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
    /// JPEG image file.
    /// Uses GDI+.
    /// </summary>
    public class JPEG : BaseImage
    {
        public readonly static List<string> Extensions = new List<string>()
        {
            "JPG",
            "JPEG"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0xFF, 0xD8, 0xFF, 0xE0 }
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

        public JPEG() { }
        public JPEG(string filepath)
        {
            Read(filepath);
        }
        public JPEG(Stream stream)
        {
            Read(stream);
        }
        public JPEG(BinaryReader reader)
        {
            Read(reader);
        }
        public JPEG(BaseImage image)
        {
            Width = image.Width;
            Height = image.Height;
            foreach(MipMap mipmap in image.MipMaps)
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
            bitmap.Save(writer.BaseStream, ImageFormat.Jpeg);
        }
    }
}
