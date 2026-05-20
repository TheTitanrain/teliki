using System.Windows.Forms;

namespace Teliki.App
{
    internal sealed class WinFormsTimerAdapter : IAppTimer
    {
        private readonly Timer _timer;

        public WinFormsTimerAdapter(Timer timer)
        {
            _timer = timer;
        }

        public int Interval
        {
            get { return _timer.Interval; }
            set { _timer.Interval = value; }
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}
