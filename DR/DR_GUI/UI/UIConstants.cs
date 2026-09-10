using System.Drawing;

namespace DR_GUI.UI
{
    internal static class UIConstants
    {
        // Background colors
        public static readonly Color BackgroundDark = Color.FromArgb(30, 30, 30);
        public static readonly Color BackgroundMedium = Color.FromArgb(40, 40, 40);
        public static readonly Color BackgroundToolbar = Color.FromArgb(45, 45, 45);

        // Accent colors
        public static readonly Color AccentBlue = Color.FromArgb(52, 152, 219);
        public static readonly Color AccentGreen = Color.FromArgb(46, 204, 113);
        public static readonly Color AccentYellow = Color.FromArgb(241, 196, 15);
        public static readonly Color AccentPurple = Color.FromArgb(155, 89, 182);
        public static readonly Color AccentRed = Color.FromArgb(231, 76, 60);

        // Text colors
        public static readonly Color TextPrimary = Color.FromArgb(236, 240, 241);
        public static readonly Color TextSecondary = Color.FromArgb(200, 200, 200);

        // Fonts
        public static readonly Font DefaultFont = new Font("Segoe UI", 9);
        public static readonly Font CodeFont = new Font("Consolas", 11);
        public static readonly Font BoldFont = new Font("Segoe UI", 10, FontStyle.Bold);
        public static readonly Font HeaderFont = new Font("Segoe UI", 9, FontStyle.Bold);
    }
}
