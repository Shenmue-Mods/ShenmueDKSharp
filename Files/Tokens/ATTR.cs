using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Tokens
{
    /// <summary>
    /// ATTR Token.
    /// Has no known value yet.
    /// </summary>
    public class ATTR : BaseToken
    {
        public static readonly string Identifier = "ATTR";

        protected override void _Read(BinaryReader reader) { }

        protected override void _Write(BinaryWriter writer)
        {
            writer.Write(Content);
        }
    }
}
