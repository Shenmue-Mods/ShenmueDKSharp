using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files
{
    /// <summary>
    /// Unknown file format class for file formats that are not covered yet.
    /// This is used mainly for the container file formats.
    /// </summary>
    public class UnknownFile : BaseFile
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        protected override void _Read(BinaryReader reader)
        {
            
        }

        protected override void _Write(BinaryWriter writer)
        {
            
        }
    }
}
