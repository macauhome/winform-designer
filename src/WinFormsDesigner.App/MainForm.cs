using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WinFormsDesigner.App.Designer;
using WinFormsDesigner.App.Layout;
using WinFormsDesigner.App.Properties;
using WinFormsDesigner.App.Toolbox;
using Krypton.Ribbon;
using Krypton.Toolkit;
using WinFormsDesigner.Core.Codegen;
using WinFormsDesigner.Core.Model;

namespace WinFormsDesigner.App;

/// <summary>
/// The main window of the designer. It arranges the toolbox, the design surface and the
/// property grid the way the Visual Studio designer does and hosts the editing commands.
/// </summary>
public sealed class MainForm : KryptonForm
{
    private const int ToolboxWidth = 230;
    private const int PropertiesWidth = 330;

    private readonly MenuStrip _menu = new();
    private readonly KryptonRibbon _ribbon = new();
    private readonly Panel _ribbonHost = new() { Dock = DockStyle.None };

    // CoreUI free icon font (embedded resource).
    private static readonly PrivateFontCollection CoreUiFonts = new();
    private static IntPtr _coreUiFontMemory = IntPtr.Zero;
    private static FontFamily? _coreUiFontFamily;

    private const char IconNew = '\uF1A1';
    private const char IconOpen = '\uF24D';
    private const char IconSave = '\uF188';
    private const char IconCode = '\uF28F';
    private const char IconCut = '\uF279';
    private const char IconCopy = '\uF280';
    private const char IconPaste = '\uF297';
    private const char IconDelete = '\uF134';
    private const char IconUndo = '\uF32E';
    private const char IconRedo = '\uF32F';
    private const char IconFront = '\uF309';
    private const char IconBack = '\uF31C';
    private const char IconToolbox = '\uF31F';
    private const char IconProps = '\uF181';
    private const char IconSnap = '\uF227';
    private const char IconGuides = '\uF1FA';
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
    private readonly ToolStripMenuItem _showRuler = new();

    private KryptonRibbonGroupButton _ribbonToolbox = new();
    private KryptonRibbonGroupButton _ribbonProps = new();
    private KryptonRibbonGroupButton _ribbonSnap = new();
    private KryptonRibbonGroupButton _ribbonGuides = new();
    private KryptonRibbonGroupButton _ribbonRuler = new();

    private DesignSession _session = new();
    private string? _currentFilePath;
    private string _savedSignature = string.Empty;

    public MainForm()
    {
        Text = "WinForms Designer";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1280, 820);
        MinimumSize = new Size(900, 600);
        Font = SystemFonts.MessageBoxFont;
        BackColor = Color.White;

        BuildLayout();
        BuildMenu();
        BuildRibbon();
        BuildStatusBar();

        AttachSession(_session);

        _surface.Session = _session;
        _properties.Session = _session;

        _toolbox.ControlRequested += OnControlRequested;
        _surface.PointerMoved += OnPointerMoved;

        _savedSignature = ComputeDocumentSignature(_session.Document);

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

        _shell.Dock = DockStyle.None;
        _menu.Dock = DockStyle.None;
        _ribbon.Dock = DockStyle.Fill;
        _status.Dock = DockStyle.None;

        _ribbonHost.Controls.Add(_ribbon);

        Controls.Add(_shell);
        Controls.Add(_menu);
        Controls.Add(_ribbonHost);
        Controls.Add(_status);

