using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Utils
{
    public class Helper
    {
        public static bool CompareSignature(byte[] signature, byte[] data)
        {
            if (data.Length < signature.Length) return false;
            for (int i = 0; i < signature.Length; i++)
            {
                if (signature[i] != data[i]) return false;
            }
            return true;
        }

        public static bool CompareArray(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length) return false;
            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i]) return false;
            }
            return true;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:X2}", b);
            return hex.ToString();
        }

        public static byte[] ReverseArray(byte[] array)
        {
            byte[] result = new byte[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                result[array.Length - i - 1] = array[i];
            }
            return result;
        }

        public static List<string> DirSearch(string sDir)
        {
            List<string> files = new List<string>();

            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        files.Add(f);
                    }
                    files.AddRange(DirSearch(d));
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
            return files;
        }
    }
}
