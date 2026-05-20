using System;
using System.Windows.Forms;

namespace Teliki.App
{
    internal interface ICursorController
    {
        void Hide();
        void Show();
    }

    internal sealed class CursorVisibilityManager
    {
        public static readonly CursorVisibilityManager Shared = new CursorVisibilityManager();

        private readonly ICursorController _cursorController;
        private bool _playbackCursorHidden;
        private int _visibleScopeCount;

        public CursorVisibilityManager()
            : this(new WinFormsCursorController())
        {
        }

        public CursorVisibilityManager(ICursorController cursorController)
        {
            _cursorController = cursorController ?? throw new ArgumentNullException(nameof(cursorController));
        }

        public void HideForPlayback()
        {
            if (_playbackCursorHidden)
            {
                return;
            }

            if (_visibleScopeCount == 0)
            {
                _cursorController.Hide();
            }

            _playbackCursorHidden = true;
        }

        public IDisposable ShowCursorWhileModalUiOpen()
        {
            _visibleScopeCount++;
            if (_playbackCursorHidden && _visibleScopeCount == 1)
            {
                _cursorController.Show();
            }

            return new Scope(this);
        }

        private void CloseVisibleScope()
        {
            if (_visibleScopeCount == 0)
            {
                throw new InvalidOperationException("Cursor visibility scope is not active.");
            }

            _visibleScopeCount--;
            if (_playbackCursorHidden && _visibleScopeCount == 0)
            {
                _cursorController.Hide();
            }
        }

        private sealed class Scope : IDisposable
        {
            private readonly CursorVisibilityManager _owner;
            private bool _disposed;

            public Scope(CursorVisibilityManager owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _owner.CloseVisibleScope();
            }
        }
    }

    internal sealed class WinFormsCursorController : ICursorController
    {
        public void Hide()
        {
            Cursor.Hide();
        }

        public void Show()
        {
            Cursor.Show();
        }
    }
}
