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
        /// <summary>
        /// Width of the image.
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// Height of the image.
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// [DEPRECATED] Pixel buffer.
        /// </summary>
        public Color4[] Pixels { get; set; }
        /// <summary>
        /// Mipmaps pixel buffers.
        /// </summary>
        public List<Color4[]> MipMaps { get; set; }
        /// <summary>
        /// True of the image has an transparency channel.
        /// </summary>
        public bool HasTransparency { get; set; }

        /// <summary>
        /// Size of the image in it's inherited format as bytes
        /// </summary>
        public abstract int DataSize { get; }

        /// <summary>
        /// Creates an Bitmap object of the current pixel data for usage inside WinForms.
        /// </summary>
        /// <returns></returns>
        public Bitmap CreateBitmap()
        {
            Bitmap bitmap = new Bitmap(Width, Height);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Color4 col4 = Pixels[y * Width + x];
                    bitmap.SetPixel(x, y, Color.FromArgb(col4.ToArgb()));
                }
            }
            return bitmap;
        }

        /// <summary>
        /// Returns the pixel buffer of the given mipmap index.
        /// </summary>
        /// <param name="mipmap">The mipmap index</param>
        /// <returns></returns>
        /// <exception cref="System.IndexOutOfRangeException">Mipmap index out of range!</exception>
        public Color4[] GetPixels(int mipmap = 0)
        {
            if (mipmap >= MipMaps.Count)
            {
                throw new IndexOutOfRangeException("Mipmap index out of range!");
            }
            return MipMaps[mipmap];
        }

        /// <summary>
        /// Returns the pixel from the given coordinates of the pixel buffer.
        /// </summary>
        /// <param name="x">The x index starting from 0</param>
        /// <param name="y">The y index starting from 0</param>
        /// <returns></returns>
        /// <exception cref="System.IndexOutOfRangeException">
        /// Index of X is out of range!
        /// or
        /// Index of Y is out of range!
        /// or
        /// Index out of range! Does the pixel buffer have an incorrect size?
        /// </exception>
        public Color4 GetPixel(int x, int y)
        {
            if (x >= Width || x < 0)
            {
                throw new IndexOutOfRangeException("Index of X is out of range!");
            }
            if (y >= Height || y < 0)
            {
                throw new IndexOutOfRangeException("Index of Y is out of range!");
            }

            int index = y * Width + x;
            if (index >= Pixels.Length)
            {
                throw new IndexOutOfRangeException("Index out of range! Does the pixel buffer have an incorrect size?");
            }
            return Pixels[y * Width + x];
        }
    }
}
