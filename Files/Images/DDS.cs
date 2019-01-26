using ShenmueDKSharp.Files.Images._DDS;
using ShenmueDKSharp.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static ShenmueDKSharp.Files.Images._DDS.DDSFormats;

namespace ShenmueDKSharp.Files.Images
{
    public class DDS : BaseImage
    {
        public static bool EnableBuffering = true;
        public override bool BufferingEnabled => EnableBuffering;

        public readonly static List<string> Extensions = new List<string>()
        {
            "DDS"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[4] { 0x44, 0x44, 0x53, 0x20 }, //DDS 
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

        public override int DataSize => Size;

        public int Size { get; set; }

        public DDSGeneral.AlphaSettings AlphaSettings { get; set; }
        public DDSGeneral.MipHandling MipHandling { get; set; }
        public DDSFormatDetails FormatDetails { get; set; }

        public DDS() { }
        public DDS(string filename)
        {
            Read(filename);
        }
        public DDS(Stream stream)
        {
            Read(stream);
        }
        public DDS(BinaryReader reader)
        {
            Read(reader);
        }
        public DDS(BaseImage image)
        {
            Width = image.Width;
            Height = image.Height;
            foreach (MipMap mipmap in image.MipMaps)
            {
                MipMaps.Add(new MipMap(mipmap));
            }
        }

        protected override void _Read(BinaryReader reader)
        {
            long baseOffset = reader.BaseStream.Position;
            byte[] buffer = reader.ReadBytes((int)reader.BaseStream.Length);

            MemoryStream memoryStream = new MemoryStream(buffer, 0, buffer.Length, true, true);
            DDS_Header header = new DDS_Header(memoryStream);
            FormatDetails = new DDSFormatDetails(header.Format, header.DX10_DXGI_AdditionalHeader.dxgiFormat);
            MipMaps = DDSGeneral.LoadDDS(memoryStream, header, 0, FormatDetails);
            Width = header.Width;
            Height = header.Height;
            memoryStream.Close();
        }

        protected override void _Write(BinaryWriter writer)
        {
            byte[] ddsData = DDSGeneral.Save(MipMaps, FormatDetails, AlphaSettings, MipHandling);
            writer.Write(ddsData);
        }
    }
}
