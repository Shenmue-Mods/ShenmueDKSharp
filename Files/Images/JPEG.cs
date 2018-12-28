using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Images
{
    public class JPEG : BaseImage
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        public override int DataSize => throw new NotImplementedException();

        protected override void _Read(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        protected override void _Write(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
