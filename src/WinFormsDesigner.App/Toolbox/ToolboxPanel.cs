using System.Windows.Forms;
using WinFormsDesigner.Core.Reflection;

namespace WinFormsDesigner.App.Toolbox;

/// <summary>
/// Shared constants for the drag and drop payload that carries a control type from the
/// toolbox to the design surface.
/// </summary>
public static class ToolboxDrag
{
    /// <summary>The clipboard format name used for the dragged control type.</summary>
    public const string DataFormat = "WinFormsDesigner.ControlType";
}

/// <summary>
/// The toolbox. Controls are grouped by category and can be dragged onto the design surface.
/// </summary>
public sealed class ToolboxPanel : UserControl
{
    private readonly ListView _list = new BufferedListView();
    private readonly ImageList _images = new();
    private bool _suppressDrag;

    public ToolboxPanel()
    {
        Dock = DockStyle.Fill;

        _images.ImageSize = new Size(16, 16);
        _images.ColorDepth = ColorDepth.Depth32Bit;

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.HideSelection = false;
        _list.MultiSelect = false;
        _list.ShowGroups = true;
        _list.HeaderStyle = ColumnHeaderStyle.None;
        _list.SmallImageList = _images;
        _list.BorderStyle = BorderStyle.None;
        _list.BackColor = SystemColors.Control;
        _list.ItemDrag += OnItemDrag;
        _list.DoubleClick += OnDoubleClick;
        _list.MouseDown += OnListMouseDown;
        _list.Resize += (_, _) => SyncColumnWidth();

        // A list view in the details view only draws the text of an item inside the first
        // column. Without a column the items stay invisible, so one is always present and
        // kept in sync with the width of the toolbox.
        _list.Columns.Add(new ColumnHeader { Text = "Control", Width = 200 });

        Controls.Add(_list);

        Populate();
        SyncColumnWidth();
    }

    /// <summary>Raised when the user asks for a control to be added to the form.</summary>
    public event EventHandler<ToolboxEntryEventArgs>? ControlRequested;

    private void Populate()
    {
        _list.BeginUpdate();
        _list.Groups.Clear();
        _list.Items.Clear();

        foreach (var category in ControlTypeRegistry.Categories)
        {
            var group = new ListViewGroup(category);
            _list.Groups.Add(group);

            foreach (var entry in ControlTypeRegistry.InCategory(category))
            {
                if (!ControlFactory.IsCreatable(entry.ControlType))
                {
                    // A type the designer cannot instantiate is not offered.
                    continue;
                }

                var item = new ListViewItem(entry.DisplayName)
                {
                    Group = group,
                    Tag = entry,
                    ImageKey = entry.Glyph,
                    ToolTipText = entry.Description,
                };

                _list.Items.Add(item);

                if (!_images.Images.ContainsKey(entry.Glyph))
                {
                    _images.Images.Add(entry.Glyph, GlyphFactory.GetGlyph(entry.Glyph));
                }
            }
        }

        _list.EndUpdate();
    }

    /// <summary>
    /// Keeps the width of the single column in sync with the toolbox so that the entry
    /// names are never clipped.
    /// </summary>
    private void SyncColumnWidth()
    {
        if (_list.Columns.Count == 0)
        {
            return;
        }

        var width = Math.Max(60, _list.ClientSize.Width - 2);
        if (_list.Columns[0].Width != width)
        {
            _list.Columns[0].Width = width;
        }
    }

    private void OnListMouseDown(object? sender, MouseEventArgs e)
    {
        _suppressDrag = false;

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var item = _list.GetItemAt(e.X, e.Y);
        if (item?.Tag is ToolboxEntry entry)
        {
            _list.SelectedItems.Clear();
            item.Selected = true;
        }
    }

    private void OnItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (_suppressDrag || e.Item is not ListViewItem listItem || listItem.Tag is not ToolboxEntry entry)
        {
            return;
        }

        var data = new DataObject();
        data.SetData(ToolboxDrag.DataFormat, entry.ControlType);

        try
        {
            _list.DoDragDrop(data, DragDropEffects.Copy);
        }
        catch (Exception)
        {
            // Drag and drop can fail when the source window closes mid drag.
        }
    }

    private void OnDoubleClick(object? sender, EventArgs e)
    {
        if (_list.SelectedItems.Count == 0)
        {
            return;
        }

        if (_list.SelectedItems[0].Tag is ToolboxEntry entry)
        {
            ControlRequested?.Invoke(this, new ToolboxEntryEventArgs(entry));
        }
    }

    /// <summary>
    /// A list view that paints through a back buffer. The common control flickers while
    /// it is resized otherwise, because it clears and redraws every entry in turn.
    /// </summary>
    private sealed class BufferedListView : ListView
    {
        public BufferedListView()
        {
            DoubleBuffered = true;
        }
    }
}

/// <summary>Event arguments carrying the toolbox entry the user picked.</summary>
public sealed class ToolboxEntryEventArgs : EventArgs
{
    public ToolboxEntryEventArgs(ToolboxEntry entry)
    {
        Entry = entry;
    }

    /// <summary>The selected toolbox entry.</summary>
    public ToolboxEntry Entry { get; }
}
