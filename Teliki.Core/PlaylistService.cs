using System;
using System.Collections.Generic;
using System.Linq;

namespace Teliki.Core
{
    public sealed class PlaylistService
    {
        private const int QuarantineFailureThreshold = 3;
        private readonly object _sync = new object();
        private readonly Dictionary<string, int> _failureCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private List<CachedMediaItem> _items = new List<CachedMediaItem>();
        private int _index = -1;

        public void Replace(PlaylistManifest manifest)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException("manifest");
            }

            lock (_sync)
            {
                _items = manifest.Items.ToList();
                _index = -1;
            }
        }

        public CachedMediaItem Next()
        {
            lock (_sync)
            {
                if (_items.Count == 0 || !_items.Any(IsPlayable))
                {
                    return null;
                }

                for (var attempts = 0; attempts < _items.Count; attempts++)
                {
                    _index = (_index + 1) % _items.Count;
                    if (IsPlayable(_items[_index]))
                    {
                        return _items[_index];
                    }
                }

                return null;
            }
        }

        public void ReportFailure(CachedMediaItem item)
        {
            if (item == null)
            {
                return;
            }

            lock (_sync)
            {
                int current;
                _failureCounts.TryGetValue(item.CachedPath, out current);
                _failureCounts[item.CachedPath] = current + 1;
            }
        }

        private bool IsPlayable(CachedMediaItem item)
        {
            int failures;
            return !_failureCounts.TryGetValue(item.CachedPath, out failures) ||
                   failures < QuarantineFailureThreshold;
        }
    }
}
