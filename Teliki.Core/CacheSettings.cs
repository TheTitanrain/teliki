using System;

namespace Teliki.Core
{
    public sealed class CacheSettings
    {
        public static readonly CacheSettings Default = new CacheSettings(1024L * 1024L * 1024L, 512L * 1024L * 1024L);

        public long MaxCacheSizeBytes { get; private set; }
        public long MinFreeDiskBytes { get; private set; }

        public CacheSettings(long maxCacheSizeBytes, long minFreeDiskBytes)
        {
            MaxCacheSizeBytes = Math.Max(1, maxCacheSizeBytes);
            MinFreeDiskBytes = Math.Max(0, minFreeDiskBytes);
        }

        public static CacheSettings FromMegabytes(int maxCacheSizeMb, int minFreeDiskMb)
        {
            return new CacheSettings(
                Math.Max(1, maxCacheSizeMb) * 1024L * 1024L,
                Math.Max(0, minFreeDiskMb) * 1024L * 1024L);
        }
    }
}
