using WinFormsDesigner.Core.Model;

namespace WinFormsDesigner.Core.Reflection;

/// <summary>
/// Metadata describing how a well known property should be presented in the property grid.
/// </summary>
public sealed record PropertyMetadata(
    string? Category = null,
    string? DisplayName = null,
    PropertyEditorKind? Editor = null,
    bool IsCollection = false,
    int Order = 500)
{
    /// <summary>The special category used for the <c>(Name)</c> row.</summary>
    public const string NameCategory = "(Name)";
}

/// <summary>
/// Curated metadata for the WinForms properties that are meaningful in a designer.
/// Anything that is not listed here and does not carry a
/// <see cref="System.ComponentModel.CategoryAttribute"/> is hidden, which keeps the property
/// grid focused and close to the Visual Studio original.
/// </summary>
public static class PropertyMetadataTable
{
    /// <summary>Category order used when grouping the property grid.</summary>
    public static readonly string[] CategoryOrder =
    [
        PropertyMetadata.NameCategory,
        "Layout",
        "Appearance",
        "Behavior",
        "Data",
        "Window Style",
        "Misc",
    ];

    private static readonly Dictionary<string, PropertyMetadata> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        // ---- identity -------------------------------------------------------
        ["Name"] = new PropertyMetadata(PropertyMetadata.NameCategory, "Name", PropertyEditorKind.Text, Order: 0),

        // ---- layout ---------------------------------------------------------
        ["AutoScaleMode"] = new("Layout", "AutoScaleMode", PropertyEditorKind.Enumeration, Order: 1),
        ["AutoScaleDimensions"] = new("Layout", "AutoScaleDimensions", PropertyEditorKind.Text, Order: 2),
        ["StartPosition"] = new("Layout", "StartPosition", PropertyEditorKind.Enumeration, Order: 3),
        ["ClientSize"] = new("Layout", "ClientSize", PropertyEditorKind.Text, Order: 4),
        ["Location"] = new("Layout", "Location", PropertyEditorKind.Text, Order: 10),
        ["Size"] = new("Layout", "Size", PropertyEditorKind.Text, Order: 11),
        ["Anchor"] = new("Layout", "Anchor", PropertyEditorKind.Text, Order: 12),
        ["Dock"] = new("Layout", "Dock", PropertyEditorKind.Enumeration, Order: 13),
        ["Margin"] = new("Layout", "Margin", PropertyEditorKind.Text, Order: 14),
        ["Padding"] = new("Layout", "Padding", PropertyEditorKind.Text, Order: 15),
        ["AutoSize"] = new("Layout", "AutoSize", PropertyEditorKind.Boolean, Order: 16),
        ["AutoSizeMode"] = new("Layout", "AutoSizeMode", PropertyEditorKind.Enumeration, Order: 17),
        ["GrowStyle"] = new("Layout", "GrowStyle", PropertyEditorKind.Enumeration, Order: 18),
        ["MaximumSize"] = new("Layout", "MaximumSize", PropertyEditorKind.Text, Order: 19),
        ["MinimumSize"] = new("Layout", "MinimumSize", PropertyEditorKind.Text, Order: 20),
        ["FlowDirection"] = new("Layout", "FlowDirection", PropertyEditorKind.Enumeration, Order: 21),
        ["WrapContents"] = new("Layout", "WrapContents", PropertyEditorKind.Boolean, Order: 22),
        ["ColumnCount"] = new("Layout", "ColumnCount", PropertyEditorKind.Number, Order: 23),
        ["RowCount"] = new("Layout", "RowCount", PropertyEditorKind.Number, Order: 24),
        ["ColumnStyles"] = new("Layout", "ColumnStyles", PropertyEditorKind.Text, Order: 25),
        ["RowStyles"] = new("Layout", "RowStyles", PropertyEditorKind.Text, Order: 26),
        ["ItemHeight"] = new("Layout", "ItemHeight", PropertyEditorKind.Number, Order: 27),
        ["ColumnWidth"] = new("Layout", "ColumnWidth", PropertyEditorKind.Number, Order: 28),
        ["MaximumColumnWidth"] = new("Layout", "MaximumColumnWidth", PropertyEditorKind.Number, Order: 29),
        ["MinimumColumnWidth"] = new("Layout", "MinimumColumnWidth", PropertyEditorKind.Number, Order: 30),
        ["SplitterDistance"] = new("Layout", "SplitterDistance", PropertyEditorKind.Number, Order: 31),
        ["Panel1Collapsed"] = new("Layout", "Panel1Collapsed", PropertyEditorKind.Boolean, Order: 32),
        ["Panel2Collapsed"] = new("Layout", "Panel2Collapsed", PropertyEditorKind.Boolean, Order: 33),
        ["FixedPanel"] = new("Layout", "FixedPanel", PropertyEditorKind.Enumeration, Order: 34),
        ["TabSize"] = new("Layout", "TabSize", PropertyEditorKind.Text, Order: 35),

