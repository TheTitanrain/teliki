using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Teliki.Core
{
    public sealed class MediaScanner
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public MediaScanner(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public ScanResult Scan(string mediaFolder)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mediaFolder) || !_fileSystem.DirectoryExists(mediaFolder))
                {
                    return ScanResult.Unavailable("Media folder is unavailable.");
                }

                var files = new List<SourceMediaFile>();
                foreach (var path in _fileSystem.EnumerateFiles(mediaFolder))
                {
                    if (!MediaTypes.IsSupported(path))
                    {
                        continue;
                    }

                    var metadata = _fileSystem.GetFileMetadata(path);
                    files.Add(new SourceMediaFile(path, Path.GetFileName(path), metadata.Length, metadata.LastWriteUtc));
                }

                var ordered = files.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
                return ordered.Count == 0 ? ScanResult.SuccessEmpty() : ScanResult.Success(ordered);
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to scan media folder.", ex);
                return ScanResult.Unavailable(ex.Message);
            }
        }
    }
}
