using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinFormsDesigner.Core.Model;
using WinFormsDesigner.Core.Reflection;

namespace WinFormsDesigner.App.Properties;

/// <summary>
/// Creates the inline editors used by the property grid and hosts the modal editors that
/// are opened for values such as colours, fonts and collections.
/// </summary>
internal static class PropertyGridEditors
{
    private static readonly string[] CommonEventNames =
    [
        "Click",
        "DoubleClick",
        "TextChanged",
        "MouseClick",
        "MouseDown",
        "MouseUp",
        "KeyDown",
        "KeyPress",
        "KeyUp",
        "Enter",
        "Leave",
        "Load",
        "FormClosing",
        "FormClosed",
        "SelectedIndexChanged",
        "CheckedChanged",
        "ValueChanged",
        "Scroll",
        "LinkClicked",
        "NodeMouseClick",
        "CellClick",
        "ItemCheck",
    ];

    /// <summary>Returns the events of a control type that are offered in the grid.</summary>
    public static IReadOnlyList<string> GetCommonEvents(Type controlType)
    {
        var result = new List<string>();
        foreach (var name in CommonEventNames)
        {
            if (controlType.GetEvent(name) is not null)
            {
                result.Add(name);
            }
        }

        return result;
    }

    /// <summary>
    /// Creates the inline editor for a property. Properties that are edited through a modal
    /// dialog do not need an inline editor and return <c>null</c>.
    /// </summary>
    public static Control? CreateEditor(PropertyGridView grid, DesignNode node, PropertyEntry entry, object? value)
    {
        return entry.Editor switch
        {
            PropertyEditorKind.Text or PropertyEditorKind.MultilineText or PropertyEditorKind.Number => CreateText(grid, entry, value),
            PropertyEditorKind.Enumeration => CreateEnumeration(grid, entry, value),
            PropertyEditorKind.Reference => CreateReference(grid, node, entry, value),
            _ => null,
        };
    }

    // ---------------------------------------------------------------------
    // Modal editors
    // ---------------------------------------------------------------------

    /// <summary>Opens the colour picker for a colour property.</summary>
    public static void ShowColorDialog(PropertyGridView grid, PropertyEntry entry, object? value)
    {
        using var dialog = new ColorDialog
        {
            FullOpen = true,
            Color = value is Color color && !color.IsEmpty ? color : SystemColors.Control,
        };

        if (dialog.ShowDialog(grid) == DialogResult.OK)
        {
            grid.Commit(entry.Info.Name, dialog.Color);
        }
    }

    /// <summary>Opens the font picker for a font property.</summary>
    public static void ShowFontDialog(PropertyGridView grid, PropertyEntry entry, object? value)
    {
        using var dialog = new FontDialog
        {
            Font = value as Font ?? SystemFonts.DefaultFont,
        };

        if (dialog.ShowDialog(grid) == DialogResult.OK)
        {
            grid.Commit(entry.Info.Name, dialog.Font);
        }
    }

