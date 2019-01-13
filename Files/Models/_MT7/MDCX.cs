using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Models._MT7
{
    /// <summary>
    /// MDCX/MDC7 Node
    /// </summary>
    public class MDCX
    {
        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x4D, 0x44, 0x43, 0x58 }, //MDCX
            new byte[4] { 0x4D, 0x44, 0x43, 0x37 }  //MDC7
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

        public uint Token;
        public Matrix4x4 Matrix; //TODO: cleanup with OpenTK matrix4
        public uint EntryCount;
        List<NodeMDEntry> Entries = new List<NodeMDEntry>();

        public MDCX(BinaryReader reader)
        {
            Token = reader.ReadUInt32();
            Matrix = new Matrix4x4(reader);
            EntryCount = reader.ReadUInt32();
            for (int i = 0; i < EntryCount; i++)
            {
                Entries.Add(new NodeMDEntry(reader));
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Token);
            Matrix.Write(writer);
            writer.Write(EntryCount);
            foreach(NodeMDEntry entry in Entries)
            {
                entry.Write(writer);
            }
        }

        public class NodeMDEntry
        {
            public uint Offset1;
            public uint Offset2;

            public NodeMDEntry(BinaryReader reader)
            {
                Offset1 = reader.ReadUInt32();
                Offset2 = reader.ReadUInt32();
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(Offset1);
                writer.Write(Offset2);
            }
        }

        public class Matrix4x4
        {
            public float Matrix_0_0;
            public float Matrix_0_1;
            public float Matrix_0_2;
            public float Matrix_0_3;

            public float Matrix_1_0;
            public float Matrix_1_1;
            public float Matrix_1_2;
            public float Matrix_1_3;

            public float Matrix_2_0;
            public float Matrix_2_1;
            public float Matrix_2_2;
            public float Matrix_2_3;

            public float Matrix_3_0;
            public float Matrix_3_1;
            public float Matrix_3_2;
            public float Matrix_3_3;

            public Matrix4x4(BinaryReader reader)
            {
                Matrix_0_0 = reader.ReadSingle();
                Matrix_0_1 = reader.ReadSingle();
                Matrix_0_2 = reader.ReadSingle();
                Matrix_0_3 = reader.ReadSingle();

                Matrix_1_0 = reader.ReadSingle();
                Matrix_1_1 = reader.ReadSingle();
                Matrix_1_2 = reader.ReadSingle();
                Matrix_1_3 = reader.ReadSingle();

                Matrix_2_0 = reader.ReadSingle();
                Matrix_2_1 = reader.ReadSingle();
                Matrix_2_2 = reader.ReadSingle();
                Matrix_2_3 = reader.ReadSingle();

                Matrix_3_0 = reader.ReadSingle();
                Matrix_3_1 = reader.ReadSingle();
                Matrix_3_2 = reader.ReadSingle();
                Matrix_3_3 = reader.ReadSingle();
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(Matrix_0_0);
                writer.Write(Matrix_0_1);
                writer.Write(Matrix_0_2);
                writer.Write(Matrix_0_3);

                writer.Write(Matrix_1_0);
                writer.Write(Matrix_1_1);
                writer.Write(Matrix_1_2);
                writer.Write(Matrix_1_3);

                writer.Write(Matrix_2_0);
                writer.Write(Matrix_2_1);
                writer.Write(Matrix_2_2);
                writer.Write(Matrix_2_3);

                writer.Write(Matrix_3_0);
                writer.Write(Matrix_3_1);
                writer.Write(Matrix_3_2);
                writer.Write(Matrix_3_3);
            }

            public Matrix4 ToMatrix4()
            {
                Matrix4 mat = new Matrix4(Matrix_0_0, Matrix_0_1, Matrix_0_2, Matrix_0_3,
                                          Matrix_1_0, Matrix_1_1, Matrix_1_2, Matrix_1_3,
                                          Matrix_2_0, Matrix_2_1, Matrix_2_2, Matrix_2_3,
                                          Matrix_3_0, Matrix_3_1, Matrix_3_2, Matrix_3_3);
                return mat;
            }
        }
    }
}
