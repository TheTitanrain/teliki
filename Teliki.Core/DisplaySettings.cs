using System;
using System.Collections.Generic;
using System.Linq;

namespace Teliki.Core
{
    public enum DisplayTargetMode
    {
        AllScreens,
        PrimaryScreen,
        SingleScreen
    }

    public static class DisplayModeParser
    {
        public const string AllScreens = "AllScreens";
        public const string PrimaryScreen = "PrimaryScreen";
        public const string SingleScreen = "SingleScreen";

        public static DisplayTargetMode Parse(string screenMode)
        {
            if (string.Equals(screenMode, PrimaryScreen, StringComparison.OrdinalIgnoreCase))
            {
                return DisplayTargetMode.PrimaryScreen;
            }

            if (string.Equals(screenMode, SingleScreen, StringComparison.OrdinalIgnoreCase))
            {
                return DisplayTargetMode.SingleScreen;
            }

            return DisplayTargetMode.AllScreens;
        }

        public static string Canonicalize(string screenMode)
        {
            switch (Parse(screenMode))
            {
                case DisplayTargetMode.PrimaryScreen:
                    return PrimaryScreen;
                case DisplayTargetMode.SingleScreen:
                    return SingleScreen;
                default:
                    return AllScreens;
            }
        }
    }

    public sealed class ScreenSelectionResult
    {
        public ScreenSelectionResult(IReadOnlyList<DisplayScreen> screens, bool usedFallback, string warning)
        {
            Screens = screens;
            UsedFallback = usedFallback;
            Warning = warning;
        }

        public IReadOnlyList<DisplayScreen> Screens { get; private set; }
        public bool UsedFallback { get; private set; }
        public string Warning { get; private set; }
    }

    public static class DisplayScreenSelector
    {
        public static ScreenSelectionResult SelectScreens(AppConfig config, IReadOnlyList<DisplayScreen> screens)
        {
            if (screens == null || screens.Count == 0)
            {
                return new ScreenSelectionResult(new DisplayScreen[0], false, null);
            }

            switch (config.DisplayMode)
            {
                case DisplayTargetMode.PrimaryScreen:
                    return CreateSingleSelection(GetPrimaryOrFirst(screens), false, null);
                case DisplayTargetMode.SingleScreen:
                    return SelectSingleScreen(config.ScreenIndex, screens);
                default:
                    return new ScreenSelectionResult(screens.ToArray(), false, null);
            }
        }

        private static ScreenSelectionResult SelectSingleScreen(int screenIndex, IReadOnlyList<DisplayScreen> screens)
        {
            if (screenIndex > 0)
            {
                var selected = screens.FirstOrDefault(screen => screen.Index == screenIndex);
                if (selected != null)
                {
                    return CreateSingleSelection(selected, false, null);
                }
            }

            var fallback = GetPrimaryOrFirst(screens);
            var warning = string.Format(
                "Configured screen index '{0}' is unavailable. Falling back to screen {1}.",
                screenIndex,
                fallback.Index);
            return CreateSingleSelection(fallback, true, warning);
        }

        private static ScreenSelectionResult CreateSingleSelection(DisplayScreen screen, bool usedFallback, string warning)
        {
            return new ScreenSelectionResult(new[] { screen }, usedFallback, warning);
        }

        private static DisplayScreen GetPrimaryOrFirst(IReadOnlyList<DisplayScreen> screens)
        {
            return screens.FirstOrDefault(screen => screen.Primary) ?? screens[0];
        }
    }
}
