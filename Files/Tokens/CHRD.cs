using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Tokens
{
    public class CHRD : BaseToken
    {
        public static readonly string Identifier = "CHRD";

        protected override void _Read(BinaryReader reader)
        {
            Tokens = TokenHelper.Tokenize(reader, (int)(Size - 8));
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
