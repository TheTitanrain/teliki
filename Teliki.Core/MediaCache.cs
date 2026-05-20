using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Teliki.Core
{
    public sealed class MediaCache
    {
        private const string ManifestFileName = "manifest.ini";
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private PlaylistManifest _current;
        private int _consecutiveEmptyScans;

        public MediaCache(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _current = PlaylistManifest.Empty;
        }

        public PlaylistManifest Promote(ScanResult scan, string cacheFolder)
        {
            return Promote(scan, cacheFolder, CacheSettings.Default);
        }

        public PlaylistManifest Promote(ScanResult scan, string cacheFolder, CacheSettings settings)
        {
            _fileSystem.CreateDirectory(cacheFolder);
            if (_current.Items.Count == 0)
            {
                _current = LoadManifest(cacheFolder);
            }

            if (scan.Status == ScanStatus.UnavailableOrError)
            {
                _consecutiveEmptyScans = 0;
                _logger.Warn("Keeping previous playlist because scan failed: " + scan.ErrorMessage);
                return _current;
            }

            if (scan.Status == ScanStatus.SuccessEmpty)
            {
                _consecutiveEmptyScans++;
                if (_consecutiveEmptyScans < 2)
                {
                    return _current;
                }

                _current = PlaylistManifest.Empty;
                SaveManifest(cacheFolder, _current);
                CleanupInactive(cacheFolder, _current, settings);
                return _current;
            }

            _consecutiveEmptyScans = 0;
            var promoted = new List<CachedMediaItem>();
            var tempFiles = new List<string>();

            try
            {
                foreach (var file in scan.Files)
                {
                    var copied = CopyStableFile(file, cacheFolder);
                    tempFiles.Add(copied.TempPath);
                    promoted.Add(new CachedMediaItem(copied.FinalPath, file.Name, MediaTypes.TryGetKind(file.Name).Value));
                }

                foreach (var item in promoted)
                {
                    var tempPath = item.CachedPath + ".tmp";
                    _fileSystem.MoveFile(tempPath, item.CachedPath);
                }

                _current = new PlaylistManifest(promoted);
                SaveManifest(cacheFolder, _current);
                CleanupInactive(cacheFolder, _current, settings);
                WarnIfActiveCacheExceedsLimit(cacheFolder, _current, settings);
                return _current;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to promote cache. Keeping last known good playlist.", ex);
                foreach (var temp in tempFiles)
                {
                    TryDelete(temp);
                }

                return _current;
            }
        }

        private CopyResult CopyStableFile(SourceMediaFile source, string cacheFolder)
        {
            if (!_fileSystem.FileExists(source.FullPath))
            {
                throw new FileNotFoundException("Source file disappeared.", source.FullPath);
            }

            var first = _fileSystem.GetFileMetadata(source.FullPath);
            Thread.Sleep(50);
            var second = _fileSystem.GetFileMetadata(source.FullPath);

            if (first.Length != second.Length || first.LastWriteUtc != second.LastWriteUtc)
            {
                throw new IOException("Source file is still changing: " + source.FullPath);
            }

            var safeName = MakeSafeName(Path.GetFileNameWithoutExtension(source.Name));
            var extension = Path.GetExtension(source.Name).ToLowerInvariant();
            var finalName = string.Format("{0}_{1}_{2}{3}", safeName, second.LastWriteUtc.Ticks, Guid.NewGuid().ToString("N"), extension);
            var finalPath = Path.Combine(cacheFolder, finalName);
            var tempPath = finalPath + ".tmp";

            using (var input = _fileSystem.OpenRead(source.FullPath))
            using (var output = _fileSystem.CreateFile(tempPath))
            {
                input.CopyTo(output);
                output.Flush();
            }

            if (!_fileSystem.FileExists(tempPath))
            {
                throw new IOException("Temporary cache file was not created.");
            }

            return new CopyResult(tempPath, finalPath);
        }

        private PlaylistManifest LoadManifest(string cacheFolder)
        {
            var path = Path.Combine(cacheFolder, ManifestFileName);
            if (!_fileSystem.FileExists(path))
            {
                return PlaylistManifest.Empty;
            }

            try
            {
                var items = new List<CachedMediaItem>();
                using (var reader = new StreamReader(_fileSystem.OpenRead(path)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split('|');
                        if (parts.Length < 3)
                        {
                            continue;
                        }

                        MediaKind kind;
                        if (!Enum.TryParse(parts[2], out kind))
                        {
                            continue;
                        }

                        if (_fileSystem.FileExists(parts[0]))
                        {
                            items.Add(new CachedMediaItem(parts[0], parts[1], kind));
                        }
                    }
                }

                return new PlaylistManifest(items);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load cache manifest.", ex);
                return PlaylistManifest.Empty;
            }
        }

        private void SaveManifest(string cacheFolder, PlaylistManifest manifest)
        {
            var path = Path.Combine(cacheFolder, ManifestFileName);
            var tempPath = path + ".tmp";
            using (var writer = new StreamWriter(_fileSystem.CreateFile(tempPath)))
            {
                foreach (var item in manifest.Items)
                {
                    writer.WriteLine("{0}|{1}|{2}", item.CachedPath, item.OriginalName, item.Kind);
                }
            }

            _fileSystem.MoveFile(tempPath, path);
        }

        private void CleanupInactive(string cacheFolder, PlaylistManifest active, CacheSettings settings)
        {
            try
            {
                var activePaths = new HashSet<string>(active.Items.Select(i => i.CachedPath), StringComparer.OrdinalIgnoreCase);
                activePaths.Add(Path.Combine(cacheFolder, ManifestFileName));
                foreach (var file in _fileSystem.EnumerateFiles(cacheFolder))
                {
                    if (!activePaths.Contains(file) && !file.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                    {
                        TryDelete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to clean inactive cache files.", ex);
            }
        }

        private void WarnIfActiveCacheExceedsLimit(string cacheFolder, PlaylistManifest active, CacheSettings settings)
        {
            try
            {
                var activeBytes = active.Items.Where(i => _fileSystem.FileExists(i.CachedPath))
                    .Select(i => _fileSystem.GetFileMetadata(i.CachedPath).Length)
                    .Sum();
                var freeBytes = _fileSystem.GetAvailableFreeSpace(cacheFolder);

                if (activeBytes > settings.MaxCacheSizeBytes)
                {
                    _logger.Warn("Active media exceeds configured cache size. Keeping active media.");
                }

                if (freeBytes < settings.MinFreeDiskBytes)
                {
                    _logger.Warn("Available free disk space is below configured threshold.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to evaluate cache limits.", ex);
            }
        }

        private void TryDelete(string path)
        {
            try
            {
                _fileSystem.DeleteFile(path);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to delete cache file: " + path, ex);
            }
        }

        private static string MakeSafeName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
            var safe = new string(chars);
            return safe.Length == 0 ? "media" : safe;
        }

        private struct CopyResult
        {
            public string TempPath { get; private set; }
            public string FinalPath { get; private set; }

            public CopyResult(string tempPath, string finalPath)
            {
                TempPath = tempPath;
                FinalPath = finalPath;
            }
        }
    }
}
