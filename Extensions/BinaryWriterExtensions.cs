using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    public static class BinaryWriterExtensions
    {
        public static void Write(this BinaryWriter writer, string value, Encoding encoding)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            writer.Write(encoding.GetBytes(value));
        }

        public static void WriteASCII(this BinaryWriter writer, string value)
        {
            Write(writer, value, Encoding.ASCII);
        }
    }
}
