using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShenmueDKSharp.Properties;
using SimpleJSON;

namespace ShenmueDKSharp.Utils
{
    /// <summary>
    /// Filename database that was cached which holds hash and filename pairs.
    /// This database is more precise and has less hash collisions due to the path hash being used.
    /// </summary>
    public static class FilenameDatabase
    {
        private static bool m_initialized = false;
        private static List<FilenameDatabaseEntry> m_entries = new List<FilenameDatabaseEntry>();

        /// <summary>
        /// Initializes the filename database from the cached GZip compressed json dump from the resources.
        /// Will be called once automatically when not initializing the database manually.
        /// </summary>
        public static void Initialize()
        {
            if (m_initialized) return;
            string json = "";

            //Decompress cached GZip compressed json dump from resources.
            using (MemoryStream stream = new MemoryStream(Resources.FilenameDatabase))
            {
                using (GZipStream streamGZip = new GZipStream(stream, CompressionMode.Decompress))
                {
                    using (StreamReader reader = new StreamReader(streamGZip))
                    {
                        json = reader.ReadToEnd();
                    }
                }
            }

            //Read json and populate filename database
            m_entries.Clear();
            JSONNode rootNode = JSON.Parse(json);
            foreach (JSONNode node in rootNode.AsArray)
            {
                FilenameDatabaseEntry newEntry = new FilenameDatabaseEntry();
                foreach (KeyValuePair<string, JSONNode> entry in node.Linq)
                {
                    if (entry.Key == "Hash")
                    {
                        newEntry.Hash = (uint)entry.Value.AsLong;
                    }
                    if (entry.Key == "HashPath")
                    {
                        newEntry.HashPath = (uint)entry.Value.AsLong;
                    }
                    if (entry.Key == "Path")
                    {
                        newEntry.Path = entry.Value.Value;
                    }
                }
                m_entries.Add(newEntry);
            }
            m_initialized = true;
            Console.WriteLine("Filename database initialized: {0} entries.", m_entries.Count);
        }

        /// <summary>
        /// Returns the first found entries hash containing the given filename.
        /// </summary>
        public static uint GetHash(string filename)
        {
            FilenameDatabaseEntry entry = GetEntry(filename);
            if (entry == null) return 0;
            return entry.Hash;
        }

        /// <summary>
        /// Returns the first found entries path hash containing the given filename.
        /// </summary>
        public static uint GetPathHash(string filename)
        {
            FilenameDatabaseEntry entry = GetEntry(filename);
            if (entry == null) return 0;
            return entry.HashPath;
        }

        /// <summary>
        /// Returns the first found entries filename matching the given hashes.
        /// </summary>
        public static string GetFilename(uint hash, uint pathHash = 0)
        {
            FilenameDatabaseEntry entry = GetEntry(hash, pathHash);
            if (entry == null) return "";
            return entry.Path;
        }

        /// <summary>
        /// Returns the first found entry containing the given filename.
        /// </summary>
        public static FilenameDatabaseEntry GetEntry(string filename)
        {
            if (!m_initialized) Initialize();
            foreach (FilenameDatabaseEntry entry in m_entries)
            {
                if (entry.Path.Contains(filename))
                {
                    return entry;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the first found entry matching the given hashes.
        /// </summary>
        public static FilenameDatabaseEntry GetEntry(uint hash, uint pathHash = 0)
        {
            if (!m_initialized) Initialize();
            if (pathHash == 0)
            {
                foreach (FilenameDatabaseEntry entry in m_entries)
                {
                    if (entry.Hash == hash)
                    {
                        return entry;
                    }
                }
            }
            else
            {
                foreach (FilenameDatabaseEntry entry in m_entries)
                {
                    if (entry.Hash == hash && entry.HashPath == pathHash)
                    {
                        return entry;
                    }
                }
            }
            return null;
        }
    }

    public class FilenameDatabaseEntry
    {
        public uint Hash { get; set; }
        public uint HashPath { get; set; }
        public string Path { get; set; }
    }
}