    /// <summary>Opens a file picker for an image property.</summary>
    public static void ShowImageDialog(PropertyGridView grid, DesignNode node, PropertyEntry entry)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico|All files|*.*",
            Title = "Select image",
        };

        if (dialog.ShowDialog(grid) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var bytes = File.ReadAllBytes(dialog.FileName);
            var key = node.Name + "_" + entry.Info.Name;
            grid.Commit(entry.Info.Name, new ImageAsset(key, bytes, dialog.FileName));
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(grid, ex.Message, "Image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>Opens the collection editor for a property that exposes a child collection.</summary>
    public static void ShowCollectionDialog(PropertyGridView grid, DesignNode node, PropertyEntry entry)
    {
        using var dialog = new CollectionEditorDialog
        {
            Items = ReadCollectionItems(grid, node, entry),
            Text = entry.DisplayName + " Editor",
        };

        if (dialog.ShowDialog(grid) == DialogResult.OK)
        {
            grid.Commit(entry.Info.Name, dialog.Items);
        }
    }

    // ---------------------------------------------------------------------
    // Inline editors
    // ---------------------------------------------------------------------

    private static Control CreateText(PropertyGridView grid, PropertyEntry entry, object? value)
    {
        var reportErrors = entry.Editor != PropertyEditorKind.Number;

        var textBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 2, 3, 2),
            Text = FormatText(entry, value),
        };

        textBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                grid.CancelEdit();
                return;
            }

            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.SuppressKeyPress = true;
            CommitText(grid, entry, textBox, reportErrors);
            grid.EndEdit();
        };

        textBox.LostFocus += (_, _) => CommitText(grid, entry, textBox, reportErrors);

        // The grid asks the editor to store the typed value before it closes the row.
        textBox.Tag = (Action)(() => CommitText(grid, entry, textBox, reportErrors));

        return textBox;
    }

    private static void CommitText(PropertyGridView grid, PropertyEntry entry, TextBox textBox, bool reportErrors)
    {
        if (textBox.IsDisposed)
        {
            return;
        }

        var text = textBox.Text;

        switch (entry.Info.Name)
        {
            case "Location":
                if (ControlFactory.TryParsePoint(text, out var point))
                {
                    grid.Commit(entry.Info.Name, point);
                }

                return;

            case "Size":
                if (ControlFactory.TryParseSize(text, out var size))
                {
                    grid.Commit(entry.Info.Name, size);
                }

                return;

            case "Padding":
                if (TryParsePadding(text, out var padding))
                {
                    grid.Commit(entry.Info.Name, padding);
                }

                return;

            case "Margin":
                if (TryParsePadding(text, out var margin))
                {
                    grid.Commit(entry.Info.Name, margin);
                }

                return;

            case "Tag":
                grid.Commit(entry.Info.Name, text);
                return;
        }

        if (!entry.TryConvertFromText(text, out var value, out var error))
        {
            if (reportErrors && !string.IsNullOrEmpty(error))
            {
                System.Windows.Forms.MessageBox.Show(grid, error, "Invalid value", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return;
        }

        grid.Commit(entry.Info.Name, value);
    }

    private static Control CreateEnumeration(PropertyGridView grid, PropertyEntry entry, object? value)
    {
        var comboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 2, 3, 2),
        };

        var type = Nullable.GetUnderlyingType(entry.ValueType) ?? entry.ValueType;
        foreach (var name in Enum.GetNames(type))
        {
            comboBox.Items.Add(name);
        }

        if (value is not null)
        {
            comboBox.SelectedItem = value.ToString();
        }
        else if (comboBox.Items.Count > 0)
        {
            comboBox.SelectedIndex = 0;
        }

        comboBox.SelectedIndexChanged += (_, _) =>
        {
            if (comboBox.SelectedItem is string name)
            {
                var parsed = Enum.Parse(type, name);
                grid.Commit(entry.Info.Name, parsed);
            }
        };

        return comboBox;
    }

    private static Control CreateReference(PropertyGridView grid, DesignNode node, PropertyEntry entry, object? value)
    {
        var comboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 2, 3, 2),
        };

        comboBox.Items.Add("(none)");

        var root = grid.Session?.Document.Root ?? node;

        foreach (var candidate in root.SelfAndDescendants())
        {
            if (candidate == node)
            {
                continue;
            }

            if (!IsAssignableTo(entry.ValueType, candidate.ControlType))
            {
                continue;
            }

            comboBox.Items.Add(candidate.Name);
        }

        if (value is string current && comboBox.Items.Contains(current))
        {
            comboBox.SelectedItem = current;
        }
        else
        {
            comboBox.SelectedIndex = 0;
        }

        comboBox.SelectedIndexChanged += (_, _) =>
        {
            var selected = comboBox.SelectedItem as string;
            grid.Commit(entry.Info.Name, selected == "(none)" ? null : selected);
        };

        return comboBox;
    }

    /// <summary>
    /// Determines whether a control can be assigned to a property of the given type.
    /// </summary>
    private static bool IsAssignableTo(Type valueType, Type controlType)
    {
        if (valueType == typeof(object))
        {
            // Without a more precise type there is no sensible candidate list.
            return false;
        }

        return valueType.IsAssignableFrom(controlType);
    }

    internal static Control CreateEventEditor(PropertyGridView grid, DesignNode node, string eventName)
    {
        var comboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            Dock = DockStyle.Fill,
            Margin = new Padding(3, 2, 3, 2),
        };

        comboBox.Items.Add("(none)");

        var defaultName = node.Name + "_" + eventName;
        comboBox.Items.Add(defaultName);

        if (node.Events.TryGetValue(eventName, out var handler))
        {
            comboBox.Text = handler;
        }
        else
        {
            comboBox.SelectedIndex = 0;
        }

        comboBox.SelectedIndexChanged += (_, _) =>
        {
            var text = comboBox.Text;
            grid.CommitEvent(eventName, text == "(none)" ? string.Empty : text);
        };

        return comboBox;
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static string FormatText(PropertyEntry entry, object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return entry.Info.Name switch
        {
            "Location" when value is Point point => $"{point.X}, {point.Y}",
            "Size" when value is Size size => $"{size.Width}, {size.Height}",
            "Padding" when value is Padding padding => $"{padding.Left}, {padding.Top}, {padding.Right}, {padding.Bottom}",
            "Margin" when value is Padding margin => $"{margin.Left}, {margin.Top}, {margin.Right}, {margin.Bottom}",
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static bool TryParsePadding(string text, out Padding padding)
    {
        padding = Padding.Empty;
        var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1 && int.TryParse(parts[0], out var all))
        {
            padding = new Padding(all);
            return true;
        }

        if (parts.Length != 4)
        {
            return false;
        }

        var values = new int[4];
        for (var i = 0; i < 4; i++)
        {
            if (!int.TryParse(parts[i], out values[i]))
            {
                return false;
            }
        }

        padding = new Padding(values[0], values[1], values[2], values[3]);
        return true;
    }

    /// <summary>Reads the current content of a collection property from the live control.</summary>
    public static List<string> ReadCollectionItems(PropertyGridView grid, DesignNode node, PropertyEntry entry)
    {
        var result = new List<string>();

        if (node.Properties.TryGetValue(entry.Info.Name, out var stored)
            && stored is System.Collections.IEnumerable storedItems
            && stored is not string)
        {
            foreach (var item in storedItems)
            {
                if (item is not null)
                {
                    result.Add(item.ToString() ?? string.Empty);
                }
            }

            return result;
        }

        var live = grid.Session?.GetLive(node);
        if (live is null)
        {
            return result;
        }

        try
        {
            if (entry.GetValue(live) is System.Collections.IEnumerable liveItems && liveItems is not string)
            {
                foreach (var item in liveItems)
                {
                    if (item is not null)
                    {
                        result.Add(item.ToString() ?? string.Empty);
                    }
                }
            }
        }
        catch (Exception)
        {
            // A control that refuses to expose its collection simply shows an empty editor.
        }

        return result;
    }
}
