#define DEBUG_BUFFER_FILES

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShenmueDKSharp.Files
{

    /// <summary>
    /// Base class for all files and nodes.
    /// Implements all the write and read wrapper methods.
    /// Includes an file buffer that is filled automatically when the sub class enabled buffering.
    /// </summary>
    public abstract class BaseFile
    {
        private string m_filename = "";

        /// <summary>
        /// Filepath of the object
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Filename of the object
        /// </summary>
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
        /// Gets if buffering of the file data is enabled.
        /// Controlled by the sub classes.
        /// </summary>
        public abstract bool BufferingEnabled { get; }

        /// <summary>
        /// File loaded in buffer.
        /// Debugging purpose only for files that have no complete and safe Write() implementation yet.
        /// </summary>
        public byte[] Buffer { get; set; }

        protected long BaseOffset { get; set; }



        /// <summary>
        /// Reads the given filepath to the current object.
        /// </summary>
        public void Read(string filepath)
        {
            FilePath = filepath;
            FileName = Path.GetFileName(filepath);
            using (FileStream stream = File.Open(filepath, FileMode.Open))
            {
                Read(stream);
            }
        }

        /// <summary>
        /// Reads the given stream to the current object.
        /// </summary>
        public void Read(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                Read(reader);
            }
        }

        /// <summary>
        /// Reads the given reader stream to the current object.
        /// </summary>
        public void Read(BinaryReader reader)
        {
            BaseOffset = reader.BaseStream.Position;
            _Read(reader);

            if (reader.BaseStream.CanSeek && BufferingEnabled)
            {
                long size = reader.BaseStream.Position - BaseOffset;
                reader.BaseStream.Seek(BaseOffset, SeekOrigin.Begin);
                Buffer = reader.ReadBytes((int)size);
            }
            //reader.Close();
        }

        /// <summary>
        /// Internal read implementation of the sub classes.
        /// </summary>
        protected abstract void _Read(BinaryReader reader);





        /// <summary>
        /// Writes the object to the given filepath.
        /// </summary>
        public void Write(string filepath)
        {
            FilePath = filepath;
            FileName = Path.GetFileName(filepath);
            string directory = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            using (FileStream stream = File.Open(filepath, FileMode.Create))
            {
                Write(stream);
            }
        }

        /// <summary>
        /// Writes the object to the given writer stream.
        /// </summary>
        public void Write(Stream stream)
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                Write(writer);
            }
        }

        /// <summary>
        /// Writes the object to the given writer stream.
        /// </summary>
        public void Write(BinaryWriter writer)
        {
            BaseOffset = writer.BaseStream.Position;
            _Write(writer);
        }

        /// <summary>
        /// Internal write implementation of the sub classes.
        /// </summary>
        protected abstract void _Write(BinaryWriter writer);





        /// <summary>
        /// Writes the buffered file data to the given filepath.
        /// </summary>
        public void WriteBuffer(string filepath)
        {
            using (FileStream stream = File.Open(filepath, FileMode.Create))
            {
                WriteBuffer(stream);
            }
        }

        /// <summary>
        /// Writes the buffered file data to the given stream.
        /// </summary>
        public void WriteBuffer(Stream stream)
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                WriteBuffer(writer);
            }
        }

        /// <summary>
        /// Writes the buffered file data to the given writer stream.
        /// </summary>
        public void WriteBuffer(BinaryWriter writer)
        {
            writer.Write(Buffer);
        }

        public override string ToString()
        {
            return FilePath;
        }
    }
}
