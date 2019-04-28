using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShenmueDKSharp.Files.Containers;

namespace ShenmueDKSharp.Utils
{
    public static class VirtualFileSystem
    {
        private static bool m_initialized = false;
        private static TAD m_TAD;


        static void LoadVFS(string filepath)
        {
            string tadFilepath;
            string tacFilepath;
            if (Path.GetExtension(filepath).ToLower() == "tac")
            {
                tadFilepath = Path.ChangeExtension(filepath, ".tad");
                tacFilepath = filepath;
            }
            else if (Path.GetExtension(filepath).ToLower() == "tad")
            {
                tadFilepath = filepath;
                tacFilepath = Path.ChangeExtension(filepath, ".tac"); 
            }
            else
            {
                throw new ArgumentException("File is neither an TAD or TAC!");
            }
            m_initialized = true;
        }

        static string FindFile(string searchString)
        {
            return "";
        }

        static void GetFile(int index)
        {

        }

        static void GetFile(string filepath)
        {

        }


    }
}
