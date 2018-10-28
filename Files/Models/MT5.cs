using ShenmueDKSharp.Files.Models._MT5;
using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Models
{
    /// <summary>
    /// MT5 model.
    /// HRCM.
    /// </summary>
    public class MT5 : BaseModel
    {
        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x48, 0x52, 0x43, 0x4D } //HRCM
        };

        public static bool IsValid(uint identifier)
        {
            return IsValid(BitConverter.GetBytes(identifier));
        }

        public static bool IsValid(byte[] identifier)
        {
            for (int i = 0; i < Identifiers.Count; i++)
            {
                if (Helper.CompareSignature(Identifiers[i], identifier)) return true;
            }
            return false;
        }

        public uint Identifier;
        public uint TextureOffset;
        public uint FirstNodeOffset;

        public MT5(BinaryReader reader)
        {
            Read(reader);
        }

        public override void Read(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                Read(reader);
            }
        }

        public override void Write(Stream stream)
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                Write(writer);
            }
        }

        public void Read(BinaryReader reader)
        {
            Buffer = reader.ReadBytes((int)reader.BaseStream.Length);
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            Identifier = reader.ReadUInt32();
            if (!IsValid(Identifier)) return;

            TextureOffset = reader.ReadUInt32();
            FirstNodeOffset = reader.ReadUInt32();

            reader.BaseStream.Seek(FirstNodeOffset, SeekOrigin.Begin);
            RootNode = new MT5Node(reader, null);

            reader.BaseStream.Seek(TextureOffset, SeekOrigin.Begin);
            TEXD texture = new TEXD(reader);

            //Populate base class textures
            foreach(Texture tex in texture.Textures)
            {
                Textures.Add(tex);
            }

            //Resolve the textures in the faces
            RootNode.ResolveFaceTextures(Textures);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Buffer);
        }
    }

    public class MT5Node : ModelNode
    {
        public uint Offset;
        public uint ID;

        public uint MeshOffset;
        public uint ChildOffset;
        public uint SiblingOffset;
        public uint ParentOffset;

        public uint ObjectName;
        public uint Unknown;
        public uint NextNode;

        public MT5Mesh MeshData;

        public MT5Node(BinaryReader reader, MT5Node parent)
        {
            Offset = (uint)reader.BaseStream.Position;
            Parent = parent;

            ID = reader.ReadUInt32();
            MeshOffset = reader.ReadUInt32();

            Rotation.X = 360.0f * reader.ReadInt32() / 0xffff;
            Rotation.Y = 360.0f * reader.ReadInt32() / 0xffff;
            Rotation.Z = 360.0f * reader.ReadInt32() / 0xffff;

            Scale.X = reader.ReadSingle();
            Scale.Y = reader.ReadSingle();
            Scale.Z = reader.ReadSingle();

            Position.X = reader.ReadSingle();
            Position.Y = reader.ReadSingle();
            Position.Z = reader.ReadSingle();

            ChildOffset = reader.ReadUInt32();
            SiblingOffset = reader.ReadUInt32();
            ParentOffset = reader.ReadUInt32();

            ObjectName = reader.ReadUInt32();
            Unknown = reader.ReadUInt32();

            //Read MT5 mesh data
            long offset = reader.BaseStream.Position;
            if (MeshOffset != 0)
            {
                HasMesh = true;
                reader.BaseStream.Seek(MeshOffset, SeekOrigin.Begin);
                MeshData = new MT5Mesh(reader, this);
                Faces = MeshData.Faces;
                Vertices = MeshData.Vertices;
                Center = MeshData.MeshCenter;
                Radius = MeshData.MeshDiameter;
                VertexCount = (uint)MeshData.VertexCount;
            }

            //Construct node tree recursively
            if (ChildOffset != 0)
            {
                reader.BaseStream.Seek(ChildOffset, SeekOrigin.Begin);
                Child = new MT5Node(reader, this);
            }
            if (SiblingOffset != 0)
            {
                reader.BaseStream.Seek(SiblingOffset, SeekOrigin.Begin);
                Sibling = new MT5Node(reader, (MT5Node)Parent);
            }
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        public override string ToString()
        {
            return String.Format("[{0}] MT5 Node: {1}", Offset, ID);
        }
    }
}
