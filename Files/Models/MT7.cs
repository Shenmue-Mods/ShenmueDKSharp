using ShenmueDKSharp.Files.Images;
using ShenmueDKSharp.Files.Misc;
using ShenmueDKSharp.Files.Models._MT7;
using ShenmueDKSharp.Structs;
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
    /// MT7 model for Shenmue II.
    /// Used for reading MT7, MAPM, PROP and CHRM files from Shenmue II.
    /// MDCX/7, MDPX/7, MDOX/7 and MDLX/7 tokens.
    /// </summary>
    public class MT7 : BaseModel
    {
        public static bool UseTextureDatabase = true;
        public static bool SearchTexturesOneDirUp = false;
        public static bool EnableBuffering = true;
        public override bool BufferingEnabled => EnableBuffering;

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
                if (FileHelper.CompareSignature(Identifiers[i], identifier)) return true;
            }
            return false;
        }

        /// <summary>
        /// True for embedding textures when writing MT7
        /// </summary>
        public bool EmbeddedTextures { get; set; } = false;

        public uint Offset;

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
        public MT7(Stream stream)
        {
            Read(stream);
        }
        public MT7(BinaryReader reader)
        {
            Read(reader);
        }

        protected override void _Read(BinaryReader reader)
        {
            Offset = (uint)reader.BaseStream.Position;

            Identifier = reader.ReadUInt32();
            if (!IsValid(Identifier)) return;

            Size = reader.ReadUInt32();
            FirstNodeOffset = reader.ReadUInt32();
            TextureCount = reader.ReadUInt32();

            //Read texture entries if there are any
            for (uint i = 0; i < TextureCount; i++)
            {
                TextureEntries.Add(new TextureEntry(reader));
            }
            foreach(TextureEntry entry in TextureEntries)
            {
                entry.ReadTextureID(reader);
            }

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
            reader.BaseStream.Seek(Offset + FirstNodeOffset, SeekOrigin.Begin);
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
                    entry.Texture = TXT7.GetTexture(entry.TextureID);
                }
            }
            
            if (SearchTexturesOneDirUp)
            {
                FileStream fileStream = (FileStream)reader.BaseStream;
                string dir = Path.GetDirectoryName(Path.GetDirectoryName(fileStream.Name));
                TextureDatabase.SearchDirectory(dir);
            }

            if (MT7.UseTextureDatabase)
            {
                foreach (TextureEntry entry in TextureEntries)
                {
                    if (entry.Texture != null) continue;

                    TEXN texture = TextureDatabase.FindTexture(entry.TextureID.Data);
                    if (texture == null)
                    {
                        Console.WriteLine("Couldn't find texture: {0}", entry.TextureID.Name);
                        continue;
                    }
                    entry.Texture = new Texture();
                    entry.Texture.TextureID = new TextureID(texture.TextureID);
                    entry.Texture.Image = texture.Texture;
                }
            }
            

            //Populate base class textures
            foreach(TextureEntry entry in TextureEntries)
            {
                if (entry.Texture == null)
                {
                    entry.Texture = new Texture();
                    entry.Texture.TextureID = entry.TextureID;
                }
                Textures.Add(entry.Texture);
            }

            //Resolve the textures in the faces
            RootNode.ResolveFaceTextures(Textures);
        }

        protected override void _Write(BinaryWriter writer)
        {
            Offset = (uint)writer.BaseStream.Position;
            writer.Write(Identifier);
            writer.BaseStream.Seek(8, SeekOrigin.Current); //Skip size and first node offset

            TextureCount = (uint)TextureEntries.Count;
            writer.Write(TextureCount);
            foreach(TextureEntry entry in TextureEntries)
            {
                entry.WriteMetadata(writer);
            }
            foreach (TextureEntry entry in TextureEntries)
            {
                entry.WriteTextureID(writer);
            }
            writer.Write(1);
            FirstNodeOffset = (uint)(writer.BaseStream.Position - Offset + 4);
            writer.Write(FirstNodeOffset);

            //Write Nodes

            Size = (uint)(writer.BaseStream.Position - Offset);

            //Write FACE (TODO: Needs full FACE implementation)
            //Write CLSG (TODO: Needs full CLSG implementation)

            //Write TXT7

            writer.BaseStream.Seek(Offset + 4, SeekOrigin.Begin);
            writer.Write(Size);
            writer.Write(FirstNodeOffset);
        }
    }

    public class MT7Node : ModelNode
    {
        public uint Offset;

        public uint XB01Offset;
        public uint ChildOffset;
        public uint SiblingOffset;
        public uint ParentOffset;
        public XB01 XB01 { get; set; }

        public object SubNode { get; set; }

        public MT7Node() { }

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
                new NotImplementedException("Never seen this, please report.");
            }
            else if (MDLX.IsValid(subNodeIdentifier))
            {
                new NotImplementedException("Never seen this, please report.");
            }

            //Read XB01 mesh data
            long offset = reader.BaseStream.Position;
            if (XB01Offset != 0)
            {
                HasMesh = true;
                reader.BaseStream.Seek(XB01Offset, SeekOrigin.Begin);
                XB01 = new XB01(reader, this);
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

        public void WriteNode(BinaryWriter writer)
        {
            Offset = (uint)writer.BaseStream.Position;
        }

        public void WriteXB01(BinaryWriter writer)
        {
            XB01Offset = (uint)writer.BaseStream.Position;

            //Convert BaseNode mesh data to XB01
        }

        public override string ToString()
        {
            return String.Format("[{0}] MT7 Node: {1} (Bone: {2})", Offset, ID, BoneID);
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
        public TextureID TextureID;

        public Texture Texture { get; set; }

        public TextureEntry(BinaryReader reader)
        {
            ReadMetadata(reader);
        }

        public void ReadMetadata(BinaryReader reader)
        {
            Width = reader.ReadUInt16();
            Height = reader.ReadUInt16();
            Unknown1 = reader.ReadUInt32();
            Unknown2 = reader.ReadUInt32();
            Index = reader.ReadUInt32();
        }

        public void ReadTextureID(BinaryReader reader)
        {
            TextureID = new TextureID(reader);
        }

        public void WriteMetadata(BinaryWriter writer)
        {
            writer.Write(Width);
            writer.Write(Height);
            writer.Write(Unknown1);
            writer.Write(Unknown2);
            writer.Write(Index);
        }

        public void WriteTextureID(BinaryWriter writer)
        {
            TextureID.Write(writer);
        }
    }

    

}
