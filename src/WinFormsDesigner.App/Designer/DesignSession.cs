using System.Drawing;
using WinFormsDesigner.Core.Model;
using WinFormsDesigner.Core.Reflection;

namespace WinFormsDesigner.App.Designer;

/// <summary>
/// Owns the document being edited and keeps a live WinForms control tree in sync with it.
/// The document is the single source of truth; the live tree is a projection of it.
/// </summary>
public sealed class DesignSession
{
    private readonly List<DesignNode> _selectedNodes = [];
    private readonly Dictionary<DesignNode, Control> _live = [];
    private readonly Dictionary<DesignNode, object> _components = [];
    private readonly List<SessionSnapshot> _undo = [];
    private readonly List<SessionSnapshot> _redo = [];
    private readonly List<DesignNode> _clipboard = [];

    private System.ComponentModel.Container _componentContainer = new();

    public DesignSession()
    {
        Document = new DesignDocument();
        RootForm = new Form
        {
            TopLevel = false,
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            Text = Document.ClassName,
        };

        RootForm.Size = new Size(800, 450);
        Document.Root.Properties.SetValue("ClientSize", new Size(800, 450));
        Document.Root.Properties.SetValue("Text", Document.ClassName);
        Document.Root.Properties.SetValue("BackColor", SystemColors.Control);
        Document.Root.SetEvent("Load", Document.ClassName + "_Load");

        RebuildLiveTree();
    }

    /// <summary>The document being edited.</summary>
    public DesignDocument Document { get; }

    /// <summary>The live form that represents the root node on the design surface.</summary>
    public Form RootForm { get; }

    /// <summary>Currently selected node, or <c>null</c> when nothing is selected.</summary>
    public DesignNode? SelectedNode => _selectedNodes.Count == 0 ? null : _selectedNodes[_selectedNodes.Count - 1];

    /// <summary>All currently selected nodes.</summary>
    public IReadOnlyList<DesignNode> SelectedNodes => _selectedNodes;

    /// <summary>Currently selected live control, or <c>null</c>.</summary>
    public Control? SelectedControl => SelectedNode is null ? null : GetLive(SelectedNode);

    /// <summary>Raised when the selection changed.</summary>
    public event EventHandler? SelectionChanged;

    /// <summary>Raised when the document or the live tree changed.</summary>
    public event EventHandler? Changed;

    /// <summary>Raised when the node tree structure changed.</summary>
    public event EventHandler? StructureChanged;

    /// <summary>Returns the live control that represents a node.</summary>
    public Control? GetLive(DesignNode node)
    {
        if (node.Parent is null)
        {
            return RootForm;
        }

        return _live.TryGetValue(node, out var control) ? control : null;
    }

    /// <summary>
    /// Returns the live instance that represents a node. This is the control for a visual
    /// node and the component instance for a node without a visual representation.
    /// </summary>
    public object? GetInstance(DesignNode node)
    {
        if (node.Parent is null)
        {
            return RootForm;
        }

        if (_live.TryGetValue(node, out var control))
        {
            return control;
        }

        return _components.TryGetValue(node, out var component) ? component : null;
    }

    /// <summary>Returns the node that a live control represents.</summary>
    public DesignNode? GetNode(Control control)
    {
        if (ReferenceEquals(control, RootForm))
        {
            return Document.Root;
        }

        foreach (var (node, live) in _live)
        {
            if (ReferenceEquals(live, control))
            {
                return node;
            }
        }

        return null;
    }

