using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;

namespace ShenmueDKSharp.Files.Models
{
    class FBX : BaseModel
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<string> Extensions = new List<string>()
        {
            "FBX"
        };

        public FBX(BaseModel model)
        {
            model.CopyTo(this);
            FilePath = Path.ChangeExtension(model.FilePath, "fbx");
        }
        public FBX(string filepath)
        {
            Read(filepath);
        }
        public FBX(Stream stream)
        {
            Read(stream);
        }
        public FBX(BinaryReader reader)
        {
            Read(reader);
        }

        protected override void _Read(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        protected override void _Write(BinaryWriter writer)
        {
            Scene scene;
        }

        private void WriteNode(BinaryWriter writer, ModelNode node)
        {

        }
    }
}
