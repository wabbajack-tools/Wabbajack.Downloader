using System;

namespace Wabbajack.Downloader.Common
{
    internal static class Utils
    {
        private static readonly Random _random = new Random();
        public static int NextRandom(int min, int max)
        {
            return _random.Next(min, max);
        }
    }
}
