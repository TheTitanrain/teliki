using System.Collections.Generic;
using System.Windows.Forms;
using Teliki.Core;

namespace Teliki.App
{
    internal sealed class WindowsScreenProvider : IScreenProvider
    {
        public IReadOnlyList<DisplayScreen> GetScreens()
        {
            var screens = new List<DisplayScreen>();
            foreach (var screen in Screen.AllScreens)
            {
                screens.Add(new DisplayScreen(
                    screen.Bounds.X,
                    screen.Bounds.Y,
                    screen.Bounds.Width,
                    screen.Bounds.Height,
                    screen.Primary));
            }

            return screens;
        }
    }
}
