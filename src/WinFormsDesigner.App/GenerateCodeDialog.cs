using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WinFormsDesigner.Core.Codegen;
using WinFormsDesigner.Core.Model;

namespace WinFormsDesigner.App;

/// <summary>
/// Lets the user review the generated code and choose where it is written to.
/// </summary>
internal sealed class GenerateCodeDialog : Form
{
    private readonly DesignDocument _document;
    private readonly CSharpFormGenerator _generator = new();

    private readonly TextBox _className = new();
    private readonly TextBox _namespace = new();
    private readonly TextBox _project = new();
    private readonly TextBox _folder = new();
    private readonly TextBox _preview = new();

    private readonly Button _browse = new();
    private readonly Button _copy = new();
    private readonly Button _ok = new();
    private readonly Button _cancel = new();

    public GenerateCodeDialog(DesignDocument document)
    {
        _document = document;

        Text = "Generate Code";
        Size = new Size(900, 680);
        MinimumSize = new Size(640, 480);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        Font = SystemFonts.MessageBoxFont;

        _className.Text = document.ClassName;
        _namespace.Text = document.Namespace;
        _project.Text = document.ProjectName;
        _folder.Text = document.OutputDirectory;

        _className.Dock = DockStyle.Fill;
        _namespace.Dock = DockStyle.Fill;
        _project.Dock = DockStyle.Fill;
        _folder.Dock = DockStyle.Fill;

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 4,
            AutoSize = true,
            Padding = new Padding(8, 8, 8, 4),
        };

        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        fields.Controls.Add(Caption("Class name"), 0, 0);
        fields.Controls.Add(_className, 1, 0);
        fields.Controls.Add(Caption("Namespace"), 0, 1);
        fields.Controls.Add(_namespace, 1, 1);
        fields.Controls.Add(Caption("Project name"), 0, 2);
        fields.Controls.Add(_project, 1, 2);
        fields.Controls.Add(Caption("Output folder"), 0, 3);

        _browse.Text = "Browse...";
        _browse.AutoSize = true;
        _browse.Dock = DockStyle.Right;
        _browse.Click += OnBrowse;

        var folderRow = new Panel { Dock = DockStyle.Fill, Height = 26 };
        folderRow.Controls.Add(_folder);
        folderRow.Controls.Add(_browse);
        fields.Controls.Add(folderRow, 1, 3);

        _preview.Dock = DockStyle.Fill;
        _preview.Multiline = true;
        _preview.ReadOnly = true;
        _preview.ScrollBars = ScrollBars.Both;
        _preview.WordWrap = false;
        _preview.Font = new Font(FontFamily.GenericMonospace, 9f);
        _preview.BackColor = SystemColors.Window;
        _preview.Margin = new Padding(8, 0, 8, 0);

        _copy.Text = "Copy";
        _copy.AutoSize = true;
        _copy.Click += OnCopy;

        _ok.Text = "Save";
        _ok.AutoSize = true;
        _ok.Click += OnSave;

        _cancel.Text = "Cancel";
        _cancel.AutoSize = true;
        _cancel.DialogResult = DialogResult.Cancel;

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(8),
        };

        bottom.Controls.Add(_cancel);
        bottom.Controls.Add(_ok);
        bottom.Controls.Add(_copy);

        Controls.Add(_preview);
        Controls.Add(fields);
        Controls.Add(bottom);

        _className.TextChanged += (_, _) => UpdatePreview();
        _namespace.TextChanged += (_, _) => UpdatePreview();
        _project.TextChanged += (_, _) => UpdatePreview();

        UpdatePreview();
    }

    private static Label Caption(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoSize = false,
    };

    private void UpdatePreview()
    {
        _document.ClassName = SanitizeIdentifier(_className.Text, "Form1");
        _document.Namespace = SanitizeIdentifier(_namespace.Text, "MyApp");
        _document.ProjectName = SanitizeIdentifier(_project.Text, "MyApp");
        _document.Root.Name = _document.ClassName;

        try
        {
            var generated = _generator.Generate(_document);
            _preview.Text = generated.Files[$"{_document.ClassName}.designer.cs"];
        }
        catch (Exception ex)
        {
            _preview.Text = "The code could not be generated:" + Environment.NewLine + ex.Message;
        }
    }

    private void OnBrowse(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose the folder the generated code is written to.",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_folder.Text) ? _folder.Text : string.Empty,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _folder.Text = dialog.SelectedPath;
        }
    }

    private void OnCopy(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_preview.Text))
        {
            return;
        }

        Clipboard.SetText(_preview.Text);
    }

    private void OnSave(object? sender, EventArgs e)
    {
        var folder = _folder.Text.Trim();
        if (folder.Length == 0)
        {
            MessageBox.Show(this, "Please choose an output folder.", "Generate Code", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Directory.CreateDirectory(folder);
            _document.OutputDirectory = folder;

            var generated = _generator.GenerateProject(_document);
            var existing = generated.Files
                .Select(f => Path.Combine(folder, f.Key))
                .Where(File.Exists)
                .ToList();
            if (existing.Count > 0)
            {
                var result = MessageBox.Show(
                    this,
                    "The following file(s) already exist in the selected folder:" + Environment.NewLine
                        + string.Join(Environment.NewLine, existing.Select(Path.GetFileName)) + Environment.NewLine + Environment.NewLine
                        + "Do you want to overwrite them?",
                    "Confirm overwrite",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            foreach (var (name, content) in generated.Files)
            {
                var path = Path.Combine(folder, name);
                File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }

            MessageBox.Show(
                this,
                $"{generated.Files.Count} file(s) were written to{Environment.NewLine}{folder}",
                "Code generated",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "The files could not be written", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>Turns arbitrary text into a valid C# identifier.</summary>
    private static string SanitizeIdentifier(string text, string fallback)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            return fallback;
        }

        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            builder.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }

        var result = builder.ToString();
        if (char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        return result;
    }
}