        // ---- appearance -----------------------------------------------------
        ["Text"] = new("Appearance", "Text", PropertyEditorKind.Text, Order: 10),
        ["BackColor"] = new("Appearance", "BackColor", PropertyEditorKind.Color, Order: 20),
        ["ForeColor"] = new("Appearance", "ForeColor", PropertyEditorKind.Color, Order: 21),
        ["Font"] = new("Appearance", "Font", PropertyEditorKind.Font, Order: 22),
        ["Image"] = new("Appearance", "Image", PropertyEditorKind.Image, Order: 23),
        ["ImageAlign"] = new("Appearance", "ImageAlign", PropertyEditorKind.Enumeration, Order: 24),
        ["BackgroundImage"] = new("Appearance", "BackgroundImage", PropertyEditorKind.Image, Order: 25),
        ["BackgroundImageLayout"] = new("Appearance", "BackgroundImageLayout", PropertyEditorKind.Enumeration, Order: 26),
        ["TextAlign"] = new("Appearance", "TextAlign", PropertyEditorKind.Enumeration, Order: 27),
        ["RightToLeft"] = new("Appearance", "RightToLeft", PropertyEditorKind.Enumeration, Order: 28),
        ["Cursor"] = new("Appearance", "Cursor", PropertyEditorKind.Text, Order: 29),
        ["UseVisualStyleBackColor"] = new("Appearance", "UseVisualStyleBackColor", PropertyEditorKind.Boolean, Order: 30),
        ["FlatStyle"] = new("Appearance", "FlatStyle", PropertyEditorKind.Enumeration, Order: 31),
        ["FlatAppearance"] = new("Appearance", "FlatAppearance", PropertyEditorKind.Text, Order: 32),
        ["BorderStyle"] = new("Appearance", "BorderStyle", PropertyEditorKind.Enumeration, Order: 33),
        ["UseMnemonic"] = new("Appearance", "UseMnemonic", PropertyEditorKind.Boolean, Order: 34),
        ["IntegralHeight"] = new("Appearance", "IntegralHeight", PropertyEditorKind.Boolean, Order: 35),
        ["DropDownStyle"] = new("Appearance", "DropDownStyle", PropertyEditorKind.Enumeration, Order: 36),
        ["DropDownOrientation"] = new("Appearance", "DropDownOrientation", PropertyEditorKind.Enumeration, Order: 37),
        ["DrawMode"] = new("Appearance", "DrawMode", PropertyEditorKind.Enumeration, Order: 38),
        ["CharacterCasing"] = new("Appearance", "CharacterCasing", PropertyEditorKind.Enumeration, Order: 39),
        ["Multiline"] = new("Appearance", "Multiline", PropertyEditorKind.Boolean, Order: 40),
        ["WordWrap"] = new("Appearance", "WordWrap", PropertyEditorKind.Boolean, Order: 41),
        ["ScrollBars"] = new("Appearance", "ScrollBars", PropertyEditorKind.Enumeration, Order: 42),
        ["AcceptsReturn"] = new("Appearance", "AcceptsReturn", PropertyEditorKind.Boolean, Order: 43),
        ["PasswordChar"] = new("Appearance", "PasswordChar", PropertyEditorKind.Text, Order: 44),
        ["CharacterSet"] = new("Appearance", "CharacterSet", PropertyEditorKind.Enumeration, Order: 45),
        ["SizeMode"] = new("Appearance", "SizeMode", PropertyEditorKind.Enumeration, Order: 46),
        ["MarqueeSpeed"] = new("Appearance", "MarqueeSpeed", PropertyEditorKind.Enumeration, Order: 47),
        ["CustomFormat"] = new("Appearance", "CustomFormat", PropertyEditorKind.Text, Order: 48),
        ["SelectionMode"] = new("Appearance", "SelectionMode", PropertyEditorKind.Enumeration, Order: 49),
        ["FirstDayOfWeek"] = new("Appearance", "FirstDayOfWeek", PropertyEditorKind.Enumeration, Order: 50),
        ["View"] = new("Appearance", "View", PropertyEditorKind.Enumeration, Order: 51),
        ["HeaderStyle"] = new("Appearance", "HeaderStyle", PropertyEditorKind.Enumeration, Order: 52),
        ["EditMode"] = new("Appearance", "EditMode", PropertyEditorKind.Enumeration, Order: 53),
        ["Appearance"] = new("Appearance", "Appearance", PropertyEditorKind.Enumeration, Order: 54),
        ["Orientation"] = new("Appearance", "Orientation", PropertyEditorKind.Enumeration, Order: 55),
        ["Style"] = new("Appearance", "Style", PropertyEditorKind.Enumeration, Order: 56),
        ["GradientStyle"] = new("Appearance", "GradientStyle", PropertyEditorKind.Enumeration, Order: 57),
        ["TickStyle"] = new("Appearance", "TickStyle", PropertyEditorKind.Enumeration, Order: 58),
        ["TrackColor"] = new("Appearance", "TrackColor", PropertyEditorKind.Color, Order: 59),
        ["BarColor"] = new("Appearance", "BarColor", PropertyEditorKind.Color, Order: 60),
        ["BorderColor"] = new("Appearance", "BorderColor", PropertyEditorKind.Color, Order: 61),
        ["FillColor"] = new("Appearance", "FillColor", PropertyEditorKind.Color, Order: 62),
        ["SelectionForeColor"] = new("Appearance", "SelectionForeColor", PropertyEditorKind.Color, Order: 63),
        ["SelectionBackColor"] = new("Appearance", "SelectionBackColor", PropertyEditorKind.Color, Order: 64),
        ["OddRowBackColor"] = new("Appearance", "OddRowBackColor", PropertyEditorKind.Color, Order: 65),
        ["EvenRowBackColor"] = new("Appearance", "EvenRowBackColor", PropertyEditorKind.Color, Order: 66),
        ["GridColor"] = new("Appearance", "GridColor", PropertyEditorKind.Color, Order: 67),
        ["BackgroundColor"] = new("Appearance", "BackgroundColor", PropertyEditorKind.Color, Order: 68),
        ["ColumnHeaderColor"] = new("Appearance", "ColumnHeaderColor", PropertyEditorKind.Color, Order: 69),
        ["ValueColor"] = new("Appearance", "ValueColor", PropertyEditorKind.Color, Order: 70),
        ["TransparencyKey"] = new("Window Style", "TransparencyKey", PropertyEditorKind.Color, Order: 17),

