using ShenmueDKSharp.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShenmueDKSharp.Files.Models._MT5.MT5Mesh;

namespace ShenmueDKSharp.Files.Models._MT5
{
    /// <summary>
    /// MT5 mesh data
    /// TODO: fix character model missing strips
    /// </summary>
    public class MT5Mesh
    {
        public MT5Node Node;
        public MT5Node ParentNode;

        uint Offset;

        uint PolyType;
        public uint VerticesOffset;
        public int VertexCount;
        public uint FacesOffset;

        //Reading and writing helper variables
        public List<MT5StripEntry> StripEntries = new List<MT5StripEntry>();
        public uint CurrentTextureIndex = 0;
        public uint CurrentUVIndex;
        public uint CurrentColorIndex;
        public bool CurrentIsUVH = true;
        public bool CurrentMirrorU = false;
        public bool CurrentMirrorV = false;
        public float CurrentUVSize = 1024.0f;

        /// <summary>
        /// All types based on sm1 asm
        /// </summary>
        public enum MT5MeshEntryType : ushort
        {
            //Skip 2 bytes
            Zero = 0x0000,
            ZeroN = 0xFFFF,

            //Skip 12 bytes
            Unknown_0E00 = 0x000E, //ignored
            Unknown_0F00 = 0x000F, //ignored

            //Skip 4 bytes
            Texture = 0x0009,
            Unknown_8000 = 0x0008, //ignored
            Unknown_A000 = 0x000A, //ignored

            UVSize_0B00 = 0x000B, 

            //unknown
            StripAttrib_0200 = 0x0002,
            StripAttrib_0300 = 0x0003,
            StripAttrib_0400 = 0x0004,
            StripAttrib_0500 = 0x0005,
            StripAttrib_0600 = 0x0006,
            StripAttrib_0700 = 0x0007,

            //faces (strips)
            Strip_1000 = 0x0010, //Pos, Norm
            Strip_1100 = 0x0011, //Pos, Norm, UV
            Strip_1200 = 0x0012, //Pos, Norm, Color
            Strip_1300 = 0x0013, //Pos, Norm
            Strip_1400 = 0x0014, //Pos, Norm, UV, Color

            Strip_1800 = 0x0018, //Pos, Norm
            Strip_1900 = 0x0019, //Pos, Norm, UV
            Strip_1A00 = 0x001A, //Pos, Norm, Color
            Strip_1B00 = 0x001B, //Pos, Norm
            Strip_1C00 = 0x001C, //Pos, Norm, UV, Color

            End = 0x8000
        }

        public MT5Mesh(ModelNode node, MT5Node newNode)
        {
            Node = newNode;
            foreach (MeshFace face in node.Faces)
            {
                MT5StripAttributes attr = new MT5StripAttributes(MT5MeshEntryType.StripAttrib_0200);
                StripEntries.Add(attr);

                MT5StripTexture tex = new MT5StripTexture();
                tex.TextureIndex = (ushort)face.TextureIndex;
                StripEntries.Add(tex);

                MT5UVSize unk0B00 = new MT5UVSize();
                StripEntries.Add(unk0B00);

                if (face.HasUVs && face.HasColors)
                {
                    MT5Strip strip = new MT5Strip(MT5MeshEntryType.Strip_1C00, this);
                    strip.Faces.Add(face);
                    StripEntries.Add(strip);
                }
                if (face.HasUVs)
                {
                    MT5Strip strip = new MT5Strip(MT5MeshEntryType.Strip_1900, this);
                    strip.Faces.Add(face);
                    StripEntries.Add(strip);
                }
                if (face.HasColors)
                {
                    MT5Strip strip = new MT5Strip(MT5MeshEntryType.Strip_1A00, this);
                    strip.Faces.Add(face);
                    StripEntries.Add(strip);
                }
            }
        }

        public MT5Mesh(BinaryReader reader, MT5Node node)
        {
            Read(reader, node);
        }

        public void Read(BinaryReader reader, MT5Node node)
        {
            Node = node;
            ParentNode = (MT5Node)node.Parent;

            Offset = (uint)reader.BaseStream.Position;

            //Console.WriteLine("MeshOffset: {0}", Offset);

            PolyType = reader.ReadUInt32();
            VerticesOffset = reader.ReadUInt32();
            VertexCount = reader.ReadInt32();
            FacesOffset = reader.ReadUInt32();

            Node.Center = new Vector3()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle()
            };
            Node.Radius = reader.ReadSingle();

            //Read strips/faces
            reader.BaseStream.Seek(FacesOffset, SeekOrigin.Begin);

