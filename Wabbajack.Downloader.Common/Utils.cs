using System;

namespace Wabbajack.Downloader.Common
{
    internal static class Utils
    {
        private static readonly Random Random = new Random();
        public static int NextRandom(int min, int max)
        {
            return Random.Next(min, max);
        }
    }
}
