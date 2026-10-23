using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsDesigner.App.Properties;

/// <summary>
/// A small editor for the child collections of a control, e.g. the items of a list box
/// or the columns of a list view.
/// </summary>
internal sealed class CollectionEditorDialog : Form
{
    private readonly ListView _list = new();
    private readonly TextBox _input = new();
    private readonly Button _add = new();
    private readonly Button _remove = new();
    private readonly Button _up = new();
    private readonly Button _down = new();
    private readonly Button _ok = new();
    private readonly Button _cancel = new();

    public CollectionEditorDialog()
    {
        Text = "Collection Editor";
        Size = new Size(440, 340);
        MinimumSize = new Size(360, 280);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = SystemFonts.DefaultFont;

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.HideSelection = false;
        _list.MultiSelect = false;
        _list.GridLines = true;
        _list.Columns.Add("Item", 320);
        _list.DoubleClick += OnListDoubleClick;

        _input.Dock = DockStyle.Bottom;
        _input.Height = 24;
        _input.Margin = new Padding(3);

        var left = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4),
        };

        left.Controls.Add(_list);
        left.Controls.Add(_input);

        _add.Text = "Add";
        _remove.Text = "Remove";
        _up.Text = "Up";
        _down.Text = "Down";
        _ok.Text = "OK";
        _cancel.Text = "Cancel";

        foreach (var button in new[] { _add, _remove, _up, _down })
        {
            button.Dock = DockStyle.Top;
            button.Height = 30;
        }

        _add.Click += (_, _) => AddItem();
        _remove.Click += (_, _) => RemoveItem();
        _up.Click += (_, _) => MoveItem(-1);
        _down.Click += (_, _) => MoveItem(1);
        _list.KeyDown += OnListKeyDown;

        var side = new Panel
        {
            Dock = DockStyle.Right,
            Width = 96,
            Padding = new Padding(4),
        };

        // The control added last is docked first, so the buttons end up top down.
        side.Controls.Add(_down);
        side.Controls.Add(_up);
        side.Controls.Add(_remove);
        side.Controls.Add(_add);

        _ok.DialogResult = DialogResult.OK;
        _cancel.DialogResult = DialogResult.Cancel;
        _ok.Width = 88;
        _cancel.Width = 88;

        var bottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            Padding = new Padding(4),
        };

        bottom.Controls.Add(_cancel);
        bottom.Controls.Add(_ok);

        Controls.Add(left);
        Controls.Add(side);
        Controls.Add(bottom);

        AcceptButton = _ok;
        CancelButton = _cancel;
    }

    /// <summary>The items shown in the editor.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public List<string> Items { get; set; } = new();

    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete)
        {
            RemoveItem();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Enter)
        {
            AddItem();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void OnListDoubleClick(object? sender, EventArgs e)
    {
        if (_list.SelectedItems.Count > 0)
        {
            _input.Text = _list.SelectedItems[0].Text;
            _input.Focus();
            _input.SelectAll();
        }
    }

    private void AddItem()
    {
        var text = _input.Text;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Items.Add(text);
        _list.Items.Add(text);
        _input.Clear();
        _input.Focus();
    }

    private void RemoveItem()
    {
        if (_list.SelectedItems.Count == 0)
        {
            return;
        }

        var index = _list.SelectedIndices[0];
        _list.Items.RemoveAt(index);
        Items.RemoveAt(index);
    }

    private void MoveItem(int offset)
    {
        if (_list.SelectedItems.Count == 0)
        {
            return;
        }

        var index = _list.SelectedIndices[0];
        var target = index + offset;
        if (target < 0 || target >= Items.Count)
        {
            return;
        }

        (Items[index], Items[target]) = (Items[target], Items[index]);

        _list.Items.Clear();
        foreach (var item in Items)
        {
            _list.Items.Add(item);
        }

        _list.Items[target].Selected = true;
        _list.EnsureVisible(target);
    }
}
