namespace WinFormsDesigner.Core.Model;

/// <summary>
/// A single node in the design tree. Every node wraps a control type, the properties that
/// have been explicitly assigned to it and its child nodes.
/// </summary>
/// <remarks>
/// Child order follows the WinForms z-order convention: index 0 is the front-most child.
/// </remarks>
public sealed class DesignNode
{
    private string _name;

    public DesignNode(Type controlType, string name)
    {
        ControlType = controlType ?? throw new ArgumentNullException(nameof(controlType));
        _name = name;
        Properties = new PropertyBag();
    }

    /// <summary>The control type represented by this node.</summary>
    public Type ControlType { get; }

    /// <summary>Name of the control, used for the generated field name.</summary>
    public string Name
    {
        get => _name;
        set
        {
            if (string.Equals(_name, value, StringComparison.Ordinal))
            {
                return;
            }

            _name = value;
            NameChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Explicitly assigned property values.</summary>
    public PropertyBag Properties { get; }

    /// <summary>Event name to handler method name.</summary>
    public IReadOnlyDictionary<string, string> Events => _events;

    private readonly Dictionary<string, string> _events = new(StringComparer.Ordinal);

    /// <summary>Wires an event to a handler method.</summary>
    public void SetEvent(string eventName, string handlerName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(handlerName))
        {
            _events.Remove(eventName);
        }
        else
        {
            _events[eventName] = handlerName;
        }

        EventsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Removes an event handler.</summary>
    public void RemoveEvent(string eventName)
    {
        if (_events.Remove(eventName))
        {
            EventsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Removes every wired event handler.</summary>
    public void ClearEvents()
    {
        if (_events.Count == 0)
        {
            return;
        }

        _events.Clear();
        EventsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised when the wired events changed.</summary>
    public event EventHandler? EventsChanged;

    /// <summary>Parent node, or <c>null</c> for the root.</summary>
    public DesignNode? Parent { get; set; }

    /// <summary>
    /// Column index when this node is hosted by a <see cref="System.Windows.Forms.TableLayoutPanel"/>,
    /// or <c>null</c> when the default placement applies.
    /// </summary>
    public int? TableColumn { get; set; }

    /// <summary>
    /// Row index when this node is hosted by a <see cref="System.Windows.Forms.TableLayoutPanel"/>,
    /// or <c>null</c> when the default placement applies.
    /// </summary>
    public int? TableRow { get; set; }

    /// <summary>Child nodes. Index 0 is the front-most child.</summary>
    public List<DesignNode> Children { get; } = new();

    /// <summary>Raised when the node name changed.</summary>
    public event EventHandler? NameChanged;

    /// <summary>Raised when one of the explicitly assigned property values changed.</summary>
    public event EventHandler? PropertiesChanged;

    /// <summary>Raised when children were added, removed or reordered.</summary>
    public event EventHandler? StructureChanged;

    /// <summary>True when this node represents the root form.</summary>
    public bool IsRoot => Parent is null;

    /// <summary>Enumerates this node and all of its descendants, depth first.</summary>
    public IEnumerable<DesignNode> Descendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.Descendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>Enumerates this node and all of its descendants, depth first, including this node.</summary>
    public IEnumerable<DesignNode> SelfAndDescendants()
    {
        yield return this;
        foreach (var descendant in Descendants())
        {
            yield return descendant;
        }
    }

    /// <summary>Computes a control name that is not yet used by a sibling.</summary>
    public string MakeUniqueName(string desiredName)
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Parent is not null)
        {
            foreach (var sibling in Parent.Children)
            {
                taken.Add(sibling.Name);
            }
        }

        if (!taken.Contains(desiredName))
        {
            return desiredName;
        }

        for (var i = 1; ; i++)
        {
            var candidate = desiredName + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    /// <summary>Notifies listeners that an explicitly assigned property value changed.</summary>
    public void RaisePropertiesChanged() => PropertiesChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>Notifies listeners that children were added, removed or reordered.</summary>
    public void RaiseStructureChanged() => StructureChanged?.Invoke(this, EventArgs.Empty);
}
