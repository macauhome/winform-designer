using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinFormsDesigner.App.Designer;
using WinFormsDesigner.Core.Model;
using WinFormsDesigner.Core.Reflection;

namespace WinFormsDesigner.App.Properties;

/// <summary>
/// A property grid that looks and behaves like the one in the Visual Studio designer but is
/// implemented from scratch, so the application does not depend on any design time assembly.
/// </summary>
/// <remarks>
/// The grid paints every category header and every property row itself and only creates a
/// real editor control for the row that is being edited. The Visual Studio original does the
/// same, and it keeps the number of windows in the grid down to a handful, which is what
/// makes the designer stay responsive while the main window is resized.
/// </remarks>
public sealed class PropertyGridView : Panel
{
    private const int HeaderHeight = 24;
    private const int RowHeight = 20;
    private const int ScrollBarWidth = 14;
    private const int NameColumnMinimum = 80;
    private const int NameColumnMaximum = 170;
    private const int ValueLeftPadding = 4;

    private static readonly Color HeaderBackground = Color.FromArgb(0xF0, 0xF0, 0xF0);
    private static readonly Color CategoryBackground = Color.FromArgb(0xF0, 0xF0, 0xF0);
    private static readonly Color SeparatorColor = Color.FromArgb(0xD4, 0xD4, 0xD4);
    private static readonly Font CategoryFont = new(SystemFonts.DefaultFont, FontStyle.Bold);
    private static readonly Font HeaderFont = new(SystemFonts.DefaultFont, FontStyle.Bold);

    private readonly Panel _editorHost = new();
    private readonly List<GridRow> _rows = [];
    private readonly Dictionary<string, bool> _expanded = new(StringComparer.OrdinalIgnoreCase);

    private DesignSession? _session;
    private bool _rebuildPending;
    private bool _suppressEvents;
    private string _headerText = "No selection";
    private int _scrollY;
    private int _contentHeight;
    private int _activeRow = -1;
    private bool _scrollDragging;
    private int _scrollDragOffset;

    /// <summary>
    /// Cached rendering of the rows. The rows only change when the selection or a value
    /// changes, so they are painted into a bitmap once and copied from there afterwards.
    /// That keeps a window resize cheap, because only the height of the grid changes then.
    /// </summary>
    private Bitmap? _rowCache;
    private int _rowCacheWidth;