        // ---- behavior -------------------------------------------------------
        ["Enabled"] = new("Behavior", "Enabled", PropertyEditorKind.Boolean, Order: 10),
        ["Visible"] = new("Behavior", "Visible", PropertyEditorKind.Boolean, Order: 11),
        ["TabIndex"] = new("Behavior", "TabIndex", PropertyEditorKind.Number, Order: 12),
        ["TabStop"] = new("Behavior", "TabStop", PropertyEditorKind.Boolean, Order: 13),
        ["ContextMenuStrip"] = new("Behavior", "ContextMenuStrip", PropertyEditorKind.Reference, Order: 14),
        ["CausesValidation"] = new("Behavior", "CausesValidation", PropertyEditorKind.Boolean, Order: 15),
        ["AllowDrop"] = new("Behavior", "AllowDrop", PropertyEditorKind.Boolean, Order: 16),
        ["ReadOnly"] = new("Behavior", "ReadOnly", PropertyEditorKind.Boolean, Order: 17),
        ["MaximumLength"] = new("Behavior", "MaximumLength", PropertyEditorKind.Number, Order: 18),
        ["UseSystemPasswordChar"] = new("Behavior", "UseSystemPasswordChar", PropertyEditorKind.Boolean, Order: 19),
        ["HideSelection"] = new("Behavior", "HideSelection", PropertyEditorKind.Boolean, Order: 20),
        ["ShortcutsEnabled"] = new("Behavior", "ShortcutsEnabled", PropertyEditorKind.Boolean, Order: 21),
        ["ShowCharCodes"] = new("Behavior", "ShowCharCodes", PropertyEditorKind.Boolean, Order: 22),
        ["SelectionLength"] = new("Behavior", "SelectionLength", PropertyEditorKind.Number, Order: 23),
        ["SelectionStart"] = new("Behavior", "SelectionStart", PropertyEditorKind.Number, Order: 24),
        ["MaxDropDownItems"] = new("Behavior", "MaxDropDownItems", PropertyEditorKind.Number, Order: 25),
        ["FormattingEnabled"] = new("Behavior", "FormattingEnabled", PropertyEditorKind.Boolean, Order: 26),
        ["MaxDate"] = new("Behavior", "MaxDate", PropertyEditorKind.Text, Order: 27),
        ["MinDate"] = new("Behavior", "MinDate", PropertyEditorKind.Text, Order: 28),
        ["DialogResult"] = new("Behavior", "DialogResult", PropertyEditorKind.Enumeration, Order: 29),
        ["CheckState"] = new("Behavior", "CheckState", PropertyEditorKind.Enumeration, Order: 30),
        ["ThreeState"] = new("Behavior", "ThreeState", PropertyEditorKind.Boolean, Order: 31),
        ["AutoCompleteMode"] = new("Behavior", "AutoCompleteMode", PropertyEditorKind.Enumeration, Order: 32),
        ["AutoCompleteSource"] = new("Behavior", "AutoCompleteSource", PropertyEditorKind.Enumeration, Order: 33),
        ["Interval"] = new("Behavior", "Interval", PropertyEditorKind.Number, Order: 34),
        ["TickFrequency"] = new("Behavior", "TickFrequency", PropertyEditorKind.Number, Order: 35),
        ["SmallChange"] = new("Behavior", "SmallChange", PropertyEditorKind.Number, Order: 36),
        ["LargeChange"] = new("Behavior", "LargeChange", PropertyEditorKind.Number, Order: 37),
        ["AllowUserToAddRows"] = new("Behavior", "AllowUserToAddRows", PropertyEditorKind.Boolean, Order: 38),
        ["AllowUserToDeleteRows"] = new("Behavior", "AllowUserToDeleteRows", PropertyEditorKind.Boolean, Order: 39),
        ["AllowUserToOrderColumns"] = new("Behavior", "AllowUserToOrderColumns", PropertyEditorKind.Boolean, Order: 40),
        ["AllowUserToResizeRows"] = new("Behavior", "AllowUserToResizeRows", PropertyEditorKind.Boolean, Order: 41),
        ["AllowUserToResizeColumns"] = new("Behavior", "AllowUserToResizeColumns", PropertyEditorKind.Boolean, Order: 42),
        ["RowHeadersVisible"] = new("Behavior", "RowHeadersVisible", PropertyEditorKind.Boolean, Order: 43),
        ["ColumnHeadersVisible"] = new("Behavior", "ColumnHeadersVisible", PropertyEditorKind.Boolean, Order: 44),
        ["MultiSelect"] = new("Behavior", "MultiSelect", PropertyEditorKind.Boolean, Order: 45),
        ["FullRowSelect"] = new("Behavior", "FullRowSelect", PropertyEditorKind.Boolean, Order: 46),
        ["GridLines"] = new("Behavior", "GridLines", PropertyEditorKind.Boolean, Order: 47),
        ["LabelEdit"] = new("Behavior", "LabelEdit", PropertyEditorKind.Boolean, Order: 48),
        ["HideSelection"] = new("Behavior", "HideSelection", PropertyEditorKind.Boolean, Order: 49),
        ["ShowLines"] = new("Behavior", "ShowLines", PropertyEditorKind.Boolean, Order: 50),
        ["ShowPlusMinus"] = new("Behavior", "ShowPlusMinus", PropertyEditorKind.Boolean, Order: 51),
        ["ShowRootLines"] = new("Behavior", "ShowRootLines", PropertyEditorKind.Boolean, Order: 52),
        ["ShowLinesBeforeRoot"] = new("Behavior", "ShowLinesBeforeRoot", PropertyEditorKind.Boolean, Order: 53),
        ["ShowItemToolTips"] = new("Behavior", "ShowItemToolTips", PropertyEditorKind.Boolean, Order: 54),
        ["ShowCheckBoxes"] = new("Behavior", "ShowCheckBoxes", PropertyEditorKind.Boolean, Order: 55),
        ["CheckBoxes"] = new("Behavior", "CheckBoxes", PropertyEditorKind.Boolean, Order: 56),
        ["ShowToday"] = new("Behavior", "ShowToday", PropertyEditorKind.Boolean, Order: 57),
        ["ShowUpDown"] = new("Behavior", "ShowUpDown", PropertyEditorKind.Boolean, Order: 58),
        ["ShowCheckBox"] = new("Behavior", "ShowCheckBox", PropertyEditorKind.Boolean, Order: 59),
        ["ShowToolTips"] = new("Behavior", "ShowToolTips", PropertyEditorKind.Boolean, Order: 60),
        ["GripStyle"] = new("Behavior", "GripStyle", PropertyEditorKind.Enumeration, Order: 61),
        ["SizingGrip"] = new("Behavior", "SizingGrip", PropertyEditorKind.Boolean, Order: 62),
        ["StandardTab"] = new("Behavior", "StandardTab", PropertyEditorKind.Boolean, Order: 63),
        ["OwnerDraw"] = new("Behavior", "OwnerDraw", PropertyEditorKind.Boolean, Order: 64),
        ["AcceptButton"] = new("Behavior", "AcceptButton", PropertyEditorKind.Reference, Order: 65),
        ["CancelButton"] = new("Behavior", "CancelButton", PropertyEditorKind.Reference, Order: 66),
        ["ImeMode"] = new("Behavior", "ImeMode", PropertyEditorKind.Enumeration, Order: 67),
        ["DetectEncoding"] = new("Behavior", "DetectEncoding", PropertyEditorKind.Boolean, Order: 68),

