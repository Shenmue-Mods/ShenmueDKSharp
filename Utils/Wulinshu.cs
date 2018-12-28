using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Utils
{
    /// <summary>
    /// Raymonf's wulinshu hash/filename database
    /// </summary>
    public static class Wulinshu
    {
        private static readonly string Url = "https://wulinshu.raymonf.me";
        private static readonly string GetFormatQueryBoth = "{0}/api/hash/get?page={1}&q={2}&sort=0&game=both";

        public static string GetFilename(uint hash)
        {
            return "";
        }

        /// <summary>
        /// Gets the first found filename from the hash database.
        /// </summary>
        public static string GetFilenameFromHash(uint hash)
        {
            return GetFilenameFromQuery(String.Format("{0:x}", hash));
        }

        public static string GetFilenameFromQuery(string query)
        {
            string result = "";

            string url = String.Format(GetFormatQueryBoth, Url, 1, query);
            WebRequest request = WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                string jsonText = sr.ReadToEnd();
                JSONNode node = JSONNode.Parse(jsonText);
                if (node.Children.Count() > 1)
                {
                    JSONArray dataNode = node.Children.ElementAt(1).AsArray;
                    foreach (JSONNode child in dataNode.Children)
                    {
                        WulinshuEntry entry = new WulinshuEntry
                        {
                            Path = child.Children.ElementAt(0).Value,
                            Hash = child.Children.ElementAt(1).Value,
                            Game = child.Children.ElementAt(3).Value
                        };
                        return entry.Path;
                    }
                }
            }
            return result;
        }
    }

    public class WulinshuEntry
    {
        public string Path { get; set; }
        public string Hash { get; set; }
        public int Matches { get; set; }
        public string Game { get; set; }
    }
}
