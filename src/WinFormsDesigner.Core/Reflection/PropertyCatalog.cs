using System.ComponentModel;
using System.Reflection;
using WinFormsDesigner.Core.Model;

namespace WinFormsDesigner.Core.Reflection;

/// <summary>
/// Builds the list of properties shown in the property grid for a given control type.
/// </summary>
public static class PropertyCatalog
{
    private static readonly Dictionary<Type, PropertyEntry[]> Cache = new();
    private static readonly object SyncRoot = new();

    /// <summary>Properties that never make sense in a designer.</summary>
    private static readonly HashSet<string> Excluded = new(StringComparer.Ordinal)
    {
        "Controls",
        "Parent",
        "Site",
        "Container",
        "Handle",
        "IsHandleCreated",
        "IsDisposed",
        "DeviceDpi",
        "BindingContext",
        "WindowHandle",
        "InvokeRequired",
        "IsInputKey",
        "IsInputCharacter",
        "IsMnemonic",
        "Focused",
        "CanSelect",
        "CanFocus",
        "AccessibilityObject",
        "ProductName",
        "ProductVersion",
        "DefaultSize",
        "DefaultCursor",
        "DefaultFont",
        "DefaultPadding",
        "DefaultMargin",
        "AutoScaleBaseSize",
        "Bounds",
        "ClientRectangle",
        "PreferredSize",
        "DoDragDrop",
    };

    /// <summary>Properties that only make sense for the root form.</summary>
    private static readonly HashSet<string> ExcludedForNonForm = new(StringComparer.Ordinal)
    {
        "ClientSize",
    };

    /// <summary>Properties that are read-only collections of designer children.</summary>
    private static readonly HashSet<string> ExcludedForAll = new(StringComparer.Ordinal)
    {
        "TabPages",
        "ToolStripItems",
        "StatusStripItems",
        "ContextMenuStripItems",
    };

    /// <summary>
    /// Returns the ordered list of properties for the given control type.
    /// </summary>
    public static IReadOnlyList<PropertyEntry> GetEntries(Type controlType)
    {
        lock (SyncRoot)
        {
            if (Cache.TryGetValue(controlType, out var cached))
            {
                return cached;
            }
        }

        var entries = Build(controlType);

        lock (SyncRoot)
        {
            Cache[controlType] = entries;
        }

        return entries;
    }

    /// <summary>Finds a single property entry by name.</summary>
    public static PropertyEntry? Find(Type controlType, string propertyName)
    {
        foreach (var entry in GetEntries(controlType))
        {
            if (string.Equals(entry.Info.Name, propertyName, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private static PropertyEntry[] Build(Type controlType)
    {
        var entries = new List<PropertyEntry>();

        foreach (var property in controlType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead)
            {
                continue;
            }

            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (Excluded.Contains(property.Name) || ExcludedForAll.Contains(property.Name))
            {
                continue;
            }

            if (!typeof(System.Windows.Forms.Form).IsAssignableFrom(controlType) && ExcludedForNonForm.Contains(property.Name))
            {
                continue;
            }

            var metadata = PropertyMetadataTable.Lookup(controlType, property.Name);

            string category;
            PropertyEditorKind editor;
            bool isCollection = metadata?.IsCollection ?? false;

            if (metadata is not null)
            {
                category = metadata.Category ?? FindCategory(property) ?? "Misc";
                editor = metadata.Editor ?? InferEditor(property.PropertyType);
            }
            else
            {
                var bclCategory = FindCategory(property);
                if (bclCategory is null)
                {
                    // Unknown and uncategorised: hide it so the grid stays clean.
                    continue;
                }

                category = bclCategory;
                editor = InferEditor(property.PropertyType);
            }

            if (isCollection && !typeof(System.Collections.IList).IsAssignableFrom(property.PropertyType))
            {
                // Only collections the specialised editor understands are exposed.
                isCollection = IsSupportedCollection(property.PropertyType);
                if (!isCollection)
                {
                    continue;
                }
            }

            if (!property.CanWrite && !isCollection)
            {
                // Read only properties cannot be edited, so they are not worth showing.
                continue;
            }

            var displayName = metadata?.DisplayName ?? property.Name;
            entries.Add(new PropertyEntry(property, displayName, category, editor, isCollection));
        }

        return entries
            .OrderBy(e => CategoryIndex(e.Category))
            .ThenBy(e => e.Category, StringComparer.Ordinal)
            .ThenBy(e => OrderOf(e))
            .ThenBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int OrderOf(PropertyEntry entry)
    {
        var metadata = PropertyMetadataTable.Lookup(entry.Info.DeclaringType ?? typeof(object), entry.Info.Name);
        return metadata?.Order ?? 500;
    }

    private static int CategoryIndex(string category)
    {
        var index = Array.IndexOf(PropertyMetadataTable.CategoryOrder, category);
        return index >= 0 ? index : PropertyMetadataTable.CategoryOrder.Length;
    }

    /// <summary>Determines whether a collection type can be edited by the collection editor.</summary>
    public static bool IsSupportedCollection(Type collectionType)
    {
        var name = collectionType.Name;
        return name.Contains("StringCollection", StringComparison.Ordinal)
            || name.Contains("ObjectCollection", StringComparison.Ordinal)
            || name.Contains("ListControlCollection", StringComparison.Ordinal)
            || name.Contains("ColumnHeaderCollection", StringComparison.Ordinal)
            || name.Contains("CheckedListBoxObjectCollection", StringComparison.Ordinal);
    }

    private static PropertyEditorKind InferEditor(Type propertyType)
    {
        if (propertyType == typeof(bool))
        {
            return PropertyEditorKind.Boolean;
        }

        if (propertyType.IsEnum)
        {
            return PropertyEditorKind.Enumeration;
        }

        if (propertyType == typeof(System.Drawing.Color))
        {
            return PropertyEditorKind.Color;
        }

        if (propertyType == typeof(System.Drawing.Font))
        {
            return PropertyEditorKind.Font;
        }

        if (propertyType == typeof(System.Drawing.Image) || typeof(System.Drawing.Image).IsAssignableFrom(propertyType))
        {
            return PropertyEditorKind.Image;
        }

        if (propertyType == typeof(string))
        {
            return PropertyEditorKind.Text;
        }

        if (propertyType == typeof(int) || propertyType == typeof(short) || propertyType == typeof(long)
            || propertyType == typeof(byte) || propertyType == typeof(float) || propertyType == typeof(double)
            || propertyType == typeof(decimal))
        {
            return PropertyEditorKind.Number;
        }

        return PropertyEditorKind.Text;
    }

    /// <summary>
    /// Finds the <see cref="CategoryAttribute"/> of a property, walking up the inheritance chain
    /// so that overridden properties keep the category of their base declaration.
    /// </summary>
    public static string? FindCategory(PropertyInfo property)
    {
        var own = property.GetCustomAttributes(typeof(CategoryAttribute), inherit: false);
        if (own.Length > 0)
        {
            return ((CategoryAttribute)own[0]).Category;
        }

        for (var type = property.DeclaringType?.BaseType; type is not null; type = type.BaseType)
        {
            var inherited = type.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var attributes = inherited?.GetCustomAttributes(typeof(CategoryAttribute), inherit: false);
            if (attributes is { Length: > 0 })
            {
                return ((CategoryAttribute)attributes[0]).Category;
            }
        }

        return null;
    }
}
