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

        public BaseFile GetFile(string filepath)
        {
            Type type = GetFileType(filepath);
            if (type == null) return null;
            return (BaseFile)Activator.CreateInstance(type);
        }

        public Type GetFileType(string filepath)
        {
            //1. Extensions types (faster)
            //2. Identifier types (slower)
            //3. Test read file with given type (slowest)

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
            //if (MT7.Extensions) return typeof(MT7);

            return null;
        }

    }
}
