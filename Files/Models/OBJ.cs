using System;
using System.Collections.Generic;
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
        public OBJ(BaseModel model)
        {
            model.CopyTo(this);
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
            throw new NotImplementedException();
        }

        public void Write(BinaryWriter writer)
        {
            foreach (ModelNode node in RootNode.GetAllNodes())
            {
                Matrix4 transformMatrix = node.GetTransformMatrix();
                foreach (MeshFace face in node.Faces)
                {
                    //Get resolved vertices (for mt5 so the UV is inside the vertices array)
                    Vertex[] vertices = face.GetVertexArray(node);

                    //Apply transform matrices to all vertices
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i].Position = Vector3.TransformPosition(vertices[i].Position, transformMatrix);
                        //Normal should not be transformed, but just in case if i'm wrong
                        //vertices[i].Normal = Vector3.TransformPosition(vertices[i].Normal, transformMatrix);
                    }

                    //Write all the vertices
                    if (vertices[0].HasVertex())
                    {
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            Console.WriteLine("v {0} {1} {2}", vertices[i].PosX, vertices[i].PosY, vertices[i].PosZ);
                        }
                    }

                    //Write all the normals
                    if (vertices[0].HasNormal())
                    {
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            Console.WriteLine("vn {0} {1} {2}", vertices[i].NormX, vertices[i].NormY, vertices[i].NormZ);
                        }
                    }

                    //Write all the uvs
                    if (vertices[0].HasUV())
                    {
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            Console.WriteLine("vt {0} {1}", vertices[i].U, vertices[i].V);
                        }
                    }

                    //Write all the face vertex indices
                    if (face.Type == MeshFace.PrimitiveType.Triangles)
                    {
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            Console.WriteLine(GetVertexFormatString(vertices[i]), i, i + 1, i + 2);
                        }
                    }
                    else if (face.Type == MeshFace.PrimitiveType.TriangleStrip)
                    {
                        for (int i = 0; i < face.VertexIndices.Length - 2; i++)
                        {
                            //triangle strip to triangles conversion (i think this is correct)
                            if ((i & 1) != 1)
                            {
                                Console.WriteLine(GetVertexFormatString(vertices[i]), i, i + 1, i + 2);
                            }
                            else
                            {
                                Console.WriteLine(GetVertexFormatString(vertices[i]), i, i + 2, i + 1);
                            }
                        }
                    }
                }
            }
            throw new NotImplementedException();
        }

        private string GetVertexFormatString(Vertex vertex)
        {
            if (vertex.HasVertex())
            {
                if (vertex.HasNormal())
                {
                    if (vertex.HasUV())
                    {
                        return "{0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}";
                    }
                    else
                    {
                        return "{0}//{0} {1}//{1} {2}//{2}";
                    }
                }
                else
                {
                    return "{0} {1} {2}";
                }
            }
            return "";
        }
    }
}
