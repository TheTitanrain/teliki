using System;
using System.Threading.Tasks;
using Teliki.Core;

namespace Teliki.App
{
    internal sealed class BackgroundScanRunner : IScanRunner
    {
        private readonly MediaScanner _scanner;
        private readonly MediaCache _cache;
        private readonly ILogger _logger;

        public BackgroundScanRunner(MediaScanner scanner, MediaCache cache, ILogger logger)
        {
            _scanner = scanner;
            _cache = cache;
            _logger = logger;
        }

        public event Action<ScanCompletion> ScanCompleted;

        public void Start(ScanRequest request)
        {
            Task.Run(delegate
            {
                try
                {
                    var scanResult = _scanner.Scan(request.Config.MediaFolder);
                    var manifest = _cache.Promote(
                        scanResult,
                        request.Config.CacheFolder,
                        CacheSettings.FromMegabytes(request.Config.MaxCacheSizeMb, request.Config.MinFreeDiskMb));
                    RaiseCompleted(new ScanCompletion(request.Generation, manifest));
                }
                catch (Exception ex)
                {
                    _logger.Error("Background scan failed.", ex);
                    RaiseCompleted(new ScanCompletion(request.Generation, PlaylistManifest.Empty));
                }
            });
        }

        private void RaiseCompleted(ScanCompletion completion)
        {
            var handler = ScanCompleted;
            if (handler != null)
            {
                handler(completion);
            }
        }
    }
}
