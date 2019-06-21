using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Utils
{
    public class FileHash
    {
        public bool HasSecondHash;

        public string FilePath;
        public string FilePathWithHash;

        public UInt32 FilePathHash;
        public UInt32 Hash;
        public UInt32 FinalHash;
    }

    /// <summary>
    /// MurmurHash2 Shenmue implementation
    /// </summary>
    public class MurmurHash2
    {
        private static uint initSeed = 0x66ee5d0;
        private static uint multiplier = 0x5BD1E995;
        private static int rotationAmount = 0x18;

        public static string Prefix = "./tex/assets/";

        /// <summary>
        /// MurmurHash2 Shenmue implementation.
        /// </summary>
        /// <param name="data">Data buffer</param>
        /// <param name="length">Length to hash</param>
        /// <returns>Shenmue MurmurHash2</returns>
        public static uint Hash(byte[] data, uint length)
        {
            //TODO: Needs a refactoring because its just plain broken up asm logic which is not that performant
            uint hash = (length / 0xFFFFFFFF + length) ^ initSeed;
            uint m = multiplier;
            int r = rotationAmount;

            UInt64 lengthRemaining = length + length / 4 * 0xfffffffffffffffc;

            if (length >= 4)
            {
                for (int i = 0; i < length; i += 4)
                {
                    if (i / 4 >= length / 4) break;
                    uint ecx = BitConverter.ToUInt32(data, i) * m;
                    hash = hash * m ^ (ecx >> r ^ ecx) * m;
                }
            }

            if (lengthRemaining > 0)
            {
                byte[] buffer = new byte[4];
                if (lengthRemaining == 1)
                {
                    buffer[0] = data[length - 1];
                }
                else if (lengthRemaining == 2)
                {
                    buffer[0] = data[length - 2];
                    buffer[1] = data[length - 1];
                }
                else if (lengthRemaining == 3)
                {
                    buffer[0] = data[length - 3];
                    buffer[1] = data[length - 2];
                    buffer[2] = data[length - 1];
                }
                hash = (hash ^ BitConverter.ToUInt32(buffer, 0)) * m;
            }

            uint edx = (hash >> 0x0D ^ hash) * m;
            hash = edx >> 0x0F ^ edx;
            return hash;
        }

        public static uint GetFilenameHashPlain(string filename, bool lower = true)
        {
            if (lower)
            {
                filename = filename.ToLower();
            }
            byte[] filenameBytes = Encoding.ASCII.GetBytes(filename);
            uint hash = Hash(filenameBytes, (uint)filenameBytes.Length);
            return hash;
        }



        public static string GetFullFilename(string filename, uint hash, bool includeHash = true)
        {
            string newFilename = filename;
            int cutoffIndex = filename.LastIndexOf('?'); //cutoff the ?usage=0 or other parameters away
            if (cutoffIndex > 0)
            {
                newFilename = filename.Substring(0, cutoffIndex);
            }
            if (includeHash)
            {
                return String.Format("{0}{1}.{2}", Prefix, newFilename, hash.ToString("x8"));
            }
            return String.Format("{0}{1}", Prefix, newFilename);
        }

        public static FileHash GetFileHash(string filepath, bool secondHash = true)
        {
            filepath = filepath.Replace("\\", "/").ToLower();
            if (filepath[0] != '/')
            {
                filepath = "/" + filepath;
            }

            Console.WriteLine(filepath);

            FileHash hash = new FileHash();
            hash.FilePath = filepath;
            hash.HasSecondHash = secondHash;

            if (secondHash)
            {
                hash.FilePathHash = Hash(Encoding.ASCII.GetBytes(filepath), (uint)filepath.Length);

                //cutoff the ?usage=0 or other parameters away
                int cutoffIndex = filepath.LastIndexOf('?');
                if (cutoffIndex > 0)
                {
                    filepath = filepath.Substring(0, cutoffIndex);
                }

                hash.FilePathWithHash = String.Format("{0}.{1}.00000000", filepath, hash.FilePathHash.ToString("x8"));

                // Remove ending zero
                filepath = hash.FilePathWithHash.Substring(0, hash.FilePathWithHash.Length - 9);

            }

            // Add asset prefix
            filepath = Prefix + filepath;

            // Remove dot
            if (filepath[0] == '.')
            {
                filepath = filepath.Substring(1);
            }

            // Remove slashes and dashes
            filepath = filepath.Replace("/", "").Replace("-", "");

            // To lower
            filepath = filepath.ToLower();

            // hash stripped filepath
            hash.Hash = Hash(Encoding.ASCII.GetBytes(filepath), (uint)filepath.Length);

            // get string length
            uint totalLength = (uint)filepath.Length;
            if (secondHash)
            {
                totalLength += 9; //include ending zeros
            }

            // calculate final hash
            hash.FinalHash = hash.Hash * 0x0001003F + (uint)filepath.Length * totalLength * 0x0002001F;

            return hash;
        }

        public static byte[] GetFilenameHash(string filename, bool hasHash = true)
        {
            if (filename[0] == '.')
            {
                filename = filename.Substring(1);
            }
            string strippedFilename = filename.ToLower().Replace("/", "").Replace("-", "");
            uint murmurHash = Hash(Encoding.ASCII.GetBytes(strippedFilename), (uint)strippedFilename.Length);
            uint totalLength = (uint)strippedFilename.Length;

            if (hasHash)
            {
                totalLength = (uint)strippedFilename.Length + 9; //include the " 00000000"
            }
            uint hash = murmurHash * 0x0001003F + (uint)strippedFilename.Length * totalLength * 0x0002001F;

            return BitConverter.GetBytes(hash);
        }
    }
}
