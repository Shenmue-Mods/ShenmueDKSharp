using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        private static readonly string GetFormat = "{0}/api/hash/get?page={1}&game={2}";

        public static string GetFilename(uint hash)
        {
            return "";
        }

        /*
        public void FetchData(string game)
        {
            string url = String.Format(GetFormat, Url, 1, game);
            WebRequest request = WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            string text;

            int pageCount = 1;

            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                text = sr.ReadToEnd();
                dynamic data = JsonConvert.DeserializeObject(text);
                pageCount = (int)data.SelectToken("last_page").Value;
            }

            for (int i = 1; i < pageCount; i++)
            {
                url = String.Format(GetFormat, Url, i, game);
                request = WebRequest.Create(url);
                response = (HttpWebResponse)request.GetResponse();
                using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                {
                    text = sr.ReadToEnd();
                    dynamic data = JsonConvert.DeserializeObject(text);
                    JToken token = data.SelectToken("data");
                    foreach (JToken child in token.Children())
                    {
                        WulinshuEntry entry = new WulinshuEntry
                        {
                            Path = child.SelectToken("path").Value<string>(),
                            Hash = child.SelectToken("hash").Value<string>(),
                            Matches = child.SelectToken("matches").Value<int>(),
                            Game = child.SelectToken("game").Value<string>()
                        };
                        Entries.Add(entry);
                    }
                }
            }
        }
        */
    }

    public class WulinshuEntry
    {
        public string Path { get; set; }
        public string Hash { get; set; }
        public int Matches { get; set; }
        public string Game { get; set; }
    }
}
