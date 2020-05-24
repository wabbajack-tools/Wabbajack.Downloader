using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Wabbajack.Downloader.Common
{
    public static class JsonExtensions
    {
        public static T FromJson<T>(this Stream stream)
        {
            using var tr = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            using var reader = new JsonTextReader(tr);
            var ser = JsonSerializer.Create();
            var result = ser.Deserialize<T>(reader);
            if (result == null)
                throw new JsonException("Type deserialized into null");
            return result;
        }
    }
}
