using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Audio
{
    /// <summary>
    /// Dreamcast sound file https://github.com/uliwitness/snd2wav/blob/master/snd2wav/snd2wav.cpp
    /// </summary>
    public class SND : BaseFile
    {
        public static bool EnableBuffering = false;
        public override bool BufferingEnabled => EnableBuffering;

        public enum Option
        {
            initChanLeft = 0x0002,      // left stereo channel
            initChanRight = 0x0003,     // right stereo channel
            waveInitChannel0 = 0x0004,  // wave-table channel 0
            waveInitChannel1 = 0x0005,  // wave-table channel 1
            waveInitChanne12 = 0x0006,  // wave-table channel 2
            waveInitChannel3 = 0x0007,  // wave-table channel 3
            initMono = 0x0080,          // monophonic channel
            initStereo = 0x00C0,        // stereo channel
            initMACE3 = 0x0300,         // 3:1 compression
            initMACE6 = 0x0400,         // 6:1 compression
            initNoInterp = 0x0004,      // no linear interpolation
            initNoDrop = 0x0008         // no drop-sample conversion
        };

        public enum Header
        {
            stdSH = 0x00,               // standard sound header
            extSH = 0xff,               // extended sound header
            cmpSH = 0xfe                // compressed sound header
        }


        protected override void _Read(BinaryReader reader)
        {
            ushort channels = 1;
            short sndFormat = reader.ReadInt16();
            if (sndFormat != 1 && sndFormat != 2)
            {
                throw new Exception("Bad snd format: " + sndFormat);
            }

            uint sndHeaderOffset = 20;
            int opts = 0;
            int referenceCount = 0;
            if (sndFormat == 2)
            {
                referenceCount = reader.ReadInt16();
                sndHeaderOffset = 14;
            }
            else
            {
                short dataTypeCount = reader.ReadInt16();
                if (dataTypeCount != 1)
                {
                    throw new Exception("Too many data types");
                }
                short dataType = reader.ReadInt16();
                if (dataType != 5)
                {
                    throw new Exception("Not sampled sound");
                }
                opts = reader.ReadInt32();
                if (opts != (int)Option.initMono && opts != 0xA0 && opts != (int)Option.initStereo)
                {
                    throw new Exception("Unhandled opts: " + opts);
                }
            }
            if (reader.ReadInt16() != 1)
            {
                throw new Exception("Too many commands");
            }
            ushort sndCommand = reader.ReadUInt16();
            if (sndCommand != 0x8051 && sndCommand != 0x8050)
            {
                throw new Exception("Not a bufferCmd or sndCmd: " + sndCommand);
            }

            short param1 = reader.ReadInt16();
            int param2 = reader.ReadInt32();
            if (param1 != 0)
            {
                throw new Exception("Bad param1");
            }
            if (param2 != sndHeaderOffset)
            {
                throw new Exception("Bad param2");
            }
            int dataPointer = reader.ReadInt32();
            if (dataPointer != 0)
            {
                throw new Exception("Bad data pointer");
            }
        }

        protected override void _Write(BinaryWriter writer)
        {
        }
    }

}
