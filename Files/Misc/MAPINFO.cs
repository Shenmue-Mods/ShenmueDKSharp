using ShenmueDKSharp.Files.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Misc
{
    /// <summary>
    /// Map information file containing various tokens.
    /// </summary>
    public class MAPINFO : BaseFile
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        public List<BaseToken> Tokens = new List<BaseToken>();

        public MAPINFO() { }
        public MAPINFO(string filepath)
        {
            Read(filepath);
        }
        public MAPINFO(Stream stream)
        {
            Read(stream);
        }
        public MAPINFO(BinaryReader reader)
        {
            Read(reader);
        }

        protected override void _Read(BinaryReader reader)
        {
            Tokens = TokenHelper.Tokenize(reader);
        }

        protected override void _Write(BinaryWriter writer)
        {
            foreach(BaseToken token in Tokens)
            {
                token.Write(writer);
            }
        }
    }
}
