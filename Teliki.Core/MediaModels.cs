using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Teliki.Core
{
    public enum MediaKind
    {
        Image,
        Video
    }

    public enum ScanStatus
    {
        Success,
        SuccessEmpty,
        UnavailableOrError
    }

    public sealed class SourceMediaFile
    {
        public string FullPath { get; private set; }
        public string Name { get; private set; }
        public long Length { get; private set; }
        public DateTime LastWriteUtc { get; private set; }

        public SourceMediaFile(string fullPath, string name, long length, DateTime lastWriteUtc)
        {
            FullPath = fullPath;
            Name = name;
            Length = length;
            LastWriteUtc = lastWriteUtc;
        }
    }

    public sealed class CachedMediaItem
    {
        public string CachedPath { get; private set; }
        public string OriginalName { get; private set; }
        public MediaKind Kind { get; private set; }
        public int FailureCount { get; private set; }

        public CachedMediaItem(string cachedPath, string originalName, MediaKind kind)
            : this(cachedPath, originalName, kind, 0)
        {
        }

        public CachedMediaItem(string cachedPath, string originalName, MediaKind kind, int failureCount)
        {
            CachedPath = cachedPath;
            OriginalName = originalName;
            Kind = kind;
            FailureCount = failureCount;
        }
    }

    public sealed class ScanResult
    {
        public ScanStatus Status { get; private set; }
        public IReadOnlyList<SourceMediaFile> Files { get; private set; }
        public string ErrorMessage { get; private set; }

        private ScanResult(ScanStatus status, IEnumerable<SourceMediaFile> files, string errorMessage)
        {
            Status = status;
            Files = files.ToList().AsReadOnly();
            ErrorMessage = errorMessage;
        }

        public static ScanResult Success(IEnumerable<SourceMediaFile> files)
        {
            return new ScanResult(ScanStatus.Success, files, null);
        }

        public static ScanResult SuccessEmpty()
        {
            return new ScanResult(ScanStatus.SuccessEmpty, Enumerable.Empty<SourceMediaFile>(), null);
        }

        public static ScanResult Unavailable(string message)
        {
            return new ScanResult(ScanStatus.UnavailableOrError, Enumerable.Empty<SourceMediaFile>(), message);
        }
    }

    public sealed class PlaylistManifest
    {
        public static readonly PlaylistManifest Empty = new PlaylistManifest(Enumerable.Empty<CachedMediaItem>());

        public IReadOnlyList<CachedMediaItem> Items { get; private set; }

        public PlaylistManifest(IEnumerable<CachedMediaItem> items)
        {
            Items = items.ToList().AsReadOnly();
        }
    }

    public static class MediaTypes
    {
        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(
            new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" },
            StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> VideoExtensions = new HashSet<string>(
            new[] { ".wmv", ".avi", ".mp4" },
            StringComparer.OrdinalIgnoreCase);

        public static bool IsSupported(string path)
        {
            return TryGetKind(path).HasValue;
        }

        public static MediaKind? TryGetKind(string path)
        {
            var extension = Path.GetExtension(path);
            if (ImageExtensions.Contains(extension))
            {
                return MediaKind.Image;
            }

            if (VideoExtensions.Contains(extension))
            {
                return MediaKind.Video;
            }

            return null;
        }
    }
}
