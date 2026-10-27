using System.Drawing;
using System.Windows.Forms;
using WinFormsDesigner.App.Toolbox;
using WinFormsDesigner.Core.Model;
using WinFormsDesigner.Core.Reflection;

namespace WinFormsDesigner.App.Designer;

/// <summary>
/// Paints the document's live control tree onto a scrollable canvas and applies canvas
/// gestures back to the design session.
/// </summary>
public sealed class DesignerSurface : Panel
{
    private const int CanvasMargin = 24;
    private const int GridSize = 8;
    private const int HandleSize = 7;
    private const int ComponentTrayGap = 12;
    private const int ComponentItemHeight = 22;
    private const int ComponentIconSize = 16;

    private readonly List<int> _verticalGuides = new();
    private readonly List<int> _horizontalGuides = new();
    private readonly DesignCanvas _canvas = new();

    private DesignSession? _session;
    private Bitmap? _renderedForm;
    private Point _formLocation = new(CanvasMargin, CanvasMargin);
    private GestureMode _gestureMode;
    private DesignNode? _gestureNode;
    private Control? _gestureControl;
    private Rectangle _gestureStartBounds;
    private Point _gestureStartPointer;
    private ResizeEdges _resizeEdges;
    private readonly List<(DesignNode Node, Control Control, Rectangle StartBounds)> _gestureGroup = [];

    public DesignerSurface()
    {
        AutoScroll = true;
        AutoScrollMinSize = Size.Empty;
        BackColor = Color.White;
        TabStop = true;
        AllowDrop = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);

