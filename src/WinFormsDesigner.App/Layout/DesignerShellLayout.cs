using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsDesigner.App.Layout;

/// <summary>
/// The three pane layout of the designer shell: the toolbox on the left, the design
/// surface in the middle and the property grid on the right.
/// </summary>
/// <remarks>
/// <para>
/// The shell used to be built from two nested split containers. That turned out to be
/// fragile: the framework paints the splitter rectangle through the window handle of the
/// container, which fails while the window is not realized yet. The exception aborted the
/// layout pass and left the panes frozen at their old size, so the shell no longer reacted
/// to a window resize.
/// </para>
/// <para>
/// The layout below positions the panes itself, draws the two splitter bars and follows the
/// mouse while a bar is dragged. It needs a single layout pass instead of the several that
/// the nested containers needed and it cannot fail while the window is created.
/// </para>
/// </remarks>
public sealed class DesignerShellLayout : Panel
{
    private const int SplitterWidth = 4;
    private const int MinToolboxWidth = 160;
    private const int MinPropertiesWidth = 220;
    private const int MinSurfaceWidth = 320;

    private static readonly Color BackgroundColor = Color.White;
    private static readonly Color SplitterColor = Color.FromArgb(0xD4, 0xD4, 0xD4);
    private static readonly Color SplitterLineColor = Color.FromArgb(0xA0, 0xA0, 0xA0);

    private Control? _toolbox;
    private Control? _surface;
    private Control? _properties;
    private int _toolboxWidth = 230;
    private int _propertiesWidth = 330;
    private bool _showToolbox = true;
    private bool _showProperties = true;
    private DragTarget _dragTarget = DragTarget.None;
    private int _dragStartX;
    private int _dragStartWidth;

    private enum DragTarget
    {
        None,
        Toolbox,
        Properties,
    }