    /// <summary>Selects a node, replacing the current selection unless <paramref name="addToSelection"/> is set.</summary>
    public void Select(DesignNode? node, bool addToSelection = false)
    {
        if (node is null)
        {
            if (_selectedNodes.Count == 0)
            {
                return;
            }

            _selectedNodes.Clear();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (addToSelection)
        {
            if (!_selectedNodes.Contains(node))
            {
                _selectedNodes.Add(node);
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }

            return;
        }

        if (_selectedNodes.Count == 1 && ReferenceEquals(_selectedNodes[0], node))
        {
            return;
        }

        _selectedNodes.Clear();
        _selectedNodes.Add(node);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Toggles a node in the current selection.</summary>
    public void ToggleSelection(DesignNode node)
    {
        if (_selectedNodes.Remove(node))
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        _selectedNodes.Add(node);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Clears the current selection.</summary>
    public void ClearSelection()
    {
        if (_selectedNodes.Count == 0)
        {
            return;
        }

        _selectedNodes.Clear();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Replaces the current selection with the given nodes.</summary>
    public void SelectNodes(IEnumerable<DesignNode> nodes)
    {
        _selectedNodes.Clear();
        foreach (var node in nodes)
        {
            if (!_selectedNodes.Contains(node))
            {
                _selectedNodes.Add(node);
            }
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Selects the live control under the given point, if any.</summary>
    public DesignNode? HitTest(Point formPoint)
    {
        if (!RootForm.ClientRectangle.Contains(formPoint))
        {
            return null;
        }

        return HitTestChildren(Document.Root, formPoint) ?? Document.Root;
    }

    private DesignNode? HitTestChildren(DesignNode parentNode, Point pointInParent)
    {
        for (var index = 0; index < parentNode.Children.Count; index++)
        {
            var childNode = parentNode.Children[index];
            if (childNode.Properties.TryGetValue("Visible", out var visible) && visible is false)
            {
                continue;
            }

            var child = GetLive(childNode);
            if (child is null || !child.Bounds.Contains(pointInParent))
            {
                continue;
            }

            var childPoint = new Point(pointInParent.X - child.Left, pointInParent.Y - child.Top);
            return HitTestChildren(childNode, childPoint) ?? childNode;
        }

        return null;
    }

    // ---------------------------------------------------------------------
    // Structure
    // ---------------------------------------------------------------------

    /// <summary>
    /// Adds a new control of the given type. When a container is selected the control is
    /// placed inside it, otherwise next to the selection.
    /// </summary>
    public DesignNode AddControl(Type controlType, Point locationInParent)
    {
        var parent = GetInsertionParent();
        var entry = ControlTypeRegistry.Find(controlType);

        var node = new DesignNode(
            controlType,
            MakeUniqueDefaultName(entry?.DefaultName ?? ControlTypeRegistry.ToCamelCase(controlType.Name)));

        if (!ControlFactory.IsComponent(controlType))
        {
            // Only values that differ from the control default are stored, so that the
            // generated code stays free of redundant assignments.
            if (locationInParent != Point.Empty)
            {
                node.Properties.SetValue("Location", locationInParent);
            }

            // Newly added controls display their name as their caption so that they are
            // identifiable on the design surface (and stay in sync after auto-renaming).
            if (controlType.GetProperty("Text") is { CanWrite: true })
            {
                node.Properties.SetValue("Text", node.Name);
            }
        }


        if (typeof(ToolStrip).IsAssignableFrom(controlType) || typeof(StatusStrip).IsAssignableFrom(controlType))
        {
            node.Properties.SetValue("Dock", DockStyle.Bottom);
        }

        PushUndo();

        parent.Children.Insert(0, node);
        node.Parent = parent;
        node.RaiseStructureChanged();

        RebuildLiveTree();
        Select(node);
        RaiseChanged();
        return node;
    }

    /// <summary>Removes the selected controls.</summary>
    public bool DeleteSelection()
    {
        var nodes = _selectedNodes.Where(n => n.Parent is not null).Distinct().ToList();
        if (nodes.Count == 0)
        {
            return false;
        }

        var parents = nodes.Select(n => n.Parent).Distinct().ToList();

        PushUndo();
        foreach (var node in nodes)
        {
            node.Parent!.Children.Remove(node);
            node.Parent = null;
        }

        _selectedNodes.Clear();

        RebuildLiveTree();
        if (parents.Count == 1 && parents[0] is not null)
        {
            Select(parents[0]);
        }
        else
        {
            Select(Document.Root);
        }

        RaiseChanged();
        return true;
    }

    /// <summary>Moves the selected control(s) to the front of the z-order.</summary>
    public void BringToFront()
    {
        var selected = _selectedNodes.Where(n => n.Parent is not null).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        // Group the selected nodes by their parent so each container is handled
        // independently. Within a container the relative stacking order of the
        // moved controls is preserved.
        var groups = selected.GroupBy(n => n.Parent!).ToList();
        var changed = false;

        foreach (var group in groups)
        {
            var parent = group.Key;
            var selectedInParent = parent.Children.Where(c => group.Contains(c)).ToList();
            if (selectedInParent.Count == 0)
            {
                continue;
            }

            // Already at the front in the same relative order: nothing to do.
            var alreadyFront = true;
            for (var i = 0; i < selectedInParent.Count; i++)
            {
                if (parent.Children.IndexOf(selectedInParent[i]) != i)
                {
                    alreadyFront = false;
                    break;
                }
            }

            if (alreadyFront)
            {
                continue;
            }

            changed = true;
            foreach (var node in selectedInParent)
            {
                parent.Children.Remove(node);
            }

            for (var i = selectedInParent.Count - 1; i >= 0; i--)
            {
                parent.Children.Insert(0, selectedInParent[i]);
            }
        }

        if (!changed)
        {
            return;
        }

        PushUndo();
        RebuildLiveTree();
        SelectNodes(selected);
        RaiseChanged();
    }

    /// <summary>Moves the selected control(s) to the back of the z-order.</summary>
    public void SendToBack()
    {
        var selected = _selectedNodes.Where(n => n.Parent is not null).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        var groups = selected.GroupBy(n => n.Parent!).ToList();
        var changed = false;

        foreach (var group in groups)
        {
            var parent = group.Key;
            var selectedInParent = parent.Children.Where(c => group.Contains(c)).ToList();
            if (selectedInParent.Count == 0)
            {
                continue;
            }

            var lastIndex = parent.Children.Count - 1;
            var alreadyBack = true;
            for (var i = 0; i < selectedInParent.Count; i++)
            {
                if (parent.Children.IndexOf(selectedInParent[i]) != lastIndex - selectedInParent.Count + 1 + i)
                {
                    alreadyBack = false;
                    break;
                }
            }

            if (alreadyBack)
            {
                continue;
            }

            changed = true;
            foreach (var node in selectedInParent)
            {
                parent.Children.Remove(node);
            }

            foreach (var node in selectedInParent)
            {
                parent.Children.Add(node);
            }
        }

        if (!changed)
        {
            return;
        }

        PushUndo();
        RebuildLiveTree();
        SelectNodes(selected);
        RaiseChanged();
    }

    /// <summary>Aligns the selected control to the given edge of its parent.</summary>
    public void Align(DesignerAlign alignment)
    {
        if (SelectedNode?.Parent is null)
        {
            return;
        }

        var node = SelectedNode;
        var parent = node.Parent;
        var live = GetLive(node);
        var parentLive = GetLive(parent);
        if (live is null || parentLive is null)
        {
            return;
        }

        var bounds = live.Bounds;
        var parentBounds = parentLive.ClientRectangle;

        switch (alignment)
        {
            case DesignerAlign.Left:
                bounds.X = 0;
                break;
            case DesignerAlign.Right:
                bounds.X = parentBounds.Width - bounds.Width;
                break;
            case DesignerAlign.Top:
                bounds.Y = 0;
                break;
            case DesignerAlign.Bottom:
                bounds.Y = parentBounds.Height - bounds.Height;
                break;
            case DesignerAlign.CenterHorizontal:
                bounds.X = (parentBounds.Width - bounds.Width) / 2;
                break;
            case DesignerAlign.CenterVertical:
                bounds.Y = (parentBounds.Height - bounds.Height) / 2;
                break;
        }

        PushUndo();
        node.Properties.SetValue("Location", bounds.Location);
        live.Location = bounds.Location;
        RaiseChanged();
    }

    /// <summary>Sets the size of the selected control.</summary>
    public void ResizeTo(DesignNode node, Size size)
    {
        var live = GetLive(node);
        if (live is null)
        {
            return;
        }

        SetBounds(node, new Rectangle(live.Location, size));
    }

    /// <summary>Sets the location of a control.</summary>
    public void MoveTo(DesignNode node, Point location)
    {
        var live = GetLive(node);
        if (live is null)
        {
            return;
        }

        SetBounds(node, new Rectangle(location, live.Size));
    }

    /// <summary>Sets a control's bounds as one undoable edit.</summary>
    public void SetBounds(DesignNode node, Rectangle bounds)
    {
        var live = GetLive(node);
        if (live is null)
        {
            return;
        }

        if (node.Parent is null)
        {
            if (live.ClientSize == bounds.Size)
            {
                return;
            }

            PushUndo();
            SetStoredProperty(node, "ClientSize", bounds.Size);
            live.ClientSize = bounds.Size;
        }
        else
        {
            if (live.Bounds == bounds)
            {
                return;
            }

            PushUndo();
            SetStoredProperty(node, "Location", bounds.Location);
            SetStoredProperty(node, "Size", bounds.Size);
            live.Bounds = bounds;
        }

        RaiseChanged();
    }

    /// <summary>Commits bounds that were already applied to the live control during a gesture.</summary>
    public void CommitBounds(DesignNode node, Rectangle originalBounds, Rectangle bounds, bool pushUndo = true)
    {
        var live = GetLive(node);
        if (live is null || originalBounds == bounds)
        {
            return;
        }

        if (pushUndo)
        {
            PushUndo();
        }

        if (node.Parent is null)
        {
            SetStoredProperty(node, "ClientSize", bounds.Size);
            if (live.ClientSize != bounds.Size)
            {
                live.ClientSize = bounds.Size;
            }
        }
        else
        {
            SetStoredProperty(node, "Location", bounds.Location);
            SetStoredProperty(node, "Size", bounds.Size);
            if (live.Bounds != bounds)
            {
                live.Bounds = bounds;
            }
        }

        RaiseChanged();
    }

    private void SetStoredProperty(DesignNode node, string propertyName, object value)
    {
        if (IsDefaultValue(node.ControlType, propertyName, value))
        {
            node.Properties.Remove(propertyName);
        }
        else
        {
            node.Properties.SetValue(propertyName, value);
        }
    }

    // ---------------------------------------------------------------------
    // Properties
    // ---------------------------------------------------------------------

    /// <summary>
    /// Assigns a property value. Values equal to the control default are removed from the
    /// document so the generated code stays free of redundant assignments.
    /// </summary>
    public void SetProperty(DesignNode node, string propertyName, object? value)
    {
        var entry = PropertyCatalog.Find(node.ControlType, propertyName);
        var live = GetLive(node);

        var current = entry is not null && live is not null ? entry.GetValue(live) : null;
        if (ValuesEqual(current, value))
        {
            return;
        }

        PushUndo();

        if (IsDefaultValue(node.ControlType, propertyName, value))
        {
            node.Properties.Remove(propertyName);
        }
        else
        {
            node.Properties.SetValue(propertyName, PropertyBag.CloneValue(value));
        }

        if (live is not null && entry is not null)
        {
            try
            {
                entry.SetValue(live, value);
            }
            catch (Exception)
            {
                // Ignore values a control refuses; the designer must stay responsive.
            }
        }

        if (string.Equals(propertyName, "Text", StringComparison.Ordinal) && node.Parent is null)
        {
            RootForm.Text = value as string ?? node.Name;
        }

        RaiseChanged();
    }

    /// <summary>Renames a node.</summary>
    public void SetName(DesignNode node, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || node.Name == name)
        {
            return;
        }

        PushUndo();
        node.Name = MakeUniqueName(name, node);
        if (GetLive(node) is { } live)
        {
            live.Name = node.Name;
        }

        RaiseChanged();
    }

    /// <summary>Wires or unwires an event handler.</summary>
    public void SetEvent(DesignNode node, string eventName, string handlerName)
    {
        PushUndo();
        if (string.IsNullOrWhiteSpace(handlerName))
        {
            node.RemoveEvent(eventName);
        }
        else
        {
            node.SetEvent(eventName, handlerName);
        }

        RaiseChanged();
    }

    // ---------------------------------------------------------------------
    // Clipboard
    // ---------------------------------------------------------------------

    /// <summary>Copies the selected controls into the internal clipboard.</summary>
    public void CopySelection()
    {
        _clipboard.Clear();
        foreach (var node in _selectedNodes.Where(n => n.Parent is not null))
        {
            _clipboard.Add(CloneSubtree(node));
        }
    }

    /// <summary>Cuts the selected controls into the internal clipboard.</summary>
    public void CutSelection()
    {
        CopySelection();
        DeleteSelection();
    }

    /// <summary>Pastes the clipboard content next to the current selection.</summary>
    public void Paste()
    {
        if (_clipboard.Count == 0)
        {
            return;
        }

        var parent = GetInsertionParent();
        PushUndo();

        var offset = new Point(10, 10);
        var pasted = new List<DesignNode>();
        foreach (var clipboardNode in _clipboard)
        {
            var copy = CloneSubtree(clipboardNode);
            copy.Name = MakeUniqueDefaultName(GetNameBase(copy.Name));
            if (copy.Properties.TryGetValue("Text", out var textValue) && textValue is string textValueString
                && string.Equals(textValueString, clipboardNode.Name, StringComparison.Ordinal))
            {
                copy.Properties.SetValue("Text", copy.Name);
            }
            copy.Parent = parent;
            parent.Children.Insert(0, copy);
            if (!typeof(System.Windows.Forms.TableLayoutPanel).IsAssignableFrom(parent.ControlType))
            {
                copy.Properties.SetValue("Location", new Point(
                    (copy.Properties.TryGetValue("Location", out var l) && l is Point p ? p.X : 0) + offset.X,
                    (copy.Properties.TryGetValue("Location", out var l2) && l2 is Point p2 ? p2.Y : 0) + offset.Y));
            }
            pasted.Add(copy);
        }

        RebuildLiveTree();
        SelectNodes(pasted);
        RaiseChanged();
    }

    /// <summary>Selects every control in the document.</summary>
    public void SelectAll()
    {
        Select(Document.Root);
    }

    /// <summary>
    /// Replaces the document content with the given document and rebuilds the live tree.
    /// Used to load an existing form (e.g. from a <c>.designer.cs</c> file) for editing.
    /// </summary>
    public void LoadDocument(DesignDocument document)
    {
        Document.Reset();
        Document.ClassName = document.ClassName;
        Document.Namespace = document.Namespace;
        Document.ProjectName = document.ProjectName;
        Document.OutputDirectory = document.OutputDirectory;
        Document.ResourceDirectory = document.ResourceDirectory;

        foreach (var (key, value) in document.Root.Properties.Values)
        {
            Document.Root.Properties.SetValue(key, value);
        }

        foreach (var (key, value) in document.Root.Events)
        {
            Document.Root.SetEvent(key, value);
        }

        Document.Root.Name = document.Root.Name;
        Document.ClassName = document.ClassName;

        foreach (var child in document.Root.Children)
        {
            var copy = CloneSubtree(child);
            copy.Parent = Document.Root;
            Document.Root.Children.Add(copy);
        }

        RebuildLiveTree();
        Select(Document.Root);
        RaiseChanged();
    }

    // ---------------------------------------------------------------------
    // Undo / redo
    // ---------------------------------------------------------------------

    /// <summary>Number of available undo steps.</summary>
    public int UndoCount => _undo.Count;

    /// <summary>Number of available redo steps.</summary>
    public int RedoCount => _redo.Count;

    /// <summary>Undoes the last change.</summary>
    public void Undo()
    {
        if (_undo.Count == 0)
        {
            return;
        }

        var snapshot = PopLast(_undo);
        _redo.Add(CaptureSnapshot());
        RestoreSnapshot(snapshot);
    }

    /// <summary>Redoes the last undone change.</summary>
    public void Redo()
    {
        if (_redo.Count == 0)
        {
            return;
        }

        var snapshot = PopLast(_redo);
        _undo.Add(CaptureSnapshot());
        RestoreSnapshot(snapshot);
    }

    private static SessionSnapshot PopLast(List<SessionSnapshot> stack)
    {
        var snapshot = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return snapshot;
    }

    public void PushUndo()
    {
        _undo.Add(CaptureSnapshot());
        if (_undo.Count > 100)
        {
            _undo.RemoveAt(0);
        }

        _redo.Clear();
    }

    private SessionSnapshot? _pendingSnapshot;

    /// <summary>
    /// Starts a continuous interaction such as a drag. The state is captured now and only
    /// pushed onto the undo stack once the interaction actually changed something.
    /// </summary>
    public void BeginInteraction()
    {
        _pendingSnapshot ??= CaptureSnapshot();
    }

    /// <summary>Ends the current interaction.</summary>
    public void EndInteraction(bool changed)
    {
        if (_pendingSnapshot is null)
        {
            return;
        }

        if (changed)
        {
            _undo.Add(_pendingSnapshot);
            if (_undo.Count > 100)
            {
                _undo.RemoveAt(0);
            }

            _redo.Clear();
        }

        _pendingSnapshot = null;
    }

    private SessionSnapshot CaptureSnapshot()
    {
        var snapshot = new SessionSnapshot();
        CaptureNode(Document.Root, snapshot.Root);
        return snapshot;
    }

    private static void CaptureNode(DesignNode node, NodeSnapshot target)
    {
        target.Type = node.ControlType;
        target.Name = node.Name;
        target.Properties = node.Properties.Clone();
        foreach (var (key, value) in node.Events)
        {
            target.Events[key] = value;
        }

        foreach (var child in node.Children)
        {
            var childSnapshot = new NodeSnapshot();
            CaptureNode(child, childSnapshot);
            target.Children.Add(childSnapshot);
        }
    }

    private void RestoreSnapshot(SessionSnapshot snapshot)
    {
        var selectedName = SelectedNode?.Name;
        var rootSnapshot = snapshot.Root;

        Document.Reset();

        // The root keeps the identity of the form; only its content is restored.
        foreach (var (key, value) in rootSnapshot.Properties.Values)
        {
            Document.Root.Properties.SetValue(key, value);
        }

        foreach (var (key, value) in rootSnapshot.Events)
        {
            Document.Root.SetEvent(key, value);
        }

        foreach (var childSnapshot in rootSnapshot.Children)
        {
            var child = RestoreNode(childSnapshot);
            child.Parent = Document.Root;
            Document.Root.Children.Add(child);
        }

        Document.Root.Name = Document.ClassName;

        RebuildLiveTree();
        Select(selectedName is null ? null : Document.FindNode(selectedName));
        RaiseChanged();
    }

    private static DesignNode RestoreNode(NodeSnapshot snapshot)
    {
        var node = new DesignNode(snapshot.Type, snapshot.Name);
        foreach (var (key, value) in snapshot.Properties.Values)
        {
            node.Properties.SetValue(key, value);
        }

        foreach (var (key, value) in snapshot.Events)
        {
            node.SetEvent(key, value);
        }

        foreach (var childSnapshot in snapshot.Children)
        {
            var child = RestoreNode(childSnapshot);
            child.Parent = node;
            node.Children.Add(child);
        }

        return node;
    }

    // ---------------------------------------------------------------------
    // Live tree
    // ---------------------------------------------------------------------

    /// <summary>
    /// Forces the creation of window handles for the root form and every descendant control so
    /// that <see cref="Control.DrawToBitmap"/> renders the whole live tree.
    /// </summary>
    public void EnsureHandles()
    {
        EnsureHandles(RootForm);
    }

    private static void EnsureHandles(Control control)
    {
        try
        {
            _ = control.Handle;
        }
        catch
        {
            // A control that refuses to create its handle is simply not rendered.
        }

        foreach (Control child in control.Controls)
        {
            EnsureHandles(child);
        }
    }

    public void RebuildLiveTree()
    {
        _live.Clear();
        _components.Clear();
        RootForm.Controls.Clear();
        RootForm.SuspendLayout();

        // The components of the previous build are disposed and replaced by a fresh container.
        _componentContainer.Dispose();
        _componentContainer = new System.ComponentModel.Container();

        var rootProperties = Document.Root.Properties;
        ControlFactory.Apply(RootForm, rootProperties);
        RootForm.FormBorderStyle = FormBorderStyle.None;
        RootForm.Name = Document.Root.Name;
        RootForm.Text = rootProperties.TryGetValue("Text", out var caption) && caption is string captionText
            ? captionText
            : Document.ClassName;

        foreach (var child in Document.Root.Children)
        {
            AttachChild(RootForm, child);
        }

        RootForm.ResumeLayout(true);
        EnsureHandles(RootForm);
        ResolveReferences();
        StructureChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Connects properties that point at another control of the same document, e.g. the
    /// context menu of a button or the accept button of a form.
    /// </summary>
    private void ResolveReferences()
    {
        foreach (var node in Document.Root.SelfAndDescendants())
        {
            var live = GetLive(node);
            if (live is null)
            {
                continue;
            }

            foreach (var (name, value) in node.Properties.Values)
            {
                if (value is not string reference)
                {
                    continue;
                }

                var entry = PropertyCatalog.Find(node.ControlType, name);
                if (entry is null || !IsReferenceProperty(entry.ValueType))
                {
                    continue;
                }

                var target = Document.FindNode(reference);
                var targetLive = target is null ? null : GetLive(target);
                if (targetLive is null)
                {
                    continue;
                }

                try
                {
                    entry.SetValue(live, targetLive);
                }
                catch (Exception)
                {
                    // A reference the control refuses is simply not applied.
                }
            }
        }
    }

    private static bool IsReferenceProperty(Type type)
        => type != typeof(string)
            && (typeof(System.Windows.Forms.Control).IsAssignableFrom(type)
                || typeof(System.Windows.Forms.IButtonControl).IsAssignableFrom(type));

    private Control CreateLive(DesignNode node)
    {
        var live = ControlFactory.Create(node.ControlType);
        _live[node] = live;
        live.Name = node.Name;
        ControlFactory.Apply(live, node.Properties);

        foreach (var child in node.Children)
        {
            AttachChild(live, child);
        }

        return live;
    }

    /// <summary>
    /// Attaches the child of a node to its live parent. Controls go into the control
    /// collection, components into the component container and tool strip items into the
    /// item collection of their strip.
    /// </summary>
    private void AttachChild(Control parent, DesignNode child)
    {
        if (IsToolStripItem(child.ControlType))
        {
            CreateStripItem(parent, child);
            return;
        }

        if (ControlFactory.IsComponent(child.ControlType))
        {
            CreateComponent(child);
            return;
        }

        var childLive = CreateLive(child);

        if (IsContextMenuStrip(child.ControlType))
        {
            // A context menu has no representation on the surface, but it must stay alive
            // so that it can be assigned to controls.
            _componentContainer.Add(childLive);
            return;
        }

        if (parent is System.Windows.Forms.TableLayoutPanel tableLayout
            && (child.TableColumn.HasValue || child.TableRow.HasValue))
        {
            tableLayout.Controls.Add(childLive, child.TableColumn ?? 0, child.TableRow ?? 0);
        }
        else
        {
            parent.Controls.Add(childLive);
        }
    }

    /// <summary>
    /// Creates the live instance of a node without a visual representation, such as a timer
    /// or a tool tip, and registers it with the component container of the form.
    /// </summary>
    private void CreateComponent(DesignNode node)
    {
        object instance;
        try
        {
            instance = ControlFactory.CreateInstance(node.ControlType, _componentContainer);
        }
        catch (Exception)
        {
            return;
        }

        _components[node] = instance;
        ControlFactory.Apply(instance, node.Properties);
    }

    /// <summary>Creates the live instance of a tool strip item.</summary>
    private void CreateStripItem(object parent, DesignNode node)
    {
        object instance;
        try
        {
            instance = ControlFactory.CreateInstance(node.ControlType);
        }
        catch (Exception)
        {
            return;
        }

        _components[node] = instance;
        ControlFactory.Apply(instance, node.Properties);

        if (instance is ToolStripItem item)
        {
            if (parent is ToolStrip strip)
            {
                strip.Items.Add(item);
            }
            else if (parent is ToolStripMenuItem menuItem)
            {
                menuItem.DropDownItems.Add(item);
            }

            // Recurse into nested items (sub-menus, drop-downs).
            foreach (var child in node.Children)
            {
                if (IsToolStripItem(child.ControlType))
                {
                    CreateStripItem(item, child);
                }
                else if (ControlFactory.IsComponent(child.ControlType))
                {
                    CreateComponent(child);
                }
            }
        }
    }

    /// <summary>Determines whether the type is a tool strip item.</summary>
    private static bool IsToolStripItem(Type type) => typeof(ToolStripItem).IsAssignableFrom(type);

    /// <summary>Determines whether the type is a context menu strip.</summary>
    private static bool IsContextMenuStrip(Type type) => typeof(ContextMenuStrip).IsAssignableFrom(type);

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    /// <summary>Returns the node that receives a newly inserted control.</summary>
    public DesignNode GetInsertionParent()
    {
        foreach (var selectedNode in Enumerable.Reverse(_selectedNodes))
        {
            if (IsContainer(selectedNode.ControlType))
            {
                return selectedNode;
            }
        }

        if (SelectedNode is null)
        {
            return Document.Root;
        }

        if (IsContainer(SelectedNode.ControlType))
        {
            return SelectedNode;
        }

        return SelectedNode.Parent ?? Document.Root;
    }

    private static bool IsContainer(Type type)
        => typeof(System.Windows.Forms.ContainerControl).IsAssignableFrom(type)
            || type == typeof(Panel)
            || type == typeof(GroupBox)
            || type == typeof(TabPage)
            || type == typeof(SplitContainer);

    /// <summary>Creates a name that is not used anywhere in the document.</summary>
    public string MakeUniqueName(string desired, DesignNode? except = null)
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in Document.AllNodes())
        {
            if (!ReferenceEquals(node, except))
            {
                taken.Add(node.Name);
            }
        }

        if (!taken.Contains(desired))
        {
            return desired;
        }

        for (var i = 1; ; i++)
        {
            var candidate = desired + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    /// <summary>
    /// Creates the name of a newly added control. The Visual Studio designer numbers the
    /// controls of a type, e.g. <c>button1</c>, <c>button2</c>, and starts at one.
    /// </summary>
    public string MakeUniqueDefaultName(string baseName)
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in Document.AllNodes())
        {
            taken.Add(node.Name);
        }

        for (var i = 1; ; i++)
        {
            var candidate = baseName + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    /// <summary>
    /// Strips the trailing number of a control name, so that <c>button1</c> becomes
    /// <c>button</c> and can be numbered again.
    /// </summary>
    private static string GetNameBase(string name)
    {
        var index = name.Length;
        while (index > 0 && char.IsDigit(name[index - 1]))
        {
            index--;
        }

        return index == name.Length ? name : name[..index];
    }

    private static DesignNode CloneSubtree(DesignNode node)
    {
        var clone = new DesignNode(node.ControlType, node.Name);
        clone.TableColumn = node.TableColumn;
        clone.TableRow = node.TableRow;
        foreach (var (key, value) in node.Properties.Values)
        {
            clone.Properties.SetValue(key, PropertyBag.CloneValue(value));
        }

        foreach (var (key, value) in node.Events)
        {
            clone.SetEvent(key, value);
        }

        foreach (var child in node.Children)
        {
            var childClone = CloneSubtree(child);
            childClone.Parent = clone;
            clone.Children.Add(childClone);
        }

        return clone;
    }

    private static bool IsDefaultValue(Type controlType, string propertyName, object? value)
    {
        var defaultValue = ControlFactory.GetDefaultValue(controlType, propertyName);
        return ValuesEqual(defaultValue, value);
    }

    private static bool ValuesEqual(object? left, object? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        if (left is string leftText && right is string rightText)
        {
            return string.Equals(leftText, rightText, StringComparison.Ordinal);
        }

        if (left is System.Drawing.Color leftColor && right is System.Drawing.Color rightColor)
        {
            // Since .NET 9 a colour comparison also takes the name into account, so a colour
            // that was built from its components would never match the named default colour.
            return leftColor.ToArgb() == rightColor.ToArgb();
        }

        return left.Equals(right);
    }

    private void RaiseChanged()
    {
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class SessionSnapshot
    {
        public NodeSnapshot Root { get; } = new();
    }

    private sealed class NodeSnapshot
    {
        public Type Type { get; set; } = typeof(Control);

        public string Name { get; set; } = string.Empty;

        public PropertyBag Properties { get; set; } = new();

        public Dictionary<string, string> Events { get; } = new(StringComparer.Ordinal);

        public List<NodeSnapshot> Children { get; } = [];
    }
}

/// <summary>Alignment options offered by the align menu.</summary>
public enum DesignerAlign
{
    Left,
    Right,
    Top,
    Bottom,
    CenterHorizontal,
    CenterVertical,
}
