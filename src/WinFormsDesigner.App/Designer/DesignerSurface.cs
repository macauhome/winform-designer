using System.Drawing;
using System.Windows.Forms;
using WinFormsDesigner.App.Toolbox;
using WinFormsDesigner.Core.Model;
using WinFormsDesigner.Core.Reflection;

namespace WinFormsDesigner.App.Designer;

/// <summary>
/// Hosts the live form, routes input from its child controls, and draws the design
/// adornments over the form.
/// </summary>
public sealed class DesignerSurface : Panel
{
    private const int CanvasMargin = 24;
    private const int GridSize = 8;
    private const int HandleSize = 7;

    private readonly CanvasPanel _canvas = new();
    private readonly AdornmentLayer _adornments = new();
    private readonly HashSet<Control> _dropTargets = [];

    private DesignSession? _session;
    private GestureMode _gestureMode;
    private DesignNode? _gestureNode;
    private Control? _gestureControl;
    private Rectangle _gestureStartBounds;
    private Point _gestureStartPointer;
    private ResizeEdges _resizeEdges;

    public DesignerSurface()
    {
        AutoScroll = true;
        AutoScrollMinSize = Size.Empty;
        BackColor = SystemColors.Control;
        TabStop = true;
        AllowDrop = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);

        _canvas.Location = Point.Empty;
        _canvas.Size = ClientSize;
        _canvas.AllowDrop = true;

        _adornments.Location = Point.Empty;
        _adornments.Size = _canvas.ClientSize;
        _adornments.Surface = this;
        _adornments.AllowDrop = true;

        _canvas.Controls.Add(_adornments);
        Controls.Add(_canvas);
        _canvas.BringToFront();

        MouseCaptureChanged += OnMouseCaptureChanged;
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

            RemoveDropTargets();
            if (_session is not null)
            {
                _session.StructureChanged -= OnSessionStructureChanged;
                _session.Changed -= OnSessionChanged;
                _session.SelectionChanged -= OnSessionChanged;
                _session.RootForm.SizeChanged -= OnRootFormSizeChanged;
                _canvas.Controls.Remove(_session.RootForm);
            }

            _session = value;
            if (_session is not null)
            {
                _session.RootForm.Location = new Point(CanvasMargin, CanvasMargin);
                _session.RootForm.SizeChanged += OnRootFormSizeChanged;
                _session.StructureChanged += OnSessionStructureChanged;
                _session.Changed += OnSessionChanged;
                _session.SelectionChanged += OnSessionChanged;
                _canvas.Controls.Add(_session.RootForm);
                _session.RootForm.BringToFront();
            }