        // ---- data -----------------------------------------------------------
        ["Items"] = new("Data", "Items", PropertyEditorKind.Collection, IsCollection: true, Order: 10),
        ["Columns"] = new("Data", "Columns", PropertyEditorKind.Collection, IsCollection: true, Order: 11),
        ["SelectedIndex"] = new("Data", "SelectedIndex", PropertyEditorKind.Number, Order: 12),
        ["SelectedItem"] = new("Data", "SelectedItem", PropertyEditorKind.Text, Order: 13),
        ["Value"] = new("Data", "Value", PropertyEditorKind.Text, Order: 14),
        ["Checked"] = new("Data", "Checked", PropertyEditorKind.Boolean, Order: 15),
        ["DataSource"] = new("Data", "DataSource", PropertyEditorKind.Reference, Order: 16),
        ["DisplayMember"] = new("Data", "DisplayMember", PropertyEditorKind.Text, Order: 17),
        ["ValueMember"] = new("Data", "ValueMember", PropertyEditorKind.Text, Order: 18),
        ["Format"] = new("Data", "Format", PropertyEditorKind.Text, Order: 19),
        ["FormatString"] = new("Data", "FormatString", PropertyEditorKind.Text, Order: 20),
        ["UseItemTextForFormatting"] = new("Data", "UseItemTextForFormatting", PropertyEditorKind.Boolean, Order: 21),
        ["Increment"] = new("Data", "Increment", PropertyEditorKind.Number, Order: 22),
        ["DecimalPlaces"] = new("Data", "DecimalPlaces", PropertyEditorKind.Number, Order: 23),
        ["Minimum"] = new("Data", "Minimum", PropertyEditorKind.Number, Order: 24),
        ["Maximum"] = new("Data", "Maximum", PropertyEditorKind.Number, Order: 25),
        ["Sorted"] = new("Data", "Sorted", PropertyEditorKind.Boolean, Order: 26),
        ["ItemsSource"] = new("Data", "ItemsSource", PropertyEditorKind.Reference, Order: 27),
        ["TodayDate"] = new("Data", "TodayDate", PropertyEditorKind.Text, Order: 28),
        ["FirstDateOfMonth"] = new("Data", "FirstDateOfMonth", PropertyEditorKind.Text, Order: 29),

