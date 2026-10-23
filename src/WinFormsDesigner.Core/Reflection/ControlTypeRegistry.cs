namespace WinFormsDesigner.Core.Reflection;

/// <summary>
/// A single entry of the toolbox.
/// </summary>
public sealed class ToolboxEntry
{
    public ToolboxEntry(string category, string displayName, Type controlType, string description, string glyph)
    {
        Category = category;
        DisplayName = displayName;
        ControlType = controlType;
        Description = description;
        Glyph = glyph;
    }

    /// <summary>Toolbox category the entry belongs to.</summary>
    public string Category { get; }

    /// <summary>Name shown in the toolbox.</summary>
    public string DisplayName { get; }

    /// <summary>The control type that is instantiated when the entry is dropped.</summary>
    public Type ControlType { get; }

    /// <summary>Short description shown as a tooltip.</summary>
    public string Description { get; }

    /// <summary>Key of the glyph used to draw the toolbox icon.</summary>
    public string Glyph { get; }

    /// <summary>
    /// Base name given to a newly created control of this type. The Visual Studio designer
    /// uses the camel cased type name followed by a number, e.g. <c>button1</c>.
    /// </summary>
    public string DefaultName => ControlTypeRegistry.ToCamelCase(ControlType.Name);

    /// <summary>Default size used when the control is dropped onto the surface.</summary>
    public System.Drawing.Size DefaultSize => ControlType == typeof(System.Windows.Forms.Form)
        ? new System.Drawing.Size(800, 450)
        : new System.Drawing.Size(100, 25);
}

