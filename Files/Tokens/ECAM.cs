using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Tokens
{
    /// <summary>
    /// ECAM Token.
    /// Camera related stuff (can also be a standalone file)
    /// </summary>
    public class ECAM : BaseToken
    {
        public static readonly string Identifier = "ECAM";

        protected override void _Read(BinaryReader reader)
        {
            Tokens = TokenHelper.Tokenize(reader, (int)(Size - 8));
        }

        protected override void _Write(BinaryWriter writer)
        {
            foreach (BaseToken token in Tokens)
            {
                token.Write(writer);
            }
        }
    }
}