        // ---- window style ---------------------------------------------------
        ["FormBorderStyle"] = new("Window Style", "FormBorderStyle", PropertyEditorKind.Enumeration, Order: 10),
        ["Icon"] = new("Window Style", "Icon", PropertyEditorKind.Image, Order: 11),
        ["ShowIcon"] = new("Window Style", "ShowIcon", PropertyEditorKind.Boolean, Order: 12),
        ["ShowInTaskbar"] = new("Window Style", "ShowInTaskbar", PropertyEditorKind.Boolean, Order: 13),
        ["TopMost"] = new("Window Style", "TopMost", PropertyEditorKind.Boolean, Order: 14),
        ["WindowState"] = new("Window Style", "WindowState", PropertyEditorKind.Enumeration, Order: 15),
        ["Opacity"] = new("Window Style", "Opacity", PropertyEditorKind.Number, Order: 16),
        ["MinimizeBox"] = new("Window Style", "MinimizeBox", PropertyEditorKind.Boolean, Order: 18),
        ["MaximizeBox"] = new("Window Style", "MaximizeBox", PropertyEditorKind.Boolean, Order: 19),
        ["ControlBox"] = new("Window Style", "ControlBox", PropertyEditorKind.Boolean, Order: 20),
        ["MdiChild"] = new("Window Style", "MdiChild", PropertyEditorKind.Boolean, Order: 21),