    public DesignerShellLayout()
    {
        Dock = DockStyle.Fill;
        BackColor = BackgroundColor;

        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.ResizeRedraw, true);
        UpdateStyles();
    }

    /// <summary>The pane that holds the toolbox.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control? Toolbox
    {
        get => _toolbox;
        set => SetPane(ref _toolbox, value);
    }

    /// <summary>The pane that holds the design surface.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control? Surface
    {
        get => _surface;
        set => SetPane(ref _surface, value);
    }

    /// <summary>The pane that holds the property grid.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control? Properties
    {
        get => _properties;
        set => SetPane(ref _properties, value);
    }

    /// <summary>Width of the toolbox pane in pixels.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ToolboxWidth
    {
        get => _toolboxWidth;
        set
        {
            _toolboxWidth = Math.Max(MinToolboxWidth, value);
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>Width of the property pane in pixels.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PropertiesWidth
    {
        get => _propertiesWidth;
        set
        {
            _propertiesWidth = Math.Max(MinPropertiesWidth, value);
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>Shows or hides the toolbox pane.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowToolbox
    {
        get => _showToolbox;
        set
        {
            if (_showToolbox == value)
            {
                return;
            }

            _showToolbox = value;
            PerformLayout();
            Invalidate();
        }
    }

    /// <summary>Shows or hides the property pane.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowProperties
    {
        get => _showProperties;
        set
        {
            if (_showProperties == value)
            {
                return;
            }

            _showProperties = value;
            PerformLayout();
            Invalidate();
        }
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);

        var width = ClientSize.Width;
        var height = ClientSize.Height;

        var toolboxWidth = 0;

        if (_toolbox is not null)
        {
            if (_showToolbox)
            {
                var available = width - MinPropertiesWidth - MinSurfaceWidth - (2 * SplitterWidth);
                toolboxWidth = Math.Clamp(_toolboxWidth, MinToolboxWidth, Math.Max(MinToolboxWidth, available));
                SetBounds(_toolbox, 0, 0, toolboxWidth, height);
                _toolbox.Visible = true;
            }
            else
            {
                _toolbox.Visible = false;
            }
        }

        var propertiesWidth = 0;

        if (_properties is not null)
        {
            if (_showProperties)
            {
                var available = width - toolboxWidth - MinSurfaceWidth - (2 * SplitterWidth);
                propertiesWidth = Math.Clamp(_propertiesWidth, MinPropertiesWidth, Math.Max(MinPropertiesWidth, available));
                SetBounds(_properties, width - propertiesWidth, 0, propertiesWidth, height);
                _properties.Visible = true;
            }
            else
            {
                _properties.Visible = false;
            }
        }

        if (_surface is not null)
        {
            var left = toolboxWidth > 0 ? toolboxWidth + SplitterWidth : 0;
            var right = propertiesWidth > 0 ? propertiesWidth + SplitterWidth : 0;
            SetBounds(_surface, left, 0, Math.Max(0, width - left - right), height);
        }
    }

    /// <summary>
    /// Moves a pane, but only when the bounds really change. Setting the same bounds
    /// again would make the pane lay out its content a second time.
    /// </summary>
    private static void SetBounds(Control control, int x, int y, int width, int height)
    {
        if (control.Left == x && control.Top == y && control.Width == width && control.Height == height)
        {
            return;
        }

        control.SetBounds(x, y, width, height);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using (var brush = new SolidBrush(BackgroundColor))
        {
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }

        using (var brush = new SolidBrush(SplitterColor))
        using (var pen = new Pen(SplitterLineColor))
        {
            if (_showToolbox && _toolbox is not null)
            {
                var x = _toolbox.Right;
                e.Graphics.FillRectangle(brush, x, 0, SplitterWidth, Height);
                e.Graphics.DrawLine(pen, x, 0, x, Height);
            }

            if (_showProperties && _properties is not null)
            {
                var x = _properties.Left - SplitterWidth;
                e.Graphics.FillRectangle(brush, x, 0, SplitterWidth, Height);
                e.Graphics.DrawLine(pen, x + SplitterWidth - 1, 0, x + SplitterWidth - 1, Height);
            }
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _dragTarget = HitTestSplitter(e.X);

        if (_dragTarget == DragTarget.None)
        {
            return;
        }

        _dragStartX = e.X;
        _dragStartWidth = _dragTarget == DragTarget.Toolbox ? _toolboxWidth : _propertiesWidth;
        Capture = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_dragTarget != DragTarget.None)
        {
            var delta = e.X - _dragStartX;

            if (_dragTarget == DragTarget.Toolbox)
            {
                _toolboxWidth = _dragStartWidth + delta;
            }
            else
            {
                _propertiesWidth = _dragStartWidth - delta;
            }

            PerformLayout();
            Invalidate();
            return;
        }

        Cursor = HitTestSplitter(e.X) == DragTarget.None ? Cursors.Default : Cursors.SizeWE;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_dragTarget != DragTarget.None)
        {
            _dragTarget = DragTarget.None;
            Capture = false;
        }
    }

    private DragTarget HitTestSplitter(int x)
    {
        if (_showToolbox && _toolbox is not null && _toolbox.Visible)
        {
            var bar = _toolbox.Right;

            if (x >= bar && x < bar + SplitterWidth)
            {
                return DragTarget.Toolbox;
            }
        }

        if (_showProperties && _properties is not null && _properties.Visible)
        {
            var bar = _properties.Left - SplitterWidth;

            if (x >= bar && x < bar + SplitterWidth)
            {
                return DragTarget.Properties;
            }
        }

        return DragTarget.None;
    }

    private void SetPane(ref Control? field, Control? value)
    {
        if (ReferenceEquals(field, value))
        {
            return;
        }

        if (field is not null)
        {
            Controls.Remove(field);
        }

        field = value;

        if (value is not null)
        {
            // The shell positions the panes itself, so they must not be docked.
            value.Dock = DockStyle.None;
            value.Anchor = AnchorStyles.None;
            Controls.Add(value);
        }

        PerformLayout();
        Invalidate();
    }
}
