using System.Drawing;
using System.Windows.Forms;

namespace WinFormsDesigner.App.Designer;

/// <summary>
/// Routes mouse input that happens over the designer surface to the surface itself.
/// This is required because the designed controls would otherwise consume the clicks.
/// </summary>
internal sealed class DesignerMessageFilter : IMessageFilter
{
    private const int WM_MOUSEMOVE = 0x0200;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;

    private readonly DesignerSurface _surface;

    public DesignerMessageFilter(DesignerSurface surface)
    {
        _surface = surface;
    }

    public bool PreFilterMessage(ref Message m)
    {
        switch (m.Msg)
        {
            case WM_MOUSEMOVE:
            case WM_LBUTTONDOWN:
            case WM_LBUTTONUP:
            case WM_LBUTTONDBLCLK:
                break;

            default:
                return false;
        }

        var target = Control.FromHandle(m.HWnd);
        if (target is null || !_surface.IsDesignerTarget(target))
        {
            return false;
        }

        var point = target.PointToScreen(GetPoint(ref m));
        var local = _surface.PointToClient(point);

        _surface.HandleDesignerMouse(m.Msg, local, target);
        return true;
    }

    private static Point GetPoint(ref Message m)
    {
        var lParam = (int)m.LParam;
        var x = (short)(lParam & 0xFFFF);
        var y = (short)((lParam >> 16) & 0xFFFF);
        return new Point(x, y);
    }
}