            //read strip functions
            while (reader.BaseStream.Position < reader.BaseStream.Length - 4)
            {
                ushort stripType = reader.ReadUInt16();
                if ((MT5MeshEntryType)stripType == MT5MeshEntryType.End)
                {
                    MT5_End stripEnd = new MT5_End();
                    StripEntries.Add(stripEnd);
                    break;
                }

                switch ((MT5MeshEntryType)stripType)
                {
                    case MT5MeshEntryType.Zero:
                        MT5_Zero stripZero = new MT5_Zero();
                        StripEntries.Add(stripZero);
                        continue;
                    case MT5MeshEntryType.ZeroN:
                        MT5_ZeroN stripZeroN = new MT5_ZeroN();
                        StripEntries.Add(stripZeroN);
                        continue;

                    case MT5MeshEntryType.StripAttrib_0200:
                    case MT5MeshEntryType.StripAttrib_0300:
                    case MT5MeshEntryType.StripAttrib_0400:
                    case MT5MeshEntryType.StripAttrib_0500:
                    case MT5MeshEntryType.StripAttrib_0600:
                    case MT5MeshEntryType.StripAttrib_0700:
                        MT5StripAttributes stripAttributes = new MT5StripAttributes((MT5MeshEntryType)stripType);
                        stripAttributes.Read(reader);
                        CurrentIsUVH = stripAttributes.IsUVH;
                        CurrentMirrorU = stripAttributes.MirrorU;
                        CurrentMirrorV = stripAttributes.MirrorV;
                        StripEntries.Add(stripAttributes);
                        continue;

                    //ignored by d3t
                    case MT5MeshEntryType.Unknown_0E00:
                    case MT5MeshEntryType.Unknown_0F00:
                        MT5_0E00_0F00 strip_0E00_0F00 = new MT5_0E00_0F00((MT5MeshEntryType)stripType);
                        strip_0E00_0F00.Read(reader);
                        StripEntries.Add(strip_0E00_0F00);
                        continue;

                    //ignored by d3t
                    case MT5MeshEntryType.Unknown_8000:
                    case MT5MeshEntryType.Unknown_A000:
                        MT5_8000_A000 strip_8000_A000 = new MT5_8000_A000((MT5MeshEntryType)stripType);
                        strip_8000_A000.Read(reader);
                        StripEntries.Add(strip_8000_A000);
                        continue;

                    case MT5MeshEntryType.UVSize_0B00:
                        MT5UVSize uvSize = new MT5UVSize();
                        uvSize.Read(reader);
                        CurrentUVSize = uvSize.Value;
                        StripEntries.Add(uvSize);
                        continue;

                    case MT5MeshEntryType.Texture:
                        MT5StripTexture stripTexture = new MT5StripTexture();
                        stripTexture.Read(reader);
                        CurrentTextureIndex = stripTexture.TextureIndex;
                        if (CurrentTextureIndex < Node.MT5.Textures.Count)
                        {
                            if (Node.MT5.Textures[(int)CurrentTextureIndex] == null) continue;
                            if (MT5.UVMirrorTextureResize)
                            {
                                Node.MT5.Textures[(int)CurrentTextureIndex].Image.MirrorResize(CurrentMirrorU, CurrentMirrorV);
                            }
                        }
                        StripEntries.Add(stripTexture);
                        continue;

                    //Face strips
                    case MT5MeshEntryType.Strip_1000:
                    case MT5MeshEntryType.Strip_1100:
                    case MT5MeshEntryType.Strip_1200:
                    case MT5MeshEntryType.Strip_1300:
                    case MT5MeshEntryType.Strip_1400:
                    case MT5MeshEntryType.Strip_1800:
                    case MT5MeshEntryType.Strip_1900:
                    case MT5MeshEntryType.Strip_1A00:
                    case MT5MeshEntryType.Strip_1B00:
                    case MT5MeshEntryType.Strip_1C00:
                        MT5Strip strip = new MT5Strip((MT5MeshEntryType)stripType, this);
                        strip.Read(reader);
                        StripEntries.Add(strip);
                        continue;

                    default:
                        //unknown type defaults to breaking
                        stripType = (ushort)MT5MeshEntryType.End;
                        break;
                }

                if ((MT5MeshEntryType)stripType == MT5MeshEntryType.End)
                {
                    MT5_End stripEnd = new MT5_End();
                    StripEntries.Add(stripEnd);
                    break;
                }
            }

