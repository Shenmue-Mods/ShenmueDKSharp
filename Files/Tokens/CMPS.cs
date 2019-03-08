using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Tokens
{
    /// <summary>
    /// CMPS token.
    /// </summary>
    public class CMPS : BaseToken
    {
        public static readonly string Identifier = "CMPS";

        protected override void _Read(BinaryReader reader)
        {
            reader.BaseStream.Seek(Size - 8, SeekOrigin.Current);
        }

        protected override void _Write(BinaryWriter writer)
        {
            writer.Write(Content);
        }
    }
}
