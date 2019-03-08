using ShenmueDKSharp.Files.Misc;
using ShenmueDKSharp.Files.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShenmueDKSharp.Utils
{
    /// <summary>
    /// [DEPRECATED] Generic node parser which creates treeview nodes.
    /// Used for debugging and reverse engineering the MAPINFO format.
    /// </summary>
    public class NodeParser
    {
        public static void GenerateTree(TreeNode rootNode, string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Open))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    while(reader.BaseStream.CanRead)
                    {
                        if (reader.BaseStream.Length - reader.BaseStream.Position < 8)
                        {
                            break;
                        }
                        BaseNode node = new BaseNode(null, reader);
                        node.CreateTreeNodes(rootNode);
                    }
                }
            }
        }
    }

    public class BaseNode
    {
        public string Token;
        public uint Size;
        public byte[] Content;

        public object Tag;
        public BaseNode Parent;
        public List<BaseNode> Children = new List<BaseNode>();

        public BaseNode(BaseNode parent, BinaryReader reader)
        {
            Parent = parent;
            long pos = reader.BaseStream.Position;
            Token = Encoding.ASCII.GetString(reader.ReadBytes(4));

            Tag = this;
            Size = reader.ReadUInt32();
            reader.BaseStream.Seek(pos + Size, SeekOrigin.Begin);

            /*
            while (reader.BaseStream.Position < pos + Size)
            {
                string tokenPeak = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (Regex.IsMatch(tokenPeak, @"^[A-Z]+$"))
                {
                    Children.Add(new BaseNode(this, reader));
                }
                else
                {
                    reader.BaseStream.Seek(-4, SeekOrigin.Current);
                    break;
                }
            }
            */
        }

        public void CreateTreeNodes(TreeNode rootNode)
        {
            TreeNode tNode = new TreeNode(Token);
            tNode.Tag = Tag;

            foreach (BaseNode node in Children)
            {
                node.CreateTreeNodes(tNode);
            }

            rootNode.Nodes.Add(tNode);
        }
    }

}
