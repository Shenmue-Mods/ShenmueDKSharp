using ShenmueDKSharp.Files.Containers;
using ShenmueDKSharp.Files.Misc;
using ShenmueDKSharp.Files.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files
{
    public class FileHelper
    {
        public BaseFile GetFileFromStream(Stream stream)
        {
            return null;
        }

        public Type GetFileType(Stream stream)
        {
            byte[] buffer = new byte[8];
            stream.Read(buffer, 0, buffer.Length);

            FileStream fStream = (FileStream)stream;
            string extension = Path.GetExtension(fStream.Name);

            if (GZ.IsValid(buffer)) return typeof(GZ);
            if (TEXN.IsValid(buffer)) return typeof(TEXN);
            if (MT5.IsValid(buffer)) return typeof(MT5);
            if (MT7.IsValid(buffer)) return typeof(MT7);

            return null;
        }
        
        public Type GetFileTypeFromExtension(string extension)
        {
            return null;
        }

    }
}
