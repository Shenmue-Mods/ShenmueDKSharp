using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Tokens
{
    /// <summary>
    /// COLS token.
    /// Collision related.
    /// </summary>
    public class COLS : BaseToken
    {
        public static readonly string Identifier = "COLS";

        protected override void _Read(BinaryReader reader)
        {
            reader.BaseStream.Seek(Size - 8, SeekOrigin.Current);
            //Tokens = TokenHelper.Tokenize(reader, (int)(Size - 8));
        }

        protected override void _Write(BinaryWriter writer)
        {
            writer.Write(Content);
            /*foreach (BaseToken token in Tokens)
            {
                token.Write(writer);
            }*/
        }
    }
}
