using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Wabbajack.Downloader.Test
{
    public static class Utils
    {
        public static async Task<string> GetHash(string file)
        {
            using var sha256 = SHA256.Create();
            byte[] data = sha256.ComputeHash(await File.ReadAllBytesAsync(file));
            var s = data.Aggregate("", (current, t) => current + t.ToString("x2"));
            return s;
        }
    }
}
