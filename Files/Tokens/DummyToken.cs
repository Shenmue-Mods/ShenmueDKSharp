using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Tokens
{
    /// <summary>
    /// Dummy token for unknown token types
    /// </summary>
    /// <seealso cref="ShenmueDKSharp.Files.Tokens.BaseToken" />
    public class DummyToken : BaseToken
    {
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