        Resize += (_, _) => LayoutChrome();
        LayoutChrome();
    }

    private void LayoutChrome()
    {
        var width = ClientSize.Width;
        if (width <= 0)
        {
            return;
        }

        _menu.Location = new Point(0, 0);
        _menu.Width = width;

        var ribbonHeight = _ribbon.GetPreferredSize(new Size(width, 0)).Height;
        if (ribbonHeight <= 0)
        {
            ribbonHeight = _ribbon.Height > 0 ? _ribbon.Height : 150;
        }
        _ribbonHost.Location = new Point(0, _menu.Bottom);
        _ribbonHost.Size = new Size(width, ribbonHeight);

        _status.Location = new Point(0, ClientSize.Height - _status.Height);
        _status.Width = width;

        _shell.Location = new Point(0, _ribbonHost.Bottom);
        _shell.Size = new Size(width, Math.Max(0, _status.Top - _ribbonHost.Bottom));
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
        ConfigureToggle(_showRuler, "Ruler", ToggleRuler);

        _showToolbox.Checked = true;
        _showProperties.Checked = true;
        _showGuides.Checked = true;
        _showRuler.Checked = true;

        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(Item("&New Form", Keys.Control | Keys.N, (_, _) => NewDocument()));
        file.DropDownItems.Add(Item("&Open...", Keys.Control | Keys.O, (_, _) => OpenFile()));
        file.DropDownItems.Add(Item("&Save", Keys.Control | Keys.S, (_, _) => Save()));
        file.DropDownItems.Add(Item("Save &As...", Keys.Control | Keys.Shift | Keys.S, (_, _) => GenerateCode()));
        file.DropDownItems.Add(Item("&Generate Code...", Keys.Control | Keys.G, (_, _) => GenerateCode()));
        file.DropDownItems.Add(new ToolStripSeparator());
        //file.DropDownItems.Add(Item("Exit", Keys.None, (_, _) => Close()));
        file.DropDownItems.Add(Item("Exit", Keys.Alt | Keys.F4, (_, _) => Close()));

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
        view.DropDownItems.Add(_showRuler);

        var format = new ToolStripMenuItem("F&ormat");
        format.DropDownItems.Add(Item("Bring to &Back", Keys.Control | Keys.B, (_, _) => BringSelectionToFront()));
        format.DropDownItems.Add(Item("Send to &Font", Keys.Control | Keys.F, (_, _) => SendSelectionToBack()));
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
        item.CheckOnClick = false;
        item.Click += (_, _) => handler();
    }

    // ---------------------------------------------------------------------
    // Toolbar and status bar
    // ---------------------------------------------------------------------

    private void BuildRibbon()
    {
        _ribbon.PaletteMode = PaletteMode.Office2010Blue;

        var home = new KryptonRibbonTab { Text = "Home" };

        var fileGroup = new KryptonRibbonGroup { TextLine1 = "File" };
        var fileLines = new KryptonRibbonGroupLines();
        AddRibbonButton(fileLines, "New", IconNew, NewDocument, Keys.Control | Keys.N);
        AddRibbonButton(fileLines, "Open", IconOpen, OpenFile, Keys.Control | Keys.O);
        AddRibbonButton(fileLines, "Save", IconSave, Save, Keys.Control | Keys.S);
        AddRibbonButton(fileLines, "Code", IconCode, GenerateCode, Keys.Control | Keys.G);
        fileGroup.Items.Add(fileLines);
        home.Groups.Add(fileGroup);

        var clipGroup = new KryptonRibbonGroup { TextLine1 = "Clipboard" };
        var clipLines = new KryptonRibbonGroupLines();
        AddRibbonButton(clipLines, "Cut", IconCut, Cut, Keys.Control | Keys.X);
        AddRibbonButton(clipLines, "Copy", IconCopy, Copy, Keys.Control | Keys.C);
        AddRibbonButton(clipLines, "Paste", IconPaste, Paste, Keys.Control | Keys.V);
        AddRibbonButton(clipLines, "Delete", IconDelete, Delete, Keys.Delete);
        clipGroup.Items.Add(clipLines);
        home.Groups.Add(clipGroup);

        var historyGroup = new KryptonRibbonGroup { TextLine1 = "History" };
        var historyLines = new KryptonRibbonGroupLines();
        AddRibbonButton(historyLines, "Undo", IconUndo, Undo, Keys.Control | Keys.Z);
        AddRibbonButton(historyLines, "Redo", IconRedo, Redo, Keys.Control | Keys.Y);
        historyGroup.Items.Add(historyLines);
        home.Groups.Add(historyGroup);

        var arrangeGroup = new KryptonRibbonGroup { TextLine1 = "Arrange" };
        var arrangeLines = new KryptonRibbonGroupLines();
        AddRibbonButton(arrangeLines, "Back", IconBack, BringSelectionToFront);
        AddRibbonButton(arrangeLines, "Front", IconFront, SendSelectionToBack);
        arrangeGroup.Items.Add(arrangeLines);
        home.Groups.Add(arrangeGroup);

        var viewGroup = new KryptonRibbonGroup { TextLine1 = "View" };
        var viewLines = new KryptonRibbonGroupLines();
        _ribbonToolbox = AddRibbonButton(viewLines, "Toolbox", IconToolbox, ToggleToolbox);
        SetRibbonToggleIcon(_ribbonToolbox, IconToolbox, _showToolbox.Checked);
        _ribbonToolbox.Checked = _showToolbox.Checked;
        _ribbonProps = AddRibbonButton(viewLines, "Props", IconProps, ToggleProperties);
        SetRibbonToggleIcon(_ribbonProps, IconProps, _showProperties.Checked);
        _ribbonProps.Checked = _showProperties.Checked;
        _ribbonSnap = AddRibbonButton(viewLines, "Snap", IconSnap, ToggleSnapToGrid);
        SetRibbonToggleIcon(_ribbonSnap, IconSnap, _snapToGrid.Checked);
        _ribbonSnap.Checked = _snapToGrid.Checked;
        _ribbonGuides = AddRibbonButton(viewLines, "Guides", IconGuides, ToggleGuides);
        SetRibbonToggleIcon(_ribbonGuides, IconGuides, _showGuides.Checked);
        _ribbonGuides.Checked = _showGuides.Checked;
        _ribbonRuler = AddRibbonButton(viewLines, "Ruler", IconGuides, ToggleRuler);
        SetRibbonToggleIcon(_ribbonRuler, IconGuides, _showRuler.Checked);
        _ribbonRuler.Checked = _showRuler.Checked;
        viewGroup.Items.Add(viewLines);
        home.Groups.Add(viewGroup);

        _ribbon.RibbonTabs.Add(home);
        _ribbon.SelectedTab = home;
    }

    private KryptonRibbonGroupButton AddRibbonButton(KryptonRibbonGroupLines lines, string text, char iconGlyph, Action handler, Keys shortcut = Keys.None)
    {
        var button = new KryptonRibbonGroupButton
        {
            TextLine1 = text,
            ImageLarge = CreateRibbonIcon(iconGlyph.ToString(), 32),
            ImageSmall = CreateRibbonIcon(iconGlyph.ToString(), 16),
            ShortcutKeys = shortcut,
        };
        button.Click += (_, _) => handler();
        lines!.Items!.Add(button);
        return button;
    }

    private static void SetRibbonToggleIcon(KryptonRibbonGroupButton button, char glyph, bool active)
    {
        var color = active ? Color.RoyalBlue : Color.Black;
        button.ImageLarge = CreateRibbonIcon(glyph.ToString(), 32, color);
        button.ImageSmall = CreateRibbonIcon(glyph.ToString(), 16, color);
    }
    private static Image CreateRibbonIcon(string glyph, int size, Color? color = null)
    {
        EnsureCoreUiFont();
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var font = new Font(_coreUiFontFamily!, size * 0.7f, FontStyle.Regular, GraphicsUnit.Pixel);
        var measured = g.MeasureString(glyph, font);
        using var brush = new SolidBrush(color ?? Color.Black);
        g.DrawString(glyph, font, brush, (size - measured.Width) / 2f, (size - measured.Height) / 2f);
        return bmp;
    }