        _canvas.Location = Point.Empty;
        _canvas.Size = ClientSize;
        _canvas.AllowDrop = true;
        _canvas.Paint += OnCanvasPaint;
        _canvas.MouseDown += OnCanvasMouseDown;
        _canvas.MouseMove += OnCanvasMouseMove;
        _canvas.MouseUp += OnCanvasMouseUp;
        _canvas.MouseCaptureChanged += OnMouseCaptureChanged;
        _canvas.DragEnter += OnDesignerDragEnter;
        _canvas.DragDrop += OnDesignerDragDrop;
        Controls.Add(_canvas);
    }

    /// <summary>The session whose live form is displayed on the surface.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public DesignSession? Session
    {
        get => _session;
        set
        {
            if (ReferenceEquals(_session, value))
            {
                return;
            }

            if (_session is not null)
            {
                _session.StructureChanged -= OnSessionStructureChanged;
                _session.Changed -= OnSessionChanged;
                _session.SelectionChanged -= OnSessionChanged;
                _session.RootForm.SizeChanged -= OnRootFormSizeChanged;
            }

            _session = value;
            _formLocation = new Point(CanvasMargin, CanvasMargin);
            if (_session is not null)
            {
                _session.RootForm.SizeChanged += OnRootFormSizeChanged;
                _session.StructureChanged += OnSessionStructureChanged;
                _session.Changed += OnSessionChanged;
                _session.SelectionChanged += OnSessionChanged;
            }

            ResizeCanvas();
            RefreshDesignerVisuals();
        }
    }

    /// <summary>Enables snapping dragged control bounds to the design grid.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        private bool _snapToGrid;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool SnapToGrid
    {
        get => _snapToGrid;
        set
        {
            if (_snapToGrid == value)
            {
                return;
            }

            _snapToGrid = value;
            RefreshDesignerVisuals();
        }
    }

    /// <summary>Shows alignment guides while a control is selected.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool ShowAlignmentGuides { get; set; } = true;

    private bool _showRuler = true;
    /// <summary>Shows a pixel ruler along the top and left edges of the canvas.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool ShowRuler
    {
        get => _showRuler;
        set
        {
            if (_showRuler == value)
            {
                return;
            }

            _showRuler = value;
            Invalidate();
        }
    }

    /// <summary>Raised as the pointer moves over the design surface.</summary>
    public event EventHandler<DesignerPointEventArgs>? PointerMoved;

    private bool IsPointerCaptured => _gestureMode != GestureMode.None;

    private void PaintDesign(Graphics graphics)
    {
        if (_session is null)
        {
            return;
        }

        var root = _session.RootForm;
        _session.EnsureHandles();
        if (_renderedForm is null || _renderedForm.Size != root.Size)
        {
            _renderedForm?.Dispose();
            _renderedForm = new Bitmap(root.Width, root.Height);
            root.DrawToBitmap(_renderedForm, new Rectangle(Point.Empty, _renderedForm.Size));
        }

        graphics.DrawImageUnscaled(_renderedForm, _formLocation);

        PaintComponentTray(graphics);

        if (SnapToGrid)
        {
            PaintGrid(graphics);
        }

        if (ShowRuler)
        {
            PaintRuler(graphics);
        }

        if (_session.SelectedNodes.Count > 0)
        {
            foreach (var selectedNode in _session.SelectedNodes)
            {
                if (_session.GetLive(selectedNode) is not { } control)
                {
                    continue;
                }

                var bounds = ControlBoundsOnCanvas(control);
                bounds.Inflate(1, 1);

                var isPrimary = ReferenceEquals(selectedNode, _session.SelectedNode);
                using var selectionPen = new Pen(isPrimary ? Color.RoyalBlue : Color.CornflowerBlue, 1)
                {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Dash,
                };
                graphics.DrawRectangle(selectionPen, bounds);

                var markSize = 8;
                var mark = new Rectangle(bounds.Left + 2, bounds.Top + 2, markSize, markSize);
                using (var markBrush = new SolidBrush(isPrimary ? Color.RoyalBlue : Color.CornflowerBlue))
                using (var markPen = new Pen(Color.White))
                {
                    graphics.FillEllipse(markBrush, mark);
                    graphics.DrawEllipse(markPen, mark);
                }

                if (isPrimary)
                {
                    using var handleBrush = new SolidBrush(Color.White);
                    using var handlePen = new Pen(Color.RoyalBlue);
                    foreach (var handle in GetHandleBounds(bounds))
                    {
                        graphics.FillRectangle(handleBrush, handle);
                        graphics.DrawRectangle(handlePen, handle);
                    }

                    if (ShowAlignmentGuides && IsPointerCaptured && _gestureMode == GestureMode.Move)
                    {
                        PaintAlignmentGuides(graphics);
                    }
                }
            }
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ResizeCanvas();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (!IsPointerCaptured)
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void OnCanvasPaint(object? sender, PaintEventArgs e)
    {
        PaintGrid(e.Graphics, _canvas.ClientRectangle);
        PaintDesign(e.Graphics);
    }

    private void PaintGrid(Graphics graphics, Rectangle bounds)
    {
        using var brush = new SolidBrush(Color.FromArgb(0xD6, 0xD6, 0xD6));
        for (var y = GridSize; y < bounds.Height; y += GridSize * 2)
        {
            for (var x = GridSize; x < bounds.Width; x += GridSize * 2)
            {
                graphics.FillRectangle(brush, x, y, 1, 1);
            }
        }
    }

    private void OnCanvasMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        Focus();
        BeginGesture(e.Location);
    }

    private void OnCanvasMouseMove(object? sender, MouseEventArgs e)
    {
        PointerMoved?.Invoke(this, new DesignerPointEventArgs(PointToClient(_canvas.PointToScreen(e.Location))));
        if (IsPointerCaptured)
        {
            UpdateGesture(e.Location);
        }
        else
        {
            Cursor.Current = CursorAt(e.Location);
        }
    }

    private void OnCanvasMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && IsPointerCaptured)
        {
            EndGesture(e.Location, commit: true);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderedForm?.Dispose();
            if (_session is not null)
            {
                _session.StructureChanged -= OnSessionStructureChanged;
                _session.Changed -= OnSessionChanged;
                _session.SelectionChanged -= OnSessionChanged;
                _session.RootForm.SizeChanged -= OnRootFormSizeChanged;
            }
        }

        base.Dispose(disposing);
    }

    private void BeginGesture(Point canvasPoint)
    {
        if (_session is null)
        {
            return;
        }

        var shiftPressed = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

        var selected = _session.SelectedNode;
        var selectedControl = selected is null ? null : _session.GetLive(selected);
        if (!shiftPressed && selected is not null && selectedControl is not null && _session.SelectedNodes.Count <= 1)
        {
            var edges = GetResizeEdges(ControlBoundsOnCanvas(selectedControl), canvasPoint);
            if (edges != ResizeEdges.None)
            {
                StartGesture(selected, selectedControl, edges, canvasPoint);
                return;
            }
        }

        var formPoint = new Point(canvasPoint.X - _formLocation.X, canvasPoint.Y - _formLocation.Y);

        // Select non-visual components from the component tray.
        foreach (var (componentNode, componentBounds) in GetComponentItems())
        {
            if (!componentBounds.Contains(canvasPoint))
            {
                continue;
            }

            if (shiftPressed)
            {
                _session.ToggleSelection(componentNode);
            }
            else if (!_session.SelectedNodes.Contains(componentNode))
            {
                _session.Select(componentNode);
            }

            return;
        }
        var hit = _session.HitTest(formPoint);
        if (hit is null && _session.RootForm.ClientRectangle.Contains(formPoint))
        {
            hit = _session.Document.Root;
        }

        if (hit is null)
        {
            if (!shiftPressed)
            {
                _session.ClearSelection();
            }
            return;
        }

        if (shiftPressed)
        {
            _session.ToggleSelection(hit);
            return;
        }

        if (!_session.SelectedNodes.Contains(hit))
        {
            _session.Select(hit);
        }

        var hitControl = _session.GetLive(hit);
        if (hit.Parent is not null && hitControl is not null)
        {
            StartGesture(hit, hitControl, ResizeEdges.None, canvasPoint);
        }
        else if (hitControl is not null)
        {
            var rootEdges = GetResizeEdges(ControlBoundsOnCanvas(hitControl), canvasPoint);
            StartGesture(hit, hitControl, rootEdges, canvasPoint);
        }
        else
        {
            Cursor.Current = CursorAt(canvasPoint);
        }
    }

    private void StartGesture(DesignNode node, Control control, ResizeEdges edges, Point canvasPoint)
    {
        _gestureGroup.Clear();
        _gestureNode = node;
        _gestureControl = control;
        _gestureStartBounds = node.Parent is null
            ? new Rectangle(_formLocation, control.Size)
            : control.Bounds;
        _gestureStartPointer = canvasPoint;
        _resizeEdges = edges;
        _gestureMode = edges == ResizeEdges.None ? GestureMode.Move : GestureMode.Resize;
        _canvas.Capture = true;
        Cursor = CursorFor(edges);
        Cursor.Current = Cursor;

        if (_gestureMode == GestureMode.Move && _session is not null && _session.SelectedNodes.Count > 1
            && _session.SelectedNodes.Contains(node))
        {
            foreach (var selectedNode in _session.SelectedNodes)
            {
                if (selectedNode.Parent is null)
                {
                    continue;
                }

                if (_session.GetLive(selectedNode) is Control live)
                {
                    _gestureGroup.Add((selectedNode, live, live.Bounds));
                }
            }

            if (_gestureGroup.Count <= 1)
            {
                _gestureGroup.Clear();
            }
        }
    }

    private void UpdateGesture(Point canvasPoint)
    {
        if (_gestureControl is null)
        {
            return;
        }

        if (_gestureGroup.Count > 0 && _gestureMode == GestureMode.Move)
        {
            _verticalGuides.Clear();
            _horizontalGuides.Clear();
            var groupDelta = new Size(canvasPoint.X - _gestureStartPointer.X, canvasPoint.Y - _gestureStartPointer.Y);
            foreach (var groupItem in _gestureGroup)
            {
                var groupBounds = groupItem.StartBounds;
                groupBounds.Offset(groupDelta.Width, groupDelta.Height);
                if (SnapToGrid)
                {
                    groupBounds.X = Snap(groupBounds.X);
                    groupBounds.Y = Snap(groupBounds.Y);
                }

                groupItem.Control.Bounds = groupBounds;
            }

            InvalidateRenderedForm();
            _canvas.Invalidate();
            return;
        }

        var delta = new Size(canvasPoint.X - _gestureStartPointer.X, canvasPoint.Y - _gestureStartPointer.Y);
        var bounds = _gestureStartBounds;

        if (_gestureMode == GestureMode.Move)
        {
            bounds.Offset(new Point(delta.Width, delta.Height));

            _verticalGuides.Clear();
            _horizontalGuides.Clear();

            if (ShowAlignmentGuides && _session is not null && _gestureNode is not null && _gestureNode.Parent is not null)
            {
                ApplyAlignmentGuides(bounds, _gestureNode, ref bounds);
            }
            else if (SnapToGrid)
            {
                bounds.X = Snap(bounds.X);
                bounds.Y = Snap(bounds.Y);
            }

            if (ReferenceEquals(_gestureControl, _session?.RootForm))
            {
                bounds.X = Math.Max(CanvasMargin, bounds.X);
                bounds.Y = Math.Max(CanvasMargin, bounds.Y);
            }
        }
        else
        {
            if ((_resizeEdges & ResizeEdges.Left) != 0)
            {
                bounds.X += delta.Width;
                bounds.Width -= delta.Width;
            }

            if ((_resizeEdges & ResizeEdges.Top) != 0)
            {
                bounds.Y += delta.Height;
                bounds.Height -= delta.Height;
            }

            if ((_resizeEdges & ResizeEdges.Right) != 0)
            {
                bounds.Width += delta.Width;
            }

            if ((_resizeEdges & ResizeEdges.Bottom) != 0)
            {
                bounds.Height += delta.Height;
            }

            var minimumSize = _gestureControl.MinimumSize;
            minimumSize.Width = Math.Max(1, minimumSize.Width);
            minimumSize.Height = Math.Max(1, minimumSize.Height);
            if (bounds.Width < minimumSize.Width)
            {
                if ((_resizeEdges & ResizeEdges.Left) != 0)
                {
                    bounds.X -= minimumSize.Width - bounds.Width;
                }

                bounds.Width = minimumSize.Width;
            }

            if (bounds.Height < minimumSize.Height)
            {
                if ((_resizeEdges & ResizeEdges.Top) != 0)
                {
                    bounds.Y -= minimumSize.Height - bounds.Height;
                }

                bounds.Height = minimumSize.Height;
            }

            if (SnapToGrid)
            {
                bounds.Width = Math.Max(minimumSize.Width, Snap(bounds.Width));
                bounds.Height = Math.Max(minimumSize.Height, Snap(bounds.Height));
                if ((_resizeEdges & ResizeEdges.Left) != 0)
                {
                    bounds.X = _gestureStartBounds.Right - bounds.Width;
                }

                if ((_resizeEdges & ResizeEdges.Top) != 0)
                {
                    bounds.Y = _gestureStartBounds.Bottom - bounds.Height;
                }
            }
        }

        var control = _gestureControl;
        var oldCanvasBounds = ControlBoundsOnCanvas(control);
        if (ReferenceEquals(control, _session?.RootForm))
        {
            if (_gestureMode == GestureMode.Move)
            {
                _formLocation = bounds.Location;
                ResizeCanvas();
            }
            else
            {
                control.ClientSize = bounds.Size;
            }
        }
        else
        {
            control.Bounds = bounds;
        }
        var newCanvasBounds = ControlBoundsOnCanvas(control);

        var dirtyBounds = Rectangle.Union(oldCanvasBounds, newCanvasBounds);
        dirtyBounds.Inflate(HandleSize + 2, HandleSize + 2);
        InvalidateRenderedForm();
        if (_gestureMode == GestureMode.Move && ShowAlignmentGuides && _gestureNode?.Parent is not null)
        {
            _canvas.Invalidate();
        }
        else
        {
            _canvas.Invalidate(dirtyBounds);
        }
    }

    private void EndGesture(Point canvasPoint, bool commit)
    {
        if (commit)
        {
            UpdateGesture(canvasPoint);
        }

        var group = _gestureGroup.ToList();
        _gestureGroup.Clear();

        var node = _gestureNode;
        var control = _gestureControl;
        var gestureMode = _gestureMode;
        var finalBounds = control is not null && ReferenceEquals(control, _session?.RootForm)
            ? new Rectangle(_formLocation, control.Size)
            : control?.Bounds ?? Rectangle.Empty;
        var originalBounds = _gestureStartBounds;

        _gestureMode = GestureMode.None;
        _verticalGuides.Clear();
        _horizontalGuides.Clear();
        _gestureNode = null;
        _gestureControl = null;
        _resizeEdges = ResizeEdges.None;
        _canvas.Capture = false;
        Cursor = Cursors.Default;
        Cursor.Current = Cursors.Default;

        if (group.Count > 0)
        {
            if (commit && _session is not null && gestureMode == GestureMode.Move)
            {
                _session.PushUndo();
                foreach (var groupItem in group)
                {
                    _session.CommitBounds(groupItem.Node, groupItem.StartBounds, groupItem.Control.Bounds, pushUndo: false);
                }
            }
            else
            {
                foreach (var groupItem in group)
                {
                    groupItem.Control.Bounds = groupItem.StartBounds;
                }
            }
        }
        else if (control is not null)
        {
            if (commit && node is not null && _session is not null
                && !(ReferenceEquals(control, _session.RootForm) && gestureMode == GestureMode.Move))
            {
                _session.CommitBounds(node, originalBounds, finalBounds);
            }
            else
            {
                if (ReferenceEquals(control, _session?.RootForm))
                {
                    if (commit)
                    {
                        control.ClientSize = finalBounds.Size;
                    }
                    else
                    {
                        _formLocation = originalBounds.Location;
                        control.ClientSize = originalBounds.Size;
                    }
                }
                else
                {
                    control.Bounds = originalBounds;
                }
            }
        }

        RefreshDesignerVisuals();
    }

    private void OnMouseCaptureChanged(object? sender, EventArgs e)
    {
        if (!_canvas.Capture && IsPointerCaptured)
        {
            EndGesture(_canvas.PointToClient(MousePosition), commit: false);
        }
    }

    private Cursor CursorAt(Point surfacePoint)
    {
        foreach (var (_, bounds) in GetComponentItems())
        {
            if (bounds.Contains(surfacePoint))
            {
                return Cursors.Default;
            }
        }
        if (_session?.SelectedNode is { } selected && _session.GetLive(selected) is { } control)
        {
            var edges = GetResizeEdges(ControlBoundsOnCanvas(control), surfacePoint);
            if (edges != ResizeEdges.None)
            {
                return CursorFor(edges);
            }
        }

        return Cursors.Default;
    }

    private Cursor CursorFor(ResizeEdges edges)
    {
        if (edges is ResizeEdges.Left or ResizeEdges.Right)
        {
            return Cursors.SizeWE;
        }

        if (edges is ResizeEdges.Top or ResizeEdges.Bottom)
        {
            return Cursors.SizeNS;
        }

        if (edges is ResizeEdges.LeftTop or ResizeEdges.RightBottom)
        {
            return Cursors.SizeNWSE;
        }

        if (edges is ResizeEdges.RightTop or ResizeEdges.LeftBottom)
        {
            return Cursors.SizeNESW;
        }

        return _gestureMode == GestureMode.Move ? Cursors.SizeAll : Cursors.Default;
    }

    private ResizeEdges GetResizeEdges(Rectangle bounds, Point point)
    {
        const int hitSize = 11;
        var handles = GetHandleBounds(bounds).ToArray();
        ResizeEdges[] edges =
        [
            ResizeEdges.LeftTop,
            ResizeEdges.Top,
            ResizeEdges.RightTop,
            ResizeEdges.Right,
            ResizeEdges.RightBottom,
            ResizeEdges.Bottom,
            ResizeEdges.LeftBottom,
            ResizeEdges.Left,
        ];

        for (var index = 0; index < handles.Length; index++)
        {
            var handle = handles[index];
            handle.Inflate((hitSize - HandleSize) / 2, (hitSize - HandleSize) / 2);
            if (handle.Contains(point))
            {
                return edges[index];
            }


        }

        return ResizeEdges.None;
    }

    private static IEnumerable<Rectangle> GetHandleBounds(Rectangle bounds)
    {
        var half = HandleSize / 2;
        yield return new Rectangle(bounds.Left - half, bounds.Top - half, HandleSize, HandleSize);
        yield return new Rectangle(bounds.Left + bounds.Width / 2 - half, bounds.Top - half, HandleSize, HandleSize);
        yield return new Rectangle(bounds.Right - half, bounds.Top - half, HandleSize, HandleSize);
        yield return new Rectangle(bounds.Right - half, bounds.Top + bounds.Height / 2 - half, HandleSize, HandleSize);
        yield return new Rectangle(bounds.Right - half, bounds.Bottom - half, HandleSize, HandleSize);
        yield return new Rectangle(bounds.Left + bounds.Width / 2 - half, bounds.Bottom - half, HandleSize, HandleSize);
        yield return new Rectangle(bounds.Left - half, bounds.Bottom - half, HandleSize, HandleSize);
        yield return new Rectangle(bounds.Left - half, bounds.Top + bounds.Height / 2 - half, HandleSize, HandleSize);
    }

    private Rectangle ControlBoundsOnCanvas(Control control)
    {
        var x = _formLocation.X;
        var y = _formLocation.Y;
        for (var current = control; current is not null && !ReferenceEquals(current, _session?.RootForm); current = current.Parent)
        {
            x += current.Left;
            y += current.Top;
        }

        return new Rectangle(x, y, control.Width, control.Height);
    }

    private List<(DesignNode Node, Rectangle Bounds)> GetComponentItems()
    {
        var result = new List<(DesignNode, Rectangle)>();
        if (_session is null)
        {
            return result;
        }

        var components = _session.Document.AllNodes()
            .Where(node => ControlFactory.IsComponent(node.ControlType))
            .ToList();
        if (components.Count == 0)
        {
            return result;
        }

        var root = _session.RootForm;
        var font = new Font(SystemFonts.DefaultFont.FontFamily, 8f);
        int x = _formLocation.X;
        int y = _formLocation.Y + root.Height + ComponentTrayGap;
        int maxX = _formLocation.X + Math.Max(root.Width, 160);

        foreach (var node in components)
        {
            var text = node.Name;
            int textWidth = TextRenderer.MeasureText(text, font).Width;
            int width = ComponentIconSize + 4 + textWidth + 10;
            if (width < 72)
            {
                width = 72;
            }

            if (x + width > maxX && x > _formLocation.X)
            {
                x = _formLocation.X;
                y += ComponentItemHeight;
            }

            result.Add((node, new Rectangle(x, y, width, ComponentItemHeight)));
            x += width + 6;
        }

        font.Dispose();
        return result;
    }

    private int GetComponentTrayHeight()
    {
        var items = GetComponentItems();
        if (items.Count == 0)
        {
            return 0;
        }

        int top = items.Min(item => item.Bounds.Top);
        int bottom = items.Max(item => item.Bounds.Bottom);
        return bottom - top + ComponentTrayGap;
    }

    private void PaintComponentTray(Graphics graphics)
    {
        var items = GetComponentItems();
        if (items.Count == 0)
        {
            return;
        }

        using var trayBrush = new SolidBrush(Color.FromArgb(245, 245, 245));
        using var borderPen = new Pen(Color.FromArgb(200, 200, 200));
        using var textBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
        using var font = new Font(SystemFonts.DefaultFont.FontFamily, 8f);
        using var selectionPen = new Pen(Color.RoyalBlue, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };

        int top = items.Min(item => item.Bounds.Top) - 4;
        int bottom = items.Max(item => item.Bounds.Bottom) + 4;
        int left = _formLocation.X;
        int right = Math.Max(items.Max(item => item.Bounds.Right), _formLocation.X + (_session?.RootForm.Width ?? 0));
        var trayRect = new Rectangle(left - 4, top, right - left + 8, bottom - top);
        graphics.FillRectangle(trayBrush, trayRect);
        graphics.DrawRectangle(borderPen, trayRect);

        foreach (var (node, bounds) in items)
        {
            var entry = ControlTypeRegistry.Find(node.ControlType);
            var glyphKey = entry?.Glyph ?? node.ControlType.Name.ToLowerInvariant();
            var icon = GlyphFactory.GetGlyph(glyphKey);
            graphics.DrawImage(icon, bounds.X, bounds.Y + (bounds.Height - ComponentIconSize) / 2);

            var textRect = new Rectangle(bounds.X + ComponentIconSize + 4, bounds.Y, bounds.Width - ComponentIconSize - 4, bounds.Height);
            TextRenderer.DrawText(graphics, node.Name, font, textRect, textBrush.Color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            if (_session is not null && _session.SelectedNodes.Contains(node))
            {
                graphics.DrawRectangle(selectionPen, bounds);
            }
        }
    }

    private int Snap(int value)
        => (int)Math.Round(value / (double)GridSize, MidpointRounding.AwayFromZero) * GridSize;

    private void PaintRuler(Graphics graphics)
    {
        var width = _canvas.Width;
        var height = _canvas.Height;
        var margin = CanvasMargin;

        using var background = new SolidBrush(Color.FromArgb(245, 245, 245));
        using var borderPen = new Pen(Color.FromArgb(170, 170, 170));
        using var tickPen = new Pen(Color.FromArgb(120, 120, 120));
        using var textBrush = new SolidBrush(Color.FromArgb(80, 80, 80));
        using var font = new Font(SystemFonts.DefaultFont.FontFamily, 7f);

        graphics.FillRectangle(background, 0, 0, width, margin);
        graphics.FillRectangle(background, 0, 0, margin, height);
        graphics.DrawLine(borderPen, 0, margin - 1, width, margin - 1);
        graphics.DrawLine(borderPen, margin - 1, 0, margin - 1, height);

        for (var x = 0; x <= width; x += 10)
        {
            var major = x % 50 == 0;
            var tickHeight = major ? margin : margin / 2;
            graphics.DrawLine(tickPen, x, margin - tickHeight, x, margin - 1);
            if (major && x > 0)
            {
                graphics.DrawString(x.ToString(), font, textBrush, x + 2, 2);
            }
        }

        for (var y = 0; y <= height; y += 10)
        {
            var major = y % 50 == 0;
            var tickWidth = major ? margin : margin / 2;
            graphics.DrawLine(tickPen, margin - tickWidth, y, margin - 1, y);
            if (major && y > 0)
            {
                graphics.DrawString(y.ToString(), font, textBrush, 2, y + 2);
            }
        }
    }


    private void ApplyAlignmentGuides(Rectangle currentBounds, DesignNode selected, ref Rectangle bounds)
    {
        if (_session is null || selected.Parent is null)
        {
            return;
        }

        const int threshold = 6;

        var parentLive = _session.GetLive(selected.Parent);
        var parentOrigin = parentLive is null ? Point.Empty : ControlBoundsOnCanvas(parentLive).Location;

        var guideCandidatesX = new List<int>();
        var guideCandidatesY = new List<int>();

        foreach (var sibling in selected.Parent.Children)
        {
            if (ReferenceEquals(sibling, selected))
            {
                continue;
            }

            if (_session.GetLive(sibling) is not { } siblingControl)
            {
                continue;
            }

            var siblingBounds = ControlBoundsOnCanvas(siblingControl);
            siblingBounds.Offset(-parentOrigin.X, -parentOrigin.Y);
            guideCandidatesX.Add(siblingBounds.Left);
            guideCandidatesX.Add(siblingBounds.Right);
            guideCandidatesX.Add((siblingBounds.Left + siblingBounds.Right) / 2);
            guideCandidatesY.Add(siblingBounds.Top);
            guideCandidatesY.Add(siblingBounds.Bottom);
            guideCandidatesY.Add((siblingBounds.Top + siblingBounds.Bottom) / 2);
        }

        if (selected.Parent == _session.Document.Root)
        {
            var formBounds = new Rectangle(Point.Empty, _session.RootForm.Size);
            guideCandidatesX.Add(formBounds.Left);
            guideCandidatesX.Add(formBounds.Right);
            guideCandidatesX.Add((formBounds.Left + formBounds.Right) / 2);
            guideCandidatesY.Add(formBounds.Top);
            guideCandidatesY.Add(formBounds.Bottom);
            guideCandidatesY.Add((formBounds.Top + formBounds.Bottom) / 2);
        }

        // X axis
        {
            int bestDiff = threshold + 1;
            int guide = currentBounds.Left;
            int newX = currentBounds.Left;

            foreach (var candidate in guideCandidatesX)
            {
                var diffLeft = Math.Abs(candidate - currentBounds.Left);
                if (diffLeft < bestDiff)
                {
                    bestDiff = diffLeft;
                    guide = candidate;
                    newX = candidate;
                }

                var diffRight = Math.Abs(candidate - currentBounds.Right);
                if (diffRight < bestDiff)
                {
                    bestDiff = diffRight;
                    guide = candidate;
                    newX = candidate - currentBounds.Width;
                }

                var center = currentBounds.Left + (currentBounds.Width / 2);
                var diffCenter = Math.Abs(candidate - center);
                if (diffCenter < bestDiff)
                {
                    bestDiff = diffCenter;
                    guide = candidate;
                    newX = candidate - (currentBounds.Width / 2);
                }
            }

            if (bestDiff <= threshold)
            {
                bounds.X = newX;
                _verticalGuides.Add(guide + parentOrigin.X);
            }
            else if (SnapToGrid)
            {
                bounds.X = Snap(bounds.X);
            }
        }

        // Y axis
        {
            int bestDiff = threshold + 1;
            int guide = currentBounds.Top;
            int newY = currentBounds.Top;

            foreach (var candidate in guideCandidatesY)
            {
                var diffTop = Math.Abs(candidate - currentBounds.Top);
                if (diffTop < bestDiff)
                {
                    bestDiff = diffTop;
                    guide = candidate;
                    newY = candidate;
                }

                var diffBottom = Math.Abs(candidate - currentBounds.Bottom);
                if (diffBottom < bestDiff)
                {
                    bestDiff = diffBottom;
                    guide = candidate;
                    newY = candidate - currentBounds.Height;
                }

                var center = currentBounds.Top + (currentBounds.Height / 2);
                var diffCenter = Math.Abs(candidate - center);
                if (diffCenter < bestDiff)
                {
                    bestDiff = diffCenter;
                    guide = candidate;
                    newY = candidate - (currentBounds.Height / 2);
                }
            }

            if (bestDiff <= threshold)
            {
                bounds.Y = newY;
                _horizontalGuides.Add(guide + parentOrigin.Y);
            }
            else if (SnapToGrid)
            {
                bounds.Y = Snap(bounds.Y);
            }
        }
    }


    private void PaintAlignmentGuides(Graphics graphics)
    {
        using var pen = new Pen(Color.FromArgb(180, Color.DarkOrange), 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
        foreach (var x in _verticalGuides)
        {
            graphics.DrawLine(pen, x, 0, x, _canvas.Height);
        }

        foreach (var y in _horizontalGuides)
        {
            graphics.DrawLine(pen, 0, y, _canvas.Width, y);
        }
    }

    private void PaintGrid(Graphics graphics)
    {
        using var brush = new SolidBrush(Color.FromArgb(45, 128, 128, 128));
        var width = _canvas.Width;
        var height = _canvas.Height;
        for (var x = 0; x < width; x += GridSize)
        {
            for (var y = 0; y < height; y += GridSize)
            {
                graphics.FillRectangle(brush, x, y, 1, 1);
            }
        }
    }


    private void ResizeCanvas()
    {
        var root = _session?.RootForm;
        var trayHeight = GetComponentTrayHeight();
        var width = Math.Max(ClientSize.Width, _formLocation.X + (root?.Width ?? 0) + CanvasMargin);
        var height = Math.Max(ClientSize.Height, _formLocation.Y + (root?.Height ?? 0) + trayHeight + ComponentTrayGap + CanvasMargin);
        _canvas.Size = new Size(Math.Max(1, width), Math.Max(1, height));
        AutoScrollMinSize = _canvas.Size;
        RefreshDesignerVisuals();
    }

    private void RefreshDesignerVisuals()
    {
        InvalidateRenderedForm();
        _canvas.Invalidate();
    }

    private void InvalidateRenderedForm()
    {
        _renderedForm?.Dispose();
        _renderedForm = null;
    }

    private void OnDesignerDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = GetDraggedControlType(e.Data) is null ? DragDropEffects.None : DragDropEffects.Copy;
    }

    private void OnDesignerDragDrop(object? sender, DragEventArgs e)
    {
        if (_session is null || GetDraggedControlType(e.Data) is not { } controlType)
        {
            return;
        }

        var screenPoint = new Point(e.X, e.Y);
        var canvasPoint = _canvas.PointToClient(screenPoint);
        var formPoint = new Point(canvasPoint.X - CanvasMargin, canvasPoint.Y - CanvasMargin);
        var hitNode = _session.HitTest(formPoint);
        var parentNode = hitNode is not null && CanContainControls(hitNode.ControlType)
            ? hitNode
            : hitNode?.Parent ?? _session.Document.Root;
        var location = formPoint;
        for (var current = parentNode; current is not null && !ReferenceEquals(current, _session.Document.Root); current = current.Parent)
        {
            if (_session.GetLive(current) is { } parent)
            {
                location.Offset(-parent.Left, -parent.Top);
            }
        }

        _session.Select(parentNode);
        _session.AddControl(controlType, new Point(Math.Max(0, location.X), Math.Max(0, location.Y)));
        Focus();
        RefreshDesignerVisuals();
    }

    private static Type? GetDraggedControlType(IDataObject? data)
    {
        if (data?.GetDataPresent(ToolboxDrag.DataFormat) != true
            || data.GetData(ToolboxDrag.DataFormat) is not Type type
            || !ControlFactory.IsCreatable(type)
            || (!typeof(Control).IsAssignableFrom(type) && !ControlFactory.IsComponent(type)))
        {
            return null;
        }

        return type;
    }

    private static bool CanContainControls(Type type)
        => typeof(ContainerControl).IsAssignableFrom(type)
            || typeof(Panel).IsAssignableFrom(type)
            || typeof(GroupBox).IsAssignableFrom(type)
            || typeof(TabPage).IsAssignableFrom(type)
            || typeof(TableLayoutPanel).IsAssignableFrom(type)
            || typeof(FlowLayoutPanel).IsAssignableFrom(type)
            || typeof(SplitContainer).IsAssignableFrom(type);

    private void OnRootFormSizeChanged(object? sender, EventArgs e)
    {
        InvalidateRenderedForm();
        ResizeCanvas();
    }

    private void OnSessionStructureChanged(object? sender, EventArgs e)
    {
        InvalidateRenderedForm();
        ResizeCanvas();
    }

    private void OnSessionChanged(object? sender, EventArgs e) => RefreshDesignerVisuals();

    private enum GestureMode
    {
        None,
        Move,
        Resize,
    }

    [Flags]
    private enum ResizeEdges
    {
        None = 0,
        Left = 1,
        Top = 2,
        Right = 4,
        Bottom = 8,
        LeftTop = Left | Top,
        RightTop = Right | Top,
        RightBottom = Right | Bottom,
        LeftBottom = Left | Bottom,
    }

    private sealed class DesignCanvas : Control
    {
        public DesignCanvas()
        {
            BackColor = Color.White;
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
                true);
        }
    }
}

/// <summary>Pointer coordinates reported by the designer surface.</summary>
public sealed class DesignerPointEventArgs : EventArgs
{
    public DesignerPointEventArgs(Point location)
    {
        Location = location;
    }

    public Point Location { get; }
}
