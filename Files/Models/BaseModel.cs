using ShenmueDKSharp.Files.Images;
using ShenmueDKSharp.Graphics;
using ShenmueDKSharp.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Models
{
    public enum BoneID
    {
        Spine = 1,
        Hips = 14,
        RightUpperLeg = 16,
        RightLowerLeg = 17,
        RightFoot = 18,
        LeftUpperLeg = 21,
        LeftLowerLeg = 22,
        LeftFoot = 23,
        RightShoulder = 4,
        RightUpperArm = 5,
        RightLowerArm = 6,
        RightWrist = 7,
        RightHand = 191,
        LeftShoulder = 9,
        LeftUpperArm = 10,
        LeftLowerArm = 11,
        LeftWrist = 12,
        LeftHand = 190,
        Head = 189,
        Jaw = 188
    }

    /// <summary>
    /// Base model class for MT5 and MT7
    /// </summary>
    public abstract class BaseModel : BaseFile
    {
        public List<Texture> Textures { get; set; } = new List<Texture>();
        public ModelNode RootNode { get; set; }

        public List<ModelNode> GetAllNodes()
        {
            return RootNode.GetAllNodes();
        }

        /// <summary>
        /// Populates the Children property of the nodes.
        /// </summary>
        /// <returns></returns>
        public ModelNode GenerateNodeTree()
        {
            RootNode.ClearTree();
            RootNode.GenerateTree(null);
            return RootNode;
        }

        public void CopyTo(BaseModel model)
        {
            //BaseModel copy = this.Copy();
            model.Textures = Textures;
            model.RootNode = RootNode;
        }

    }

    /// <summary>
    /// Base model node class for MT5 and MT7 nodes
    /// </summary>
    public class ModelNode
    {
        public bool HasMesh = false;
        public string ModelName = "";
        public string CHRTID = "";
        public string CHRTIMAGE = "";

        public Vector3 Position = Vector3.Zero;
        /// <summary>
        /// Rotation in degrees
        /// </summary>
        public Vector3 Rotation = Vector3.Zero;
        public Vector3 Scale = Vector3.One;

        public ModelNode Child = null;
        public ModelNode Sibling = null;
        public ModelNode Parent = null;
        public List<ModelNode> Children = new List<ModelNode>();

        public uint ID;
        public BoneID BoneID
        {
            get
            {
                return (BoneID)(ID & 0xFF);
            }
        }

        public Vector3 Center = Vector3.Zero;
        public float Radius = 0.0f;

        public int VertexCount
        {
            get
            {
                return VertexPositions.Count;
            }
        }

        public List<Vector3> VertexPositions = new List<Vector3>();
        public List<Vector3> VertexNormals = new List<Vector3>();
        public List<Vector2> VertexUVs = new List<Vector2>();
        public List<Color4> VertexColors = new List<Color4>();

        public List<MeshFace> Faces = new List<MeshFace>();

        /// <summary>
        /// Returns an transform matrix with the position, rotation and scale recursively including the parents.
        /// </summary>
        public Matrix4 GetTransformMatrix()
        {
            Matrix4 matrix = Matrix4.Identity;
            if (Parent != null)
            {
                matrix = Parent.GetTransformMatrix();
            }
            return GetTransformMatrixSelf() * matrix;
        }

        /// <summary>
        /// Returns the transform matrix with the position, rotation and scale.
        /// </summary>
        public Matrix4 GetTransformMatrixSelf()
        {
            //Need to do an quaternion for each axis because Quaternion.FromEulerAngles returns an wrong order of rotation.
            Quaternion quatX = Quaternion.FromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(Rotation.X));
            Quaternion quatY = Quaternion.FromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(Rotation.Y));
            Quaternion quatZ = Quaternion.FromAxisAngle(Vector3.UnitZ, MathHelper.DegreesToRadians(Rotation.Z));
            Matrix4 rotationMatrixX = Matrix4.CreateFromQuaternion(quatX);
            Matrix4 rotationMatrixY = Matrix4.CreateFromQuaternion(quatY);
            Matrix4 rotationMatrixZ = Matrix4.CreateFromQuaternion(quatZ);
            Matrix4 scaleMatrix = Matrix4.CreateScale(Scale.X, Scale.Y, Scale.Z);
            Matrix4 translateMatrix = Matrix4.CreateTranslation(Position.X, Position.Y, Position.Z);
            return scaleMatrix * rotationMatrixX * rotationMatrixY * rotationMatrixZ * translateMatrix;
        }

        /// <summary>
        /// Returns all the child/sibling nodes relative to this node.
        /// </summary>
        /// <returns></returns>
        public List<ModelNode> GetAllNodes(bool includeSibling = true, bool includeChildren = true)
        {
            List<ModelNode> result = new List<ModelNode>();
            result.Add(this);
            if (Child != null && includeChildren)
            {
                result.AddRange(Child.GetAllNodes());
            }
            if (Sibling != null && includeSibling)
            {
                result.AddRange(Sibling.GetAllNodes());
            }
            return result;
        }

        /// <summary>
        /// Returns all the child nodes relative to this node.
        /// </summary>
        /// <returns></returns>
        public List<ModelNode> GetChildNodes()
        {
            List<ModelNode> result = new List<ModelNode>();
            if (Child != null)
            {
                result.Add(Child);
                result.AddRange(Child.GetAllNodes());
            }
            return result;
        }

        public void GenerateTree(ModelNode parent)
        {
            if (parent != null)
            {
                parent.Children.Add(this);
            }
            if (Child != null)
            {
                Child.GenerateTree(this);
            }
            if (Sibling != null)
            {
                Sibling.GenerateTree(parent);
            }
        }

        public void ClearTree()
        {
            if (Child != null)
            {
                Child.ClearTree();
            }
            if (Sibling != null)
            {
                Sibling.ClearTree();
            }
            Children.Clear();
        }

        public void ResolveFaceTextures(List<Texture> entries)
        {
            if (entries == null) return;
            foreach(MeshFace face in Faces)
            {
                if (face.TextureIndex < entries.Count)
                {
                    face.Material.Texture = entries[(int)face.TextureIndex];
                }
            }
            if (Child != null)
            {
                Child.ResolveFaceTextures(entries);
            }
            if (Sibling != null)
            {
                Sibling.ResolveFaceTextures(entries);
            }
        }
    }

    public class Material
    {
        public Texture Texture { get; set; }
    }

    public class Texture
    {
        private readonly static Encoding m_shiftJis = Encoding.GetEncoding("shift_jis");

        public BaseImage Image { get; set; }
        public TextureID TextureID { get; set; } = new TextureID();
        public Color4 TintColor { get; set; } = Color4.White;

        public override string ToString()
        {
            return TextureID.Name;
        }
    }

    public class MeshFace
    {
        public enum PrimitiveType
        {
            Triangles,
            TriangleStrip
        }

        /// <summary>
        /// Texture wrap modes (OpenGL enum)
        /// </summary>
        public enum WrapMode
        {
            Clamp = 0x812D,
            Repeat = 0x2901,
            MirroredRepeat = 0x8370
        }

        public bool Unlit { get; set; } = false;
        public bool Transparent { get; set; } = false;
        public bool MirrorUVs { get; set; } = false;
        public WrapMode Wrap { get; set; } = WrapMode.MirroredRepeat;
        public uint TextureIndex { get; set; } = 0;
        public Color4 StripColor { get; set; } = Color4.White;
        public Material Material { get; set; } = new Material();
        public PrimitiveType Type { get; set; }

        public List<ushort> PositionIndices { get; set; } = new List<ushort>();
        public List<ushort> NormalIndices { get; set; } = new List<ushort>();
        public List<ushort> UVIndices { get; set; } = new List<ushort>();
        public List<ushort> ColorIndices { get; set; } = new List<ushort>();


        public bool HasUVs
        {
            get { return UVIndices.Count != 0; }
        }

        public bool HasColors
        {
            get { return ColorIndices.Count != 0; }
        }

        /// <summary>
        /// Returns the resolved vertex indices as vertices.
        /// </summary>
        /// <param name="node">The node that holds the vertices.</param>
        public Vertex[] GetVertexArray(ModelNode node)
        {
            int vertexCount = PositionIndices.Count;
            Vertex[] result = new Vertex[vertexCount];

            bool hasNormal = NormalIndices.Count != 0;
            bool hasUV = UVIndices.Count != 0;
            bool hasColor = ColorIndices.Count != 0;

            for (int i = 0; i < vertexCount; i++)
            {
                int posIndex = PositionIndices[i];
                int normIndex = hasNormal ? NormalIndices[i] : -1;
                int uvIndex = hasUV ? UVIndices[i] : -1;
                int colorIndex = hasColor ? ColorIndices[i] : -1;
                Vector3 pos = new Vector3();
                Vector3 norm = new Vector3();
                Vector2 uv = new Vector2();
                Color4 color = new Color4();

                if (posIndex < node.VertexPositions.Count && posIndex >= 0)
                {
                    pos = node.VertexPositions[posIndex];
                }
                if (posIndex < node.VertexPositions.Count && normIndex >= 0)
                {
                    norm = node.VertexNormals[normIndex];
                }
                if (uvIndex < node.VertexUVs.Count && uvIndex >= 0)
                {
                    uv = node.VertexUVs[uvIndex];
                }
                if (colorIndex < node.VertexColors.Count && colorIndex >= 0)
                {
                    color = node.VertexColors[colorIndex];
                }
                result[i] = new Vertex(pos, norm, color, uv);
            }
            return result;
        }

        /// <summary>
        /// Returns the resolved vertex indices as an float array.
        /// </summary>
        /// <param name="node">The node that holds the vertices.</param>
        /// <returns></returns>
        public float[] GetFloatArray(ModelNode node, Vertex.VertexFormat format = Vertex.VertexFormat.Undefined)
        {
            return GetFloatArray(GetVertexArray(node).ToList(), format);
        }

        /// <summary>
        /// Returns the resolved vertex indices as an float array.
        /// </summary>
        /// <param name="vertices">Vertex list that holds the vertices.</param>
        /// <returns></returns>
        public float[] GetFloatArray(List<Vertex> vertices, Vertex.VertexFormat format = Vertex.VertexFormat.Undefined)
        {
            List<float> result = new List<float>();
            foreach(Vertex vert in vertices)
            {
                if (vert == null) continue;
                result.AddRange(vert.GetData(format));
            }
            return result.ToArray();
        }

        public override string ToString()
        {
            return String.Format("Vertex Count: {0}", PositionIndices.Count);
        }
    }

    /// <summary>
    /// Generic vertex
    /// </summary>
    public class Vertex
    {
        private enum DataFlags
        {
            Vertex = 1,
            Normal = 2,
            UV = 4,
            Color = 8
        }

        public enum VertexFormat
        {
            Undefined = 0,
            Vertex = DataFlags.Vertex,
            VertexNormal = DataFlags.Vertex | DataFlags.Normal,
            VertexNormalUV = DataFlags.Vertex | DataFlags.Normal | DataFlags.UV,
            VertexNormalUVColor = DataFlags.Vertex | DataFlags.Normal | DataFlags.UV | DataFlags.Color,
            VertexNormalColor = DataFlags.Vertex | DataFlags.Normal | DataFlags.Color
        }

        public VertexFormat Format;

        /// <summary>
        /// Vertex position
        /// </summary>
        public Vector3 Position = new Vector3();
        /// <summary>
        /// Vertex normal
        /// </summary>
        public Vector3 Normal = new Vector3();
        /// <summary>
        /// Vertex color
        /// </summary>
        public Color4 Color = new Color4();
        /// <summary>
        /// Vertex texture coordinates (UV)
        /// </summary>
        public Vector2 TexCoord = new Vector2();

        /// <summary>
        /// Byte size of the vertex
        /// </summary>
        public uint Stride
        {
            get
            {
                return GetStride(Format);
            }
        }


        public float PosX
        {
            get { return Position.X; }
            set { Position.X = value; }
        }

        public float PosY
        {
            get { return Position.Y; }
            set { Position.Y = value; }
        }

        public float PosZ
        {
            get { return Position.Z; }
            set { Position.Z = value; }
        }


        public float NormX
        {
            get { return Normal.X; }
            set { Normal.X = value; }
        }

        public float NormY
        {
            get { return Normal.Y; }
            set { Normal.Y = value; }
        }

        public float NormZ
        {
            get { return Normal.Z; }
            set { Normal.Z = value; }
        }
        

        public float R
        {
            get { return Color.R; }
            set { Color.R = value; }
        }

        public float G
        {
            get { return Color.G; }
            set { Color.G = value; }
        }

        public float B
        {
            get { return Color.B; }
            set { Color.B = value; }
        }

        public float A
        {
            get { return Color.A; }
            set { Color.A = value; }
        }


        public float U
        {
            get { return TexCoord.X; }
            set { TexCoord.X = value; }
        }

        public float V
        {
            get { return TexCoord.Y; }
            set { TexCoord.Y = value; }
        }

        public Vertex(Vertex vertex)
        {
            Position = new Vector3(vertex.Position);
            Normal = new Vector3(vertex.Normal);
            Color = new Color4(vertex.Color);
            TexCoord = new Vector2(vertex.TexCoord.X, vertex.TexCoord.Y);
            Format = vertex.Format;
        }

        public Vertex(VertexFormat format)
        {
            Format = format;
        }

        public Vertex(Vector3 pos)
        {
            Format = VertexFormat.Vertex;
            Position = pos;
        }

        public Vertex(float posX, float posY, float posZ)
        {
            Format = VertexFormat.Vertex;
            Position = new Vector3(posX, posY, posZ);
        }

        public Vertex(Vector3 pos, Vector3 norm)
        {
            Format = VertexFormat.VertexNormal;
            Position = pos;
            Normal = norm;
        }

        public Vertex(float posX, float posY, float posZ,
                      float normX, float normY, float normZ)
        {
            Format = VertexFormat.VertexNormal;
            Position = new Vector3(posX, posY, posZ);
            Normal = new Vector3(normX, normY, normZ);
        }

        public Vertex(Vector3 pos, Vector3 norm, Vector2 uv)
        {
            Format = VertexFormat.VertexNormalUV;
            Position = pos;
            Normal = norm;
            TexCoord = uv;
        }

        public Vertex(float posX, float posY, float posZ,
                      float normX, float normY, float normZ,
                      float u, float v)
        {
            Format = VertexFormat.VertexNormalUV;
            Position = new Vector3(posX, posY, posZ);
            Normal = new Vector3(normX, normY, normZ);
            TexCoord = new Vector2(u, v);
        }

        public Vertex(Vector3 pos, Vector3 norm, Color4 color)
        {
            Format = VertexFormat.VertexNormalColor;
            Position = pos;
            Normal = norm;
            Color = color;
        }

        public Vertex(float posX, float posY, float posZ,
                      float normX, float normY, float normZ,
                      float colR, float colG, float colB, float colA)
        {
            Format = VertexFormat.VertexNormalColor;
            Position = new Vector3(posX, posY, posZ);
            Normal = new Vector3(normX, normY, normZ);
            Color = new Color4(colR, colG, colB, colA);
        }

        public Vertex(Vector3 pos, Vector3 norm, Color4 color, Vector2 uv)
        {
            Format = VertexFormat.VertexNormalUVColor;
            Position = pos;
            Normal = norm;
            Color = color;
            TexCoord = uv;
        }

        public Vertex(float posX, float posY, float posZ,
                      float normX, float normY, float normZ,
                      float colR, float colG, float colB, float colA,
                      float u, float v)
        {
            Format = VertexFormat.VertexNormalUVColor;
            Position = new Vector3(posX, posY, posZ);
            Normal = new Vector3(normX, normY, normZ);
            Color = new Color4(colR, colG, colB, colA);
            TexCoord = new Vector2(u, v);
        }

        /// <summary>
        /// Returns the vertex as an float array according to the data format.
        /// </summary>
        /// <returns></returns>
        public float[] GetData(VertexFormat format = VertexFormat.Undefined)
        {
            if (format == VertexFormat.Undefined)
            {
                format = Format;
            }

            float[] result = new float[GetStride(format) / 4];
            int index = 0;
            if (((DataFlags)format & DataFlags.Vertex) != 0)
            {
                result[index++] = Position.X;
                result[index++] = Position.Y;
                result[index++] = Position.Z;
            }
            if (((DataFlags)format & DataFlags.Normal) != 0)
            {
                result[index++] = Normal.X;
                result[index++] = Normal.Y;
                result[index++] = Normal.Z;
            }
            if (((DataFlags)format & DataFlags.UV) != 0)
            {
                result[index++] = TexCoord.X;
                result[index++] = TexCoord.Y;
            }
            if (((DataFlags)format & DataFlags.Color) != 0)
            {
                result[index++] = Color.R;
                result[index++] = Color.G;
                result[index++] = Color.B;
                result[index++] = Color.A;
            }
            return result;
        }

        public bool HasVertex()
        {
            return ((DataFlags)Format & DataFlags.Vertex) != 0;
        }

        public bool HasNormal()
        {
            return ((DataFlags)Format & DataFlags.Normal) != 0;
        }

        public bool HasUV()
        {
            return ((DataFlags)Format & DataFlags.UV) != 0;
        }

        public bool HasColor()
        {
            return ((DataFlags)Format & DataFlags.Color) != 0;
        }

        public static VertexFormat GetFormat(uint stride)
        {
            switch (stride)
            {
                case 12:
                    return VertexFormat.Vertex;
                case 24:
                    return VertexFormat.VertexNormal;
                case 32:
                    return VertexFormat.VertexNormalUV;
                case 40:
                    return VertexFormat.VertexNormalColor;
                case 48:
                    return VertexFormat.VertexNormalUVColor;
                default:
                    return VertexFormat.VertexNormalUV;
            }
        }

        public static uint GetStride(VertexFormat format)
        {
            uint stride = 0;
            if (((DataFlags)format & DataFlags.Vertex) != 0)
            {
                stride += 12; //3 (float count) * 4 (float size)
            }
            if (((DataFlags)format & DataFlags.Normal) != 0)
            {
                stride += 12; //3 (float count) * 4 (float size)
            }
            if (((DataFlags)format & DataFlags.Color) != 0)
            {
                stride += 16; //4 (float count) * 4 (float size)
            }
            if (((DataFlags)format & DataFlags.UV) != 0)
            {
                stride += 8; //2 (float count) * 4 (float size)
            }
            return stride;
        }
    }
}
