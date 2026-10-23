using System.Drawing;
using System.Windows.Forms;
using WinFormsDesigner.App.Designer;
using WinFormsDesigner.App.Layout;
using WinFormsDesigner.App.Properties;
using WinFormsDesigner.App.Toolbox;
using WinFormsDesigner.Core.Codegen;

namespace WinFormsDesigner.App;

/// <summary>
/// The main window of the designer. It arranges the toolbox, the design surface and the
/// property grid the way the Visual Studio designer does and hosts the editing commands.
/// </summary>
public sealed class MainForm : Form
{
    private const int ToolboxWidth = 230;
    private const int PropertiesWidth = 330;

    private readonly MenuStrip _menu = new();
    private readonly ToolStrip _toolbar = new();
    private readonly StatusStrip _status = new();

    private readonly DesignerShellLayout _shell = new();

    private readonly ToolboxPanel _toolbox = new();
    private readonly DesignerSurface _surface = new();
    private readonly PropertyGridView _properties = new();

    private readonly ToolStripStatusLabel _statusSelection = new();
    private readonly ToolStripStatusLabel _statusPointer = new();

    private readonly ToolStripMenuItem _showToolbox = new();
    private readonly ToolStripMenuItem _showProperties = new();
    private readonly ToolStripMenuItem _snapToGrid = new();
    private readonly ToolStripMenuItem _showGuides = new();

    private DesignSession _session = new();

    public MainForm()
    {
        Text = "WinForms Designer";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1280, 820);
        MinimumSize = new Size(900, 600);
        Font = SystemFonts.MessageBoxFont;

        BuildLayout();
        BuildMenu();
        BuildToolbar();
        BuildStatusBar();

        _surface.Session = _session;
        _properties.Session = _session;

        _toolbox.ControlRequested += OnControlRequested;
        _surface.PointerMoved += OnPointerMoved;
        _session.SelectionChanged += (_, _) => UpdateStatus();
        _session.Changed += (_, _) => UpdateTitle();

