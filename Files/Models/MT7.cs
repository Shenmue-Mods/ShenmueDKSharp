using ShenmueDKSharp.Files.Images;
using ShenmueDKSharp.Files.Models._MT7;
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
    /// MT7 model.
    /// MDCX/7, MDPX/7, MDOX/7 and MDLX/7
    /// </summary>
    public class MT7 : BaseModel
    {
        private static Encoding m_shiftJis = Encoding.GetEncoding("shift_jis");

        public readonly static List<string> Extensions = new List<string>()
        {
            "MT7",
            "MAPM",
            "PROP",
            "CHRM"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x4D, 0x44, 0x43, 0x58 }, //MDCX
            new byte[4] { 0x4D, 0x44, 0x43, 0x37 }, //MDC7
            new byte[4] { 0x4D, 0x44, 0x50, 0x58 }, //MDPX
            new byte[4] { 0x4D, 0x44, 0x50, 0x37 }, //MDP7
            new byte[4] { 0x4D, 0x44, 0x4F, 0x58 }, //MDOX
            new byte[4] { 0x4D, 0x44, 0x4F, 0x37 }, //MDO7
            new byte[4] { 0x4D, 0x44, 0x4C, 0x58 }, //MDLX
            new byte[4] { 0x4D, 0x44, 0x4C, 0x37 }  //MDL7
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
        public uint Size;
        public uint FirstNodeOffset;
        public uint TextureCount;

        //Optional
        public List<TextureEntry> TextureEntries = new List<TextureEntry>();
        public uint NodeOffsetTableSize;
        public List<uint> NodeOffsetTable = new List<uint>();
        public TXT7 TXT7;
        public CLSG CLSG;
        public FACE FACE;

        public MT7(string filename)
        {
            Read(filename);
        }

        public MT7(BinaryReader reader)
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

            Size = reader.ReadUInt32();
            FirstNodeOffset = reader.ReadUInt32();
            TextureCount = reader.ReadUInt32();

            //Read texture entries if there are any
            for (uint i = 0; i < TextureCount; i++)
            {
                uint offset = (TextureCount - i - 1) * 16 + i * 8;
                TextureEntries.Add(new TextureEntry(reader, offset));
            }
            reader.BaseStream.Seek(TextureCount * 8, SeekOrigin.Current);

            //If we are not at the first node yet, we still have an node offset table to read
            if (reader.BaseStream.Position != FirstNodeOffset)
            {
                NodeOffsetTableSize = reader.ReadUInt32();
                for (int i = 0; i < NodeOffsetTableSize; i++)
                {
                    NodeOffsetTable.Add(reader.ReadUInt32());
                }
            }

            //Read first node and as an result create the whole node tree structure
            reader.BaseStream.Seek(FirstNodeOffset, SeekOrigin.Begin);
            RootNode = new MT7Node(reader, null);


            //Search for optional extra sections until EOF
            while (reader.BaseStream.Position < reader.BaseStream.Length - 4)
            {
                uint identifier = reader.ReadUInt32();
                if (FACE.IsValid(identifier))
                {
                    reader.BaseStream.Seek(-4, SeekOrigin.Current);
                    FACE = new FACE(reader);
                }
                else if (CLSG.IsValid(identifier))
                {
                    reader.BaseStream.Seek(-4, SeekOrigin.Current);
                    CLSG = new CLSG(reader);
                }
                else if (TXT7.IsValid(identifier))
                {
                    reader.BaseStream.Seek(-4, SeekOrigin.Current);
                    TXT7 = new TXT7(reader);
                }
            }

            //Filling the texture entries with the actual textures
            if (TXT7 != null)
            {
                foreach (TextureEntry entry in TextureEntries)
                {
                    entry.Texture = TXT7.GetTexture(entry.ID, entry.NameData);
                }
            }
            
            //Crawling for textures up one dictionary (TODO: make this dictionary changeable)
            FileStream fileStream = (FileStream)reader.BaseStream;
            string dir = Path.GetDirectoryName(Path.GetDirectoryName(fileStream.Name));
            List<string> files = Helper.DirSearch(dir);

            foreach (TextureEntry entry in TextureEntries)
            {
                if (entry.Texture != null) continue;
                string searchHex = Helper.ByteArrayToString(entry.Data);
                foreach (string file in files)
                {
                    string filename = Path.GetFileName(file);
                    if (filename.Contains(searchHex))
                    {
                        using (FileStream stream = File.Open(file, FileMode.Open))
                        {
                            Texture tex = new Texture();
                            tex.ID = entry.ID;
                            tex.NameData = entry.NameData;
                            using (BinaryReader br = new BinaryReader(stream))
                            {
                                tex.Image = new PVRT(br);
                            }
                            entry.Texture = tex;
                        }
                        break;
                    }
                }
            }

            //Populate base class textures
            foreach(TextureEntry entry in TextureEntries)
            {
                Textures.Add(entry.Texture);
            }

            //Resolve the textures in the faces
            RootNode.ResolveFaceTextures(Textures);
        }

        public void Write(BinaryWriter writer)
        {
            throw new NotImplementedException();
            writer.Write(Identifier);
            writer.BaseStream.Seek(4, SeekOrigin.Current); //Skip size and write at end
        }
    }

    public class MT7Node : ModelNode
    {
        public uint Offset;
        public uint ID;

        public uint XB01Offset;
        public uint ChildOffset;
        public uint SiblingOffset;
        public uint ParentOffset;
        public XB01 XB01 { get; set; }

        public object SubNode { get; set; }

        public MT7Node(BinaryReader reader, MT7Node parent)
        {
            Parent = parent;

            Offset = (uint)reader.BaseStream.Position;
            ID = reader.ReadUInt32();

            Position = new Vector3
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle()
            };

            Rotation = new Vector3
            {
                X = 360.0f * reader.ReadInt32() / 0xffff,
                Y = 360.0f * reader.ReadInt32() / 0xffff,
                Z = 360.0f * reader.ReadInt32() / 0xffff
            };

            Scale = new Vector3
            {
                X = reader.ReadSingle(),
                Y = reader.ReadSingle(),
                Z = reader.ReadSingle()
            };

            XB01Offset = reader.ReadUInt32();
            ChildOffset = reader.ReadUInt32();
            SiblingOffset = reader.ReadUInt32();
            ParentOffset = reader.ReadUInt32();

            reader.BaseStream.Seek(8, SeekOrigin.Current);

            //Check for sub nodes
            uint subNodeIdentifier = reader.ReadUInt32();
            reader.BaseStream.Seek(-4, SeekOrigin.Current);
            if (MDCX.IsValid(subNodeIdentifier))
            {
                SubNode = new MDCX(reader);
            }
            else if (MDPX.IsValid(subNodeIdentifier))
            {
                SubNode = new MDPX(reader);
            }
            else if (MDOX.IsValid(subNodeIdentifier))
            {
                new NotImplementedException("Never seen this please report.");
            }
            else if (MDLX.IsValid(subNodeIdentifier))
            {
                new NotImplementedException("Never seen this please report.");
            }

            //Read XB01 mesh data
            long offset = reader.BaseStream.Position;
            if (XB01Offset != 0)
            {
                HasMesh = true;
                reader.BaseStream.Seek(XB01Offset, SeekOrigin.Begin);
                XB01 = new XB01(reader);
                Faces = XB01.Faces;
                Vertices = XB01.Vertices;
                Center = XB01.MeshCenter;
                Radius = XB01.MeshDiameter;
                VertexCount = (uint)XB01.Vertices.Count;
            }

            //Construct node tree recursively
            if (ChildOffset != 0)
            {
                reader.BaseStream.Seek(ChildOffset, SeekOrigin.Begin);
                Child = new MT7Node(reader, this);
            }
            if (SiblingOffset != 0)
            {
                reader.BaseStream.Seek(SiblingOffset, SeekOrigin.Begin);
                Sibling = new MT7Node(reader, (MT7Node)Parent);
            }
            reader.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        public override string ToString()
        {
            return String.Format("[{0}] MT7 Node: {1}", Offset, ID);
        }
    }

    /// <summary>
    /// Texture entry for texture lookup
    /// </summary>
    public class TextureEntry
    {
        private readonly static Encoding m_shiftJis = Encoding.GetEncoding("shift_jis");

        public ushort Width;
        public ushort Height;
        public uint Unknown1;
        public uint Unknown2;
        public uint Index;
        public byte[] NameData;
        public uint ID;
        public byte[] Data;

        public string Name
        {
            get { return m_shiftJis.GetString(NameData); }
        }

        public Texture Texture { get; set; }

        public TextureEntry(BinaryReader reader, uint offset)
        {
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            Index = reader.ReadUInt32();

            long position = reader.BaseStream.Position;
            reader.BaseStream.Seek(offset, SeekOrigin.Current);
            ID = reader.ReadUInt32();
            NameData = reader.ReadBytes(4);
            reader.BaseStream.Seek(-8, SeekOrigin.Current);
            Data = reader.ReadBytes(8);
            reader.BaseStream.Seek(position, SeekOrigin.Begin);
        }
    }

    

}
