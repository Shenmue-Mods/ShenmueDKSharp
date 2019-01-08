using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Extensions
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Write data to this stream at the current position from another stream at it's current position.
        /// </summary>
        /// <param name="TargetStream">Stream to copy from.</param>
        /// <param name="SourceStream">Stream to copy to.</param>
        /// <param name="Length">Number of bytes to read.</param>
        /// <param name="bufferSize">Size of buffer to use while copying.</param>
        /// <returns>Number of bytes read.</returns>
        public static int ReadFrom(this Stream TargetStream, Stream SourceStream, long Length, int bufferSize = 4096)
        {
            byte[] buffer = new byte[bufferSize];
            int read;
            int numRead = 0;
            do
            {
                read = SourceStream.Read(buffer, 0, (int)Math.Min(bufferSize, Length));
                if (read == 0)
                    break;
                Length -= read;
                TargetStream.Write(buffer, 0, read);
                numRead += read;

            } while (Length > 0);

            return numRead;
        }

        /// <summary>
        /// Reads an int from stream at the current position and advances 4 bytes.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <returns>Integer read from stream.</returns>
        public static int ReadInt32(this Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.Default, true))
                return br.ReadInt32();
        }

        /// <summary>
        /// Reads an int from stream at the current position and advances 4 bytes.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <returns>Unsigned integer read from stream.</returns>
        public static uint ReadUInt32(this Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.Default, true))
                return br.ReadUInt32();
        }

        /// <summary>
        /// Reads an uint from stream at the current position and advances 4 bytes.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <returns>Integer read from stream.</returns>
        public static uint ReadUInt32FromStream(this Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.Default, true))
                return br.ReadUInt32();
        }


        /// <summary>
        /// Reads a long from stream at the current position.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <returns>Long read from stream.</returns>
        public static long ReadInt64FromStream(this Stream stream)
        {
            using (BinaryReader br = new BinaryReader(stream, Encoding.Default, true))
                return br.ReadInt64();
        }

        /// <summary>
        /// Reads a number of bytes from stream at the current position and advances that number of bytes.
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="Length">Number of bytes to read.</param>
        /// <returns>Bytes read from stream.</returns>
        public static byte[] ReadBytes(this Stream stream, int Length)
        {
            byte[] bytes = new byte[Length];
            stream.Read(bytes, 0, Length);
            return bytes;
        }


        /// <summary>
        /// Reads a string from a stream. Must be null terminated or have the length written at the start (Pascal strings or something?)
        /// </summary>
        /// <param name="stream">Stream to read from.</param>
        /// <param name="HasLengthWritten">True = Attempt to read string length from stream first.</param>
        /// <returns>String read from stream.</returns>
        public static string ReadString(this Stream stream, bool HasLengthWritten = false)
        {
            if (stream == null || !stream.CanRead)
                throw new IOException("Stream cannot be read.");

            int length = -1;
            List<char> chars = new List<char>();
            if (HasLengthWritten)
            {
                length = stream.ReadInt32();
                for (int i = 0; i < length; i++)
                    chars.Add((char)stream.ReadByte());
            }
            else
            {
                char c = 'a';
                while ((c = (char)stream.ReadByte()) != '\0')
                {
                    chars.Add(c);
                }
            }

            return new String(chars.ToArray());
        }


        /// <summary>
        /// KFreon: Borrowed this from the DevIL C# Wrapper found here: https://code.google.com/p/devil-net/
        /// 
        /// Reads a stream until the end is reached into a byte array. Based on
        /// <a href="http://www.yoda.arachsys.com/csharp/readbinary.html">Jon Skeet's implementation</a>.
        /// It is up to the caller to dispose of the stream.
        /// </summary>
        /// <param name="stream">Stream to read all bytes from</param>
        /// <param name="initialLength">Initial buffer length, default is 32K</param>
        /// <returns>The byte array containing all the bytes from the stream</returns>
        public static byte[] ReadStreamFully(this Stream stream, int initialLength = 32768)
        {
            stream.Seek(0, SeekOrigin.Begin);
            if (initialLength < 1)
            {
                initialLength = 32768; //Init to 32K if not a valid initial length
            }

            byte[] buffer = new byte[initialLength];
            int position = 0;
            int chunk;

            while ((chunk = stream.Read(buffer, position, buffer.Length - position)) > 0)
            {
                position += chunk;

                //If we reached the end of the buffer check to see if there's more info
                if (position == buffer.Length)
                {
                    int nextByte = stream.ReadByte();

                    //If -1 we reached the end of the stream
                    if (nextByte == -1)
                    {
                        return buffer;
                    }

                    //Not at the end, need to resize the buffer
                    byte[] newBuffer = new byte[buffer.Length * 2];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    newBuffer[position] = (byte)nextByte;
                    buffer = newBuffer;
                    position++;
                }
            }

            //Trim the buffer before returning
            byte[] toReturn = new byte[position];
            Array.Copy(buffer, toReturn, position);
            return toReturn;
        }

        /// <summary>
        /// Writes byte array to current position in stream.
        /// </summary>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="data">Data to write to stream.</param>
        public static void WriteBytes(this Stream stream, byte[] data)
        {
            if (!stream.CanWrite)
                throw new IOException("Stream is read only.");

            stream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Writes a long (int64) to stream at its current position.
        /// </summary>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="value">Int64 to write to stream.</param>
        public static void WriteInt64(this Stream stream, long value)
        {
            using (BinaryWriter br = new BinaryWriter(stream, Encoding.Default, true))
                br.Write(value);
        }


        /// <summary>
        /// FROM GIBBED.
        /// Writes an int to stream at the current position.
        /// </summary>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="value">Integer to write.</param>
        public static void WriteInt32(this Stream stream, int value)
        {
            using (BinaryWriter bw = new BinaryWriter(stream, Encoding.Default, true))
                bw.Write(value);
        }


        /// <summary>
        /// FROM GIBBED.
        /// Writes uint to stream at current position.
        /// </summary>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="value">uint to write.</param>
        public static void WriteUInt32(this Stream stream, uint value)
        {
            using (BinaryWriter bw = new BinaryWriter(stream, Encoding.Default, true))
                bw.Write(value);
        }

        /// <summary>
        /// FROM GIBBED.
        /// Writes a float to stream at the current position.
        /// </summary>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="value">Float to write to stream.</param>
        public static void WriteFloat32(this Stream stream, float value)
        {
            if (stream?.CanWrite != true)
                throw new IOException("Stream is null or read only.");

            stream.WriteBytes(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Writes string to stream. Terminated by a null char, and optionally writes string length at start of string. (Pascal strings?)
        /// </summary>
        /// <param name="stream">Stream to write to.</param>
        /// <param name="str">String to write.</param>
        /// <param name="WriteLength">True = Writes str length before writing string.</param>
        public static void WriteString(this Stream stream, string str, bool WriteLength = false)
        {
            if (WriteLength)
                stream.WriteInt32(str.Length);

            foreach (char c in str)
                stream.WriteByte((byte)c);

            stream.WriteByte((byte)'\0');
        }
    }
}
