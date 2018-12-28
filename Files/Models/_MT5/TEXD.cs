using ShenmueDKSharp.Files.Images;
using ShenmueDKSharp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Models._MT5
{
    /// <summary>
    /// MT5 texture segment
    /// </summary>
    public class TEXD
    {
        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x54, 0x45, 0x58, 0x44 } //TEXD
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

        public uint Offset;

        public uint Identifier;
        public uint HeaderSize;
        public uint TextureCount;
        public List<Texture> Textures = new List<Texture>();

        public TEXL TEXL;
        public PTRL PTRL;

        public TEXD(BinaryReader reader)
        {
            Offset = (uint)reader.BaseStream.Position;
                 
            Identifier = reader.ReadUInt32();
            HeaderSize = reader.ReadUInt32();
            TextureCount = reader.ReadUInt32();

            reader.BaseStream.Seek(Offset + HeaderSize, SeekOrigin.Begin);

            for (int i = 0; i < TextureCount; i++)
            {
                uint nodeOffset = (uint)reader.BaseStream.Position;
                uint NodeIdentifier = reader.ReadUInt32();
                uint NodeSize = reader.ReadUInt32();

                if (NodeIdentifier == 0x4E584554) //TEXN
                {
                    Texture tex = new Texture();
                    tex.ID = reader.ReadUInt32();
                    tex.NameData = reader.ReadBytes(4);
                    tex.Image = new PVRT(reader);
                    Textures.Add(tex);
                }
                else if (NodeIdentifier == 0x454D414E) //NAME
                {
                    for (i = 0; i < ((NodeSize - 8) / 8); i++)
                    {
                        Texture tex = new Texture();
                        tex.ID = reader.ReadUInt32();
                        tex.NameData = reader.ReadBytes(4);

                        reader.BaseStream.Seek(-8, SeekOrigin.Current);
                        byte[] nameData = reader.ReadBytes(8);

                        FileStream fileStream = (FileStream)reader.BaseStream;
                        string dir = Path.GetDirectoryName(Path.GetDirectoryName(fileStream.Name));
                        List<string> files = FileHelper.DirSearch(dir);

                        string searchHex = Helper.ByteArrayToString(nameData);
                        bool found = false;
                        foreach (string file in files)
                        {
                            string extension = Path.GetExtension(file).ToUpper();
                            if (extension == ".TEXN")
                            {
                                using (FileStream stream = File.Open(file, FileMode.Open))
                                {
                                    using (BinaryReader br = new BinaryReader(stream))
                                    {
                                        br.BaseStream.Seek(8, SeekOrigin.Current); //Skip TEXN and size
                                        byte[] texnNameData = br.ReadBytes(8);
                                        if (Helper.CompareArray(nameData, texnNameData))
                                        {
                                            tex.Image = new PVRT(br);
                                            Textures.Add(tex);
                                            found = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (extension == ".PVR") //old method
                            {
                                string filename = Path.GetFileName(file);
                                if (filename.Contains(searchHex))
                                {
                                    using (FileStream stream = File.Open(file, FileMode.Open))
                                    {
                                        using (BinaryReader br = new BinaryReader(stream))
                                        {
                                            tex.Image = new PVRT(br);
                                            Textures.Add(tex);
                                        }
                                    }
                                    found = true;
                                    break;
                                }
                            }
                        }
                        if (!found)
                        {
                            Textures.Add(null);
                        }
                    }
                }
                reader.BaseStream.Seek(nodeOffset + NodeSize, SeekOrigin.Begin);
            }

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                uint identifier = reader.ReadUInt32();

                if (TEXL.IsValid(identifier))
                {
                    reader.BaseStream.Seek(-4, SeekOrigin.Current);
                    TEXL = new TEXL(reader);
                }

                if (PTRL.IsValid(identifier))
                {
                    reader.BaseStream.Seek(-4, SeekOrigin.Current);
                    PTRL = new PTRL(reader);
                }
            }
        }
    }
}
