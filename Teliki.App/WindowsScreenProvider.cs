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
            var index = 1;
            foreach (var screen in Screen.AllScreens)
            {
                screens.Add(new DisplayScreen(
                    index,
                    screen.Bounds.X,
                    screen.Bounds.Y,
                    screen.Bounds.Width,
                    screen.Bounds.Height,
                    screen.Primary));
                index++;
            }

            return screens;
        }
    }
}
