using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Misc
{
    public class SRL
    {
        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x53, 0x43, 0x52, 0x30 } //SCR0
        };
    }
}
