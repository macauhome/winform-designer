namespace WinFormsDesigner.Core.Model;

/// <summary>
/// The document being edited: a form plus the metadata required to generate code for it.
/// </summary>
public sealed class DesignDocument
{
    private string _className = "Form1";

    public DesignDocument()
    {
        Root = new DesignNode(typeof(System.Windows.Forms.Form), "Form1");
    }

    /// <summary>The root node, always of type <see cref="System.Windows.Forms.Form"/>.</summary>
    public DesignNode Root { get; }

    /// <summary>
    /// Name of the generated form class, e.g. <c>MainForm</c>. Renaming the class also
    /// renames the automatically created load handler, so that the generated code stays
    /// consistent.
    /// </summary>
    public string ClassName
    {
        get => _className;
        set
        {
            var name = string.IsNullOrWhiteSpace(value) ? "Form1" : value;
            if (string.Equals(_className, name, StringComparison.Ordinal))
            {
                return;
            }

            var previousHandler = _className + "_Load";
            if (Root.Events.TryGetValue("Load", out var handler)
                && string.Equals(handler, previousHandler, StringComparison.Ordinal))
            {
                Root.SetEvent("Load", name + "_Load");
            }

            _className = name;
            Root.Name = name;
        }
    }

    /// <summary>Namespace of the generated code.</summary>
    public string Namespace { get; set; } = "MyApp";

    /// <summary>Name of the generated project.</summary>
    public string ProjectName { get; set; } = "MyApp";

    /// <summary>Directory the generated files are written to.</summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>Optional file the generated image resources are written next to.</summary>
    public string ResourceDirectory { get; set; } = string.Empty;

    /// <summary>Finds a node by its unique name, searching the whole tree.</summary>
    public DesignNode? FindNode(string name)
    {
        foreach (var node in Root.SelfAndDescendants())
        {
            if (string.Equals(node.Name, name, StringComparison.Ordinal))
            {
                return node;
            }
        }

        return null;
    }

    /// <summary>Enumerates every node of the document including the root.</summary>
    public IEnumerable<DesignNode> AllNodes() => Root.SelfAndDescendants();

    /// <summary>Resets the document back to an empty form.</summary>
    public void Reset()
    {
        Root.Children.Clear();
        Root.Properties.Clear();
        Root.ClearEvents();
        Root.Name = ClassName;
    }
}
