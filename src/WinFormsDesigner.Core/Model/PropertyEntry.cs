using System.ComponentModel;
using System.Reflection;

namespace WinFormsDesigner.Core.Model;

/// <summary>
/// Describes a single settable property of a control, together with the metadata the
/// property grid needs to render it (category, caption and editor kind).
/// </summary>
public sealed class PropertyEntry
{
    internal PropertyEntry(PropertyInfo info, string displayName, string category, PropertyEditorKind editor, bool isCollection)
    {
        Info = info;
        DisplayName = displayName;
        Category = category;
        Editor = editor;
        IsCollection = isCollection;
    }

    /// <summary>Reflection information for the property.</summary>
    public PropertyInfo Info { get; }

    /// <summary>Caption shown in the left hand column of the property grid.</summary>
    public string DisplayName { get; }

    /// <summary>Category header the property is grouped under.</summary>
    public string Category { get; }

    /// <summary>Editor the property grid should use.</summary>
    public PropertyEditorKind Editor { get; }

    /// <summary>True when the property exposes a child collection that needs a specialised editor.</summary>
    public bool IsCollection { get; }

    /// <summary>Declared type of the property.</summary>
    public Type ValueType => Info.PropertyType;

    /// <summary>Reads the property value from <paramref name="target"/>.</summary>
    public object? GetValue(object target) => Info.GetValue(target);

    /// <summary>Writes the property value to <paramref name="target"/>.</summary>
    public void SetValue(object target, object? value) => Info.SetValue(target, value);

    /// <summary>
    /// Attempts to convert a string (as typed by the user) to the property type.
    /// </summary>
    public bool TryConvertFromText(string text, out object? value, out string? error)
    {
        value = null;
        error = null;
        var type = ValueType;

        if (type == typeof(string))
        {
            value = text;
            return true;
        }

        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying.IsEnum)
        {
            try
            {
                value = Enum.Parse(underlying, text, ignoreCase: true);
                return true;
            }
            catch (Exception)
            {
                error = $"'{text}' is not a valid {underlying.Name} value.";
                return false;
            }
        }

        try
        {
            value = Convert.ChangeType(text, underlying, System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception)
        {
            error = $"'{text}' is not a valid {underlying.Name} value.";
            return false;
        }
    }

    /// <summary>Formats a property value for display in the grid.</summary>
    public string FormatValue(object? value)
    {
        if (value is null)
        {
            return "(null)";
        }

        if (value is System.Drawing.Color color)
        {
            return ColorText.Format(color);
        }

        if (value is System.Drawing.Font font)
        {
            return $"{font.Name}, {font.Size:0.##}";
        }

        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            return $"({count(enumerable)} items)";
        }

        if (value.GetType().IsEnum)
        {
            return value.ToString() ?? string.Empty;
        }

        return value.ToString() ?? string.Empty;
    }

    private static int count(System.Collections.IEnumerable enumerable)
    {
        var n = 0;
        foreach (var _ in enumerable)
        {
            n++;
        }

        return n;
    }
}
