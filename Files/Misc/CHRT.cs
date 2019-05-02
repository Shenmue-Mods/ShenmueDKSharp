using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShenmueDKSharp.Files.Models;

namespace ShenmueDKSharp.Files.Misc
{
    public class CHRT : BaseFile
    {
        public readonly static List<string> Extensions = new List<string>()
        {
            "CHRT"
        };

        public override bool BufferingEnabled => false;

        public ModelNode RootNode;

        private string ReadString(BinaryReader reader, UInt32 offset)
        {
            long pos = reader.BaseStream.Position;

            if ((offset & 0xff000000) != 0) // check for string
            {
                reader.BaseStream.Seek(-4, SeekOrigin.Current);
                return Encoding.ASCII.GetString(reader.ReadBytes(4));
            }

            reader.BaseStream.Seek(offset - 4, SeekOrigin.Current);

            string str = "";
            while (reader.BaseStream.CanRead)
            {
                byte character = reader.ReadByte();
                if (character == 0) break;
                str += (char)character;
            }

            reader.BaseStream.Seek(pos, SeekOrigin.Begin);
            return str;
        }

        protected override void _Write(BinaryWriter writer)
        {

        }

        protected override void _Read(BinaryReader reader)
        {
            RootNode = new ModelNode();
            ModelNode lastNode = RootNode;
            ModelNode currentNode = null;

            uint chrsSignature = reader.ReadUInt32();
            uint chrsSize = reader.ReadUInt32();

            while (reader.BaseStream.Position < chrsSize)
            {
                uint offset = reader.ReadUInt32();
                string str = ReadString(reader, offset);
                switch (str.ToUpper())
                {
                    case "DEFIMAGE":
                        {
                            uint unknown1 = reader.ReadUInt32();
                            uint strOffset = reader.ReadUInt32();
                            string id = ReadString(reader, strOffset);
                            uint unknown2 = reader.ReadUInt32();
                            lastNode.Sibling = new ModelNode();
                            currentNode = lastNode.Sibling;
                            lastNode = currentNode;
                            currentNode.CHRTID = id;
                            //Console.WriteLine("DEFIMAGE({0}, {1}, {2})", unknown1, id, unknown2);
                            break;
                        }
                    case "IMAGE":
                        {
                            uint i1 = reader.ReadUInt32();
                            if (i1 == 25) //Model?
                            {
                                float i2 = reader.ReadSingle();
                                uint strOffset = reader.ReadUInt32();
                                string model = ReadString(reader, strOffset);
                                //Console.WriteLine("IMAGE({0}, {1}, {2})", i1, i2, model);
                                model = model.TrimStart(new char[] { '$', '@' });
                                currentNode.ModelName = Path.GetFileNameWithoutExtension(model);
                            }
                            else if (i1 == 3)
                            {
                                uint strOffset = reader.ReadUInt32();
                                string i2 = ReadString(reader, strOffset);
                                //Console.WriteLine("IMAGE({0}, {1})", i1, i2);
                                ModelNode n = RootNode.Sibling;
                                while (n != null)
                                {
                                    if (n.CHRTID == i2)
                                    {
                                        currentNode.ModelName = n.ModelName;
                                        currentNode.CHRTIMAGE = n.CHRTID;
                                        break;
                                    }
                                    n = n.Sibling;
                                }
                            }
                            else
                            {
                                //Console.WriteLine("Unexpected Image type {0}", i1);
                            }
                            break;
                        }
                    case "CHARACTER":
                        {
                            uint unknown1 = reader.ReadUInt32();
                            uint strOffset = reader.ReadUInt32();
                            string id = ReadString(reader, strOffset);
                            uint unknown2 = reader.ReadUInt32();
                            lastNode.Sibling = new ModelNode();
                            currentNode = lastNode.Sibling;
                            lastNode = currentNode;
                            currentNode.CHRTID = id;
                            //Console.WriteLine("CHARACTER({0}, {1}, {2})", unknown1, id, unknown2);
                            break;
                        }
                    case "FACE":
                        {
                            uint unknown1 = reader.ReadUInt32();
                            uint strOffset = reader.ReadUInt32();
                            string model = ReadString(reader, strOffset);
                            //Console.WriteLine("FACE({0}, {1})", unknown1, model);
                            break;
                        }
                    case "HAND":
                        {
                            uint unknown1 = reader.ReadUInt32();
                            uint strOffset = reader.ReadUInt32();
                            string modelLeft = ReadString(reader, strOffset);
                            strOffset = reader.ReadUInt32();
                            string modelRight = ReadString(reader, strOffset);
                            //Console.WriteLine("HAND({0}, {1}, {1})", unknown1, modelLeft, modelRight);
                            break;
                        }
                    case "HUMAN":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("HUMAN({0})", unknown);
                            break;
                        }
                    case "POSITION":
                        {
                            uint unk = reader.ReadUInt32(); //buffer?
                            float x = reader.ReadSingle();
                            float y = reader.ReadSingle();
                            float z = reader.ReadSingle();
                            currentNode.Position = new Vector3(x, y, z);
                            //Console.WriteLine("POSITION({0}, {1}, {2}, {3})", unk, x, y, z);
                            break;
                        }
                    case "ANGLE":
                        {
                            uint type = reader.ReadUInt32();
                            float rotx = 0, roty, rotz = 0;
                            if (type == 1)
                            {
                                roty = reader.ReadSingle();
                            }
                            else
                            {
                                rotx = reader.ReadSingle();
                                roty = reader.ReadSingle();
                                rotz = reader.ReadSingle();
                            }
                            currentNode.Rotation = new Vector3(rotx, roty, rotz);
                            //Console.WriteLine("ANGLE({0}, {1}, {2}, {3})", type, rotx, roty, rotz);
                            break;
                        }
                    case "SCALE":
                        {
                            uint type = reader.ReadUInt32();
                            if (type == 1)
                            {
                                float scl = reader.ReadSingle();
                                currentNode.Scale = new Vector3(scl, scl, scl);
                                //Console.WriteLine("SCALE({0}, {1})", type, scl);
                            }
                            else
                            {
                                float sclx = reader.ReadSingle();
                                float scly = reader.ReadSingle();
                                float sclz = reader.ReadSingle();
                                currentNode.Scale = new Vector3(sclx, scly, sclz);
                                //Console.WriteLine("SCALE({0}, {1}, {2}, {3})", type, sclx, scly, sclz);
                            }
                            break;
                        }
                    case "SIZE":
                        {
                            uint type = reader.ReadUInt32();
                            if (type == 1)
                            {
                                float size = reader.ReadSingle();
                                currentNode.Scale = new Vector3(size, size, size);
                                //Console.WriteLine("SIZE({0}, {1})", type, size);
                            }
                            else
                            {
                                float sizex = reader.ReadSingle();
                                float sizey = reader.ReadSingle();
                                float sizez = reader.ReadSingle();
                                currentNode.Scale = new Vector3(sizex, sizey, sizez);
                                //Console.WriteLine("SIZE({0}, {1}, {2}, {3})", type, sizex, sizey, sizez);
                            }
                            break;
                        }
                    case "HEIGHT":
                        {
                            uint type = reader.ReadUInt32();
                            float value = reader.ReadSingle();
                            //Console.WriteLine("HEIGHT({0}, {1})", type, value);
                            break;
                        }
                    case "RADIUS":
                        {
                            uint unknown = reader.ReadUInt32();
                            float radius = reader.ReadSingle();
                            //Console.WriteLine("RADIUS({0}, {1})", unknown, radius);
                            break;
                        }
                    case "RANGE":
                        {
                            uint start = reader.ReadUInt32();
                            uint end = reader.ReadUInt32();
                            //Console.WriteLine("RANGE({0}, {1})", start, end);
                            break;
                        }
                    case "ENTRY":
                        {
                            uint unknown1 = reader.ReadUInt32();
                            uint unknown2 = reader.ReadUInt32();
                            uint unknown3 = reader.ReadUInt32();
                            //Console.WriteLine("ENTRY({0}, {1}, {2})", unknown1, unknown2, unknown3);
                            break;
                        }
                    case "OBJECT":
                        {
                            uint obj = reader.ReadUInt32();
                            //Console.WriteLine("OBJECT({0})", obj);
                            break;
                        }
                    case "TRANS":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("TRANS({0})", unknown);
                            break;
                        }
                    case "ADJUST":
                        {
                            uint adj = reader.ReadUInt32();
                            uint strOffset = reader.ReadUInt32();
                            string adj2 = ReadString(reader, strOffset);
                            //Console.WriteLine("ADJUST({0}, {1})", adj, adj2);
                            break;
                        }
                    case "LEVEL":
                        {
                            uint unknown = reader.ReadUInt32();
                            float level = reader.ReadSingle();
                            //Console.WriteLine("LEVEL({0}, {1})", unknown, level);
                            break;
                        }
                    case "MODE":
                        {
                            uint unknown = reader.ReadUInt32();
                            float mode = reader.ReadSingle();
                            //Console.WriteLine("MODE({0}, {1})", unknown, mode);
                            break;
                        }
                    case "VENDER":
                        {
                            uint unknown = reader.ReadUInt32();
                            float vender = reader.ReadSingle();
                            //Console.WriteLine("VENDER({0}, {1})", unknown, vender);
                            break;
                        }
                    case "DISP": // Should not be an function
                        {
                            //Console.WriteLine("DISP()");
                            break;
                        }
                    case "SLEEP": // Should not be an function
                        {
                            //Console.WriteLine("SLEEP()");
                            break;
                        }
                    case "NOMAPEV": // Should not be an function
                        {
                            //Console.WriteLine("NOMAPEV()");
                            break;
                        }
                    case "REFBOARD":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("REFBOARD({0})", unknown);
                            break;
                        }
                    case "BONE":
                        {
                            uint unknown1 = reader.ReadUInt32();
                            uint unknown2 = reader.ReadUInt32();
                            //Console.WriteLine("BONE({0}, {1})", unknown1, unknown2);
                            break;
                        }
                    case "MOTCLIP":
                        {
                            uint unknown = reader.ReadUInt32();
                            uint strOffset = reader.ReadUInt32();
                            string state = ReadString(reader, strOffset);
                            //Console.WriteLine("MOTCLIP({0})", unknown, state);
                            break;
                        }
                    case "TINYMOT":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("TINYMOT({0})", unknown);
                            break;
                        }
                    case "TINYGAME":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("TINYGAME({0})", unknown);
                            break;
                        }
                    case "VEHICLE":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("VEHICLE({0})", unknown);
                            break;
                        }
                    case "BICYCLE":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("BICYCLE({0})", unknown);
                            break;
                        }
                    case "KNIFE":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("KNIFE({0})", unknown);
                            break;
                        }
                    case "CLOCK":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("CLOCK({0})", unknown);
                            break;
                        }
                    case "LINEWALK":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("LINEWALK({0})", unknown);
                            break;
                        }
                    case "OSAGE": // Pigtail
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("OSAGE({0})", unknown);
                            break;
                        }
                    case "TEL":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("TEL({0})", unknown);
                            break;
                        }
                    case "CALENDAR":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("CALENDAR({0})", unknown);
                            break;
                        }
                    case "CUTMVSHADOW":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("CUTMVSHADOW({0})", unknown);
                            break;
                        }
                    case "TAILLAMP":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("TAILLAMP({0})", unknown);
                            break;
                        }
                    case "CLOTH":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("CLOTH({0})", unknown);
                            break;
                        }
                    case "PLAYER":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("PLAYER({0})", unknown);
                            break;
                        }
                    case "DUMMYCHARA":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("DUMMYCHARA({0})", unknown);
                            break;
                        }
                    case "WALK3D":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("WALK3D({0})", unknown);
                            break;
                        }
                    case "PATROL":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("PATROL({0})", unknown);
                            break;
                        }
                    case "PROJECTIONTRANS":
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("PROJECTIONTRANS({0})", unknown);
                            break;
                        }
                    case "STEP":
                        {
                            uint unknown = reader.ReadUInt32();
                            uint strOffset = reader.ReadUInt32();
                            string state = ReadString(reader, strOffset);
                            //Console.WriteLine("STEP({0}, {1})", unknown, state);
                            break;
                        }
                    case "FLAGS":
                        {
                            uint flags = reader.ReadUInt32();
                            //Console.WriteLine("FLAGS({0})", flags);
                            break;
                        }
                    case "SHADOWOFF":
                        {
                            uint shadowOff = reader.ReadUInt32();
                            //Console.WriteLine("SHADOWOFF({0})", shadowOff);
                            break;
                        }
                    case "SHADOW":
                        {
                            uint shadow = reader.ReadUInt32();
                            uint shadow2 = reader.ReadUInt32();
                            //Console.WriteLine("SHADOW({0}, {1})", shadow, shadow2);
                            break;
                        }
                    case "FIGHT":
                        {
                            uint unknown1 = reader.ReadUInt32();
                            uint unknown2 = reader.ReadUInt32();
                            //Console.WriteLine("FIGHT({0}, {1})", unknown1, unknown2);
                            break;
                        }
                    case "CONTACT":
                        {
                            uint unknown1 = reader.ReadUInt32();
                            uint unknown2 = reader.ReadUInt32();
                            //Console.WriteLine("CONTACT({0}, {1})", unknown1, unknown2);
                            break;
                        }
                    case "BLUR": // Should not be an function
                        {
                            uint unknown = reader.ReadUInt32();
                            //Console.WriteLine("BLUR({0})", unknown);
                            break;
                        }
                    case "COLIOFF":
                        {
                            uint coliOff = reader.ReadUInt32();
                            //Console.WriteLine("COLIOFF({0})", coliOff);
                            break;
                        }
                    case "COLI":
                        {
                            uint coli = reader.ReadUInt32();
                            uint coli2 = reader.ReadUInt32();
                            //Console.WriteLine("COLI({0}, {1})", coli, coli2);
                            break;
                        }
                    default:
                        break;
                }
            }
        }
    }
}
