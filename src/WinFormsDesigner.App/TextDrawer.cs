using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace WinFormsDesigner.App;

/// <summary>
/// Draws the text of the custom painted parts of the designer.
/// </summary>
/// <remarks>
/// The text renderer of WinForms creates a font handle and a device context for every
/// single call. That costs roughly 85 microseconds per line, so painting the rows of the
/// property grid needs about five milliseconds per repaint, which is clearly visible while
/// the window is resized. Drawing the text with a cached string format needs about ten
/// microseconds per line instead.
/// </remarks>
internal static class TextDrawer
{
    private static readonly ConcurrentDictionary<int, StringFormat> Formats = new();

    private static readonly ConcurrentDictionary<int, Brush> Brushes = new();

    /// <summary>Draws a single line of text inside the given rectangle.</summary>
    public static void Draw(
        Graphics graphics,
        string text,
        Font? font,
        Color color,
        Rectangle bounds,
        TextFormatFlags format)
    {
        if (string.IsNullOrEmpty(text) || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        font ??= Control.DefaultFont;

        var alignment = (format & TextFormatFlags.Right) != 0
            ? StringAlignment.Far
            : (format & TextFormatFlags.HorizontalCenter) != 0
                ? StringAlignment.Center
                : StringAlignment.Near;

        var lineAlignment = (format & TextFormatFlags.VerticalCenter) != 0
            ? StringAlignment.Center
            : StringAlignment.Near;

        var trimming = (format & TextFormatFlags.EndEllipsis) != 0
            ? StringTrimming.EllipsisCharacter
            : StringTrimming.None;

        var key = ((int)alignment * 4) + (int)lineAlignment * 2 + (int)trimming;

        var stringFormat = Formats.GetOrAdd(key, _ => new StringFormat
        {
            Alignment = alignment,
            LineAlignment = lineAlignment,
            Trimming = trimming,
            FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.NoWrap,
        });

        graphics.DrawString(text, font, BrushFor(color), bounds, stringFormat);
    }

    private static Brush BrushFor(Color color)
    {
        var key = color.ToArgb();
        return Brushes.GetOrAdd(key, _ => new SolidBrush(color));
    }
}
