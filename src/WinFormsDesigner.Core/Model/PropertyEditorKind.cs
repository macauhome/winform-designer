namespace WinFormsDesigner.Core.Model;

/// <summary>
/// Kind of editor the property grid should host for a given property.
/// </summary>
public enum PropertyEditorKind
{
    /// <summary>Single-line text.</summary>
    Text,

    /// <summary>Multi-line text.</summary>
    MultilineText,

    /// <summary>Numeric value.</summary>
    Number,

    /// <summary>Boolean value rendered as a check box.</summary>
    Boolean,

    /// <summary>Enumeration rendered as a drop-down list.</summary>
    Enumeration,

    /// <summary>A <see cref="System.Drawing.Color"/> value.</summary>
    Color,

    /// <summary>A <see cref="System.Drawing.Font"/> value.</summary>
    Font,

    /// <summary>A raster image, optionally linked to a file on disk.</summary>
    Image,

    /// <summary>A string collection such as <c>Items</c> or <c>Columns</c>.</summary>
    Collection,

    /// <summary>A reference to another component in the document (e.g. <c>ContextMenuStrip</c>).</summary>
    Reference,
}
