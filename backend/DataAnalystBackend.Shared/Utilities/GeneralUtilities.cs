using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAnalystBackend.Shared.Utilities
{
    public static class GeneralUtilities
    {
        public static byte[] DecompressFile(byte[] file)
        {
            using (MemoryStream ms = new MemoryStream(file))
            using (MemoryStream response = new MemoryStream())
            using (var decompressor = new GZipStream(ms, CompressionMode.Decompress))
            {
                decompressor.CopyTo(response);
                return response.ToArray();
            }
        }
    }
}
