using ShenmueDKSharp.Files;
using ShenmueDKSharp.Files.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShenmueDKSharp.Files.Images;

namespace ShenmueDKSharp.Utils
{
    /// <summary>
    /// Texture database.
    /// Used for MT5 and MT7 non-embedded texture search.
    /// </summary>
    public static class TextureDatabase
    {
        private static List<TEXN> m_textures = new List<TEXN>();

        /// <summary>
        /// True for adding textures automatically when reading PKF or SPR archives.
        /// </summary>
        public static bool Automatic { get; set; } = true;

        /// <summary>
        /// Searches the given directory for TEXN files and adds them to the database.
        /// </summary>
        public static void SearchDirectory(string folder)
        {
            if (!Directory.Exists(folder)) return;
            List<string> filepaths = FileHelper.DirSearch(folder);
            foreach (string filepath in filepaths)
            {
                if (FileHelper.IsFileLocked(filepath)) continue;
                if (Path.GetExtension(filepath).ToUpper() != ".TEXN") continue;
                using (FileStream stream = new FileStream(filepath, FileMode.Open))
                {
                    byte[] buffer = new byte[4];
                    stream.Read(buffer, 0, 4);
                    if (TEXN.IsValid(buffer))
                    {
                        stream.Seek(-4, SeekOrigin.Current);
                        TEXN texture = new TEXN(stream);
                        texture.FilePath = filepath;
                        AddTexture(texture);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the TEXN entry with the given texture ID and name.
        /// </summary>
        public static TEXN FindTexture(UInt64 idName)
        {
            foreach (TEXN texture in m_textures)
            {
                if (texture.TextureID.Data == idName)
                {
                    return texture;
                }
            }
            return null;
        }

        /// <summary>
        /// Adds the given TEXN entry to the database.
        /// Duplicates will not be added.
        /// </summary>
        public static void AddTexture(TEXN texture)
        {
            //Check for duplicate
            foreach(TEXN tex in m_textures)
            {
                if (tex.TextureID == texture.TextureID)
                {
                    return;
                }
            }
            m_textures.Add(texture);
        }

        public static void DumpDatabase(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            foreach(TEXN tex in m_textures)
            {
                string textureName = "tex_" + tex.TextureID.Data.ToString("x16") + ".png"; 

                PNG png = new PNG(tex.Texture);
                png.Write(folder + textureName);
            }
        }

        /// <summary>
        /// Clears the texture database.
        /// </summary>
        public static void Clear()
        {
            m_textures.Clear();
        }
    }
}