            //Read vertices
            reader.BaseStream.Seek(VerticesOffset, SeekOrigin.Begin);
            for (int i = 0; i < VertexCount; i++)
            {
                Vector3 pos;
                pos.X = reader.ReadSingle();
                pos.Y = reader.ReadSingle();
                pos.Z = reader.ReadSingle();
                Node.VertexPositions.Add(pos);

                Vector3 norm;
                norm.X = reader.ReadSingle();
                norm.Y = reader.ReadSingle();
                norm.Z = reader.ReadSingle();
                Node.VertexNormals.Add(norm);
            }

            if (ParentNode != null && ParentNode.MeshData != null)
            {
                //Because for performance/memory saving the vertices from the parent can be used via negativ vertex indices
                //we just copy the parent vertices so we can use them with modified vertex offsets

                //Apply the inverted transform matrix of the node on vertices so they get canceled out by the final transform.
                Matrix4 matrix = Node.GetTransformMatrixSelf().Inverted();
                for (int i = 0; i < ParentNode.VertexCount; i++)
                {
                    Vector3 pos = new Vector3(ParentNode.VertexPositions[i]);
                    Vector3 norm = new Vector3(ParentNode.VertexNormals[i]);
                    pos = Vector3.TransformPosition(pos, matrix);
                    norm = Vector3.TransformPosition(norm, matrix);
                    
                    Node.VertexPositions.Add(pos);
                    Node.VertexNormals.Add(norm);
                }
            }
        }

        public void WriteData(BinaryWriter writer)
        {
            //TODO: Optimize strips to use parent vertices

            //Write strips
            FacesOffset = (uint)writer.BaseStream.Position;
            foreach (MT5StripEntry entry in StripEntries)
            {
                entry.Write(writer);
            }

            //Write Vertices
            VerticesOffset = (uint)writer.BaseStream.Position;
            VertexCount = Node.VertexPositions.Count;
            for (int i = 0; i < VertexCount; i++)
            {
                Vector3 pos = Node.VertexPositions[i];
                Vector3 norm = Node.VertexNormals[i];
                writer.Write(pos.X);
                writer.Write(pos.Y);
                writer.Write(pos.Z);
                writer.Write(norm.X);
                writer.Write(norm.Y);
                writer.Write(norm.Z);
            }
        }

