using System;
using System.Collections.Generic;

namespace Teliki.Core
{
    public interface IMediaRenderer
    {
        void Render(CachedMediaItem item);
        void ShowBlank();
    }

    public interface ITimer
    {
        event EventHandler Tick;
        void Start();
        void Stop();
    }

    public interface IScreenProvider
    {
        IReadOnlyList<DisplayScreen> GetScreens();
    }

    public sealed class DisplayScreen
    {
        public int Index { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool Primary { get; private set; }
        public string DisplayLabel
        {
            get
            {
                return string.Format(
                    "{0}. {1}{2}x{3} ({4},{5})",
                    Index,
                    Primary ? "Primary, " : string.Empty,
                    Width,
                    Height,
                    X,
                    Y);
            }
        }

        public DisplayScreen(int index, int x, int y, int width, int height, bool primary)
        {
            Index = index;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Primary = primary;
        }
    }

    public sealed class DisplayCoordinator
    {
        private readonly PlaylistService _playlist;
        private readonly IReadOnlyList<IMediaRenderer> _renderers;
        private readonly ILogger _logger;

        public DisplayCoordinator(PlaylistService playlist, IReadOnlyList<IMediaRenderer> renderers, ILogger logger)
        {
            _playlist = playlist;
            _renderers = renderers;
            _logger = logger;
        }

        public CachedMediaItem Advance()
        {
            var item = _playlist.Next();
            Render(item);
            return item;
        }

        public void Render(CachedMediaItem item)
        {
            foreach (var renderer in _renderers)
            {
                try
                {
                    if (item == null)
                    {
                        renderer.ShowBlank();
                    }
                    else
                    {
                        renderer.Render(item);
                    }
                }
                catch (Exception ex)
                {
                    if (item != null)
                    {
                        _playlist.ReportFailure(item);
                    }

                    _logger.Error("Renderer failed.", ex);
                }
            }
        }
    }
}
