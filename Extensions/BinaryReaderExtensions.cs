using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    public static class BinaryReaderExtensions
    {
        public static string ReadLine(this BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            string line = "";
            using (StreamReader streamReader = new StreamReader(reader.BaseStream))
            {
                line = streamReader.ReadLine();
            }
            return line;
        }

        public static string ReadToEnd(this BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            string line = "";
            using (StreamReader streamReader = new StreamReader(reader.BaseStream))
            {
                line = streamReader.ReadToEnd();
            }
            return line;
        }
    }
}