        public void WriteHeader(BinaryWriter writer)
        {
            writer.Write(PolyType);
            writer.Write(VerticesOffset);
            writer.Write(VertexCount);
            writer.Write(FacesOffset);
            writer.Write(Node.Center.X);
            writer.Write(Node.Center.Y);
            writer.Write(Node.Center.Z);
            writer.Write(Node.Radius);
        }
    }


    //Strip entry types
    //Needed for easier writing

    public abstract class MT5StripEntry
    {
        public abstract MT5MeshEntryType Type { get; set; }
        public abstract void Read(BinaryReader reader);
        public abstract void Write(BinaryWriter writer);
        public override string ToString()
        {
            return Type.ToString();
        }
    }

    public class MT5_Zero : MT5StripEntry
    {
        public override MT5MeshEntryType Type
        {
            get { return MT5MeshEntryType.Zero; }
            set { }
        }

        public override void Read(BinaryReader reader)
        {
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((ushort)Type);
        }
    }

    public class MT5_ZeroN : MT5StripEntry
    {
        public override MT5MeshEntryType Type
        {
            get { return MT5MeshEntryType.ZeroN; }
            set { }
        }

        public override void Read(BinaryReader reader)
        {
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((ushort)Type);
        }
    }

    public class MT5_End : MT5StripEntry
    {
        public override MT5MeshEntryType Type
        {
            get { return MT5MeshEntryType.End; }
            set { }
        }

        public override void Read(BinaryReader reader)
        {
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((ushort)Type);
        }
    }

    public class MT5_0E00_0F00 : MT5StripEntry
    {
        private MT5MeshEntryType m_type;
        public override MT5MeshEntryType Type
        {
            get
            {
                return m_type;
            }
            set
            {
                if (value == MT5MeshEntryType.Unknown_0E00 ||
                    value == MT5MeshEntryType.Unknown_0F00)
                {
                    m_type = value;
                }
            }
        }

        public byte[] Data;

        public MT5_0E00_0F00(MT5MeshEntryType stripType)
        {
            m_type = stripType;
        }

        public override void Read(BinaryReader reader)
        {
            Data = reader.ReadBytes(10);
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((ushort)Type);
            writer.Write(Data);
        }
    }

    public class MT5_8000_A000 : MT5StripEntry
    {
        private MT5MeshEntryType m_type;
        public override MT5MeshEntryType Type
        {
            get
            {
                return m_type;
            }
            set
            {
                if (value == MT5MeshEntryType.Unknown_8000 ||
                    value == MT5MeshEntryType.Unknown_A000)
                {
                    m_type = value;
                }
            }
        }

        public ushort Value;

        public MT5_8000_A000(MT5MeshEntryType stripType)
        {
            m_type = stripType;
        }

        public override void Read(BinaryReader reader)
        {
            Value = reader.ReadUInt16();
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((ushort)Type);
            writer.Write(Value);
        }
    }

    public class MT5StripAttributes : MT5StripEntry
    {
        private MT5MeshEntryType m_type;
        public override MT5MeshEntryType Type
        {
            get
            {
                return m_type;
            }
            set
            {
                if (value == MT5MeshEntryType.StripAttrib_0200 ||
                    value == MT5MeshEntryType.StripAttrib_0300 ||
                    value == MT5MeshEntryType.StripAttrib_0400 ||
                    value == MT5MeshEntryType.StripAttrib_0500 ||
                    value == MT5MeshEntryType.StripAttrib_0600 ||
                    value == MT5MeshEntryType.StripAttrib_0700)
                {
                    m_type = value;
                }
            }
        }

        ushort Size = 4;
        byte[] Data = new byte[] { 0, 0, 0, 0 };

        public MT5StripAttributes(MT5MeshEntryType stripType)
        {
            m_type = stripType;
        }

        public override void Read(BinaryReader reader)
        {
            Size = reader.ReadUInt16();
            Data = reader.ReadBytes(Size);
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((ushort)Type);
            writer.Write(Size);
            writer.Write(Data);
        }

        public bool IsUVH {
            get
            {
                if (Data.Length < 1) return false;
                return (Data[0] & 1) == 1;
            }
        }
        public bool MirrorU
        {
            get
            {
                if (Data.Length < 11) return false;
                return (Data[10] & 4) == 4;
            }
        }

        public bool MirrorV
        {
            get
            {
                if (Data.Length < 11) return false;
                return (Data[10] & 2) == 2;
            }
        }
    }

    public class MT5StripTexture : MT5StripEntry
    {
        public override MT5MeshEntryType Type
        {
            get { return MT5MeshEntryType.Texture; }
            set { }
        }

        public ushort TextureIndex;

        public override void Read(BinaryReader reader)
        {
            TextureIndex = reader.ReadUInt16();
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((ushort)Type);
            writer.Write(TextureIndex);
        }
    }

    public class MT5Strip : MT5StripEntry
    {
        private MT5MeshEntryType m_type;
        private MT5Mesh m_mesh;

        public override MT5MeshEntryType Type
        {
            get
            {
                return m_type;
            }
            set
            {
                if (value == MT5MeshEntryType.Strip_1000 ||
                    value == MT5MeshEntryType.Strip_1100 ||
                    value == MT5MeshEntryType.Strip_1200 ||
                    value == MT5MeshEntryType.Strip_1300 ||
                    value == MT5MeshEntryType.Strip_1400 ||
                    value == MT5MeshEntryType.Strip_1800 ||
                    value == MT5MeshEntryType.Strip_1900 ||
                    value == MT5MeshEntryType.Strip_1A00 ||
                    value == MT5MeshEntryType.Strip_1B00 ||
                    value == MT5MeshEntryType.Strip_1C00)
                {
                    m_type = value;
                }
            }
        }

        public bool HasUV
        {
            get
            {
                return Type == MT5MeshEntryType.Strip_1100 ||
                       Type == MT5MeshEntryType.Strip_1900 ||
                       Type == MT5MeshEntryType.Strip_1400 ||
                       Type == MT5MeshEntryType.Strip_1C00;
            }
        }

        public bool HasColor
        {
            get
            {
                return Type == MT5MeshEntryType.Strip_1200 ||
                       Type == MT5MeshEntryType.Strip_1A00 ||
                       Type == MT5MeshEntryType.Strip_1400 ||
                       Type == MT5MeshEntryType.Strip_1C00;
            }
        }

        public ushort Unknown;

        public List<MeshFace> Faces = new List<MeshFace>();

        public MT5Strip(MT5MeshEntryType stripType, MT5Mesh mesh)
        {
            m_type = stripType;
            m_mesh = mesh;
        }

        public override void Read(BinaryReader reader)
        {
            Unknown = reader.ReadUInt16();
            ushort stripCount = reader.ReadUInt16();
            if (stripCount == 0) return;

            bool hasUV = HasUV;
            bool hasColor = HasColor;
            bool isUVH = m_mesh.CurrentIsUVH;
            float uvSize = m_mesh.CurrentUVSize;
            bool uMirror = m_mesh.CurrentMirrorU;
            bool vMirror = m_mesh.CurrentMirrorV;

            for (int i = 0; i < stripCount; i++)
            {
                MeshFace face = new MeshFace();
                face.Type = MeshFace.PrimitiveType.TriangleStrip;
                face.TextureIndex = m_mesh.CurrentTextureIndex;
                face.Wrap = MeshFace.WrapMode.Repeat; //TODO: find wrap mode

                short stripLength = reader.ReadInt16();
                if (stripLength < 0)
                {
                    stripLength = Math.Abs(stripLength);
                }

                face.PositionIndices = new List<ushort>();
                face.NormalIndices = new List<ushort>();
                for (int j = 0; j < stripLength; j++)
                {
                    short vertIndex = reader.ReadInt16();
                    while (vertIndex < 0)
                    {
                        if (m_mesh.ParentNode == null || m_mesh.ParentNode.MeshData == null)
                        {
                            vertIndex = 0;
                        }
                        else
                        {
                            //Offset parent vertex indices by own vertex count so we use the appended parents vertices
                            vertIndex = (short)(m_mesh.VertexCount + vertIndex + m_mesh.ParentNode.MeshData.VertexCount);
                        }
                    }
                    face.PositionIndices.Add((ushort)vertIndex);
                    face.NormalIndices.Add((ushort)vertIndex);

                    if (hasUV)
                    {
                        float texU = reader.ReadInt16();
                        float texV = reader.ReadInt16();

                        //UV/N normal-resolution 0 - 255
                        //UVH high-resolution 0 - 1023
                        if (isUVH)
                        {
                            texU = (float)(texU * 0.000015258789);
                            texV = (float)(texV * 0.000015258789);
                        }
                        else
                        {
                            if (texU < 61440.0)
                            {
                                texU /= uvSize;
                            }
                            else
                            {
                                texU = (float)(texU * 0.00000000023283064);
                            }
                            if (texV < 61440.0)
                            {
                                texV /= uvSize;
                            }
                            else
                            {
                                texV = (float)(texV * 0.00000000023283064);
                            }
                        }

                        if (uMirror)
                        {
                            if (texU > 1.0f)
                            {
                                texU /= 2.0f;
                            }
                        }

                        if (vMirror)
                        {
                            if (texV > 1.0f)
                            {
                                texV /= 2.0f;
                            }
                        }

                        m_mesh.Node.VertexUVs.Add(new Vector2(texU, texV));
                        face.UVIndices.Add((ushort)m_mesh.CurrentUVIndex);
                        m_mesh.CurrentUVIndex++;
                    }

                    if (hasColor)
                    {
                        //BGRA (8888) 32BPP
                        byte b = reader.ReadByte();
                        byte g = reader.ReadByte();
                        byte r = reader.ReadByte();
                        byte a = reader.ReadByte();

                        m_mesh.Node.VertexColors.Add(new Color4(r, g, b, a));
                        face.ColorIndices.Add((ushort)m_mesh.CurrentColorIndex);
                        m_mesh.CurrentColorIndex++;
                    }
                }
                m_mesh.Node.Faces.Add(face);
                Faces.Add(face);
            }
        }

        public override void Write(BinaryWriter writer)
        {
            ushort stripCount = (ushort)Faces.Count;

            writer.Write((ushort)Type);
            writer.Write(Unknown);
            writer.Write(stripCount);
            if (stripCount == 0) return;

            bool hasUV = HasUV;
            bool hasColor = HasColor;

            for (int i = 0; i < stripCount; i++)
            {
                MeshFace face = Faces[i];
                short stripLength = (short)face.PositionIndices.Count;
                writer.Write(stripLength);

                for (int j = 0; j < stripLength; j++)
                {
                    writer.Write((short)face.PositionIndices[j]);
                    if (hasUV)
                    {
                        Vector2 uv = m_mesh.Node.VertexUVs[face.UVIndices[j]];
                        short texU = (short)(uv.X * 1024.0f);
                        short texV = (short)(uv.Y * 1024.0f);
                        writer.Write(texU);
                        writer.Write(texV);
                    }
                    if (hasColor)
                    {
                        Color4 color = m_mesh.Node.VertexColors[face.ColorIndices[j]];
                        writer.Write(color.B_);
                        writer.Write(color.G_);
                        writer.Write(color.R_);
                        writer.Write(color.A_);
                    }
                }
            }
        }
    }

    public class MT5UVSize : MT5StripEntry
    {
        public override MT5MeshEntryType Type
        {
            get { return MT5MeshEntryType.UVSize_0B00; }
            set { }
        }

        public float Value;

        public override void Read(BinaryReader reader)
        {
            Value = reader.ReadUInt16();
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((ushort)Type);
            writer.Write((ushort)Value);
        }
    }
}