    public PropertyGridView()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(0);
        BackColor = SystemColors.Control;
        TabStop = true;

        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, true);

        _editorHost.BorderStyle = BorderStyle.None;
        _editorHost.BackColor = SystemColors.Control;
        _editorHost.Visible = false;

        Controls.Add(_editorHost);
    }

    /// <summary>The session whose selection is shown.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
                _session.Changed -= OnSessionChanged;
                _session.SelectionChanged -= OnSessionSelectionChanged;
            }

            _session = value;

            if (_session is not null)
            {
                _session.Changed += OnSessionChanged;
                _session.SelectionChanged += OnSessionSelectionChanged;
            }

            Rebuild();
        }
    }

    /// <summary>Number of rows the grid currently shows, category headers included.</summary>
    public int RowCount => _rows.Count;

    /// <summary>Title of the row with the given index, or an empty string.</summary>
    public string RowTitle(int index)
        => index >= 0 && index < _rows.Count ? _rows[index].Title : string.Empty;

    /// <summary>Name of the property the row with the given index shows.</summary>
    public string RowKey(int index)
        => index >= 0 && index < _rows.Count ? _rows[index].Key ?? string.Empty : string.Empty;

    /// <summary>Index of the row that is currently being edited, or -1.</summary>
    public int ActiveRowIndex => _activeRow;

    /// <summary>The editor control of the active row, or <c>null</c> when no row is edited.</summary>
    public Control? ActiveEditor => _editorHost.Controls.Count > 0 ? _editorHost.Controls[0] : null;

    /// <summary>Forces the grid to reload the properties of the current selection.</summary>
    public void Rebuild()
    {
        DestroyEditor();

        _rows.Clear();
        _activeRow = -1;

        var node = _session?.SelectedNode;
        if (node is null || _session is null)
        {
            _headerText = "No selection";
            _contentHeight = 0;
            ClearRowCache();
            ClampScroll();
            Invalidate();
            return;
        }

        _headerText = ControlTypeRegistry.Find(node.ControlType)?.DisplayName ?? node.ControlType.Name;

        var entries = PropertyCatalog.GetEntries(node.ControlType);
        var groups = entries
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var order = new List<string> { PropertyMetadata.NameCategory };
        order.AddRange(PropertyMetadataTable.CategoryOrder.Where(c => groups.ContainsKey(c) && c != PropertyMetadata.NameCategory));
        order.AddRange(groups.Keys.Where(k => !order.Contains(k, StringComparer.OrdinalIgnoreCase)));

        var y = 0;

        foreach (var category in order)
        {
            if (!groups.TryGetValue(category, out var categoryEntries))
            {
                continue;
            }

            // The Visual Studio designer shows every category expanded until the user
            // collapses one of them.
            var expanded = _expanded.TryGetValue(category, out var value) ? value : true;
            _expanded[category] = expanded;

            _rows.Add(new GridRow
            {
                Kind = GridRowKind.Category,
                Title = category,
                Category = category,
                Expanded = expanded,
                Top = y,
            });

            y += RowHeight;

            if (!expanded)
            {
                continue;
            }

            foreach (var entry in categoryEntries)
            {
                var propertyValue = GetPropertyValue(node, entry);

                _rows.Add(new GridRow
                {
                    Kind = GridRowKind.Property,
                    Title = entry.DisplayName,
                    Key = entry.Info.Name,
                    Entry = entry,
                    Value = propertyValue,
                    ValueText = FormatPropertyValue(entry, propertyValue),
                    Top = y,
                });

                y += RowHeight;
            }
        }

        AddEventRows(node, ref y);

        _contentHeight = y;
        ClearRowCache();
        ClampScroll();
        Invalidate();
    }

    private void AddEventRows(DesignNode node, ref int y)
    {
        var events = PropertyGridEditors.GetCommonEvents(node.ControlType);
        if (events.Count == 0)
        {
            return;
        }

        const string Category = "Events";
        var expanded = _expanded.TryGetValue(Category, out var value) ? value : true;
        _expanded[Category] = expanded;

        _rows.Add(new GridRow
        {
            Kind = GridRowKind.Category,
            Title = Category,
            Category = Category,
            Expanded = expanded,
            Top = y,
        });

        y += RowHeight;

        if (!expanded)
        {
            return;
        }

        foreach (var eventName in events)
        {
            var handler = node.Events.TryGetValue(eventName, out var stored) ? stored : string.Empty;

            _rows.Add(new GridRow
            {
                Kind = GridRowKind.Event,
                Title = eventName,
                Key = eventName,
                EventName = eventName,
                ValueText = string.IsNullOrEmpty(handler) ? string.Empty : handler,
                Top = y,
            });

            y += RowHeight;
        }
    }

    /// <summary>
    /// Reads the value that should be shown for a property. Explicitly assigned values win,
    /// otherwise the value of the live control is used and finally the control default.
    /// </summary>
    private object? GetPropertyValue(DesignNode node, PropertyEntry entry)
    {
        if (entry.Info.Name == "Name")
        {
            return node.Name;
        }

        if (node.Properties.TryGetValue(entry.Info.Name, out var stored))
        {
            return stored;
        }

        if (_session?.GetLive(node) is { } live)
        {
            try
            {
                return entry.GetValue(live);
            }
            catch (Exception)
            {
                // Fall through to the control default.
            }
        }

        return ControlFactory.GetDefaultValue(node.ControlType, entry.Info.Name);
    }

    private static string FormatPropertyValue(PropertyEntry entry, object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is bool boolean)
        {
            return boolean ? "True" : "False";
        }

        return entry.FormatValue(value);
    }

    // ---------------------------------------------------------------------
    // Committing values
    // ---------------------------------------------------------------------

    /// <summary>Commits a new value for a property of the selected node.</summary>
    internal void Commit(string propertyName, object? value)
    {
        if (_suppressEvents || _session?.SelectedNode is null)
        {
            return;
        }

        if (propertyName == "Name")
        {
            _session.SetName(_session.SelectedNode, value as string ?? string.Empty);
            return;
        }

        _session.SetProperty(_session.SelectedNode, propertyName, value);
    }

    internal void CommitEvent(string eventName, string handlerName)
    {
        if (_suppressEvents || _session?.SelectedNode is null)
        {
            return;
        }

        _session.SetEvent(_session.SelectedNode, eventName, handlerName);
    }

    /// <summary>Suppresses commit handling while the grid is rebuilt.</summary>
    internal void SuppressEvents(bool suppress) => _suppressEvents = suppress;

    /// <summary>Closes the editor of the active row and stores the value that was typed.</summary>
    public void EndEdit()
    {
        DestroyEditor(commitPending: true);
        Invalidate();
    }

    /// <summary>Closes the editor of the active row and discards the pending change.</summary>
    public void CancelEdit() => DestroyEditor(commitPending: false);

    // ---------------------------------------------------------------------
    // Painting
    // ---------------------------------------------------------------------

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var graphics = e.Graphics;
        var contentWidth = ContentWidth;

        using (var brush = new SolidBrush(HeaderBackground))
        {
            graphics.FillRectangle(brush, 0, 0, Width, HeaderHeight);
        }

        using (var pen = new Pen(SeparatorColor))
        {
            graphics.DrawLine(pen, 0, HeaderHeight - 1, Width, HeaderHeight - 1);
        }

        TextDrawer.Draw(
            graphics,
            _headerText,
            HeaderFont,
            SystemColors.ControlText,
            new Rectangle(6, 0, Width - 12, HeaderHeight),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

        var state = graphics.Save();
        graphics.SetClip(new Rectangle(0, HeaderHeight, contentWidth, Math.Max(0, Height - HeaderHeight)));

        var cache = EnsureRowCache(contentWidth);

        if (cache is not null)
        {
            var visible = Math.Min(cache.Height - _scrollY, Height - HeaderHeight);

            if (visible > 0)
            {
                graphics.DrawImage(
                    cache,
                    new Rectangle(0, HeaderHeight, contentWidth, visible),
                    new Rectangle(0, _scrollY, contentWidth, visible),
                    GraphicsUnit.Pixel);
            }
        }

        graphics.Restore(state);

        DrawScrollBar(graphics);
    }

    /// <summary>
    /// Returns the cached rows, painting them again when the content or the width changed.
    /// </summary>
    private Bitmap? EnsureRowCache(int contentWidth)
    {
        if (contentWidth <= 0 || _contentHeight <= 0)
        {
            ClearRowCache();
            return null;
        }

        if (_rowCache is not null && _rowCacheWidth == contentWidth && _rowCache.Height == _contentHeight)
        {
            return _rowCache;
        }

        ClearRowCache();

        var bitmap = new Bitmap(contentWidth, _contentHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

        using (var cacheGraphics = Graphics.FromImage(bitmap))
        {
            cacheGraphics.Clear(BackColor);

            foreach (var row in _rows)
            {
                DrawRow(cacheGraphics, row, row.Top, contentWidth);
            }
        }

        _rowCache = bitmap;
        _rowCacheWidth = contentWidth;
        return _rowCache;
    }

    /// <summary>Drops the cached rows so that they are painted again.</summary>
    private void ClearRowCache()
    {
        _rowCache?.Dispose();
        _rowCache = null;
        _rowCacheWidth = 0;
    }

    private void DrawRow(Graphics graphics, GridRow row, int y, int contentWidth)
    {
        var nameWidth = NameColumnWidth;

        if (row.Kind == GridRowKind.Category)
        {
            using (var brush = new SolidBrush(CategoryBackground))
            {
                graphics.FillRectangle(brush, 0, y, contentWidth, row.Height);
            }

            TextDrawer.Draw(
                graphics,
                (row.Expanded ? "▼ " : "▶ ") + row.Title,
                CategoryFont,
                SystemColors.ControlText,
                new Rectangle(4, y, contentWidth - 8, row.Height),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            return;
        }

        TextDrawer.Draw(
            graphics,
            row.Title,
            SystemFonts.DefaultFont,
            SystemColors.ControlText,
            new Rectangle(0, y, nameWidth - 4, row.Height),
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

        var valueLeft = nameWidth;
        var valueRect = new Rectangle(valueLeft, y, Math.Max(0, contentWidth - valueLeft), row.Height);

        if (row.Kind == GridRowKind.Property && row.Entry is { } entry)
        {
            switch (entry.Editor)
            {
                case PropertyEditorKind.Boolean:
                    DrawBoolean(graphics, row, valueRect);
                    return;

                case PropertyEditorKind.Color:
                    DrawColor(graphics, row, valueRect);
                    return;
            }
        }

        var text = row.ValueText;
        var hint = GetValueHint(row);

        TextDrawer.Draw(
            graphics,
            text,
            SystemFonts.DefaultFont,
            SystemColors.ControlText,
            new Rectangle(valueRect.Left + ValueLeftPadding, y, Math.Max(0, valueRect.Width - ValueLeftPadding - 16), row.Height),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

        if (!string.IsNullOrEmpty(hint))
        {
            TextDrawer.Draw(
                graphics,
                hint,
                SystemFonts.DefaultFont,
                SystemColors.ControlDark,
                new Rectangle(Math.Max(valueRect.Left, valueRect.Right - 16), y, 14, row.Height),
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    private void DrawBoolean(Graphics graphics, GridRow row, Rectangle valueRect)
    {
        var box = new Rectangle(valueRect.Left + ValueLeftPadding, valueRect.Top + 3, 13, 13);
        var isChecked = row.Value is bool value && value;

        using (var brush = new SolidBrush(SystemColors.Window))
        {
            graphics.FillRectangle(brush, box);
        }

        using (var pen = new Pen(SystemColors.ControlDark))
        {
            graphics.DrawRectangle(pen, box);
        }

        if (isChecked)
        {
            using var pen = new Pen(SystemColors.ControlText, 2);
            graphics.DrawLines(pen,
            [
                new Point(box.Left + 3, box.Top + 7),
                new Point(box.Left + 5, box.Top + 9),
                new Point(box.Left + 10, box.Top + 3),
            ]);
        }

        TextDrawer.Draw(
            graphics,
            row.ValueText,
            SystemFonts.DefaultFont,
            SystemColors.ControlText,
            new Rectangle(box.Right + 4, valueRect.Top, Math.Max(0, valueRect.Width - box.Width - 12), valueRect.Height),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }

    private void DrawColor(Graphics graphics, GridRow row, Rectangle valueRect)
    {
        var swatch = new Rectangle(valueRect.Left + ValueLeftPadding, valueRect.Top + 3, 14, 13);
        var color = row.Value is Color value ? value : Color.Empty;

        using (var brush = new SolidBrush(color.IsEmpty ? SystemColors.Control : color))
        {
            graphics.FillRectangle(brush, swatch);
        }

        using (var pen = new Pen(SystemColors.ControlDark))
        {
            graphics.DrawRectangle(pen, swatch);
        }

        TextDrawer.Draw(
            graphics,
            row.ValueText,
            SystemFonts.DefaultFont,
            SystemColors.ControlText,
            new Rectangle(swatch.Right + 4, valueRect.Top, Math.Max(0, valueRect.Width - swatch.Width - 12), valueRect.Height),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }

    private static string GetValueHint(GridRow row)
    {
        if (row.Kind == GridRowKind.Event)
        {
            return "▼";
        }

        if (row.Entry is not { } entry)
        {
            return string.Empty;
        }

        return entry.Editor switch
        {
            PropertyEditorKind.Enumeration or PropertyEditorKind.Reference => "▼",
            PropertyEditorKind.Font or PropertyEditorKind.Image or PropertyEditorKind.Collection => "…",
            _ => string.Empty,
        };
    }

    private void DrawScrollBar(Graphics graphics)
    {
        if (!ScrollBarVisible)
        {
            return;
        }

        var track = new Rectangle(Width - ScrollBarWidth, HeaderHeight, ScrollBarWidth, Height - HeaderHeight);

        using (var brush = new SolidBrush(SystemColors.Control))
        {
            graphics.FillRectangle(brush, track);
        }

        var thumb = ScrollBarThumb();
        using (var brush = new SolidBrush(SystemColors.ControlDark))
        {
            graphics.FillRectangle(brush, thumb);
        }
    }

    // ---------------------------------------------------------------------
    // Layout helpers
    // ---------------------------------------------------------------------

    private bool ScrollBarVisible => _contentHeight > Math.Max(0, Height - HeaderHeight);

    private int ContentWidth => Width - (ScrollBarVisible ? ScrollBarWidth : 0);

    private int NameColumnWidth => Math.Clamp((int)(ContentWidth * 0.38), NameColumnMinimum, NameColumnMaximum);

    private Rectangle ScrollBarThumb()
    {
        var viewport = Math.Max(1, Height - HeaderHeight);
        var thumbHeight = Math.Max(24, (int)((double)viewport * viewport / Math.Max(1, _contentHeight)));
        var travel = Math.Max(1, viewport - thumbHeight);
        var offset = (int)((double)travel * (_scrollY / (double)Math.Max(1, _contentHeight - viewport)));

        return new Rectangle(Width - ScrollBarWidth + 1, HeaderHeight + offset, ScrollBarWidth - 2, thumbHeight);
    }

    private void ClampScroll()
    {
        var max = Math.Max(0, _contentHeight - (Height - HeaderHeight));
        _scrollY = Math.Clamp(_scrollY, 0, max);
    }

    private int HitTestRow(Point point)
    {
        if (point.Y < HeaderHeight)
        {
            return -1;
        }

        var contentY = point.Y - HeaderHeight + _scrollY;

        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            if (contentY >= row.Top && contentY < row.Top + row.Height)
            {
                return i;
            }
        }

        return -1;
    }

    private void ScrollRowIntoView(int index)
    {
        if (index < 0 || index >= _rows.Count)
        {
            return;
        }

        var row = _rows[index];
        var viewport = Math.Max(1, Height - HeaderHeight);

        if (row.Top < _scrollY)
        {
            _scrollY = row.Top;
        }
        else if (row.Top + row.Height > _scrollY + viewport)
        {
            _scrollY = row.Top + row.Height - viewport;
        }

        ClampScroll();
    }

    // ---------------------------------------------------------------------
    // Editing
    // ---------------------------------------------------------------------

    /// <summary>Activates the row with the given index so that its value can be edited.</summary>
    public bool ActivateRow(int index)
    {
        if (index < 0 || index >= _rows.Count)
        {
            return false;
        }

        var row = _rows[index];

        if (row.Kind == GridRowKind.Category)
        {
            ToggleCategory(row.Category!);
            return false;
        }

        if (row.Kind == GridRowKind.Property && row.Entry is { } entry && _session?.SelectedNode is { } node)
        {
            switch (entry.Editor)
            {
                case PropertyEditorKind.Boolean:
                    Commit(entry.Info.Name, row.Value is bool current ? !current : true);
                    return false;

                case PropertyEditorKind.Color:
                    PropertyGridEditors.ShowColorDialog(this, entry, row.Value);
                    return false;

                case PropertyEditorKind.Font:
                    PropertyGridEditors.ShowFontDialog(this, entry, row.Value);
                    return false;

                case PropertyEditorKind.Image:
                    PropertyGridEditors.ShowImageDialog(this, node, entry);
                    return false;

                case PropertyEditorKind.Collection:
                    PropertyGridEditors.ShowCollectionDialog(this, node, entry);
                    return false;
            }
        }

        DestroyEditor();
        _activeRow = index;

        Control? editor = null;

        if (row.Kind == GridRowKind.Event && _session?.SelectedNode is not null)
        {
            editor = PropertyGridEditors.CreateEventEditor(this, _session.SelectedNode, row.EventName!);
        }
        else if (row.Entry is not null && _session?.SelectedNode is not null)
        {
            editor = PropertyGridEditors.CreateEditor(this, _session.SelectedNode, row.Entry, row.Value);
        }

        if (editor is null)
        {
            _activeRow = -1;
            return false;
        }

        _editorHost.Controls.Add(editor);
        _editorHost.Visible = true;

        ScrollRowIntoView(index);
        PositionEditor();
        editor.Focus();

        if (editor is TextBox textBox)
        {
            textBox.SelectAll();
        }

        Invalidate();
        return true;
    }

    private void PositionEditor()
    {
        if (_activeRow < 0 || _activeRow >= _rows.Count || _editorHost.Controls.Count == 0)
        {
            _editorHost.Visible = false;
            return;
        }

        var row = _rows[_activeRow];
        var y = HeaderHeight + row.Top - _scrollY;
        var visible = y >= HeaderHeight && y + row.Height <= Height;

        _editorHost.Visible = visible;

        if (!visible)
        {
            return;
        }

        var left = NameColumnWidth;
        _editorHost.Bounds = new Rectangle(left, y, Math.Max(20, ContentWidth - left), row.Height);
    }

    private void DestroyEditor(bool commitPending = true)
    {
        if (_editorHost.Controls.Count == 0 && _activeRow < 0)
        {
            return;
        }

        // The value the user typed has to reach the document before the editor goes away.
        if (commitPending && _editorHost.Controls.Count > 0 && _editorHost.Controls[0].Tag is Action commit)
        {
            commit();
        }

        // The editor may commit its value when it loses the focus. That must not happen
        // while the grid tears the editor down.
        var wasSuppressed = _suppressEvents;
        _suppressEvents = true;

        try
        {
            _editorHost.Controls.Clear();
            _editorHost.Visible = false;
        }
        finally
        {
            _suppressEvents = wasSuppressed;
        }

        _activeRow = -1;
    }

    private void ToggleCategory(string category)
    {
        var expanded = _expanded.TryGetValue(category, out var value) ? value : false;
        _expanded[category] = !expanded;
        Rebuild();
    }

    // ---------------------------------------------------------------------
    // Mouse and keyboard
    // ---------------------------------------------------------------------

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        Focus();

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (ScrollBarVisible && e.X >= Width - ScrollBarWidth)
        {
            HandleScrollBarClick(e.Location);
            return;
        }

        var index = HitTestRow(e.Location);
        if (index < 0)
        {
            DestroyEditor();
            Invalidate();
            return;
        }

        ActivateRow(index);
    }

    private void HandleScrollBarClick(Point point)
    {
        var thumb = ScrollBarThumb();
        var viewport = Math.Max(1, Height - HeaderHeight);

        if (thumb.Contains(point))
        {
            _scrollDragging = true;
            _scrollDragOffset = point.Y - thumb.Top;
            Capture = true;
            return;
        }

        if (point.Y < thumb.Top)
        {
            _scrollY -= viewport / 2;
        }
        else
        {
            _scrollY += viewport / 2;
        }

        ClampScroll();
        PositionEditor();
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_scrollDragging)
        {
            return;
        }

        var viewport = Math.Max(1, Height - HeaderHeight);
        var travel = Math.Max(0, viewport - ScrollBarThumb().Height);
        var max = Math.Max(0, _contentHeight - viewport);

        _scrollY = max <= 0 || travel <= 0
            ? 0
            : (int)Math.Round((e.Y - HeaderHeight - _scrollDragOffset) / (double)travel * max);

        ClampScroll();
        PositionEditor();
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_scrollDragging)
        {
            _scrollDragging = false;
            Capture = false;
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        var delta = -Math.Sign(e.Delta) * RowHeight * 3;
        _scrollY += delta;
        ClampScroll();
        PositionEditor();
        Invalidate();
    }

    protected override bool IsInputKey(Keys keyData) => keyData switch
    {
        Keys.Up or Keys.Down => true,
        _ => base.IsInputKey(keyData),
    };

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.KeyCode)
        {
            case Keys.Up:
                MoveActiveRow(-1);
                e.Handled = true;
                break;

            case Keys.Down:
                MoveActiveRow(1);
                e.Handled = true;
                break;

            case Keys.Escape:
                DestroyEditor(commitPending: false);
                Invalidate();
                e.Handled = true;
                break;
        }
    }

    private void MoveActiveRow(int delta)
    {
        if (_rows.Count == 0)
        {
            return;
        }

        var index = _activeRow;
        if (index < 0)
        {
            index = 0;
        }
        else
        {
            index = Math.Clamp(index + delta, 0, _rows.Count - 1);
        }

        ActivateRow(index);
        ScrollRowIntoView(index);
        PositionEditor();
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        ClampScroll();
        PositionEditor();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_session is not null)
            {
                _session.Changed -= OnSessionChanged;
                _session.SelectionChanged -= OnSessionSelectionChanged;
            }

            ClearRowCache();
        }

        base.Dispose(disposing);
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        if (_rebuildPending || !IsHandleCreated)
        {
            return;
        }

        _rebuildPending = true;
        BeginInvoke(new Action(() =>
        {
            _rebuildPending = false;
            Rebuild();
        }));
    }

    private void OnSessionSelectionChanged(object? sender, EventArgs e) => Rebuild();

    private enum GridRowKind
    {
        Category,
        Property,
        Event,
    }

    private sealed class GridRow
    {
        public required GridRowKind Kind { get; init; }

        /// <summary>Text shown in the left hand column.</summary>
        public required string Title { get; init; }

        /// <summary>Stable identity of the row.</summary>
        public string? Key { get; init; }

        /// <summary>Category the row belongs to, for category rows.</summary>
        public string? Category { get; init; }

        /// <summary>Property the row shows, for property rows.</summary>
        public PropertyEntry? Entry { get; init; }

        /// <summary>Event the row shows, for event rows.</summary>
        public string? EventName { get; init; }

        /// <summary>Current value of the property.</summary>
        public object? Value { get; init; }

        /// <summary>Formatted value shown in the right hand column.</summary>
        public string ValueText { get; init; } = string.Empty;

        /// <summary>True when a category row is expanded.</summary>
        public bool Expanded { get; init; }

        /// <summary>Vertical position of the row in the scrollable content.</summary>
        public int Top { get; init; }

        /// <summary>Height of the row in the scrollable content.</summary>
        public int Height { get; init; } = RowHeight;
    }
}
