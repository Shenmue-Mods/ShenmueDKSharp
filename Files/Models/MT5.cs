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
    /// MT5 model for Shenmue I.
    /// Used for reading MT5 files from Shenmue II.
    /// HRCM token.
    /// </summary>
    public class MT5 : BaseModel
    {
        public static bool UVMirrorTextureResize = true;
        public static bool UseTextureDatabase = true;
        public static bool SearchTexturesOneDirUp = false;
        public static bool EnableBuffering = true;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<string> Extensions = new List<string>()
        {
            "MT5"
        };

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
                if (FileHelper.CompareSignature(Identifiers[i], identifier)) return true;
            }
            return false;
        }

        public uint Identifier = 1296257608;
        public uint TextureOffset;
        public uint FirstNodeOffset;

        public MT5(string filepath)
        {
            Read(filepath);
        }
        public MT5(Stream stream)
        {
            Read(stream);
        }
        public MT5(BinaryReader reader)
        {
            Read(reader);
        }

        /// <summary>
        /// Creates a new MT5 instance from the given model.
        /// </summary>
        public MT5(BaseModel model)
        {
            Textures = model.Textures;
            RootNode = new MT5Node(model.RootNode);
            RootNode.ResolveFaceTextures(Textures);
        }

        protected override void _Read(BinaryReader reader)
        {
            Buffer = reader.ReadBytes((int)reader.BaseStream.Length);
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            Identifier = reader.ReadUInt32();
            if (!IsValid(Identifier)) return;

            TextureOffset = reader.ReadUInt32();
            FirstNodeOffset = reader.ReadUInt32();

            if (TextureOffset != 0)
            {
                reader.BaseStream.Seek(TextureOffset, SeekOrigin.Begin);
                TEXD textureDatabase = new TEXD(reader);

                //Populate base class textures
                foreach (Texture tex in textureDatabase.Textures)
                {
                    Textures.Add(tex);
                }
            }

            reader.BaseStream.Seek(FirstNodeOffset, SeekOrigin.Begin);
            RootNode = new MT5Node(reader, null, this);

            //Resolve the textures in the faces
            RootNode.ResolveFaceTextures(Textures);
        }

        protected override void _Write(BinaryWriter writer)
        {
            long baseOffset = writer.BaseStream.Position;
            //Write some header stuff
            writer.Write(Identifier);
            writer.Write(TextureOffset); //Will be overwritten later
            writer.Write(FirstNodeOffset); //Will be overwritten later

            //Write MT5Nodes
            
            foreach (ModelNode node in RootNode.GetAllNodes())
            {
                MT5Node mt5Node = (MT5Node)node;
                if (mt5Node.MeshData == null) continue;
                mt5Node.WriteMeshData(writer);
            }
            foreach (ModelNode node in RootNode.GetAllNodes())
            {
                MT5Node mt5Node = (MT5Node)node;
                if (mt5Node.MeshData == null) continue;
                mt5Node.WriteMeshHeader(writer);
            }

            FirstNodeOffset = (uint)writer.BaseStream.Position;
            MT5Node rootNode = (MT5Node)RootNode;
            rootNode.WriteNode(writer, 0);

            //Write TEXD
            TextureOffset = (uint)writer.BaseStream.Position;
            TEXD texd = new TEXD(Textures);
            texd.Write(writer);

            writer.BaseStream.Seek(baseOffset + 4, SeekOrigin.Begin);
            writer.Write(TextureOffset);
            writer.Write(FirstNodeOffset);
        }
    }

    public class MT5Node : ModelNode
    {
        public uint Offset;

        public uint MeshOffset;
        public uint ChildOffset;
        public uint SiblingOffset;
        public uint ParentOffset;

        public uint ObjectName;
        public uint Unknown;
        public uint NextNode;

        public MT5Mesh MeshData;
        public MT5 MT5;

        /// <summary>
        /// Creates a new MT5Node instance from the given model node.
        /// </summary>
        public MT5Node(ModelNode node, MT5Node parent = null, MT5 mt5 = null)
        {
            MT5 = mt5;

            ID = node.ID;
            Rotation = node.Rotation;
            Position = node.Position;
            Scale = node.Scale;

            Center = node.Center;
            Radius = node.Radius;

            VertexPositions = node.VertexPositions;
            VertexNormals = node.VertexNormals;
            VertexUVs = node.VertexUVs;
            VertexColors = node.VertexColors;

            Faces = node.Faces;

            MeshData = new MT5Mesh(node, this);
            if (node.Child != null)
            {
                Child = new MT5Node(node.Child, this, mt5);
            }
            if (node.Sibling != null)
            {
                Sibling = new MT5Node(node.Sibling, this, mt5);
            }
            Parent = parent;
        }

        public MT5Node(BinaryReader reader, MT5Node parent, MT5 mt5 = null)
        {
            MT5 = mt5;
            Read(reader, parent);
        }

        public void Read(BinaryReader reader, MT5Node parent)
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

            //Console.WriteLine("Node Unknown: {0:X}", Unknown);

            //Read MT5 mesh data
            long offset = reader.BaseStream.Position;
            if (MeshOffset != 0)
            {
                HasMesh = true;
                reader.BaseStream.Seek(MeshOffset, SeekOrigin.Begin);
                MeshData = new MT5Mesh(reader, this);
            }

            //Construct node tree recursively
            if (ChildOffset != 0)
            {
                reader.BaseStream.Seek(ChildOffset, SeekOrigin.Begin);
                Child = new MT5Node(reader, this, MT5);
            }
            if (SiblingOffset != 0)
            {
                reader.BaseStream.Seek(SiblingOffset, SeekOrigin.Begin);
                Sibling = new MT5Node(reader, (MT5Node)Parent, MT5);
            }
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        public void WriteMeshData(BinaryWriter writer)
        {
            MeshData.WriteData(writer);
        }

        public void WriteMeshHeader(BinaryWriter writer)
        {
            MeshOffset = (uint)writer.BaseStream.Position;
            MeshData.WriteHeader(writer);
        }

        public void WriteNode(BinaryWriter writer, uint parentOffset)
        {
            uint offset = (uint)writer.BaseStream.Position;

            writer.Write(ID);
            writer.Write(MeshOffset);

            int rotX = (int)(Rotation.X / 360.0f * 0xffff);
            int rotY = (int)(Rotation.Y / 360.0f * 0xffff);
            int rotZ = (int)(Rotation.Z / 360.0f * 0xffff);
            writer.Write(rotX);
            writer.Write(rotY);
            writer.Write(rotZ);
            writer.Write(Scale.X);
            writer.Write(Scale.Y);
            writer.Write(Scale.Z);
            writer.Write(Position.X);
            writer.Write(Position.Y);
            writer.Write(Position.Z);

            uint nodeOffsets = (uint)writer.BaseStream.Position;
            writer.Write(ChildOffset);
            writer.Write(SiblingOffset);
            writer.Write(parentOffset);

            writer.Write(ObjectName);
            writer.Write(Unknown);

            //Write child and childs children
            if (Child == null)
            {
                ChildOffset = 0;
            }
            else
            {
                ChildOffset = (uint)writer.BaseStream.Position;
                MT5Node child = (MT5Node)Child;
                child.WriteNode(writer, offset);
            }
            
            //Write sibling and siblings children
            if (Sibling == null)
            {
                SiblingOffset = 0;
            }
            else
            {
                SiblingOffset = (uint)writer.BaseStream.Position;
                MT5Node sibling = (MT5Node)Sibling;
                sibling.WriteNode(writer, offset);
            }

            uint endOffset = (uint)writer.BaseStream.Position;
            writer.BaseStream.Seek(nodeOffsets, SeekOrigin.Begin);
            writer.Write(ChildOffset);
            writer.Write(SiblingOffset);
            writer.BaseStream.Seek(endOffset, SeekOrigin.Begin);
        }

        public override string ToString()
        {
            return String.Format("[{0}] MT5 Node: {1} (Bone: {2}, Name: {3}, Unknown: {4})", Offset, ID, BoneID, ObjectName, Unknown);
        }
    }
}