        UpdateTitle();
        UpdateStatus();
    }

    // ---------------------------------------------------------------------
    // Layout
    // ---------------------------------------------------------------------

    private void BuildLayout()
    {
        _shell.Toolbox = CreatePanelHeader("Toolbox", _toolbox);
        _shell.Surface = _surface;
        _shell.Properties = CreatePanelHeader("Properties", _properties);
        _shell.ToolboxWidth = ToolboxWidth;
        _shell.PropertiesWidth = PropertiesWidth;

        _menu.Dock = DockStyle.Top;
        _toolbar.Dock = DockStyle.Top;
        _status.Dock = DockStyle.Bottom;

        // The docking order runs from the last added control to the first one, so the
        // status bar claims the bottom, the menu the top and the toolbar sits below it.
        Controls.Add(_shell);
        Controls.Add(_toolbar);
        Controls.Add(_menu);
        Controls.Add(_status);
    }

    private static Panel CreatePanelHeader(string caption, Control content)
    {
        var host = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0),
        };

        var header = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            Text = caption,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
            Padding = new Padding(6, 0, 0, 0),
            BackColor = Color.FromArgb(0xF0, 0xF0, 0xF0),
        };

        content.Dock = DockStyle.Fill;

        host.Controls.Add(content);
        host.Controls.Add(header);
        return host;
    }

    // ---------------------------------------------------------------------
    // Menu
    // ---------------------------------------------------------------------

    private void BuildMenu()
    {
        ConfigureToggle(_showToolbox, "Toolbox", ToggleToolbox);
        ConfigureToggle(_showProperties, "Properties", ToggleProperties);
        ConfigureToggle(_snapToGrid, "Snap to Grid", ToggleSnapToGrid);
        ConfigureToggle(_showGuides, "Alignment Guides", ToggleGuides);

        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(Item("&New Form", Keys.Control | Keys.N, (_, _) => NewDocument()));
        file.DropDownItems.Add(Item("&Open...", Keys.Control | Keys.O, (_, _) => OpenFile()));
        file.DropDownItems.Add(Item("&Generate Code...", Keys.Control | Keys.G, (_, _) => GenerateCode()));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("E&xit", Keys.Alt | Keys.F4, (_, _) => Close()));

        var edit = new ToolStripMenuItem("&Edit");
        edit.DropDownItems.Add(Item("&Undo", Keys.Control | Keys.Z, (_, _) => Undo()));
        edit.DropDownItems.Add(Item("&Redo", Keys.Control | Keys.Y, (_, _) => Redo()));
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add(Item("Cu&t", Keys.Control | Keys.X, (_, _) => Cut()));
        edit.DropDownItems.Add(Item("&Copy", Keys.Control | Keys.C, (_, _) => Copy()));
        edit.DropDownItems.Add(Item("&Paste", Keys.Control | Keys.V, (_, _) => Paste()));
        edit.DropDownItems.Add(Item("De&lete", Keys.Delete, (_, _) => Delete()));
        edit.DropDownItems.Add(new ToolStripSeparator());
        edit.DropDownItems.Add(Item("Select &All", Keys.Control | Keys.A, (_, _) => _session.SelectAll()));

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add(_showToolbox);
        view.DropDownItems.Add(_showProperties);
        view.DropDownItems.Add(new ToolStripSeparator());
        view.DropDownItems.Add(_snapToGrid);
        view.DropDownItems.Add(_showGuides);

        var format = new ToolStripMenuItem("F&ormat");
        format.DropDownItems.Add(Item("Bring to &Front", Keys.Control | Keys.Oem6, (_, _) => BringSelectionToFront()));
        format.DropDownItems.Add(Item("Send to &Back", Keys.Control | Keys.Oem4, (_, _) => SendSelectionToBack()));
        format.DropDownItems.Add(new ToolStripSeparator());
        format.DropDownItems.Add(Item("&Align Left", Keys.Control | Keys.Alt | Keys.Left, (_, _) => Align(DesignerAlign.Left)));
        format.DropDownItems.Add(Item("&Center Horizontally", Keys.Control | Keys.Alt | Keys.D, (_, _) => Align(DesignerAlign.CenterHorizontal)));
        format.DropDownItems.Add(Item("Align &Right", Keys.Control | Keys.Alt | Keys.Right, (_, _) => Align(DesignerAlign.Right)));
        format.DropDownItems.Add(Item("Align &Top", Keys.Control | Keys.Alt | Keys.T, (_, _) => Align(DesignerAlign.Top)));
        format.DropDownItems.Add(Item("Center &Vertically", Keys.Control | Keys.Alt | Keys.M, (_, _) => Align(DesignerAlign.CenterVertical)));
        format.DropDownItems.Add(Item("Align &Bottom", Keys.Control | Keys.Alt | Keys.B, (_, _) => Align(DesignerAlign.Bottom)));

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(Item("&About...", Keys.None, (_, _) => ShowAbout()));

        _menu.Items.Add(file);
        _menu.Items.Add(edit);
        _menu.Items.Add(view);
        _menu.Items.Add(format);
        _menu.Items.Add(help);
    }

    private static ToolStripMenuItem Item(string text, Keys shortcut, EventHandler handler)
    {
        var item = new ToolStripMenuItem(text, null, handler);
        if (shortcut != Keys.None)
        {
            item.ShortcutKeys = shortcut;
        }

        return item;
    }

    private void ConfigureToggle(ToolStripMenuItem item, string text, Action handler)
    {
        item.Text = text;
        item.CheckOnClick = true;
        item.Checked = true;
        item.Click += (_, _) => handler();
    }

    // ---------------------------------------------------------------------
    // Toolbar and status bar
    // ---------------------------------------------------------------------

    private void BuildToolbar()
    {
        _toolbar.GripStyle = ToolStripGripStyle.Hidden;
        _toolbar.RenderMode = ToolStripRenderMode.System;

        _toolbar.Items.Add(ToolButton("New", NewDocument));
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(ToolButton("Undo", Undo));
        _toolbar.Items.Add(ToolButton("Redo", Redo));
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(ToolButton("Cut", Cut));
        _toolbar.Items.Add(ToolButton("Copy", Copy));
        _toolbar.Items.Add(ToolButton("Paste", Paste));
        _toolbar.Items.Add(ToolButton("Delete", Delete));
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(ToolButton("Front", BringSelectionToFront));
        _toolbar.Items.Add(ToolButton("Back", SendSelectionToBack));
        _toolbar.Items.Add(new ToolStripSeparator());
        _toolbar.Items.Add(ToolButton("Code", GenerateCode));
    }

    private static ToolStripButton ToolButton(string text, Action handler)
    {
        var button = new ToolStripButton(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            AutoSize = true,
        };

        button.Click += (_, _) => handler();
        return button;
    }

    private void BuildStatusBar()
    {
        _statusSelection.Spring = true;
        _statusSelection.TextAlign = ContentAlignment.MiddleLeft;

        _statusPointer.TextAlign = ContentAlignment.MiddleRight;
        _statusPointer.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _statusPointer.BorderStyle = Border3DStyle.Etched;

        _status.Items.Add(_statusSelection);
        _status.Items.Add(_statusPointer);
    }

    // ---------------------------------------------------------------------
    // Commands
    // ---------------------------------------------------------------------

    private void NewDocument()
    {
        var previous = _session;

        _session = new DesignSession();
        _surface.Session = _session;
        _properties.Session = _session;

        previous.RootForm.Dispose();

        FocusSurface();
        UpdateTitle();
        UpdateStatus();
    }

    private void OpenFile()
    {
        try
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Designer files (*.designer.cs)|*.designer.cs|C# files (*.cs)|*.cs|All files (*.*)|*.*",
                Title = "Open a form to design",
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var document = DesignerFileLoader.Load(dialog.FileName);
            if (document is null)
            {
                MessageBox.Show(
                    this,
                    "Could not parse the selected file as a WinForms designer file.",
                    "Open",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var previous = _session;
            _session = new DesignSession();
            _surface.Session = _session;
            _properties.Session = _session;

            _session.LoadDocument(document);
            previous.RootForm.Dispose();

            FocusSurface();
            UpdateTitle();
            UpdateStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Error opening file:\n{ex.Message}\n\n{ex.StackTrace}",
                "Open Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void Undo()
    {
        _session.Undo();
        FocusSurface();
    }

    private void Redo()
    {
        _session.Redo();
        FocusSurface();
    }

    private void Cut()
    {
        _session.CutSelection();
        FocusSurface();
    }

    private void Copy()
    {
        _session.CopySelection();
        FocusSurface();
    }

    private void Paste()
    {
        _session.Paste();
        FocusSurface();
    }

    private void Delete()
    {
        _session.DeleteSelection();
        FocusSurface();
    }
    private void BringSelectionToFront()
    {
        _session.BringToFront();
        FocusSurface();
    }
    private void SendSelectionToBack()
    {
        _session.SendToBack();
        FocusSurface();
    }

    private void Align(DesignerAlign alignment)
    {
        _session.Align(alignment);
        FocusSurface();
    }

    private void ToggleSnapToGrid() => _surface.SnapToGrid = _snapToGrid.Checked;

    private void ToggleGuides() => _surface.ShowAlignmentGuides = _showGuides.Checked;

    private void ToggleToolbox()
    {
        _shell.ShowToolbox = _showToolbox.Checked;
    }

    private void ToggleProperties()
    {
        _shell.ShowProperties = _showProperties.Checked;
    }

    private void OnControlRequested(object? sender, ToolboxEntryEventArgs e)
    {
        var root = _session.RootForm;
        var size = e.Entry.DefaultSize;
        var location = new Point(
            Math.Max(0, (root.ClientSize.Width - size.Width) / 2),
            Math.Max(0, (root.ClientSize.Height - size.Height) / 2));

        _session.AddControl(e.Entry.ControlType, location);
        FocusSurface();
    }

    private void OnPointerMoved(object? sender, DesignerPointEventArgs e)
    {
        _statusPointer.Text = $"{e.Location.X}, {e.Location.Y}";
    }

    private void GenerateCode()
    {
        using var dialog = new GenerateCodeDialog(_session.Document);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            FocusSurface();
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            this,
            "WinForms Designer" + Environment.NewLine + Environment.NewLine
                + "A standalone Visual Studio style designer for Windows Forms." + Environment.NewLine
                + "It runs on .NET 10 and needs no design time assemblies.",
            "About",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    // ---------------------------------------------------------------------
    // Housekeeping
    // ---------------------------------------------------------------------

    private void FocusSurface() => _surface.Focus();

    private void UpdateTitle()
    {
        Text = $"{_session.Document.ClassName} - WinForms Designer";
    }

    private void UpdateStatus()
    {
        var node = _session.SelectedNode;
        if (node is null)
        {
            _statusSelection.Text = "No selection";
            return;
        }

        var live = _session.GetLive(node);
        var size = live is null ? string.Empty : $"  {live.Size.Width} x {live.Size.Height}";
        _statusSelection.Text = $"{node.ControlType.Name}  ({node.Name}){size}";
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // The design surface keeps the focus while the user works on the form, so the
        // common editing keys are handled here.
        if (_surface.Focused)
        {
            switch (keyData)
            {
                case Keys.Delete:
                    Delete();
                    return true;
                case Keys.Control | Keys.C:
                    Copy();
                    return true;
                case Keys.Control | Keys.X:
                    Cut();
                    return true;
                case Keys.Control | Keys.V:
                    Paste();
                    return true;
                case Keys.Control | Keys.Z:
                    Undo();
                    return true;
                case Keys.Control | Keys.Y:
                    Redo();
                    return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
