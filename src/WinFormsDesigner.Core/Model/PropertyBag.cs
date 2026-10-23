namespace WinFormsDesigner.Core.Model;

/// <summary>
/// Stores the explicitly assigned property values of a single node.
/// Only values that differ from the control's default are kept, which mirrors the
/// behaviour of the Visual Studio designer and keeps generated code free of noise.
/// </summary>
public sealed class PropertyBag
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    /// <summary>All stored values keyed by property name.</summary>
    public IReadOnlyDictionary<string, object?> Values => _values;

    /// <summary>Number of explicitly stored values.</summary>
    public int Count => _values.Count;

    /// <summary>Determines whether a value has been explicitly stored.</summary>
    public bool Contains(string propertyName) => _values.ContainsKey(propertyName);

    /// <summary>Reads a stored value.</summary>
    public bool TryGetValue(string propertyName, out object? value) => _values.TryGetValue(propertyName, out value);

    /// <summary>Stores (or replaces) a value.</summary>
    public void SetValue(string propertyName, object? value) => _values[propertyName] = value;

    /// <summary>Removes a stored value so the control falls back to its default.</summary>
    public bool Remove(string propertyName) => _values.Remove(propertyName);

    /// <summary>Removes every stored value.</summary>
    public void Clear() => _values.Clear();

    /// <summary>Creates an independent copy of this bag.</summary>
    public PropertyBag Clone()
    {
        var clone = new PropertyBag();
        foreach (var (key, value) in _values)
        {
            clone._values[key] = CloneValue(value);
        }

        return clone;
    }

    /// <summary>
    /// Creates a shallow-but-safe copy of a property value. Immutable value types can be
    /// shared; mutable ones (font, image, collections) are copied.
    /// </summary>
    public static object? CloneValue(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case System.Drawing.Font font:
                return (System.Drawing.Font)font.Clone();
            case System.Drawing.Image image:
                return image;
            case List<string> strings:
                // The collection editor stores the items as a plain list of strings.
                return new List<string>(strings);
            case System.Collections.ICollection collection:
                {
                    var copy = new List<object?>();
                    foreach (var item in collection)
                    {
                        copy.Add(CloneValue(item));
                    }

                    return copy;
                }

            default:
                return value;
        }
    }
}