            _canvas.Controls.SetChildIndex(_adornments, 0);
            ResizeCanvas();
            ConfigureDropTargets();
            RefreshDesignerVisuals();
        }
    }

    /// <summary>Enables snapping dragged control bounds to the design grid.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool SnapToGrid { get; set; }

    /// <summary>Shows alignment guides while a control is selected.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool ShowAlignmentGuides { get; set; } = true;

    /// <summary>Raised as the pointer moves over the design surface.</summary>
    public event EventHandler<DesignerPointEventArgs>? PointerMoved;

    private bool IsPointerCaptured => _gestureMode != GestureMode.None;

    internal void PaintAdornments(Graphics graphics)
    {
        if (_session is null || _session.SelectedNode is not { } selected)
        {
            return;
        }

        var control = _session.GetLive(selected);
        if (control is null || control.IsDisposed)
        {
            return;
        }

        var bounds = ControlBoundsOnCanvas(control);
        bounds.Inflate(1, 1);

        using var selectionPen = new Pen(Color.RoyalBlue, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        graphics.DrawRectangle(selectionPen, bounds);

        using var handleBrush = new SolidBrush(Color.White);
        using var handlePen = new Pen(Color.RoyalBlue);
        foreach (var handle in GetHandleBounds(bounds))
        {
            graphics.FillRectangle(handleBrush, handle);
            graphics.DrawRectangle(handlePen, handle);
        }

        if (ShowAlignmentGuides && IsPointerCaptured && _gestureMode == GestureMode.Move)
        {
            PaintAlignmentGuides(graphics, bounds, selected);
        }
    }

    internal void PaintScrollBars(Graphics graphics)
    {
        // The surface uses the native scroll bars provided by ScrollableControl.
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            RemoveDropTargets();
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

    private void BeginGesture(Point surfacePoint)
    {
        if (_session is null)
        {
            return;
        }

        var selected = _session.SelectedNode;
        var selectedControl = selected is null ? null : _session.GetLive(selected);
        if (selected is not null && selectedControl is not null)
        {
            var edges = GetResizeEdges(ControlBoundsOnCanvas(selectedControl), ToCanvasPoint(surfacePoint));
            if (edges != ResizeEdges.None)
            {
                StartGesture(selected, selectedControl, edges, surfacePoint);
                return;
            }
        }

        var formPoint = _session.RootForm.PointToClient(PointToScreen(surfacePoint));
        var hit = _session.HitTest(formPoint);
        if (hit is null && _session.RootForm.ClientRectangle.Contains(formPoint))
        {
            hit = _session.Document.Root;
        }

        if (hit is null)
        {
            _session.Select(null);
            return;
        }

        _session.Select(hit);

        var hitControl = _session.GetLive(hit);
        if (hit.Parent is not null && hitControl is not null)
        {
            StartGesture(hit, hitControl, ResizeEdges.None, surfacePoint);
        }
        else if (hitControl is not null)
        {
            var rootEdges = GetResizeEdges(ControlBoundsOnCanvas(hitControl), ToCanvasPoint(surfacePoint));
            if (rootEdges != ResizeEdges.None)
            {
                StartGesture(hit, hitControl, rootEdges, surfacePoint);
            }
        }
        else
        {
            Cursor.Current = CursorAt(surfacePoint);
        }
    }

    private void StartGesture(DesignNode node, Control control, ResizeEdges edges, Point surfacePoint)
    {
        _gestureNode = node;
        _gestureControl = control;
        _gestureStartBounds = control.Bounds;
        _gestureStartPointer = ToCanvasPoint(surfacePoint);
        _resizeEdges = edges;
        _gestureMode = edges == ResizeEdges.None ? GestureMode.Move : GestureMode.Resize;
        Capture = true;
        Cursor = CursorFor(edges);
        Cursor.Current = Cursor;
    }

    private void UpdateGesture(Point canvasPoint)
    {
        if (_gestureControl is null)
        {
            return;
        }

        var delta = new Size(canvasPoint.X - _gestureStartPointer.X, canvasPoint.Y - _gestureStartPointer.Y);
        var bounds = _gestureStartBounds;

        if (_gestureMode == GestureMode.Move)
        {
            bounds.Offset(new Point(delta.Width, delta.Height));
            if (SnapToGrid)
            {
                bounds.X = Snap(bounds.X);
                bounds.Y = Snap(bounds.Y);
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
        var parent = control.Parent;
        var previousBounds = control.Bounds;
        var oldCanvasBounds = ControlBoundsOnCanvas(control);
        control.Bounds = bounds;
        var newCanvasBounds = ControlBoundsOnCanvas(control);

        var dirtyBounds = Rectangle.Union(oldCanvasBounds, newCanvasBounds);
        dirtyBounds.Inflate(HandleSize + 2, HandleSize + 2);
        _canvas.Invalidate(dirtyBounds, invalidateChildren: true);
        _adornments.Invalidate(dirtyBounds);
        if (parent is not null)
        {
            parent.Invalidate(Rectangle.Union(previousBounds, bounds), invalidateChildren: true);
            parent.Update();
        }

        control.Invalidate(invalidateChildren: true);
        control.Update();
        _adornments.Update();
    }

    private void EndGesture(Point surfacePoint, bool commit)
    {
        if (commit)
        {
            UpdateGesture(ToCanvasPoint(surfacePoint));
        }

        var node = _gestureNode;
        var control = _gestureControl;
        var finalBounds = control?.Bounds ?? Rectangle.Empty;
        var originalBounds = _gestureStartBounds;

        _gestureMode = GestureMode.None;
        _gestureNode = null;
        _gestureControl = null;
        _resizeEdges = ResizeEdges.None;
        Capture = false;
        Cursor = Cursors.Default;
        Cursor.Current = Cursors.Default;

        if (control is not null)
        {
            if (commit && node is not null && _session is not null)
            {
                _session.CommitBounds(node, originalBounds, finalBounds);
            }
            else
            {
                control.Bounds = originalBounds;
            }
        }

        RefreshDesignerVisuals();
    }

    private void OnMouseCaptureChanged(object? sender, EventArgs e)
    {
        if (!Capture && IsPointerCaptured)
        {
            EndGesture(PointToClient(MousePosition), commit: false);
        }
    }

    private void HandlePointerMove(Point surfacePoint)
    {
        PointerMoved?.Invoke(this, new DesignerPointEventArgs(surfacePoint));

        if (IsPointerCaptured)
        {
            UpdateGesture(ToCanvasPoint(surfacePoint));
        }
        else
        {
            Cursor.Current = CursorAt(surfacePoint);
        }
    }

    private void OnDesignerMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || sender is not Control target)
        {
            return;
        }

        var surfacePoint = PointToClient(target.PointToScreen(e.Location));
        BeginGesture(surfacePoint);
    }

    private void OnDesignerMouseMove(object? sender, MouseEventArgs e)
    {
        if (sender is not Control target)
        {
            return;
        }

        HandlePointerMove(PointToClient(target.PointToScreen(e.Location)));
    }

    private void OnDesignerMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || !IsPointerCaptured || sender is not Control target)
        {
            return;
        }

        var surfacePoint = PointToClient(target.PointToScreen(e.Location));
        EndGesture(surfacePoint, commit: true);
    }

    private Cursor CursorAt(Point surfacePoint)
    {
        if (_session?.SelectedNode is { } selected && _session.GetLive(selected) is { } control)
        {
            var edges = GetResizeEdges(ControlBoundsOnCanvas(control), ToCanvasPoint(surfacePoint));
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
        var parent = control.Parent;
        var location = parent is null
            ? _canvas.PointToClient(control.PointToScreen(Point.Empty))
            : _canvas.PointToClient(parent.PointToScreen(control.Location));
        return new Rectangle(location, control.Size);
    }

    private Point ToCanvasPoint(Point surfacePoint)
        => _canvas.PointToClient(PointToScreen(surfacePoint));

    private int Snap(int value)
        => (int)Math.Round(value / (double)GridSize, MidpointRounding.AwayFromZero) * GridSize;

    private void PaintAlignmentGuides(Graphics graphics, Rectangle selectedBounds, DesignNode selected)
    {
        if (_session is null || selected.Parent is null)
        {
            return;
        }

        using var pen = new Pen(Color.FromArgb(180, Color.DarkOrange), 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
        foreach (var sibling in selected.Parent.Children)
        {
            if (ReferenceEquals(sibling, selected) || _session.GetLive(sibling) is not { } control)
            {
                continue;
            }

            var siblingBounds = ControlBoundsOnCanvas(control);
            if (selectedBounds.Left == siblingBounds.Left || selectedBounds.Right == siblingBounds.Right)
            {
                graphics.DrawLine(pen, siblingBounds.Left, 0, siblingBounds.Left, _canvas.Height);
            }

            if (selectedBounds.Top == siblingBounds.Top || selectedBounds.Bottom == siblingBounds.Bottom)
            {
                graphics.DrawLine(pen, 0, siblingBounds.Top, _canvas.Width, siblingBounds.Top);
            }
        }
    }

    private void ResizeCanvas()
    {
        var root = _session?.RootForm;
        var width = Math.Max(ClientSize.Width, (root?.Right ?? 0) + CanvasMargin);
        var height = Math.Max(ClientSize.Height, (root?.Bottom ?? 0) + CanvasMargin);
        _canvas.Size = new Size(Math.Max(1, width), Math.Max(1, height));
        AutoScrollMinSize = _canvas.Size;
        _adornments.Size = _canvas.ClientSize;
        RefreshDesignerVisuals();
    }

    private void RefreshDesignerVisuals()
    {
        _canvas.Invalidate(true);
        _adornments.Invalidate();
        _session?.RootForm.Invalidate(true);
    }

    private void ConfigureDropTargets()
    {
        RemoveDropTargets();
        WireDropTarget(this);
        WireDropTarget(_canvas);
        WireDropTarget(_adornments);

        if (_session is not null)
        {
            WireControlTree(_session.RootForm);
        }
    }

    private void WireControlTree(Control control)
    {
        WireDropTarget(control);
        foreach (Control child in control.Controls)
        {
            WireControlTree(child);
        }
    }

    private void WireDropTarget(Control control)
    {
        if (!_dropTargets.Add(control))
        {
            return;
        }

        control.AllowDrop = true;
        control.DragEnter += OnDesignerDragEnter;
        control.DragDrop += OnDesignerDragDrop;
        control.MouseDown += OnDesignerMouseDown;
        control.MouseMove += OnDesignerMouseMove;
        control.MouseUp += OnDesignerMouseUp;
    }

    private void RemoveDropTargets()
    {
        foreach (var control in _dropTargets)
        {
            control.AllowDrop = false;
            control.DragEnter -= OnDesignerDragEnter;
            control.DragDrop -= OnDesignerDragDrop;
            control.MouseDown -= OnDesignerMouseDown;
            control.MouseMove -= OnDesignerMouseMove;
            control.MouseUp -= OnDesignerMouseUp;
        }

        _dropTargets.Clear();
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
        var formPoint = _session.RootForm.PointToClient(screenPoint);
        var hitNode = _session.HitTest(formPoint);
        var parentNode = hitNode is not null && CanContainControls(hitNode.ControlType)
            ? hitNode
            : hitNode?.Parent ?? _session.Document.Root;
        var parentControl = _session.GetLive(parentNode) ?? _session.RootForm;
        var location = parentControl.PointToClient(screenPoint);

        _session.Select(parentNode);
        _session.AddControl(controlType, new Point(Math.Max(0, location.X), Math.Max(0, location.Y)));
        Focus();
        RefreshDesignerVisuals();
    }

    private static Type? GetDraggedControlType(IDataObject? data)
    {
        if (data?.GetDataPresent(ToolboxDrag.DataFormat) != true
            || data.GetData(ToolboxDrag.DataFormat) is not Type type
            || !typeof(Control).IsAssignableFrom(type)
            || !ControlFactory.IsCreatable(type))
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

    private void OnRootFormSizeChanged(object? sender, EventArgs e) => ResizeCanvas();
    private void OnSessionStructureChanged(object? sender, EventArgs e)
    {
        ConfigureDropTargets();
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

    private sealed class CanvasPanel : Panel
    {
        public CanvasPanel()
        {
            BackColor = Color.FromArgb(0xF0, 0xF0, 0xF0);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            using var brush = new SolidBrush(Color.FromArgb(0xD6, 0xD6, 0xD6));
            for (var y = GridSize; y < Height; y += GridSize * 2)
            {
                for (var x = GridSize; x < Width; x += GridSize * 2)
                {
                    e.Graphics.FillRectangle(brush, x, y, 1, 1);
                }
            }
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
