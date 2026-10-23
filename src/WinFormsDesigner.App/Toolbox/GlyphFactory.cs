using System.Drawing;
using System.Drawing.Drawing2D;

namespace WinFormsDesigner.App.Toolbox;

/// <summary>
/// Draws the small icons used by the toolbox. Everything is generated at runtime so the
/// application stays a single self contained executable without external resources.
/// </summary>
public static class GlyphFactory
{
    private static readonly Dictionary<string, Bitmap> Cache = new();
    private static readonly object SyncRoot = new();

    /// <summary>Returns the 16x16 glyph for a toolbox entry.</summary>
    public static Bitmap GetGlyph(string key)
    {
        lock (SyncRoot)
        {
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached;
            }
        }

        var bitmap = Draw(key);

        lock (SyncRoot)
        {
            Cache[key] = bitmap;
        }

        return bitmap;
    }

    private static Bitmap Draw(string key)
    {
        var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.Clear(Color.Transparent);

        var border = new Pen(Color.FromArgb(0x60, 0x60, 0x60));
        var fill = new SolidBrush(Color.FromArgb(0xF0, 0xF0, 0xF0));
        var accent = new SolidBrush(Color.FromArgb(0x21, 0x96, 0xF3));
        var dark = new Pen(Color.FromArgb(0x30, 0x30, 0x30));
        var darkBrush = new SolidBrush(Color.FromArgb(0x30, 0x30, 0x30));
        var borderBrush = new SolidBrush(Color.FromArgb(0x60, 0x60, 0x60));

        switch (key)
        {
            case "button":
                graphics.FillRectangle(fill, 2, 5, 12, 7);
                graphics.DrawRectangle(border, 2, 5, 12, 7);
                graphics.DrawLine(dark, 5, 9, 11, 9);
                break;

            case "label":
                graphics.DrawLine(dark, 2, 9, 13, 9);
                graphics.DrawLine(dark, 3, 11, 12, 11);
                break;

            case "textbox":
                graphics.FillRectangle(fill, 1, 4, 14, 8);
                graphics.DrawRectangle(border, 1, 4, 14, 8);
                graphics.DrawLine(dark, 3, 9, 10, 9);
                break;

            case "richtextbox":
                graphics.FillRectangle(fill, 1, 2, 14, 12);
                graphics.DrawRectangle(border, 1, 2, 14, 12);
                graphics.DrawLine(dark, 3, 5, 12, 5);
                graphics.DrawLine(dark, 3, 8, 12, 8);
                graphics.DrawLine(dark, 3, 11, 9, 11);
                break;

            case "maskedtextbox":
                graphics.FillRectangle(fill, 1, 4, 14, 8);
                graphics.DrawRectangle(border, 1, 4, 14, 8);
                graphics.DrawLine(dark, 3, 9, 7, 9);
                graphics.DrawLine(dark, 9, 9, 12, 9);
                break;

            case "picturebox":
                graphics.FillRectangle(fill, 1, 3, 14, 10);
                graphics.DrawRectangle(border, 1, 3, 14, 10);
                graphics.FillPolygon(accent, [new Point(3, 12), new Point(7, 6), new Point(10, 12)]);
                graphics.FillEllipse(accent, 9, 5, 3, 3);
                break;

            case "checkbox":
                graphics.DrawRectangle(dark, 2, 4, 6, 6);
                graphics.DrawLine(dark, 4, 11, 13, 11);
                break;

            case "radiobutton":
                graphics.DrawEllipse(dark, 2, 4, 6, 6);
                graphics.FillEllipse(accent, 4, 6, 2, 2);
                graphics.DrawLine(dark, 10, 11, 14, 11);
                break;

            case "combobox":
                graphics.FillRectangle(fill, 1, 5, 14, 7);
                graphics.DrawRectangle(border, 1, 5, 14, 7);
                graphics.DrawLine(dark, 3, 9, 9, 9);
                graphics.FillPolygon(accent, [new Point(10, 7), new Point(13, 7), new Point(12, 10)]);
                break;

            case "listbox":
                graphics.FillRectangle(fill, 2, 2, 12, 12);
                graphics.DrawRectangle(border, 2, 2, 12, 12);
                graphics.DrawLine(dark, 4, 5, 12, 5);
                graphics.DrawLine(dark, 4, 8, 12, 8);
                graphics.DrawLine(dark, 4, 11, 10, 11);
                break;

            case "numericupdown":
                graphics.FillRectangle(fill, 1, 5, 10, 7);
                graphics.DrawRectangle(border, 1, 5, 10, 7);
                graphics.FillRectangle(accent, 11, 5, 4, 7);
                graphics.DrawRectangle(border, 11, 5, 4, 7);
                graphics.FillPolygon(darkBrush, [new Point(12, 9), new Point(14, 9), new Point(13, 7)]);
                break;

            case "datetimepicker":
                graphics.FillRectangle(fill, 1, 4, 14, 9);
                graphics.DrawRectangle(border, 1, 4, 14, 9);
                graphics.FillRectangle(accent, 2, 5, 12, 2);
                graphics.DrawLine(dark, 4, 10, 9, 10);
                break;

            case "monthcalendar":
                graphics.FillRectangle(fill, 1, 3, 14, 11);
                graphics.DrawRectangle(border, 1, 3, 14, 11);
                graphics.FillRectangle(accent, 2, 4, 12, 2);
                graphics.FillRectangle(accent, 5, 7, 2, 2);
                graphics.FillRectangle(accent, 9, 7, 2, 2);
                break;

            case "linklabel":
                graphics.DrawLine(dark, 2, 9, 13, 9);
                graphics.DrawLine(dark, 2, 11, 13, 11);
                using (var underline = new Pen(Color.FromArgb(0x00, 0x00, 0xEE)))
                {
                    graphics.DrawLine(underline, 2, 12, 13, 12);
                }

                break;

            case "panel":
                graphics.DrawRectangle(border, 1, 3, 14, 10);
                graphics.DrawRectangle(new Pen(Color.FromArgb(0xA0, 0xA0, 0xA0)) { DashStyle = DashStyle.Dot }, 3, 5, 10, 6);
                break;

            case "groupbox":
                graphics.DrawRectangle(border, 1, 5, 14, 8);
                graphics.FillRectangle(fill, 4, 3, 6, 3);
                graphics.DrawLine(dark, 4, 4, 10, 4);
                break;

            case "tabcontrol":
                graphics.DrawRectangle(border, 1, 4, 14, 9);
                graphics.FillRectangle(fill, 2, 2, 6, 4);
                graphics.DrawRectangle(border, 2, 2, 6, 4);
                graphics.FillRectangle(accent, 3, 3, 4, 2);
                break;

            case "tabpage":
                graphics.DrawRectangle(border, 2, 3, 12, 10);
                graphics.FillRectangle(fill, 4, 2, 5, 3);
                graphics.DrawRectangle(border, 4, 2, 5, 3);
                break;

            case "splitcontainer":
                graphics.DrawRectangle(border, 1, 2, 14, 12);
                graphics.FillRectangle(accent, 7, 2, 2, 12);
                break;

            case "flowlayoutpanel":
                graphics.DrawRectangle(border, 1, 3, 14, 10);
                graphics.FillRectangle(accent, 2, 4, 4, 4);
                graphics.FillRectangle(accent, 7, 4, 4, 4);
                break;

            case "tablelayoutpanel":
                graphics.DrawRectangle(border, 1, 2, 14, 12);
                graphics.DrawLine(border, 1, 8, 15, 8);
                graphics.DrawLine(border, 8, 2, 8, 14);
                break;

            case "datagridview":
                graphics.FillRectangle(fill, 1, 2, 14, 12);
                graphics.DrawRectangle(border, 1, 2, 14, 12);
                graphics.FillRectangle(accent, 2, 3, 12, 2);
                graphics.DrawLine(border, 1, 7, 15, 7);
                graphics.DrawLine(border, 8, 5, 8, 14);
                break;

            case "listview":
                graphics.FillRectangle(fill, 1, 2, 14, 12);
                graphics.DrawRectangle(border, 1, 2, 14, 12);
                graphics.FillRectangle(accent, 2, 3, 12, 2);
                graphics.DrawLine(border, 1, 7, 15, 7);
                graphics.DrawLine(dark, 3, 9, 9, 9);
                graphics.DrawLine(dark, 3, 12, 12, 12);
                break;

            case "treeview":
                graphics.DrawLine(border, 4, 3, 4, 13);
                graphics.DrawLine(border, 4, 6, 8, 6);
                graphics.DrawLine(border, 4, 10, 8, 10);
                graphics.DrawRectangle(border, 8, 4, 5, 4);
                graphics.DrawRectangle(border, 8, 8, 5, 4);
                break;

            case "menustrip":
                graphics.FillRectangle(fill, 1, 3, 14, 4);
                graphics.DrawRectangle(border, 1, 3, 14, 4);
                graphics.DrawLine(dark, 3, 5, 12, 5);
                break;

            case "contextmenustrip":
                graphics.FillRectangle(fill, 2, 4, 12, 9);
                graphics.DrawRectangle(border, 2, 4, 12, 9);
                graphics.DrawLine(dark, 4, 7, 12, 7);
                graphics.DrawLine(dark, 4, 10, 10, 10);
                break;

            case "statusstrip":
                graphics.FillRectangle(fill, 1, 9, 14, 4);
                graphics.DrawRectangle(border, 1, 9, 14, 4);
                break;

            case "toolstrip":
                graphics.FillRectangle(fill, 1, 5, 14, 5);
                graphics.DrawRectangle(border, 1, 5, 14, 5);
                graphics.FillRectangle(accent, 3, 6, 3, 3);
                graphics.FillRectangle(accent, 7, 6, 3, 3);
                break;

            case "progressbar":
                graphics.FillRectangle(fill, 1, 5, 14, 6);
                graphics.DrawRectangle(border, 1, 5, 14, 6);
                graphics.FillRectangle(accent, 2, 6, 8, 4);
                break;

            case "trackbar":
                graphics.DrawLine(dark, 2, 8, 13, 8);
                graphics.FillRectangle(accent, 6, 5, 3, 6);
                break;

            case "scrollbar":
                graphics.DrawRectangle(border, 4, 1, 8, 14);
                graphics.FillRectangle(accent, 5, 4, 6, 4);
                break;

            case "hscrollbar":
                graphics.DrawRectangle(border, 1, 4, 14, 8);
                graphics.FillRectangle(accent, 2, 5, 5, 6);
                graphics.FillRectangle(accent, 9, 5, 5, 6);
                break;

            case "vscrollbar":
                graphics.DrawRectangle(border, 4, 1, 8, 14);
                graphics.FillRectangle(accent, 5, 2, 6, 5);
                graphics.FillRectangle(accent, 5, 9, 6, 5);
                break;

            case "splitter":
                graphics.FillRectangle(accent, 7, 1, 2, 14);
                graphics.DrawLine(dark, 6, 4, 6, 12);
                graphics.DrawLine(dark, 10, 4, 10, 12);
                break;

            case "timer":
                graphics.DrawEllipse(dark, 2, 2, 12, 12);
                graphics.DrawLine(dark, 8, 5, 8, 9);
                graphics.DrawLine(dark, 8, 9, 11, 11);
                break;

            case "tooltip":
                graphics.FillRectangle(fill, 1, 4, 14, 8);
                graphics.DrawRectangle(border, 1, 4, 14, 8);
                graphics.FillPolygon(borderBrush, [new Point(5, 12), new Point(5, 14), new Point(8, 12)]);
                break;

            case "notifyicon":
                graphics.DrawEllipse(dark, 3, 4, 10, 9);
                graphics.FillRectangle(accent, 3, 9, 10, 4);
                break;

            case "errorprovider":
                graphics.DrawEllipse(dark, 2, 2, 12, 12);
                graphics.DrawLine(new Pen(Color.Red), 8, 5, 8, 9);
                graphics.FillEllipse(Brushes.Red, 7, 10, 2, 2);
                break;

            case "domainupdown":
                graphics.FillRectangle(fill, 1, 5, 10, 7);
                graphics.DrawRectangle(border, 1, 5, 10, 7);
                graphics.FillRectangle(accent, 11, 5, 4, 7);
                graphics.DrawRectangle(border, 11, 5, 4, 7);
                graphics.FillPolygon(darkBrush, [new Point(12, 8), new Point(14, 8), new Point(13, 10)]);
                break;

            default:
                graphics.DrawRectangle(border, 2, 3, 12, 10);
                break;
        }

        return bitmap;
    }
}
