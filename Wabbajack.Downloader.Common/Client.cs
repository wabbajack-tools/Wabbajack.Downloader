using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Wabbajack.Downloader.Common
{
    public class Client
    {
        public List<(string, string?)> Headers = new List<(string, string?)>();

        public List<Cookie> Cookies = new List<Cookie>();

        public int MaxRetries { get; set; } = 4;

        public static string DefaultUserAgent
        {
            get
            {
                var platformType = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                var headerString =
                    $"Wabbajack.Downloader/{Assembly.GetEntryAssembly()?.GetName()?.Version ?? new Version(0, 1)} ({Environment.OSVersion.VersionString}; {platformType}) {RuntimeInformation.FrameworkDescription}";
                return headerString;
            }
        }

        public async Task<HttpResponseMessage> GetAsync(string url, HttpCompletionOption responseHeadersRead = HttpCompletionOption.ResponseHeadersRead, bool errorsAsExceptions = true)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return await SendAsync(request, responseHeadersRead, errorsAsExceptions: errorsAsExceptions);
        }

        public async Task<HttpResponseMessage> GetAsync(Uri url, HttpCompletionOption responseHeadersRead = HttpCompletionOption.ResponseHeadersRead, bool errorsAsExceptions = true)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return await SendAsync(request, responseHeadersRead, errorsAsExceptions: errorsAsExceptions);
        }

        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content, HttpCompletionOption responseHeadersRead = HttpCompletionOption.ResponseHeadersRead, bool errorsAsExceptions = true)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            return await SendAsync(request, responseHeadersRead, errorsAsExceptions);
        }

        public async Task<HttpResponseMessage> PutAsync(string url, HttpContent content, HttpCompletionOption responseHeadersRead = HttpCompletionOption.ResponseHeadersRead)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
            return await SendAsync(request, responseHeadersRead);
        }

        public async Task<string> GetStringAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return await SendStringAsync(request);
        }

        public async Task<string> GetStringAsync(Uri url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return await SendStringAsync(request);
        }

        public async Task<string> DeleteStringAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            return await SendStringAsync(request);
        }

        private async Task<string> SendStringAsync(HttpRequestMessage request)
        {
            using var result = await SendAsync(request);
            if (!result.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Bad HTTP request {result.StatusCode} {result.ReasonPhrase} - {request.RequestUri}");
            }
            return await result.Content.ReadAsStringAsync();
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage msg, HttpCompletionOption responseHeadersRead = HttpCompletionOption.ResponseHeadersRead, bool errorsAsExceptions = true)
        {
            foreach (var (k, v) in Headers)
                msg.Headers.Add(k, v);
            if (Cookies.Count > 0)
                Cookies.ForEach(c => ClientFactory.Cookies.Add(c));

            var retries = 0;
            TOP:
            try
            {
                var response = await ClientFactory.Client.SendAsync(msg, responseHeadersRead);
                if (response.IsSuccessStatusCode) return response;

                if (!errorsAsExceptions) return response;
                response.Dispose();
                throw new HttpException(response);
            }
            catch (Exception ex)
            {
                if (ex is HttpException http)
                {
                    if (http.Code < 500) throw;

                    retries++;
                    var ms = Utils.NextRandom(100, 1000);

                    await Task.Delay(ms);
                    msg = CloneMessage(msg);
                    goto TOP;
                }

                if (retries > MaxRetries) throw;

                retries++;
                await Task.Delay(100 * retries);
                msg = CloneMessage(msg);
                goto TOP;
            }

        }

        private static HttpRequestMessage CloneMessage(HttpRequestMessage msg)
        {
            var newMessage = new HttpRequestMessage(msg.Method, msg.RequestUri);
            foreach ((var key, IEnumerable<string> value) in msg.Headers)
                newMessage.Headers.Add(key, value);
            newMessage.Content = msg.Content;
            return newMessage;
        }
    }
}
