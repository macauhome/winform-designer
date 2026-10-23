using System.Reflection;
using WinFormsDesigner.Core.Model;

namespace WinFormsDesigner.Core.Reflection;

/// <summary>
/// Creates live control instances and applies the stored property values onto them.
/// </summary>
public static class ControlFactory
{
    private static readonly Dictionary<Type, object> Defaults = new();
    private static readonly object SyncRoot = new();

    /// <summary>Properties that are applied after everything else because they influence layout.</summary>
    private static readonly string[] LayoutPhaseOne = ["Padding", "Margin", "MaximumSize", "MinimumSize"];

    /// <summary>Properties applied just before the final bounds.</summary>
    private static readonly string[] LayoutPhaseTwo = ["AutoSize", "Anchor", "Dock"];

    /// <summary>Determines whether a control type can be instantiated by the designer.</summary>
    public static bool IsCreatable(Type type)
        => !type.IsAbstract && !type.IsGenericTypeDefinition
            && type.GetConstructor(Type.EmptyTypes) is not null
            && (typeof(System.Windows.Forms.Control).IsAssignableFrom(type) || IsComponent(type));

    /// <summary>
    /// Determines whether the type is a component without a visual representation, such as a
    /// timer or a tool tip.
    /// </summary>
    public static bool IsComponent(Type type)
        => !typeof(System.Windows.Forms.Control).IsAssignableFrom(type)
            && typeof(System.ComponentModel.Component).IsAssignableFrom(type);

    /// <summary>
    /// Creates a new instance of the given control type.
    /// </summary>
    public static Control Create(Type type)
    {
        if (!IsCreatable(type) || !typeof(System.Windows.Forms.Control).IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"'{type.Name}' cannot be created by the designer.");
        }

