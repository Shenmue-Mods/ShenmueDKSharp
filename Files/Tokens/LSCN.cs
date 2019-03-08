using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Tokens
{
    /// <summary>
    /// LSCN token.
    /// </summary>
    public class LSCN : BaseToken
    {
        public static readonly string Identifier = "LSCN";

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
