namespace Teliki.App
{
    internal sealed class ApplicationShutdownCoordinator
    {
        private bool _exitRequested;

        public bool RequestExit()
        {
            if (_exitRequested)
            {
                return false;
            }

            _exitRequested = true;
            return true;
        }

        public bool ShouldExitThreadAfterFormClosed(int remainingOpenForms)
        {
            return _exitRequested || remainingOpenForms <= 0;
        }
    }
}
