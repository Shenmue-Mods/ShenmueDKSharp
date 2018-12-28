using ShenmueDKSharp.Files.Images;
using ShenmueDKSharp.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Models
{
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

        public Vector3 Position;
        /// <summary>
        /// Rotation in degrees
        /// </summary>
        public Vector3 Rotation;
        public Vector3 Scale;

        public ModelNode Child;
        public ModelNode Sibling;
        public ModelNode Parent;
        public List<ModelNode> Children = new List<ModelNode>();

        public Vector3 Center;
        public float Radius;

        public uint VertexCount = 0;
        public List<Vertex> Vertices = new List<Vertex>();
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
        public List<ModelNode> GetAllNodes()
        {
            List<ModelNode> result = new List<ModelNode>();
            result.Add(this);
            if (Child != null)
            {
                result.AddRange(Child.GetAllNodes());
            }
            if (Sibling != null)
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
        public uint ID { get; set; }
        public byte[] NameData { get; set; }
        public string Name
        {
            get { return m_shiftJis.GetString(NameData); }
        }
        public Color4 TintColor { get; set; }
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
        public WrapMode Wrap { get; set; } = WrapMode.Clamp;
        public uint TextureIndex { get; set; } = 0;
        public Color4 StripColor { get; set; } = Color4.White;
        public Material Material { get; set; } = new Material();
        public PrimitiveType Type { get; set; }
        public ushort[] VertexIndices { get; set; }


        //MT5 stuff
        public List<Vector2> UVs = new List<Vector2>();
        public List<Color4> Colors = new List<Color4>();


        /// <summary>
        /// Returns the resolved vertex indices as vertices.
        /// </summary>
        /// <param name="node">The node that holds the vertices.</param>
        public Vertex[] GetVertexArray(ModelNode node)
        {
            return GetVertexArray(node.Vertices);
        }

        /// <summary>
        /// Returns the resolved vertex indices as vertices.
        /// </summary>
        /// <param name="vertices">Vertex list that holds the vertices.</param>
        public Vertex[] GetVertexArray(List<Vertex> vertices)
        {
            Vertex[] result = new Vertex[VertexIndices.Length];
            for (int i = 0; i < VertexIndices.Length; i++)
            {
                ushort index = VertexIndices[i];
                if (index >= vertices.Count)
                {
                    result[i] = null;
                    continue;
                }
                result[i] = vertices[VertexIndices[i]].Copy();

                if (UVs.Count > 0)
                {
                    result[i].U = UVs[i].X;
                    result[i].V = UVs[i].Y;
                    result[i].Format = Vertex.VertexFormat.VertexNormalUV;
                }

                if (Colors.Count > 0)
                {
                    result[i].R = Colors[i].R;
                    result[i].G = Colors[i].G;
                    result[i].B = Colors[i].B;
                    result[i].A = Colors[i].A;
                    result[i].Format = Vertex.VertexFormat.VertexNormalUVColor;
                }
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
            return GetFloatArray(node.Vertices, format);
        }

        /// <summary>
        /// Returns the resolved vertex indices as an float array.
        /// </summary>
        /// <param name="vertices">Vertex list that holds the vertices.</param>
        /// <returns></returns>
        public float[] GetFloatArray(List<Vertex> vertices, Vertex.VertexFormat format = Vertex.VertexFormat.Undefined)
        {
            Vertex[] verts = GetVertexArray(vertices);
            List<float> result = new List<float>();
            foreach(Vertex vert in verts)
            {
                if (vert == null) continue;
                result.AddRange(vert.GetData(format));
            }
            return result.ToArray();
        }

        public override string ToString()
        {
            return String.Format("Vertex Count: {0}", VertexIndices.Length);
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
        public Vector4 Color = new Vector4();
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
            get { return Color.X; }
            set { Color.X = value; }
        }

        public float G
        {
            get { return Color.Y; }
            set { Color.Y = value; }
        }

        public float B
        {
            get { return Color.Z; }
            set { Color.Z = value; }
        }

        public float A
        {
            get { return Color.W; }
            set { Color.W = value; }
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
            Color = new Vector4(vertex.Color);
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

        public Vertex(Vector3 pos, Vector3 norm, Vector4 color)
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
            Color = new Vector4(colR, colG, colB, colA);
        }

        public Vertex(Vector3 pos, Vector3 norm, Vector4 color, Vector2 uv)
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
            Color = new Vector4(colR, colG, colB, colA);
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
                result[index++] = Color.X;
                result[index++] = Color.Y;
                result[index++] = Color.Z;
                result[index++] = Color.W;
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