        // ---- misc -----------------------------------------------------------
        ["Tag"] = new("Misc", "Tag", PropertyEditorKind.Text, Order: 10),
        ["AccessibleName"] = new("Misc", "AccessibleName", PropertyEditorKind.Text, Order: 11),
        ["AccessibleDescription"] = new("Misc", "AccessibleDescription", PropertyEditorKind.Text, Order: 12),
        ["AccessibilityRole"] = new("Misc", "AccessibilityRole", PropertyEditorKind.Enumeration, Order: 13),
        ["HelpKey"] = new("Misc", "HelpKey", PropertyEditorKind.Text, Order: 14),
    };

    /// <summary>Per type overrides that resolve categories which differ between controls.</summary>
    private static readonly Dictionary<(Type Type, string Property), PropertyMetadata> Overrides = new()
    {
        [(typeof(System.Windows.Forms.DateTimePicker), "Format")] = new("Appearance", "Format", PropertyEditorKind.Enumeration, Order: 47),
        [(typeof(System.Windows.Forms.ProgressBar), "Minimum")] = new("Behavior", "Minimum", PropertyEditorKind.Number, Order: 70),
        [(typeof(System.Windows.Forms.ProgressBar), "Maximum")] = new("Behavior", "Maximum", PropertyEditorKind.Number, Order: 71),
        [(typeof(System.Windows.Forms.ProgressBar), "Value")] = new("Behavior", "Value", PropertyEditorKind.Number, Order: 72),
        [(typeof(System.Windows.Forms.TrackBar), "Minimum")] = new("Behavior", "Minimum", PropertyEditorKind.Number, Order: 70),
        [(typeof(System.Windows.Forms.TrackBar), "Maximum")] = new("Behavior", "Maximum", PropertyEditorKind.Number, Order: 71),
        [(typeof(System.Windows.Forms.TrackBar), "Value")] = new("Behavior", "Value", PropertyEditorKind.Number, Order: 72),
        [(typeof(System.Windows.Forms.DataGridView), "BorderStyle")] = new("Appearance", "BorderStyle", PropertyEditorKind.Enumeration, Order: 33),
        [(typeof(System.Windows.Forms.DataGridView), "SelectionMode")] = new("Appearance", "SelectionMode", PropertyEditorKind.Enumeration, Order: 49),
    };

    /// <summary>Looks up metadata for a property name.</summary>
    public static PropertyMetadata? Lookup(string propertyName)
        => Map.TryGetValue(propertyName, out var metadata) ? metadata : null;

    /// <summary>Looks up metadata for a property of a specific control type.</summary>
    public static PropertyMetadata? Lookup(Type controlType, string propertyName)
    {
        for (var type = controlType; type is not null; type = type.BaseType)
        {
            if (Overrides.TryGetValue((type, propertyName), out var metadata))
            {
                return metadata;
            }
        }

        return Lookup(propertyName);
    }

    /// <summary>All known property names.</summary>
    public static IEnumerable<string> KnownNames => Map.Keys;
}
