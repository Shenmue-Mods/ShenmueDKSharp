using ShenmueDKSharp.Files.Tokens._CHRD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShenmueDKSharp.Files.Tokens
{
    public static class TokenHelper
    {
        public static BaseToken CreateToken(Type type)
        {
            if (type == null) return null;
            return (BaseToken)Activator.CreateInstance(type);
        }

        public static Type GetTokenType(BinaryReader reader)
        {
            string token = new String(reader.ReadChars(4));
            reader.BaseStream.Seek(-4, SeekOrigin.Current);
            return GetTokenType(token);
        }

        public static Type GetTokenType(string token)
        {
            if (token == ATTR.Identifier) return typeof(ATTR);
            if (token == CHRD.Identifier) return typeof(CHRD);
            if (token == CHRS.Identifier) return typeof(CHRS);
            if (token == CMPS.Identifier) return typeof(CMPS);
            if (token == COLS.Identifier) return typeof(COLS);
            if (token == DIRT.Identifier) return typeof(DIRT);
            if (token == ECAM.Identifier) return typeof(ECAM);
            if (token == END.Identifier) return typeof(END);
            if (token == FOG.Identifier) return typeof(FOG);
            if (token == LGHT.Identifier) return typeof(LGHT);
            if (token == LSCN.Identifier) return typeof(LSCN);
            if (token == MAPR.Identifier) return typeof(MAPR);
            if (token == MAPT.Identifier) return typeof(MAPT);
            if (token == REGD.Identifier) return typeof(REGD);
            if (token == SCN3.Identifier) return typeof(SCN3);
            if (token == SCRL.Identifier) return typeof(SCRL);
            if (token == SCOF.Identifier) return typeof(SCOF);
            if (token == SNDD.Identifier) return typeof(SNDD);
            if (token == STRG.Identifier) return typeof(STRG);
            return typeof(DummyToken);
        }

        public static List<BaseToken> Tokenize(BinaryReader reader, int size = -1)
        {
            List<BaseToken> tokens = new List<BaseToken>();
            long pos = reader.BaseStream.Length;
            if (size > 0) pos = reader.BaseStream.Position + size;
            while (reader.BaseStream.Position < pos - 7)
            {
                Type type = GetTokenType(reader);
                BaseToken token = CreateToken(type);
                token.Read(reader);
                tokens.Add(token);
            }
            return tokens;
        }

        public static TreeNode CreateTree(List<BaseToken> tokens)
        {
            TreeNode tNode = new TreeNode();
            foreach(BaseToken token in tokens)
            {
                TreeNode tokenNode = CreateTreeRecursive(token);
                tNode.Nodes.Add(tokenNode);
            }
            return tNode;
        }

        private static TreeNode CreateTreeRecursive(BaseToken token)
        {
            TreeNode tNode = new TreeNode(token.ToString());
            tNode.Tag = token;
            foreach (BaseToken t in token.Tokens)
            {
                tNode.Nodes.Add(CreateTreeRecursive(t));
            }
            return tNode;
        }
    }
}
