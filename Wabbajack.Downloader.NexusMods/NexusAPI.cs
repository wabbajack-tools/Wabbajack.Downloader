﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Wabbajack.Downloader.Common;

namespace Wabbajack.Downloader.NexusMods
{
    public class NexusAPIClient
    {
        public Client HttpClient { get; } = new Client();

        public string? ApiKey { get; set; }

        public int MaxRetries { get; set; } = 4;

        public NexusAPIClient(string applicationName, string applicationVersion, string? apiKey, string userAgent = "")
        {
            ApiKey = apiKey;

            HttpClient.Headers.AddRange(new List<(string, string?)>
            {
                ("User-Agent", string.IsNullOrEmpty(userAgent) ? Client.DefaultUserAgent : userAgent),
                ("apikey", apiKey),
                ("Accept", "application/json"),
                ("Application-Name", applicationName),
                ("Application-Version", applicationVersion)
            });
        }

        #region Rate Tracking

        private readonly object _remainingLock = new object();

        private int _dailyRemaining;
        public virtual int DailyRemaining
        {
            get
            {
                lock (_remainingLock)
                {
                    return _dailyRemaining;
                }
            }
            protected set
            {
                lock (_remainingLock)
                {
                    _dailyRemaining = value;
                }
            }
        }

        private int _hourlyRemaining;
        public virtual int HourlyRemaining
        {
            get
            {
                lock (_remainingLock)
                {
                    return _hourlyRemaining;
                }
            }
            protected set
            {
                lock (_remainingLock)
                {
                    _hourlyRemaining = value;
                }
            }

        }

        public virtual async Task UpdateRemaining(HttpResponseMessage response)
        {
            var dailyRemaining = int.Parse(response.Headers.GetValues("x-rl-daily-remaining").First());
            var hourlyRemaining = int.Parse(response.Headers.GetValues("x-rl-hourly-remaining").First());

            lock (_remainingLock)
            {
                _dailyRemaining = Math.Min(_dailyRemaining, dailyRemaining);
                _hourlyRemaining = Math.Min(_hourlyRemaining, hourlyRemaining);
            }
        }

        #endregion

        public virtual async Task<T> Get<T>(string url)
        {
            var retries = 0;
            TOP:
            try
            {
                using var response = await HttpClient.GetAsync(url);
                await UpdateRemaining(response);
                if(!response.IsSuccessStatusCode)
                    throw new HttpException(response);

                await using var stream = await response.Content.ReadAsStreamAsync();
                return stream.FromJson<T>();
            }
            catch (TimeoutException)
            {
                if (retries == MaxRetries)
                    throw;
                retries++;
                goto TOP;
            }
        }

        public virtual async Task<UserStatus> GetUserStatus()
        {
            return await Get<UserStatus>("https://api.nexusmods.com/v1/users/validate.json");
        }

        public virtual async Task<string> GetNexusDownloadLink(string game, int modID, int fileID)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var url = $"https://api.nexusmods.com/v1/games/{game}/mods/{modID}/files/{fileID}/download_link.json";
            return (await Get<List<DownloadLink>>(url)).First().URI;
        }

        public virtual async Task<GetModFilesResponse> GetModFiles(string game, int modID)
        {
            var url = $"https://api.nexusmods.com/v1/games/{game}/mods/{modID}/files.json";
            var result = await Get<GetModFilesResponse>(url);
            if (result.files == null)
                throw new InvalidOperationException("Got Null data from the Nexus while finding mod files");
            return result;
        }
    }
}
