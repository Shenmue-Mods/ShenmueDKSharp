using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ShenmueDKSharp.Files.Models.MeshFace;
using static ShenmueDKSharp.Files.Models.Vertex;

namespace ShenmueDKSharp.Files.Models._MT7
{
    /// <summary>
    /// Mesh data for the MT7 format which holds the vertices, triangle strips and material data.
    /// </summary>
    public class XB01
    {
        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x58, 0x42, 0x30, 0x31 } //XB01
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

        public uint Offset;

        public uint Identifier;
        public uint Unknown;

        public Vector3 MeshCenter;
        public float MeshDiameter;

        public uint FirstEntryOffset;
        public uint Size;

        public uint vertUnknown1;
        public uint vertUnknown2;
        public uint vertUnknown3;
        public uint verticesSize;
        public ushort vertSize;

        public List<MeshFace> Faces = new List<MeshFace>();
        public List<XB01Group> Groups = new List<XB01Group>();
        public List<Vertex> Vertices = new List<Vertex>();

        public float[] GetVertices()
        {
            List<float> vertices = new List<float>();
            foreach (MeshFace face in Faces)
            {
                vertices.AddRange(face.GetFloatArray(Vertices));
            }
            return vertices.ToArray();
        }

        public XB01(BinaryReader reader)
        {
            Offset = (uint)reader.BaseStream.Position;
            Identifier = reader.ReadUInt32();
            Unknown = reader.ReadUInt32();

            MeshCenter = new Vector3()
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle()
            };
            MeshDiameter = reader.ReadSingle();

            FirstEntryOffset = reader.ReadUInt32();
            Size = reader.ReadUInt32();
            uint offsetVertices = Size * 4 + (uint)reader.BaseStream.Position - 4;

            reader.BaseStream.Seek(Offset + (FirstEntryOffset + 6) * 4, SeekOrigin.Begin);


            XB01Group group = new XB01Group(reader);
            Groups.Add(group);

            XB01_Tex currentTexture = null;
            XB01_TexAttr currentTexAttr = null;
            while (reader.BaseStream.Position < offsetVertices - 8)
            {
                uint zeroCheck = reader.ReadUInt32();
                if (zeroCheck != 0)
                {
                    reader.BaseStream.Seek(-4, SeekOrigin.Current);
                }
                else
                {
                    if (reader.BaseStream.Position >= group.Offset + group.Size)
                    {
                        group = new XB01Group(reader);
                        Groups.Add(group);
                    }
                    else
                    {
                        continue;
                    }
                }

                byte type = reader.ReadByte();
                reader.BaseStream.Seek(-1, SeekOrigin.Current);

                XB01Entry entry;
                switch (type)
                {
                    case 0x00:
                        entry = new XB01_Zero(reader);
                        group.Entries.Add(entry);
                        break;
                    case 0x04:
                        entry = new XB01_Floats(reader);
                        group.Entries.Add(entry);
                        break;
                    case 0x0B:
                        currentTexture = new XB01_Tex(reader);
                        group.Entries.Add(currentTexture);
                        break;
                    case 0x0D:
                        currentTexAttr = new XB01_TexAttr(reader);
                        group.Entries.Add(currentTexAttr);
                        break;
                    case 0x10:
                        XB01_Strip strip = new XB01_Strip(reader);
                        MeshFace face = new MeshFace
                        {
                            TextureIndex = currentTexture.Textures[0],
                            VertexIndices = strip.VertIndices.ToArray(),
                            Type = PrimitiveType.Triangles,
                            Wrap = currentTexAttr.Wrap,
                            Transparent = currentTexAttr.Transparent,
                            Unlit = currentTexAttr.Unlit
                        };
                        Faces.Add(face);
                        group.Entries.Add(strip);
                        break;
                    default:
                        entry = new XB01_Unknown(reader);
                        group.Entries.Add(entry);
                        break;
                }
            }

            reader.BaseStream.Seek(offsetVertices, SeekOrigin.Begin);
            vertUnknown1 = reader.ReadUInt32();
            vertUnknown2 = reader.ReadUInt32();
            vertSize = reader.ReadUInt16();
            vertUnknown3 = reader.ReadUInt16();
            verticesSize = reader.ReadUInt32();

