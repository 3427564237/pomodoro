using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace APP.Core.Config
{
    public readonly record struct FlipThemePalette(
        Color FocusPrimary,
        Color FocusSoft,
        Color BreakPrimary,
        Color BreakSoft,
        Color Danger);

    public class ThemeService
    {
        public void ApplyTheme(FlipTheme theme, ThemeTone tone = ThemeTone.Focus)
        {
            var resources = Application.Current?.Resources;
            if (resources == null)
                return;

            ApplyTheme(resources, theme, tone);
        }

        public static FlipThemePalette GetPalette(FlipTheme theme)
        {
            return theme == FlipTheme.Violet
                ? new FlipThemePalette(
                    Color.FromArgb("#6750A4"),
                    Color.FromArgb("#E8DEF8"),
                    Color.FromArgb("#006C73"),
                    Color.FromArgb("#CCE8E6"),
                    Color.FromArgb("#BF4055"))
                : new FlipThemePalette(
                    Color.FromArgb("#2EC4B6"),
                    Color.FromArgb("#CBF3F0"),
                    Color.FromArgb("#FF9F1C"),
                    Color.FromArgb("#FFBF69"),
                    Color.FromArgb("#E71D36"));
        }

        private static void ApplyTheme(ResourceDictionary resources, FlipTheme theme, ThemeTone tone)
        {
            var palette = GetPalette(theme);
            var activePrimary = tone switch
            {
                ThemeTone.Break => palette.BreakPrimary,
                ThemeTone.Danger => palette.Danger,
                _ => palette.FocusPrimary
            };

            var activeSoft = tone switch
            {
                ThemeTone.Break => palette.BreakSoft,
                ThemeTone.Danger => Color.FromRgba(palette.Danger.Red, palette.Danger.Green, palette.Danger.Blue, 0.18f),
                _ => palette.FocusSoft
            };

            Set(resources, "Primary", palette.FocusPrimary);
            Set(resources, "Secondary", palette.FocusSoft);
            Set(resources, "Magenta", palette.Danger);

            Set(resources, "FlipFocusInner", palette.FocusPrimary);
            Set(resources, "FlipFocusOuter", palette.FocusSoft);
            Set(resources, "FlipBreakInner", palette.BreakPrimary);
            Set(resources, "FlipBreakOuter", palette.BreakSoft);
            Set(resources, "FlipDanger", palette.Danger);

            Set(resources, "FlipRingInner", activePrimary);
            Set(resources, "FlipRingOuter", activeSoft);
            Set(resources, "FlipRingText", Color.FromArgb("#EFFFFD"));
            Set(resources, "FlipMutedText", activePrimary);
            Set(resources, "FlipAccent", palette.BreakPrimary);
            Set(resources, "FlipOverlayBackground", Color.FromRgba(activePrimary.Red, activePrimary.Green, activePrimary.Blue, 0.82f));
        }

        private static void Set(ResourceDictionary resources, string key, object value)
        {
            if (resources.ContainsKey(key))
                resources[key] = value;
            else
                resources.Add(key, value);
        }
    }
}