private static void EnsureCoreUiFont()
    {
        if (_coreUiFontFamily != null) return;
        var asm = typeof(MainForm).Assembly;
        const string resourceName = "WinFormsDesigner.App.Assets.CoreUI-Icons-Free.ttf";
        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException("Embedded icon font resource not found: " + resourceName);
        var data = new byte[stream.Length];
        stream.ReadExactly(data, 0, data.Length);
        _coreUiFontMemory = Marshal.AllocCoTaskMem(data.Length);
        Marshal.Copy(data, 0, _coreUiFontMemory, data.Length);
        CoreUiFonts.AddMemoryFont(_coreUiFontMemory, data.Length);
        _coreUiFontFamily = CoreUiFonts.Families[0];
    }
private void BuildStatusBar()
    {
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
        if (!ConfirmDiscardChanges())
        {
            return;
        }

        var previous = _session;

        var session = new DesignSession();
        AttachSession(session);
        _surface.Session = _session;
        _properties.Session = _session;

        previous.RootForm.Dispose();

        _currentFilePath = null;
        _savedSignature = ComputeDocumentSignature(_session.Document);

        FocusSurface();
        UpdateTitle();
        UpdateStatus();
    }

    private void OpenFile()
    {
        if (!ConfirmDiscardChanges())
        {
            return;
        }

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
            var session = new DesignSession();
            AttachSession(session);
            _surface.Session = _session;
            _properties.Session = _session;

            _session.LoadDocument(document);
            previous.RootForm.Dispose();

            _currentFilePath = dialog.FileName;
            _savedSignature = ComputeDocumentSignature(_session.Document);

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

    private void ToggleSnapToGrid()
    {
        _snapToGrid.Checked = !_snapToGrid.Checked;
        _surface.SnapToGrid = _snapToGrid.Checked;
        _ribbonSnap.Checked = _snapToGrid.Checked;
        SetRibbonToggleIcon(_ribbonSnap, IconSnap, _snapToGrid.Checked);
    }

    private void ToggleGuides()
    {
        _showGuides.Checked = !_showGuides.Checked;
        _surface.ShowAlignmentGuides = _showGuides.Checked;
        _ribbonGuides.Checked = _showGuides.Checked;
        SetRibbonToggleIcon(_ribbonGuides, IconGuides, _showGuides.Checked);
    }

    private void ToggleRuler()
    {
        _showRuler.Checked = !_showRuler.Checked;
        _surface.ShowRuler = _showRuler.Checked;
        _ribbonRuler.Checked = _showRuler.Checked;
        SetRibbonToggleIcon(_ribbonRuler, IconGuides, _showRuler.Checked);
    }

    private void ToggleToolbox()
    {
        _showToolbox.Checked = !_showToolbox.Checked;
        _shell.ShowToolbox = _showToolbox.Checked;
        _ribbonToolbox.Checked = _showToolbox.Checked;
        SetRibbonToggleIcon(_ribbonToolbox, IconToolbox, _showToolbox.Checked);
    }

    private void ToggleProperties()
    {
        _showProperties.Checked = !_showProperties.Checked;
        _shell.ShowProperties = _showProperties.Checked;
        _ribbonProps.Checked = _showProperties.Checked;
        SetRibbonToggleIcon(_ribbonProps, IconProps, _showProperties.Checked);
    }

    private void OnControlRequested(object? sender, ToolboxEntryEventArgs e)
    {
        var parentNode = _session.GetInsertionParent();
        var parent = _session.GetLive(parentNode) ?? _session.RootForm;
        var size = e.Entry.DefaultSize;
        var location = new Point(
            Math.Max(0, (parent.ClientSize.Width - size.Width) / 2),
            Math.Max(0, (parent.ClientSize.Height - size.Height) / 2));

        _session.AddControl(e.Entry.ControlType, location);
        FocusSurface();
    }

    private void OnPointerMoved(object? sender, DesignerPointEventArgs e)
    {
        _statusPointer.Text = $"{e.Location.X}, {e.Location.Y}";
    }

    private void Save()
    {
        if (TrySaveToOriginalFile())
        {
            FocusSurface();
            return;
        }

        GenerateCode();
    }

    private bool TrySaveToOriginalFile()
    {
        if (string.IsNullOrWhiteSpace(_currentFilePath) || !File.Exists(_currentFilePath))
        {
            return false;
        }

        try
        {
            var original = File.ReadAllText(_currentFilePath);
            var patched = DesignerFileLoader.PatchSource(original, _session.Document);
            File.WriteAllText(_currentFilePath, patched, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            _savedSignature = ComputeDocumentSignature(_session.Document);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    private void GenerateCode()
    {
        using var dialog = new GenerateCodeDialog(_session.Document);

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _savedSignature = ComputeDocumentSignature(_session.Document);
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

    private void AttachSession(DesignSession session)
    {
        if (_session is not null && !ReferenceEquals(_session, session))
        {
            _session.SelectionChanged -= Session_SelectionChanged;
            _session.Changed -= Session_Changed;
        }

        _session = session;
        _session.SelectionChanged += Session_SelectionChanged;
        _session.Changed += Session_Changed;
    }

    private void Session_SelectionChanged(object? sender, EventArgs e) => UpdateStatus();

    private void Session_Changed(object? sender, EventArgs e) => UpdateTitle();

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!ConfirmDiscardChanges())
        {
            e.Cancel = true;
            return;
        }

        base.OnFormClosing(e);
    }

    private bool IsDirty()
    {
        return !string.Equals(ComputeDocumentSignature(_session.Document), _savedSignature, StringComparison.Ordinal);
    }

    private bool ConfirmDiscardChanges()
    {
        if (!IsDirty())
        {
            return true;
        }

        var result = MessageBox.Show(
            this,
            "The current form has unsaved changes. Do you want to save them before continuing?",
            "Unsaved changes",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        if (result == DialogResult.Cancel)
        {
            return false;
        }

        if (result == DialogResult.Yes)
        {
            if (TrySaveToOriginalFile())
            {
                return true;
            }

            using var dialog = new GenerateCodeDialog(_session.Document);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return false;
            }

            _savedSignature = ComputeDocumentSignature(_session.Document);
        }

        return true;
    }

    private static string ComputeDocumentSignature(DesignDocument document)
    {
        try
        {
            var generated = new CSharpFormGenerator().Generate(document);
            var builder = new StringBuilder();
            foreach (var pair in generated.Files.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                builder.Append(pair.Key);
                builder.Append('\u0001');
                builder.Append(pair.Value);
                builder.Append('\u0002');
            }

            var bytes = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(builder.ToString()));
            return Convert.ToHexString(bytes);
        }
        catch
        {
            return Guid.NewGuid().ToString("N");
        }
    }

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

        var count = _session.SelectedNodes.Count;
        var live = _session.GetLive(node);
        var size = live is null ? string.Empty : $"  {live.Size.Width} x {live.Size.Height}";
        if (count > 1)
        {
            _statusSelection.Text = $"{count} controls selected{size}";
        }
        else
        {
            _statusSelection.Text = $"{node.ControlType.Name}  ({node.Name}){size}";
        }
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
