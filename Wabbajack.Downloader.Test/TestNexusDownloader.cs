using System;
using System.Threading.Tasks;
using Wabbajack.Downloader.NexusMods;
using Xunit;

namespace Wabbajack.Downloader.Test
{
    [Collection("Nexus Tests")]
    public class TestNexusDownloader
    {
        [Fact]
        public async Task TestDownload()
        {
            var apiKey = Environment.GetEnvironmentVariable("NEXUSAPIKEY", EnvironmentVariableTarget.Machine);
            Assert.NotNull(apiKey);

            var nexusApi = new NexusAPIClient("Mozilla/5.0 (Windows NT 10.0; rv:68.0) Gecko/20100101 Firefox/68.0", "Wabbajack.Downloader.Test", "1.0.0", apiKey);
            Assert.NotNull(nexusApi);

            var status = await nexusApi.GetUserStatus();
            Assert.True(status.is_premium);

            var link = await nexusApi.GetNexusDownloadLink("skyrimspecialedition", 12604, 35407);
            Assert.False(string.IsNullOrEmpty(link));
        }
    }
}
