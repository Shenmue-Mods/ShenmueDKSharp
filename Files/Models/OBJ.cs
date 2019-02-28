using ShenmueDKSharp.Files.Models._OBJ;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Models
{
    /// <summary>
    /// Wavefront OBJ format
    /// </summary>
    public class OBJ : BaseModel
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        private static CultureInfo m_cultureInfo = CultureInfo.InvariantCulture;

        public OBJ(BaseModel model)
        {
            model.CopyTo(this);
            FilePath = Path.ChangeExtension(model.FilePath, "obj");
        }
        public OBJ(string filepath)
        {
            Read(filepath);
        }
        public OBJ(Stream stream)
        {
            Read(stream);
        }
        public OBJ(BinaryReader reader)
        {
            Read(reader);
        }

        protected override void _Read(BinaryReader reader)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            Dictionary<int, Vertex> nodeVertices = new Dictionary<int, Vertex>();

            MeshFace currentFace = null;

            int currentTextureIndex = -1;

            MTL mtl = null;
            Textures.Clear();
            RootNode = new ModelNode();
            RootNode.HasMesh = true;

            bool hasUV = false;
            bool hasNormals = false;
            bool faceTextureChanged = false;

            string[] lines = reader.ReadToEnd().Split('\n');
            foreach(string line in lines)
            {
                if (line.StartsWith("#") || String.IsNullOrEmpty(line))
                {
                    continue;
                }
                else if (line.StartsWith("mtllib "))
                {
                    string[] values = line.Split(' ');
                    if (values.Length < 2) continue;

                    string mtlPath = "";
                    string dir = Path.GetDirectoryName(FilePath);
                    mtlPath = String.Format("{0}\\{1}", Path.GetDirectoryName(FilePath), values[1]);

                    if (File.Exists(mtlPath))
                    {
                        mtl = new MTL(mtlPath);
                        Textures = mtl.Textures;
                    }
                }
                else if (line.StartsWith("v "))
                {
                    string[] values = line.Split(' ');
                    if (values.Length < 4) continue;
                    Vector3 vertex = new Vector3();
                    vertex.X = float.Parse(values[1], m_cultureInfo);
                    vertex.Y = float.Parse(values[2], m_cultureInfo);
                    vertex.Z = float.Parse(values[3], m_cultureInfo);
                    RootNode.VertexPositions.Add(vertex);
                }
                else if (line.StartsWith("vt "))
                {
                    string[] values = line.Split(' ');
                    if (values.Length < 3) continue;
                    Vector2 uv = new Vector2();
                    uv.X = float.Parse(values[1], m_cultureInfo);
                    uv.Y = float.Parse(values[2], m_cultureInfo);
                    RootNode.VertexUVs.Add(uv);
                }
                else if (line.StartsWith("vn "))
                {
                    string[] values = line.Split(' ');
                    if (values.Length < 4) continue;
                    Vector3 normal = new Vector3();
                    normal.X = float.Parse(values[1], m_cultureInfo);
                    normal.Y = float.Parse(values[2], m_cultureInfo);
                    normal.Z = float.Parse(values[3], m_cultureInfo);
                    RootNode.VertexNormals.Add(normal);
                }
                else if (line.StartsWith("f "))
                {
                    if (currentFace == null) continue;

                    if (line.Contains("//"))
                    {
                        string[] stringSeparators = new string[] { "//" };

                        string[] values = line.Split(' ');
                        if (values.Length < 4) continue;

                        string[] vert1Values = values[1].Split(stringSeparators, StringSplitOptions.None);
                        string[] vert2Values = values[2].Split(stringSeparators, StringSplitOptions.None);
                        string[] vert3Values = values[3].Split(stringSeparators, StringSplitOptions.None);

                        currentFace.PositionIndices.Add((ushort)(int.Parse(vert1Values[0]) - 1));
                        currentFace.NormalIndices.Add((ushort)(int.Parse(vert1Values[1]) - 1));
                        currentFace.PositionIndices.Add((ushort)(int.Parse(vert2Values[0]) - 1));
                        currentFace.NormalIndices.Add((ushort)(int.Parse(vert2Values[1]) - 1));
                        currentFace.PositionIndices.Add((ushort)(int.Parse(vert3Values[0]) - 1));
                        currentFace.NormalIndices.Add((ushort)(int.Parse(vert3Values[1]) - 1));
                    }
                    else if(line.Contains("/"))
                    {
                        string[] stringSeparators = new string[] { "/" };

                        string[] values = line.Split(' ');
                        if (values.Length < 4) continue;

                        string[] vert1Values = values[1].Split(stringSeparators, StringSplitOptions.None);
                        string[] vert2Values = values[2].Split(stringSeparators, StringSplitOptions.None);
                        string[] vert3Values = values[3].Split(stringSeparators, StringSplitOptions.None);

                        currentFace.PositionIndices.Add((ushort)(int.Parse(vert1Values[0]) - 1));
                        currentFace.UVIndices.Add((ushort)(int.Parse(vert1Values[1]) - 1));
                        if (vert1Values.Length > 2) {
                            currentFace.NormalIndices.Add((ushort)(int.Parse(vert1Values[2]) - 1));
                        }

                        currentFace.PositionIndices.Add((ushort)(int.Parse(vert2Values[0]) - 1));
                        currentFace.UVIndices.Add((ushort)(int.Parse(vert2Values[1]) - 1));
                        if (vert2Values.Length > 2) {
                            currentFace.NormalIndices.Add((ushort)(int.Parse(vert2Values[2]) - 1));
                        }

                        currentFace.PositionIndices.Add((ushort)(int.Parse(vert3Values[0]) - 1));
                        currentFace.UVIndices.Add((ushort)(int.Parse(vert3Values[1]) - 1));
                        if (vert3Values.Length > 2) {
                            currentFace.NormalIndices.Add((ushort)(int.Parse(vert3Values[2]) - 1));
                        }
                    }
                    else
                    {
                        string[] values = line.Split(' ');
                        if (values.Length < 4) continue;

                        currentFace.PositionIndices.Add((ushort)(int.Parse(values[0]) - 1));
                        currentFace.PositionIndices.Add((ushort)(int.Parse(values[1]) - 1));
                        currentFace.PositionIndices.Add((ushort)(int.Parse(values[2]) - 1));
                    }
                }
                else if (line.StartsWith("usemtl "))
                {
                    if (mtl == null) continue;
                    string[] values = line.Split(' ');
                    if (values.Length < 2) continue;

                    if (faceTextureChanged)
                    {
                        RootNode.Faces.Add(currentFace);
                    }
                    currentFace = new MeshFace();
                    currentTextureIndex = mtl.GetMaterialTextureIndex(values[1]);
                    currentFace.TextureIndex = (uint)currentTextureIndex;
                    faceTextureChanged = true;
                }
                else if (line.StartsWith("o ") || line.StartsWith("g "))
                {
                    if (currentFace != null)
                    {
                        RootNode.Faces.Add(currentFace);
                    }
                    currentFace = new MeshFace();
                    faceTextureChanged = false;
                }
            }
            RootNode.Faces.Add(currentFace);
            RootNode.ResolveFaceTextures(Textures);
        }

        protected override void _Write(BinaryWriter writer)
        {
            int objNum = 0;

            int totalPositions = 0;
            int totalNormals = 0;
            int totalUVs = 0;

            MTL mtl = new MTL(Textures);
            if (String.IsNullOrEmpty(FilePath))
            {
                //TODO: Make this somehow better
                throw new ArgumentException("Filepath was not given.");
            }

            string mtlPath = "";
            string dir = Path.GetDirectoryName(FilePath);
            if (String.IsNullOrEmpty(dir) || dir == "\\" || Path.GetExtension(FilePath) == ".OBJ" || Path.GetExtension(FilePath) == ".obj")
            {
                mtlPath = Path.ChangeExtension(FilePath, ".mtl");
            }
            else
            {
                mtlPath = String.Format("{0}\\{1}", Path.GetDirectoryName(FilePath), Path.ChangeExtension(FilePath, ".mtl"));
            }
            mtl.Write(mtlPath);

            writer.WriteASCII("# OBJ Generated by ShenmueDKSharp\n");

            writer.WriteASCII(String.Format("mtllib {0}\n", Path.GetFileName(mtlPath)));

            StringBuilder sbVertices = new StringBuilder();
            StringBuilder sbNormals = new StringBuilder();
            StringBuilder sbUVs = new StringBuilder();

            StringBuilder sbMeshes = new StringBuilder();

            foreach (ModelNode node in RootNode.GetAllNodes())
            {
                Matrix4 transformMatrix = node.GetTransformMatrix();

                if (node.Faces.Count == 0) continue;

                Vector3[] positions = node.VertexPositions.ToArray();
                Vector3[] normals = node.VertexNormals.ToArray();
                Vector2[] uvs = node.VertexUVs.ToArray();
                if (positions.Length == 0) continue;

                sbMeshes.Append(String.Format("o obj_{0}\n", objNum));
           
                for (int i = 0; i < positions.Length; i++)
                {
                    positions[i] = Vector3.TransformPosition(positions[i], transformMatrix);
                    sbVertices.Append(String.Format(m_cultureInfo, "v {0:F6} {1:F6} {2:F6}\n", positions[i].X, positions[i].Y, positions[i].Z));
                }

                if (uvs.Length > 0)
                {
                    for (int i = 0; i < uvs.Length; i++)
                    {
                        sbUVs.Append(String.Format(m_cultureInfo, "vt {0:F6} {1:F6}\n", uvs[i].X, uvs[i].Y));
                    }
                }

                if (normals.Length > 0)
                {
                    for (int i = 0; i < normals.Length; i++)
                    {
                        //normals[i] = Vector3.TransformPosition(normals[i], transformMatrix);
                        sbNormals.Append(String.Format(m_cultureInfo, "vn {0:F6} {1:F6} {2:F6}\n", normals[i].X, normals[i].Y, normals[i].Z));
                    }
                }

                foreach (MeshFace face in node.Faces)
                {
                    uint textureIndex = face.TextureIndex;
                    Texture texture = face.Material.Texture;

                    sbMeshes.Append(String.Format("usemtl mat_{0}\n", textureIndex));
                    sbMeshes.Append("s 1\n");

                    bool hasUV = face.UVIndices.Count > 0;
                    bool hasNormal = face.NormalIndices.Count > 0;

                    if (face.Type == MeshFace.PrimitiveType.Triangles)
                    {
                        for (int i = 0; i < face.PositionIndices.Count - 2; i += 3)
                        {
                            int posIndex1 = face.PositionIndices[i] + totalPositions + 1;
                            int posIndex2 = face.PositionIndices[i + 1] + totalPositions + 1;
                            int posIndex3 = face.PositionIndices[i + 2] + totalPositions + 1;
                            if (hasNormal)
                            {
                                int normIndex1 = face.NormalIndices[i] + totalNormals + 1;
                                int normIndex2 = face.NormalIndices[i + 1] + totalNormals + 1;
                                int normIndex3 = face.NormalIndices[i + 2] + totalNormals + 1;
                                if (hasUV)
                                {
                                    int uvIndex1 = face.UVIndices[i] + totalUVs + 1;
                                    int uvIndex2 = face.UVIndices[i + 1] + totalUVs + 1;
                                    int uvIndex3 = face.UVIndices[i + 2] + totalUVs + 1;
                                    sbMeshes.Append(String.Format(GetVertexFormatString(hasUV, hasNormal), posIndex1, posIndex2, posIndex3,
                                                                                                           uvIndex1, uvIndex2, uvIndex3,
                                                                                                           normIndex1, normIndex2, normIndex3));
                                }
                                else
                                {
                                    sbMeshes.Append(String.Format(GetVertexFormatString(hasUV, hasNormal), posIndex1, posIndex2, posIndex3,
                                                                                                           normIndex1, normIndex2, normIndex3));
                                }
                            }
                            else
                            {
                                sbMeshes.Append(String.Format(GetVertexFormatString(hasUV, hasNormal), posIndex1, posIndex2, posIndex3));
                            }
                        }
                    }
                    else if (face.Type == MeshFace.PrimitiveType.TriangleStrip)
                    {
                        for (int i = 0; i < face.PositionIndices.Count - 2; i++)
                        {
                            int posIndex1 = face.PositionIndices[i] + totalPositions + 1;
                            int posIndex2 = face.PositionIndices[i + 1] + totalPositions + 1;
                            int posIndex3 = face.PositionIndices[i + 2] + totalPositions + 1;
                            if (hasNormal)
                            {
                                int normIndex1 = face.NormalIndices[i] + totalNormals + 1;
                                int normIndex2 = face.NormalIndices[i + 1] + totalNormals + 1;
                                int normIndex3 = face.NormalIndices[i + 2] + totalNormals + 1;
                                if (hasUV)
                                {
                                    int uvIndex1 = face.UVIndices[i] + totalUVs + 1;
                                    int uvIndex2 = face.UVIndices[i + 1] + totalUVs + 1;
                                    int uvIndex3 = face.UVIndices[i + 2] + totalUVs + 1;
                                    if ((i & 1) == 1)
                                    {
                                        sbMeshes.Append(String.Format(GetVertexFormatString(hasUV, hasNormal), posIndex1, posIndex2, posIndex3,
                                                                                                            uvIndex1, uvIndex2, uvIndex3,
                                                                                                            normIndex1, normIndex2, normIndex3));
                                    }
                                    else
                                    {
                                        sbMeshes.Append(String.Format(GetVertexFormatString(hasUV, hasNormal), posIndex1, posIndex3, posIndex2,
                                                                                                            uvIndex1, uvIndex3, uvIndex2,
                                                                                                            normIndex1, normIndex3, normIndex2));
                                    }
                                }
                                else
                                {
                                    if ((i & 1) == 1)
                                    {
                                        sbMeshes.Append(String.Format(GetVertexFormatString(hasUV, hasNormal), posIndex1, posIndex2, posIndex3,
                                                                                                           normIndex1, normIndex2, normIndex3));
                                    }
                                    else
                                    {
                                        sbMeshes.Append(String.Format(GetVertexFormatString(hasUV, hasNormal), posIndex1, posIndex3, posIndex2,
                                                                                                           normIndex1, normIndex3, normIndex2));
                                    }
                                }
                            }
                            else
                            {
                                if ((i & 1) == 1)
                                {
                                    sbMeshes.Append(String.Format(GetVertexFormatString(hasUV, hasNormal), posIndex1, posIndex2, posIndex3));
                                }
                                else
                                {
                                    sbMeshes.Append(String.Format(GetVertexFormatString(hasUV, hasNormal), posIndex1, posIndex3, posIndex2));
                                }
                            }
                        }
                    }
                }
                objNum++;
                totalPositions += positions.Length;
                totalNormals += normals.Length;
                totalUVs += uvs.Length;
            }

            writer.WriteASCII(sbVertices.ToString());
            writer.WriteASCII(sbUVs.ToString());
            writer.WriteASCII(sbNormals.ToString());
            writer.WriteASCII(sbMeshes.ToString());
            writer.WriteASCII(String.Format("# Total Positions: {0}\n", totalPositions));
            writer.WriteASCII(String.Format("# Total Normals: {0}\n", totalNormals));
            writer.WriteASCII(String.Format("# Total UVs: {0}\n", totalUVs));
        }

        private string GetVertexFormatString(bool hasUV, bool hasNormal)
        {
            if (hasNormal)
            {
                if (hasUV)
                {
                    return "f {0}/{3}/{6} {1}/{4}/{7} {2}/{5}/{8}\n";
                }
                else
                {
                    return "f {0}//{3} {1}//{4} {2}//{5}\n";
                }
            }
            else
            {
                return "f {0} {1} {2}\n";
            }
        }
    }
}
