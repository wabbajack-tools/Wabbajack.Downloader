using System;
using System.IO;
using System.Threading.Tasks;
using Wabbajack.Downloader.Common;
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

            var nexusApi = new NexusAPIClient("Wabbajack.Downloader.Test", "1.0.0", apiKey);
            Assert.NotNull(nexusApi);

            var status = await nexusApi.GetUserStatus();
            Assert.True(status.is_premium);

            var link = await nexusApi.GetNexusDownloadLink("skyrimspecialedition", 12604, 35407);
            Assert.False(string.IsNullOrEmpty(link));

            var result = await HTTPDownloader.Download(link, "SkyUI.7z", nexusApi.HttpClient);
            Assert.True(result);
            Assert.True(File.Exists("SkyUI.7z"));
            Assert.Equal("5375e0e91051f57ad463dfe3412bba58abb975f8479a968dd9d4e5d329431662", await Utils.GetHash("SkyUI.7z"));
        }
    }
}
