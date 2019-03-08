using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Tokens
{
    /// <summary>
    /// REGD Token.
    /// Has no known value yet.
    /// </summary>
    public class REGD : BaseToken
    {
        public static readonly string Identifier = "REGD";

        protected override void _Read(BinaryReader reader) { }

        protected override void _Write(BinaryWriter writer)
        {
            writer.Write(Content);
        }
    }
}