            VertexFormat vertexFormat = Vertex.GetFormat(vertSize);
            for (uint i = 0; i < verticesSize; i += vertSize)
            {
                Vertex vert = new Vertex(vertexFormat);
                vert.PosX = reader.ReadSingle();
                vert.PosY = reader.ReadSingle();
                vert.PosZ = reader.ReadSingle();

                if (vertSize > 12)
                {
                    vert.NormX = reader.ReadSingle();
                    vert.NormY = reader.ReadSingle();
                    vert.NormZ = reader.ReadSingle();
                    if (vertSize > 24)
                    {
                        vert.U = reader.ReadSingle();
                        vert.V = reader.ReadSingle();
                    }
                }
                Vertices.Add(vert);
            }
        }

        public class XB01Group
        {
            public byte ID;
            public ushort Size;
            public ushort[] Data; //looks like ushort values and not floats
            public uint Offset;
            public List<XB01Entry> Entries = new List<XB01Entry>();

            public XB01Group(BinaryReader reader)
            {
                Offset = (uint)reader.BaseStream.Position;

                ID = reader.ReadByte();
                Size = reader.ReadUInt16();
                reader.BaseStream.Seek(1, SeekOrigin.Current);

                Data = new ushort[8];
                for (int i = 0; i < 8; i++)
                {
                    Data[i] = reader.ReadUInt16();
                }
            }
        }

        public enum XB01EntryType
        {
            Zero = 0x00, //Always at start, Always size 5 
            Type01 = 0x01,
            Type02 = 0x02,
            Floats = 0x04,
            Type05 = 0x05,
            Texture = 0x0B,
            TexAttr = 0x0D,
            Type0E = 0x0E,
            Strip = 0x10, //Strip/VertexArray
            Type16 = 0x16,
            Type89 = 0x89,
            TypeD8 = 0xD8
        }

        public abstract class XB01Entry
        {
            public abstract XB01EntryType Type { get; set; }
            public abstract uint Size { get; set; }
            public abstract uint Offset { get; set; }
            public abstract byte[] Data { get; set; }

            public abstract void Read(BinaryReader reader);

            public XB01Entry(BinaryReader reader)
            {
                Read(reader);
            }
        }

        public class XB01_Tex : XB01Entry
        {
            public override uint Size { get; set; }
            public override XB01EntryType Type { get; set; }
            public override byte[] Data { get; set; }
            public override uint Offset { get; set; }

            public uint Unknown;
            public List<uint> Textures = new List<uint>();

            public XB01_Tex(BinaryReader reader) : base(reader) { }

            public override void Read(BinaryReader reader)
            {
                long position = reader.BaseStream.Position;
                Offset = (uint)position;

                Type = (XB01EntryType)reader.ReadByte();
                Size = (byte)(reader.ReadByte() - 1);
                reader.BaseStream.Seek(2, SeekOrigin.Current);
                Unknown = reader.ReadUInt32();

                for (int i = 0; i < Size - 2; i++)
                {
                    Textures.Add(reader.ReadUInt32());
                }

                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                Data = reader.ReadBytes((int)Size * 4);

                reader.BaseStream.Seek(position + (Size + 1) * 4, SeekOrigin.Begin);
            }
        }

        public class XB01_TexAttr : XB01Entry
        {
            public override uint Size { get; set; }
            public override XB01EntryType Type { get; set; }
            public override byte[] Data { get; set; }
            public override uint Offset { get; set; }

            public uint AttributeCount { get; set; }
            public WrapMode Wrap { get; set; }
            public bool Transparent { get; set; } = false;
            public bool Unlit { get; set; } = false;

            public XB01_TexAttr(BinaryReader reader) : base(reader) { }

            public override void Read(BinaryReader reader)
            {
                long position = reader.BaseStream.Position;
                Offset = (uint)position;

                Type = (XB01EntryType)reader.ReadByte();
                Size = reader.ReadByte();
                reader.BaseStream.Seek(2, SeekOrigin.Current);

                AttributeCount = reader.ReadUInt32();

                for (int i = 0; i < AttributeCount; i++)
                {
                    uint attr = reader.ReadUInt16();

                    if (attr == 0x0010) //Transparency stuff
                    {
                        Transparent = true;
                        uint val = reader.ReadUInt16();
                        if (val == 0x0400)
                        {
                            Unlit = true;
                        }
                    }
                    else if (attr == 0x0100)
                    {
                        uint val = reader.ReadUInt16();
                    }
                    else if (attr == 0x0000)
                    {
                        uint val = reader.ReadUInt16();
                        if (val == 0x0002)
                        {
                            Wrap = WrapMode.MirroredRepeat;
                        }
                        else if (val == 0x0001)
                        {
                            Wrap = WrapMode.Repeat;
                        }
                        else if (val == 0x0003)
                        {
                            Wrap = WrapMode.Repeat;
                        }
                    }
                    else
                    {
                        uint val = reader.ReadUInt16();
                    }
                }

                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                Data = reader.ReadBytes((int)Size * 4);
                reader.BaseStream.Seek(position, SeekOrigin.Begin);

                reader.BaseStream.Seek(position + Size * 4, SeekOrigin.Begin);
            }
        }

        public class XB01_Floats : XB01Entry
        {
            public override uint Size { get; set; }
            public override XB01EntryType Type { get; set; }
            public override byte[] Data { get; set; }
            public override uint Offset { get; set; }

            public List<float> Floats = new List<float>();

            public XB01_Floats(BinaryReader reader) : base(reader) { }

            public override void Read(BinaryReader reader)
            {
                long position = reader.BaseStream.Position;
                Offset = (uint)position;

                Type = (XB01EntryType)reader.ReadByte();
                Size = reader.ReadByte();
                reader.BaseStream.Seek(2, SeekOrigin.Current);

                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                Data = reader.ReadBytes((int)Size * 4);
                reader.BaseStream.Seek(position, SeekOrigin.Begin);

                for (int i = 0; i < Size - 1; i++)
                {
                    Floats.Add(reader.ReadSingle());
                }

                reader.BaseStream.Seek(position + Size * 4, SeekOrigin.Begin);
            }
        }

        public class XB01_Unknown : XB01Entry
        {
            public override uint Size { get; set; }
            public override XB01EntryType Type { get; set; }
            public override byte[] Data { get; set; }
            public override uint Offset { get; set; }

            public XB01_Unknown(BinaryReader reader) : base(reader) { }

            public override void Read(BinaryReader reader)
            {
                long position = reader.BaseStream.Position;
                Offset = (uint)position;

                Type = (XB01EntryType)reader.ReadByte();
                Size = (byte)(reader.ReadByte() & 0x0F);
                reader.BaseStream.Seek(-2, SeekOrigin.Current);

                Data = reader.ReadBytes((byte)Size * 4);

                reader.BaseStream.Seek(position + Size * 4, SeekOrigin.Begin);

                if (Size == 0)
                {
                    reader.BaseStream.Seek(position + 4, SeekOrigin.Begin);
                }
            }
        }

        public class XB01_Strip : XB01Entry
        {
            public override uint Size { get; set; }
            public override XB01EntryType Type { get; set; }
            public override byte[] Data { get; set; }
            public override uint Offset { get; set; }

            public uint VertCount;
            public uint Unknown; //Should be strip type (5 = GL_TRIANGLES)
            public List<ushort> VertIndices = new List<ushort>();

            public XB01_Strip(BinaryReader reader) : base(reader) { }

            public override void Read(BinaryReader reader)
            {
                long position = reader.BaseStream.Position;
                Offset = (uint)position;

                Type = (XB01EntryType)reader.ReadByte();
                Size = reader.ReadUInt16();
                reader.BaseStream.Seek(1, SeekOrigin.Current);
                Unknown = reader.ReadUInt32();
                VertCount = reader.ReadUInt32();

                for (int i = 0; i < VertCount; i++)
                {
                    VertIndices.Add(reader.ReadUInt16());
                }

                reader.BaseStream.Seek(position, SeekOrigin.Begin);
                Data = reader.ReadBytes((int)Size * 4);

                reader.BaseStream.Seek(position + Size * 4, SeekOrigin.Begin);
                //Print();
            }

            public float[] GetVertices(XB01 xb01)
            {
                List<float> vertices = new List<float>();
                foreach (uint index in VertIndices)
                {
                    if (index >= xb01.Vertices.Count) continue;
                    Vertex vert = xb01.Vertices[(int)index];
                    vertices.Add(vert.PosX);
                    vertices.Add(vert.PosY);
                    vertices.Add(vert.PosZ);
                    vertices.Add(vert.NormX);
                    vertices.Add(vert.NormY);
                    vertices.Add(vert.NormZ);
                    vertices.Add(vert.U);
                    vertices.Add(vert.V);
                }
                return vertices.ToArray();
            }
        }


        public class XB01_Zero : XB01Entry
        {
            public override uint Size { get; set; }
            public override XB01EntryType Type { get; set; }
            public override byte[] Data { get; set; }
            public override uint Offset { get; set; }

            public XB01_Zero(BinaryReader reader) : base(reader) { }

            public override void Read(BinaryReader reader)
            {
                Offset = (uint)reader.BaseStream.Position;
                Type = (XB01EntryType)reader.ReadByte();
                Size = reader.ReadByte();
                reader.BaseStream.Seek(-2, SeekOrigin.Current);
                Data = reader.ReadBytes(5 * 4);
            }
        }
    }
}
