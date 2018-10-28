#define DEBUG_BUFFER_FILES

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShenmueDKSharp.Files
{

    public abstract class BaseFile
    {
        private string m_filename = "";
        public string FilePath { get; set; }
        public string FileName
        {
            get
            {
                if (String.IsNullOrEmpty(m_filename)) return Path.GetFileName(FilePath);
                return m_filename;
            }
            set
            {
                m_filename = value;
            }
        }

        /// <summary>
        /// File loaded in buffer.
        /// Debugging purpose only for files that have no Write() implementation yet.
        /// </summary>
        public byte[] Buffer { get; set; }

        public void Read(string filepath)
        {
            FilePath = filepath;
            FileName = Path.GetFileName(filepath);
            using (FileStream stream = File.Open(filepath, FileMode.Open))
            {
                #if DEBUG_BUFFER_FILES
                Buffer = new byte[stream.Length];
                stream.Read(Buffer, 0, Buffer.Length);
                stream.Seek(0, SeekOrigin.Begin);
                #endif

                Read(stream);
            }
        }

        public void Write(string filepath)
        {
            using (FileStream stream = File.Open(filepath, FileMode.Open))
            {
                Write(stream);
            }
        }

        public abstract void Read(Stream stream);
        public abstract void Write(Stream stream);

    }
}