/// <summary>
/// The catalogue of controls offered by the toolbox.
/// </summary>
public static class ControlTypeRegistry
{
    private static readonly ToolboxEntry[] Entries =
    [
        // Common controls
        new("Common", "Button", typeof(System.Windows.Forms.Button), "A push button.", "button"),
        new("Common", "Label", typeof(System.Windows.Forms.Label), "A text label.", "label"),
        new("Common", "TextBox", typeof(System.Windows.Forms.TextBox), "A single or multi line text input.", "textbox"),
        new("Common", "PictureBox", typeof(System.Windows.Forms.PictureBox), "Displays an image.", "picturebox"),
        new("Common", "CheckBox", typeof(System.Windows.Forms.CheckBox), "A check box with a text caption.", "checkbox"),
        new("Common", "RadioButton", typeof(System.Windows.Forms.RadioButton), "A radio button.", "radiobutton"),
        new("Common", "ComboBox", typeof(System.Windows.Forms.ComboBox), "A drop down list of items.", "combobox"),
        new("Common", "ListBox", typeof(System.Windows.Forms.ListBox), "A list of selectable items.", "listbox"),
        new("Common", "NumericUpDown", typeof(System.Windows.Forms.NumericUpDown), "A numeric spin control.", "numericupdown"),
        new("Common", "DateTimePicker", typeof(System.Windows.Forms.DateTimePicker), "Selects a date and time.", "datetimepicker"),
        new("Common", "MonthCalendar", typeof(System.Windows.Forms.MonthCalendar), "Displays a month.", "monthcalendar"),
        new("Common", "LinkLabel", typeof(System.Windows.Forms.LinkLabel), "A hyperlink style label.", "linklabel"),
        new("Common", "MaskedTextBox", typeof(System.Windows.Forms.MaskedTextBox), "A masked text input.", "maskedtextbox"),
        new("Common", "RichTextBox", typeof(System.Windows.Forms.RichTextBox), "Rich formatted text input.", "richtextbox"),
        new("Common", "DomainUpDown", typeof(System.Windows.Forms.DomainUpDown), "An up-down domain selector.", "domainupdown"),

        // Containers
        new("Containers", "Panel", typeof(System.Windows.Forms.Panel), "A container control.", "panel"),
        new("Containers", "GroupBox", typeof(System.Windows.Forms.GroupBox), "A titled group of controls.", "groupbox"),
        new("Containers", "TabControl", typeof(System.Windows.Forms.TabControl), "A tabbed container.", "tabcontrol"),
        new("Containers", "TabPage", typeof(System.Windows.Forms.TabPage), "A single page of a TabControl.", "tabpage"),
        new("Containers", "SplitContainer", typeof(System.Windows.Forms.SplitContainer), "A resizable split container.", "splitcontainer"),
        new("Containers", "FlowLayoutPanel", typeof(System.Windows.Forms.FlowLayoutPanel), "Lays out children in a flow.", "flowlayoutpanel"),
        new("Containers", "TableLayoutPanel", typeof(System.Windows.Forms.TableLayoutPanel), "Lays out children in a grid.", "tablelayoutpanel"),

        // Data
        new("Data", "DataGridView", typeof(System.Windows.Forms.DataGridView), "A tabular data grid.", "datagridview"),
        new("Data", "ListView", typeof(System.Windows.Forms.ListView), "A list view with columns.", "listview"),
        new("Data", "TreeView", typeof(System.Windows.Forms.TreeView), "A hierarchical tree.", "treeview"),

        // Menus and strips
        new("Menus & Toolbars", "MenuStrip", typeof(System.Windows.Forms.MenuStrip), "A main menu bar.", "menustrip"),
        new("Menus & Toolbars", "ContextMenuStrip", typeof(System.Windows.Forms.ContextMenuStrip), "A context menu.", "contextmenustrip"),
        new("Menus & Toolbars", "StatusStrip", typeof(System.Windows.Forms.StatusStrip), "A status bar.", "statusstrip"),
        new("Menus & Toolbars", "ToolStrip", typeof(System.Windows.Forms.ToolStrip), "A generic toolbar.", "toolstrip"),

        // Misc
        new("Misc", "ProgressBar", typeof(System.Windows.Forms.ProgressBar), "Shows progress of an operation.", "progressbar"),
        new("Misc", "TrackBar", typeof(System.Windows.Forms.TrackBar), "A slider.", "trackbar"),
        new("Misc", "HScrollBar", typeof(System.Windows.Forms.HScrollBar), "A horizontal scroll bar.", "hscrollbar"),
        new("Misc", "VScrollBar", typeof(System.Windows.Forms.VScrollBar), "A vertical scroll bar.", "vscrollbar"),
        new("Misc", "Splitter", typeof(System.Windows.Forms.Splitter), "A draggable splitter.", "splitter"),
        new("Misc", "Timer", typeof(System.Windows.Forms.Timer), "Raises events on an interval.", "timer"),
        new("Misc", "ToolTip", typeof(System.Windows.Forms.ToolTip), "Shows tool tips.", "tooltip"),
        new("Misc", "NotifyIcon", typeof(System.Windows.Forms.NotifyIcon), "A notification area icon.", "notifyicon"),
        new("Misc", "ErrorProvider", typeof(System.Windows.Forms.ErrorProvider), "Shows error icons next to controls.", "errorprovider"),
    ];

    /// <summary>All toolbox entries in display order.</summary>
    public static IReadOnlyList<ToolboxEntry> All => Entries;

    /// <summary>The distinct toolbox categories in display order.</summary>
    public static IReadOnlyList<string> Categories
        => Entries.Select(e => e.Category).Distinct(StringComparer.Ordinal).ToArray();

    /// <summary>Returns the entries of a single category.</summary>
    public static IReadOnlyList<ToolboxEntry> InCategory(string category)
        => Entries.Where(e => string.Equals(e.Category, category, StringComparison.Ordinal)).ToArray();

    /// <summary>Finds the toolbox entry for a control type, if any.</summary>
    public static ToolboxEntry? Find(Type controlType)
        => Entries.FirstOrDefault(e => e.ControlType == controlType);

    /// <summary>Turns a type name into the camel cased base name used for controls.</summary>
    public static string ToCamelCase(string name)
        => string.IsNullOrEmpty(name)
            ? name
            : char.ToLowerInvariant(name[0]) + name[1..];
}
