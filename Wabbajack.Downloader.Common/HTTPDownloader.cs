using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Wabbajack.Downloader.Common
{
    public class HTTPDownloader
    {
        public static async Task<bool> Download(string url, string path, Client? client = null)
        {
            var parentDir = Path.GetDirectoryName(path);
            if(!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            await using var fs = File.Create(path);

            client ??= new Client();
            client.Headers.Add(("User-Agent", Client.DefaultUserAgent));

            long totalRead = 0;
            const int bufferSize = 1024 * 32;

            var response = await client.GetAsync(url);
            TOP:

            if (!response.IsSuccessStatusCode)
                return false;

            Stream stream;
            try
            {
                stream = await response.Content.ReadAsStreamAsync();
            }
            catch (Exception)
            {
                return false;
            }

            string? headerVar = null;
            if (response.Content.Headers.Contains("Content-Length"))
            {
                headerVar = response.Content.Headers.GetValues("Content-Length").FirstOrDefault();
                if (headerVar != null)
                    long.TryParse(headerVar, out _);
            }

            var supportsResume = response.Headers.AcceptRanges.FirstOrDefault(f => f == "bytes") != null;
            var contentSize = headerVar != null ? long.Parse(headerVar) : 1;

            // ReSharper disable once ConvertToUsingDeclaration
            await using (var webs = stream)
            {
                var buffer = new byte[bufferSize];
                var readThisCycle = 0;

                while (true)
                {
                    int read;
                    try
                    {
                        read = await webs.ReadAsync(buffer, 0, bufferSize);
                    }
                    catch (Exception)
                    {
                        if (readThisCycle == 0)
                            throw;

                        if (totalRead < contentSize)
                        {
                            if (!supportsResume) throw;

                            var msg = new HttpRequestMessage(HttpMethod.Get, url);
                            msg.Headers.Range = new RangeHeaderValue(totalRead, null);
                            response.Dispose();
                            response = await client.SendAsync(msg);
                            goto TOP;
                        }

                        break;
                    }

                    readThisCycle += read;

                    if (read == 0) break;

                    fs!.Write(buffer, 0, read);
                    totalRead += read;
                }
            }

            response.Dispose();
            return true;
        }
    }
}
