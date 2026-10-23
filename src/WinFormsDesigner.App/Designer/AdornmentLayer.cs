using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsDesigner.App.Designer;

/// <summary>
/// A transparent, unbuffered overlay that paints selection adornments above the designed form.
/// Double buffering is intentionally disabled because a buffered transparent child can render
/// its untouched pixels as black instead of showing the controls underneath.
/// </summary>
internal sealed class AdornmentLayer : Control
{
    /// <summary>
    /// WS_EX_TRANSPARENT. The window is painted after the siblings below it and does not
    /// erase its background, so the designed form stays visible underneath.
    /// </summary>
    private const int WS_EX_TRANSPARENT = 0x00000020;

    public AdornmentLayer()
    {
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    /// <summary>The surface that owns this layer and provides the painting logic.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DesignerSurface? Surface { get; set; }

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= WS_EX_TRANSPARENT;
            return parameters;
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Intentionally empty: painting a background would erase the designed form.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Surface?.PaintAdornments(e.Graphics);

        // The viewport uses native scroll bars; keep this hook for surface painting.
        Surface?.PaintScrollBars(e.Graphics);
    }
}
