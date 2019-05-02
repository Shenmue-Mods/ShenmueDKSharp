using ShenmueDKSharp.Files.Containers;
using ShenmueDKSharp.Files.Images;
using ShenmueDKSharp.Files.Misc;
using ShenmueDKSharp.Files.Models;
using ShenmueDKSharp.Files.Subtitles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files
{
    public static class FileHelper
    {
        /// <summary>
        /// Trys creating a file object with the given type for the given file.
        /// </summary>
        public static T OpenFile<T>(string filepath)
        {
            return (T)Activator.CreateInstance(typeof(T));
        }

        /// <summary>
        /// Trys to find the fitting file type for the given file and creates a instance of that file object.
        /// </summary>
        public static BaseFile GetFile(string filepath)
        {
            Type type = GetFileType(filepath);
            if (type == null) return null;
            return (BaseFile)Activator.CreateInstance(type);
        }

        /// <summary>
        /// Trys to find the fitting file type for the given file.
        /// </summary>
        public static Type GetFileType(string filepath)
        {
            string extension = Path.GetExtension(filepath).Substring(1, 4).ToUpper();
            Type typeExtension = GetFileTypeFromExtension(extension);
            //1. Extensions types (faster)
            //2. Identifier types (slower)
            //3. Test read file with given type (slowest)

            return null;
        }

        public static string GetExtensionFromBuffer(byte[] buffer)
        {
            //Archives
            if (AFS.IsValid(buffer)) return "AFS";
            if (GZ.IsValid(buffer)) return "GZ";
            if (IDX.IsValid(buffer)) return "IDX";
            if (IPAC.IsValid(buffer)) return "IPAC";
            if (PKF.IsValid(buffer)) return "PKF";
            if (PKS.IsValid(buffer)) return "PKS";
            //if (SPR.IsValid(buffer)) return typeof(SPR); //same as TEXN skip and base identification on extension
            if (TAD.IsValid(buffer)) return "TAD";

            //Textures/Images
            if (TEXN.IsValid(buffer)) return "TEXN";
            if (PVRT.IsValid(buffer)) return "PVRT";
            if (DDS.IsValid(buffer)) return "DDS";

            //Models
            if (MT5.IsValid(buffer)) return "MT5";
            if (MT7.IsValid(buffer)) return "MT7";

            //Subtitles
            if (SUB.IsValid(buffer)) return "SUB";

            return "UNKNOWN";
        }

        /// <summary>
        /// Trys to find the fitting file type for the given file with the file signature.
        /// </summary>
        public static Type GetFileTypeFromSignature(Stream stream)
        {
            byte[] buffer = new byte[8];
            stream.Read(buffer, 0, buffer.Length);

            //Archives
            if (AFS.IsValid(buffer)) return typeof(AFS);
            if (GZ.IsValid(buffer)) return typeof(GZ);
            if (IDX.IsValid(buffer)) return typeof(IDX);
            if (IPAC.IsValid(buffer)) return typeof(IPAC);
            if (PKF.IsValid(buffer)) return typeof(PKF);
            if (PKS.IsValid(buffer)) return typeof(PKS);
            //if (SPR.IsValid(buffer)) return typeof(SPR); //same as TEXN skip and base identification on extension
            if (TAD.IsValid(buffer)) return typeof(TAD);

            //Textures/Images
            if (TEXN.IsValid(buffer)) return typeof(TEXN);
            if (PVRT.IsValid(buffer)) return typeof(PVRT);
            if (DDS.IsValid(buffer)) return typeof(DDS);

            //Models
            if (MT5.IsValid(buffer)) return typeof(MT5);
            if (MT7.IsValid(buffer)) return typeof(MT7);

            //Subtitles
            if (SUB.IsValid(buffer)) return typeof(SUB);

            return null;
        }


        public static Type GetImageFileTypeFromSignature(byte[] buffer)
        {
            if (TEXN.IsValid(buffer)) return typeof(TEXN);
            if (PVRT.IsValid(buffer)) return typeof(PVRT);
            if (DDS.IsValid(buffer)) return typeof(DDS);
            if (BMP.IsValid(buffer)) return typeof(BMP);
            if (JPEG.IsValid(buffer)) return typeof(JPEG);
            if (PNG.IsValid(buffer)) return typeof(PNG);
            return null;
        }

        public static Type GetImageFileTypeFromExtension(string extension)
        {
            extension = extension.ToUpper();
            if (CompareExtension(DDS.Extensions, extension)) return typeof(DDS);
            if (CompareExtension(TEXN.Extensions, extension)) return typeof(TEXN);
            if (CompareExtension(PVRT.Extensions, extension)) return typeof(PVRT);
            if (CompareExtension(PNG.Extensions, extension)) return typeof(PNG);
            if (CompareExtension(JPEG.Extensions, extension)) return typeof(JPEG);
            if (CompareExtension(BMP.Extensions, extension)) return typeof(BMP);
            return null;
        }

        /// <summary>
        /// Trys to find the fitting file type for the given file with the file extension.
        /// </summary>
        public static Type GetFileTypeFromExtension(string extension)
        {
            extension = extension.ToUpper();

            //Archives
            if (CompareExtension(AFS.Extensions, extension)) return typeof(AFS);
            if (CompareExtension(GZ.Extensions, extension)) return typeof(GZ);
            if (CompareExtension(IDX.Extensions, extension)) return typeof(IDX);
            if (CompareExtension(IPAC.Extensions, extension)) return typeof(IPAC);
            if (CompareExtension(PKF.Extensions, extension)) return typeof(PKF);
            if (CompareExtension(PKS.Extensions, extension)) return typeof(PKS);
            if (CompareExtension(SPR.Extensions, extension)) return typeof(SPR);
            if (CompareExtension(TAD.Extensions, extension)) return typeof(TAD);

            //Textures/Images
            if (CompareExtension(DDS.Extensions, extension)) return typeof(DDS);
            if (CompareExtension(TEXN.Extensions, extension)) return typeof(TEXN);
            if (CompareExtension(PVRT.Extensions, extension)) return typeof(PVRT);
            if (CompareExtension(PNG.Extensions, extension)) return typeof(PNG);
            if (CompareExtension(JPEG.Extensions, extension)) return typeof(JPEG);
            if (CompareExtension(BMP.Extensions, extension)) return typeof(BMP);

            //Models
            if (CompareExtension(MT7.Extensions, extension)) return typeof(MT7);
            if (CompareExtension(MT5.Extensions, extension)) return typeof(MT5);

            //Subtitles
            if (CompareExtension(SUB.Extensions, extension)) return typeof(SUB);

            return null;
        }

        /// <summary>
        /// Compares a list of extensions with the given extension.
        /// </summary>
        public static bool CompareExtension(List<string> extensions, string extension)
        {
            foreach (string ext in extensions)
            {
                if (ext == extension) return true;
            }
            return false;
        }

        /// <summary>
        /// Compares the given signature with the given data buffer.
        /// </summary>
        public static bool CompareSignature(byte[] signature, byte[] data)
        {
            if (data.Length < signature.Length) return false;
            for (int i = 0; i < signature.Length; i++)
            {
                if (signature[i] != data[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Returns all the files for the given direcotry recursively.
        /// </summary>
        public static List<string> DirSearch(string directory)
        {
            List<string> files = new List<string>();
            try
            {
                foreach (string filepath in Directory.GetFiles(directory))
                {
                    files.Add(filepath);
                }
                foreach (string dir in Directory.GetDirectories(directory))
                {
                    files.AddRange(DirSearch(dir));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return files;
        }

        public static bool IsFileLocked(string filepath)
        {
            FileInfo file = new FileInfo(filepath);
            FileStream stream = null;
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }
            return false;
        }
    }
}
