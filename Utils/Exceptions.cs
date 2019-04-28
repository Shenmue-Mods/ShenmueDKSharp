using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ShenmueDKSharp.Utils
{
    class InvalidFileSignatureException : Exception
    {
        public InvalidFileSignatureException() : base("The file signature could not be found!") { }

        public InvalidFileSignatureException(string message) : base(message) {}

    }
}
