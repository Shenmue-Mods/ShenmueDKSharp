using ShenmueDKSharp.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Models._MT5
{
    /// <summary>
    /// MT5 mesh data
    /// TODO: fix character model missing strips
    /// </summary>
    public class MT5Mesh
    {
        private MT5Node m_node;
        private MT5Node m_parentNode;

        uint Offset;

        uint PolyType;
        public uint VerticesOffset;
        public int VertexCount;
        public uint FacesOffset;

        /// <summary>
        /// All types based on sm1 asm
        /// </summary>
        public enum MT5MeshEntryType : ushort
        {
            //Skip 2 bytes
            Zero = 0x0000,

            //Skip 12 bytes
            Unknown_0E00 = 0x000E, //ignored
            Unknown_0F00 = 0x000F, //ignored

            //Skip 4 bytes
            Texture = 0x0009,
            Unknown_8000 = 0x0008, //ignored
            Unknown_A000 = 0x000A, //ignored
            Unknown_0B00 = 0x000B,
            StripAttrib_0200 = 0x0002,
            StripAtrrib_0300 = 0x0003,

            //faces (strips)
            Strip_1000 = 0x0010,
            Strip_1100 = 0x0011,
            Strip_1200 = 0x0012,
            Strip_1300 = 0x0013,
            Strip_1400 = 0x0014,

            Strip_1800 = 0x0018,
            Strip_1900 = 0x0019,
            Strip_1A00 = 0x001A,
            Strip_1B00 = 0x001B,
            Strip_1C00 = 0x001C,

            End = 0x8000
        }

        public MT5Mesh(BinaryReader reader, MT5Node node)
        {
            m_node = node;
            m_parentNode = (MT5Node)node.Parent;

            Offset = (uint)reader.BaseStream.Position;

            //Console.WriteLine("MeshOffset: {0}", Offset);

            PolyType = reader.ReadUInt32();
            VerticesOffset = reader.ReadUInt32();
            VertexCount = reader.ReadInt32();
            FacesOffset = reader.ReadUInt32();

            m_node.Center = new Vector3()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle()
            };
            m_node.Radius = reader.ReadSingle();

            //Read strips/faces
            reader.BaseStream.Seek(FacesOffset, SeekOrigin.Begin);

            //initialize states
            uint textureIndex = 0;
            Color4 stripColor = Color4.White;
            bool uvFlag = false;

            uint unknown_0B00 = 0; //polytype? uv mirror? (used when reading strip)

            int uvIndex = 0;
            int colorIndex = 0;

            //read strip functions
            while (reader.BaseStream.Position < reader.BaseStream.Length - 4)
            {
                ushort stripType = reader.ReadUInt16();
                if ((MT5MeshEntryType)stripType == MT5MeshEntryType.End) break;
                
                //Console.WriteLine("StripType: {0}", (MT5MeshEntryType)stripType);

                switch ((MT5MeshEntryType)stripType)
                {
                    case MT5MeshEntryType.Zero:
                        continue;
                    
                    //ignored by d3t
                    case MT5MeshEntryType.Unknown_0E00:
                    case MT5MeshEntryType.Unknown_0F00:
                        reader.BaseStream.Seek(10, SeekOrigin.Current);
                        continue;

                    //ignored by d3t
                    case MT5MeshEntryType.Unknown_8000:
                    case MT5MeshEntryType.Unknown_A000:
                        reader.BaseStream.Seek(2, SeekOrigin.Current);
                        continue;
                    
                    case MT5MeshEntryType.Unknown_0B00:
                        unknown_0B00 = reader.ReadUInt16();
                        //Console.WriteLine("0B00: {0:X}", unknown_0B00);
                        continue;

                    case MT5MeshEntryType.StripAttrib_0200:
                    case MT5MeshEntryType.StripAtrrib_0300:
                        uint size = reader.ReadUInt16();
                        long offset = reader.BaseStream.Position;

                        //Debug output whole attribute bytes
                        byte[] bytes = reader.ReadBytes((int)size);
                        StringBuilder hex = new StringBuilder(bytes.Length * 2);
                        foreach (byte a in bytes)
                        {
                            hex.AppendFormat("{0:X2}", a);
                        }
                        //Console.WriteLine(hex.ToString());
                        reader.BaseStream.Seek(offset, SeekOrigin.Begin);

                        //first byte controls uv somehow
                        uint unknown = reader.ReadUInt32();
                        uvFlag = (unknown & 1) == 1; //TODO: use this flag

                        //skip unknown stuff
                        reader.BaseStream.Seek(3, SeekOrigin.Current);

                        //strip color
                        byte stripB = reader.ReadByte();
                        byte stripG = reader.ReadByte();
                        byte stripR = reader.ReadByte();
                        stripColor = new Color4(stripR, stripG, stripB, 255);

                        reader.BaseStream.Seek(offset + size, SeekOrigin.Begin);
                        continue;

                    case MT5MeshEntryType.Texture:
                        textureIndex = reader.ReadUInt16();
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

                        bool hasUV = false;
                        bool hasColor = false;
                        if ((MT5MeshEntryType)stripType == MT5MeshEntryType.Strip_1200 ||
                            (MT5MeshEntryType)stripType == MT5MeshEntryType.Strip_1A00)
                        {
                            hasColor = true;
                        }
                        if ((MT5MeshEntryType)stripType == MT5MeshEntryType.Strip_1100 ||
                            (MT5MeshEntryType)stripType == MT5MeshEntryType.Strip_1900)
                        {
                            hasUV = true;
                        }
                        if ((MT5MeshEntryType)stripType == MT5MeshEntryType.Strip_1400 ||
                            (MT5MeshEntryType)stripType == MT5MeshEntryType.Strip_1C00)
                        {
                            hasUV = true;
                            hasColor = true;
                        }

                        reader.BaseStream.Seek(2, SeekOrigin.Current); //d3t skips this short value
                        ushort stripCount = reader.ReadUInt16();
                        if (stripCount == 0) continue;
            
                        for (int i = 0; i < stripCount; i++)
                        {
                            MeshFace face = new MeshFace();
                            face.Type = MeshFace.PrimitiveType.TriangleStrip;
                            face.TextureIndex = textureIndex;
                            face.StripColor = stripColor;
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
                                    if (m_parentNode == null || m_parentNode.MeshData == null)
                                    {
                                        vertIndex = (short)(vertIndex + VertexCount);
                                    }
                                    else
                                    {
                                        //Offset parent vertex indices by own vertex count so we use the appended parents vertices
                                        vertIndex = (short)(VertexCount + vertIndex + m_parentNode.VertexCount);
                                    }
                                }
                                face.PositionIndices.Add((ushort)vertIndex);
                                face.NormalIndices.Add((ushort)vertIndex);

                                if (hasUV)
                                {
                                    short texU = reader.ReadInt16();
                                    short texV = reader.ReadInt16();

                                    //UV/N normal-resolution 0 - 255
                                    //UVH high-resolution 0 - 1023
                                    float u = texU / 1024.0f;
                                    float v = texV / 1024.0f;

                                    m_node.VertexUVs.Add(new Vector2(u, v));
                                    face.UVIndices.Add((ushort)uvIndex);
                                    uvIndex++;
                                }

                                if (hasColor)
                                {
                                    //BGRA (8888) 32BPP
                                    byte b = reader.ReadByte();
                                    byte g = reader.ReadByte();
                                    byte r = reader.ReadByte();
                                    byte a = reader.ReadByte();

                                    m_node.VertexColors.Add(new Color4(r, g, b, a));
                                    face.ColorIndices.Add((ushort)colorIndex);
                                    colorIndex++;
                                }
                            }
                            m_node.Faces.Add(face);
                        }
                        continue;

                    default:
                        //unknown type defaults to breaking
                        stripType = (ushort)MT5MeshEntryType.End;
                        break;
                }
                if ((MT5MeshEntryType)stripType == MT5MeshEntryType.End) break;
            }

            //Read vertices
            reader.BaseStream.Seek(VerticesOffset, SeekOrigin.Begin);
            for (int i = 0; i < VertexCount; i++)
            {
                Vector3 pos;
                pos.X = reader.ReadSingle();
                pos.Y = reader.ReadSingle();
                pos.Z = reader.ReadSingle();
                m_node.VertexPositions.Add(pos);

                Vector3 norm;
                norm.X = reader.ReadSingle();
                norm.Y = reader.ReadSingle();
                norm.Z = reader.ReadSingle();
                m_node.VertexNormals.Add(norm);
            }

            if (m_parentNode != null && m_parentNode.MeshData != null)
            {
                //Because for performance/memory saving the vertices from the parent can be used via negativ vertex indices
                //we just copy the parent vertices so we can use them with modified vertex offsets

                //Apply the inverted transform matrix of the node on vertices so they get canceled out by the final transform.
                Matrix4 matrix = m_node.GetTransformMatrixSelf().Inverted();
                for (int i = 0; i < m_parentNode.VertexCount; i++)
                {
                    Vector3 pos = new Vector3(m_parentNode.VertexPositions[i]);
                    Vector3 norm = new Vector3(m_parentNode.VertexNormals[i]);
                    pos = Vector3.TransformPosition(pos, matrix);
                    norm = Vector3.TransformPosition(norm, matrix);
                    m_node.VertexPositions.Add(pos);
                    m_node.VertexNormals.Add(norm);
                }
            }
        }
    }
}