        var control = (Control)Activator.CreateInstance(type)!;
        control.Name = type.Name;
        return control;
    }

    /// <summary>
    /// Creates a new instance of the given type. Components are created with the supplied
    /// container when their type supports one, so that they are disposed along with the form.
    /// </summary>
    public static object CreateInstance(Type type, System.ComponentModel.IContainer? container = null)
    {
        if (!IsCreatable(type))
        {
            throw new InvalidOperationException($"'{type.Name}' cannot be created by the designer.");
        }

        object instance;
        if (container is not null)
        {
            var containerConstructor = type.GetConstructor(new[] { typeof(System.ComponentModel.IContainer) });
            instance = containerConstructor is not null
                ? containerConstructor.Invoke([container])
                : Activator.CreateInstance(type)!;
        }
        else
        {
            instance = Activator.CreateInstance(type)!;
        }

        if (instance is Control control)
        {
            control.Name = type.Name;
        }

        return instance;
    }

    /// <summary>
    /// Returns a pristine instance of the given type, used to determine default property values.
    /// </summary>
    public static object GetDefault(Type type)
    {
        lock (SyncRoot)
        {
            if (Defaults.TryGetValue(type, out var cached))
            {
                return cached;
            }
        }

        var instance = CreateInstance(type);

        lock (SyncRoot)
        {
            Defaults[type] = instance;
        }

        return instance;
    }

    /// <summary>Reads the default value of a property for a control type.</summary>
    public static object? GetDefaultValue(Type controlType, string propertyName)
    {
        try
        {
            var entry = PropertyCatalog.Find(controlType, propertyName);
            if (entry is null)
            {
                return null;
            }

            return entry.GetValue(GetDefault(controlType));
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Applies the stored property values of a node onto a live control.
    /// </summary>
    public static void Apply(object target, PropertyBag bag)
    {
        if (target is null || bag is null)
        {
            return;
        }

        var entries = PropertyCatalog.GetEntries(target.GetType());

        foreach (var (name, value) in bag.Values)
        {
            if (string.Equals(name, "Size", StringComparison.Ordinal)
                || string.Equals(name, "Location", StringComparison.Ordinal))
            {
                continue;
            }

            if (IsCollectionProperty(name))
            {
                // Collections are populated separately, after the regular properties.
                continue;
            }

            ApplyOne(target, entries, name, value);
        }

        foreach (var name in LayoutPhaseOne)
        {
            if (bag.TryGetValue(name, out var value))
            {
                ApplyOne(target, entries, name, value);
            }
        }

        foreach (var name in LayoutPhaseTwo)
        {
            if (bag.TryGetValue(name, out var value))
            {
                ApplyOne(target, entries, name, value);
            }
        }

        if (bag.TryGetValue("Size", out var size))
        {
            ApplyOne(target, entries, "Size", size);
        }

        if (bag.TryGetValue("Location", out var location))
        {
            ApplyOne(target, entries, "Location", location);
        }

        ApplyCollections(target, bag);
    }

    /// <summary>
    /// Determines whether a property name denotes one of the child collections the
    /// designer can edit.
    /// </summary>
    public static bool IsCollectionProperty(string propertyName)
        => string.Equals(propertyName, "Items", StringComparison.Ordinal)
            || string.Equals(propertyName, "Columns", StringComparison.Ordinal);

    /// <summary>
    /// Fills the child collections of a control, e.g. the items of a list box or the
    /// columns of a list view.
    /// </summary>
    private static void ApplyCollections(object target, PropertyBag bag)
    {
        if (bag.TryGetValue("Items", out var items) && items is System.Collections.IEnumerable itemList)
        {
            var property = target.GetType().GetProperty("Items");
            if (property?.GetValue(target) is System.Collections.IList targetList)
            {
                targetList.Clear();
                foreach (var item in itemList)
                {
                    if (item is not null)
                    {
                        targetList.Add(item.ToString());
                    }
                }
            }
        }

        if (bag.TryGetValue("Columns", out var columns) && columns is System.Collections.IEnumerable columnList)
        {
            var property = target.GetType().GetProperty("Columns");
            if (property?.GetValue(target) is System.Windows.Forms.ListView.ColumnHeaderCollection columnTarget)
            {
                columnTarget.Clear();
                foreach (var column in columnList)
                {
                    if (column is not null)
                    {
                        columnTarget.Add(column.ToString());
                    }
                }
            }
        }
    }

    private static void ApplyOne(object target, IReadOnlyList<PropertyEntry> entries, string name, object? value)
    {
        var entry = entries.FirstOrDefault(e => string.Equals(e.Info.Name, name, StringComparison.Ordinal));
        if (entry is null)
        {
            return;
        }

        try
        {
            if (value is null)
            {
                if (!entry.ValueType.IsValueType && Nullable.GetUnderlyingType(entry.ValueType) is null)
                {
                    entry.SetValue(target, null);
                }

                return;
            }

            if (value is ImageAsset asset)
            {
                // The document stores the raw bytes; the live control needs a real image.
                value = asset.Decode();
            }

            if (!entry.ValueType.IsInstanceOfType(value))
            {
                value = ConvertValue(value, entry.ValueType);
                if (value is null)
                {
                    return;
                }
            }

            entry.SetValue(target, value);
        }
        catch (Exception)
        {
            // A property that refuses the value is simply skipped; the designer must stay responsive.
        }
    }

    /// <summary>Converts a stored value to the declared property type when possible.</summary>
    public static object? ConvertValue(object value, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            if (underlying.IsInstanceOfType(value))
            {
                return value;
            }

            if (underlying.IsEnum)
            {
                return value is string text
                    ? Enum.Parse(underlying, text, ignoreCase: true)
                    : Enum.ToObject(underlying, value);
            }

            if (value is string s)
            {
                if (underlying == typeof(System.Drawing.Size))
                {
                    var parts = s.Split(',');
                    return parts.Length == 2
                        ? new System.Drawing.Size(int.Parse(parts[0].Trim()), int.Parse(parts[1].Trim()))
                        : System.Drawing.Size.Empty;
                }

                if (underlying == typeof(System.Drawing.Point))
                {
                    var parts = s.Split(',');
                    return parts.Length == 2
                        ? new System.Drawing.Point(int.Parse(parts[0].Trim()), int.Parse(parts[1].Trim()))
                        : System.Drawing.Point.Empty;
                }
            }

            if (underlying == typeof(System.Drawing.Size) && value is System.Drawing.SizeF sizeF)
            {
                return new System.Drawing.Size((int)sizeF.Width, (int)sizeF.Height);
            }

            if (underlying == typeof(System.Drawing.Point) && value is System.Drawing.PointF pointF)
            {
                return new System.Drawing.Point((int)pointF.X, (int)pointF.Y);
            }

            return Convert.ChangeType(value, underlying, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Produces a display string for a property value, matching the way the Visual Studio
    /// property grid renders complex values.
    /// </summary>
    public static string FormatValue(object? value) => value switch
    {
        null => "(null)",
        System.Drawing.Color color => Model.ColorText.Format(color),
        System.Drawing.Font font => $"{font.Name}, {font.Size:0.##}",
        System.Drawing.Point point => $"{point.X}, {point.Y}",
        System.Drawing.Size size => $"{size.Width}, {size.Height}",
        System.Drawing.Rectangle rect => $"{rect.X}, {rect.Y}, {rect.Width}, {rect.Height}",
        System.Windows.Forms.AnchorStyles anchors => FormatAnchors(anchors),
        bool b => b ? "True" : "False",
        Enum e => e.ToString(),
        _ => value.ToString() ?? string.Empty,
    };

    private static string FormatAnchors(System.Windows.Forms.AnchorStyles anchors)
    {
        if (anchors == (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left))
        {
            return "Top, Left";
        }

        if (anchors == System.Windows.Forms.AnchorStyles.None)
        {
            return "None";
        }

        var parts = new List<string>();
        if (anchors.HasFlag(System.Windows.Forms.AnchorStyles.Top))
        {
            parts.Add("Top");
        }

        if (anchors.HasFlag(System.Windows.Forms.AnchorStyles.Bottom))
        {
            parts.Add("Bottom");
        }

        if (anchors.HasFlag(System.Windows.Forms.AnchorStyles.Left))
        {
            parts.Add("Left");
        }

        if (anchors.HasFlag(System.Windows.Forms.AnchorStyles.Right))
        {
            parts.Add("Right");
        }

        return parts.Count == 0 ? anchors.ToString() : string.Join(", ", parts);
    }

    /// <summary>
    /// Parses a location or size in the <c>X, Y</c> notation used by the property grid.
    /// </summary>
    public static bool TryParsePoint(string text, out System.Drawing.Point value)
    {
        value = System.Drawing.Point.Empty;
        var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var x) || !int.TryParse(parts[1], out var y))
        {
            return false;
        }

        value = new System.Drawing.Point(x, y);
        return true;
    }

    /// <summary>Parses a size in the <c>Width, Height</c> notation used by the property grid.</summary>
    public static bool TryParseSize(string text, out System.Drawing.Size value)
    {
        value = System.Drawing.Size.Empty;
        var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var w) || !int.TryParse(parts[1], out var h))
        {
            return false;
        }

        value = new System.Drawing.Size(w, h);
        return true;
    }

    /// <summary>Returns the type of the property, resolving nullable value types.</summary>
    public static Type UnwrapNullable(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    /// <summary>Returns the public parameterless constructor of a type, if it has one.</summary>
    public static ConstructorInfo? GetDefaultConstructor(Type type)
        => type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);
}
