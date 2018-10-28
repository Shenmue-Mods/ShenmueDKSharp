using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VrSharp;
using VrSharp.PvrTexture;

namespace REFileKit.Headers
{

    public class PVR_Header : AbstractHeader
    {
        #region Properties
        /// <summary>
        /// Image format.
        /// </summary>
        public override ImageEngineFormat Format
        {
            get
            {
                return ImageEngineFormat.PVR;
            }
        }

        public PvrPixelFormat PixelFormat { get; private set; }
        public PvrDataFormat DataFormat { get; private set; }
        public PvrCompressionFormat CompressionFormat { get; private set; }
        public bool NeedsExternalPalette { get; private set; }
        public int GbixOffset { get; private set; }
        public int PvrtOffset { get; private set; }
        public uint GlobalIndex { get; private set; }

        #endregion Properties

        /// <summary>
        /// Reads a PVR header from stream.
        /// </summary>
        /// <param name="stream"></param>
        public PVR_Header(Stream stream)
        {
            Load(stream);
        }

        /// <summary>
        /// Reads the header of a PVR image.
        /// </summary>
        /// <param name="stream">Fully formatted PVR image.</param>
        /// <returns>Length of header.</returns>
        protected override long Load(Stream stream)
        {
            base.Load(stream);

            PvrTexture texture;
            
            // Initalize the texture (dirty two-way pass)
            try
            {
                texture = new PvrTexture(stream);
                Width = texture.TextureWidth;
                Height = texture.TextureHeight;

                DataFormat = texture.DataFormat;
                CompressionFormat = texture.CompressionFormat;
                PixelFormat = texture.PixelFormat;
                NeedsExternalPalette = texture.NeedsExternalPalette;
                GbixOffset = texture.GbixOffset;
                PvrtOffset = texture.PvrtOffset;

                GlobalIndex = texture.GlobalIndex;
            }
            catch (NotAValidTextureException)
            {
                Console.WriteLine("Error: This is not a valid PVR texture.");
                throw;
                //return null;
            }

            return 0;
        }

        internal static bool CheckIdentifier(byte[] IDBlock)
        {
            return (IDBlock[0] == 'G' && IDBlock[1] == 'B' && IDBlock[2] == 'I' && IDBlock[3] == 'X'); //TODO: pvr header check without gbix
        }
    }
}
