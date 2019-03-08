using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Tokens
{
    /// <summary>
    /// END Token.
    /// Signaling end of token structure.
    /// </summary>
    public class END : BaseToken
    {
        public static readonly string Identifier = "END\0";

        protected override void _Read(BinaryReader reader) { }

        protected override void _Write(BinaryWriter writer)
        {
            writer.Write(Content);
        }
    }
}
