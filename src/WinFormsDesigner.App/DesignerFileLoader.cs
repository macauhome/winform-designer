using System.Globalization;
using System.Text.RegularExpressions;
using WinFormsDesigner.Core.Model;
using WinFormsDesigner.Core.Reflection;

namespace WinFormsDesigner.App;

/// <summary>
/// Loads an existing Visual Studio style <c>.designer.cs</c> file into a
/// <see cref="DesignDocument"/> so it can be edited in the designer.
/// </summary>
public static partial class DesignerFileLoader
{
    private static readonly Regex NamespaceRegex = new(@"^\s*namespace\s+([^\s{;]+)", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex ClassRegex = new(@"(?:partial\s+)?class\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex PropertyAssignRegex = new(@"(?<![.\w])(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.+?)\s*;", RegexOptions.Compiled);
    private static readonly Regex NewControlRegex = new(@"(?<![.\w])(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)\s*=\s*new\s+([\w\.\[\],\s<>]+)\s*\(\s*\)\s*;", RegexOptions.Compiled);
    private static readonly Regex ControlsAddRegex = new(@"(?<![.\w])(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)?\s*\.?\s*Controls\.Add\s*\(\s*(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)(?:\s*,\s*(\d+)\s*,\s*(\d+))?\s*\)\s*;", RegexOptions.Compiled);
    private static readonly Regex ControlsAddRangeRegex = new(@"(?<![.\w])(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)?\s*\.?\s*Controls\.AddRange\s*\(\s*new\s+[\w\.\[\],\s]+?\{([\s\S]*?)\}\s*\)\s*;", RegexOptions.Compiled);
    private static readonly Regex ToolStripItemsAddRegex = new(@"(?<![.\w])(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)?\s*\.?\s*(?:Items|DropDownItems)\.Add\s*\(\s*(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)(?:\s*,[^)]*)?\)\s*;", RegexOptions.Compiled);
    private static readonly Regex ToolStripItemsAddRangeRegex = new(@"(?<![.\w])(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)?\s*\.?\s*(?:Items|DropDownItems)\.AddRange\s*\(\s*new\s+[\w\.\[\],\s]+?\{([\s\S]*?)\}\s*\)\s*;", RegexOptions.Compiled);
    private static readonly Regex ChildReferenceRegex = new(@"(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex EventAssignRegex = new(@"(?<![.\w])(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)\s*\+=\s*(?:new\s+[\w\.\[\],\s<>]+\s*\(\s*)?(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)(?:\s*\))?\s*;", RegexOptions.Compiled);
    private static readonly Regex RootPropertyAssignRegex = new(@"(?<![.\w])(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.+?)\s*;", RegexOptions.Compiled);
    private static readonly Regex PointRegex = new(@"new\s+(?:System\.Drawing\.)?Point\s*\(\s*([-]?\d+)\s*,\s*([-]?\d+)\s*\)", RegexOptions.Compiled);
    private static readonly Regex SizeRegex = new(@"new\s+(?:System\.Drawing\.)?Size\s*\(\s*([-]?\d+)\s*,\s*([-]?\d+)\s*\)", RegexOptions.Compiled);
    private static readonly Regex StringRegex = new("\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Compiled);
    private static readonly Regex NumberRegex = new(@"^-?\d+(\.\d+)?([FDLM])?$", RegexOptions.Compiled);
    private static readonly Regex EnumRegex = new(@"^([\w\.]+)\.([A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.Compiled);
private static readonly Regex ColorSystemRegex = new(@"^System\.Drawing\.SystemColors\.([A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.Compiled);
    private static readonly Regex ColorKnownRegex = new(@"^System\.Drawing\.Color\.([A-Za-z_][A-Za-z0-9_]*)$", RegexOptions.Compiled);
    private static readonly Regex ColorArgbRegex = new(@"^System\.Drawing\.Color\.FromArgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)$", RegexOptions.Compiled);
    private static readonly Regex ColorArgb3Regex = new(@"^System\.Drawing\.Color\.FromArgb\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)$", RegexOptions.Compiled);
private static readonly Regex FieldDeclRegex = new(@"(?:(?:private|protected|internal|public|static|readonly)\s+)*([A-Za-z_][\w\.\[\],\s<>]*?)\s+([A-Za-z_][A-Za-z0-9_]*)\s*;", RegexOptions.Compiled);
    private static readonly Regex SizeFRegex = new(@"new\s+(?:System\.Drawing\.)?SizeF\s*\(\s*(-?\d+(?:\.\d+)?)(?:F)?\s*,\s*(-?\d+(?:\.\d+)?)(?:F)?\s*\)", RegexOptions.Compiled);
    private static readonly Regex FontRegex = new(@"new\s+(?:System\.Drawing\.)?Font\s*\(\s*""((?:[^""\\]|\\.)*)""\s*,\s*(\d+(?:\.\d+)?)(?:F)?\s*,\s*(?:System\.Drawing\.)?FontStyle\.([A-Za-z]+)(?:\s*,\s*(?:System\.Drawing\.)?GraphicsUnit\.([A-Za-z]+))?", RegexOptions.Compiled);

    /// <summary>Parses a .designer.cs file into a <see cref="DesignDocument"/>.</summary>
    public static DesignDocument? Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var designerPath = FindDesignerFile(filePath);
        if (designerPath is null)
        {
            return null;
        }

        var source = File.ReadAllText(designerPath);
        var document = new DesignDocument();

        // Namespace.
        var nsMatch = NamespaceRegex.Match(source);
        if (nsMatch.Success)
        {
            document.Namespace = nsMatch.Groups[1].Value.Trim();
        }

        // Class name.
        var classMatch = ClassRegex.Match(source);
        if (!classMatch.Success)
        {
            return null;
        }

        var className = classMatch.Groups[1].Value;
        document.ClassName = className;
        document.Root.Name = className;

        // Collect declared fields and resolve their control/component types.
        var fieldTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (Match m in FieldDeclRegex.Matches(source))
        {
            var typeName = m.Groups[1].Value.Trim();
            var fieldName = m.Groups[2].Value;
            var type = ResolveType(typeName);
            if (type is not null)
            {
                fieldTypes[fieldName] = type;
            }
        }

        // Collect control instantiations: this.field = new Type();
        var controlTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (Match m in NewControlRegex.Matches(source))
        {
            var field = m.Groups[1].Value;
            var typeName = m.Groups[2].Value.Trim();
            var type = ResolveType(typeName);
            if (type is not null)
            {
                controlTypes[field] = type;
                fieldTypes.TryAdd(field, type);
            }
        }

        // Build nodes for every control.
        var fieldToNode = new Dictionary<string, DesignNode>(StringComparer.Ordinal);

        // Root form node.
        var rootNode = document.Root;

        // Parse property assignments.
        var propertyAssignments = new List<(string Field, string Property, string Value)>();
        foreach (Match m in PropertyAssignRegex.Matches(source))
        {
            propertyAssignments.Add((m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value));
        }

        // Parse property assignments for root form: this.Property = value;
        foreach (Match m in RootPropertyAssignRegex.Matches(source))
        {
            var property = m.Groups[1].Value;
            var value = m.Groups[2].Value;
            // Skip if it looks like a field assignment (this.field = new Type())
            // but allow new System.Drawing.Size/Point/Color for root properties like ClientSize
            if (value.TrimStart().StartsWith("new ") 
                && !value.Contains("System.Drawing.Size")
                && !value.Contains("System.Drawing.Point")
                && !value.Contains("System.Drawing.Color")
                && !value.Contains("System.Drawing.Font"))
            {
                continue;
            }
            // Skip event assignments (already handled separately)
            if (value.Contains("+="))
            {
                continue;
            }
            // Skip Controls.Add / Items.Add calls
            if ((property == "Controls" || property == "Items" || property == "DropDownItems") && value.Contains("Add"))
            {
                continue;
            }
            ApplyRootProperty(document, rootNode, property, value);
        }

        // Create nodes for all controls that have a type.
        foreach (var (field, type) in controlTypes)
        {
            if (field == document.ClassName)
            {
                continue; // "this" refers to the form itself, not a field
            }

            var nodeName = field;
            var node = new DesignNode(type, nodeName);
            fieldToNode[field] = node;
        }

        // Also add nodes for fields that only have a declaration.
        foreach (var (field, type) in fieldTypes)
        {
            if (!fieldToNode.ContainsKey(field) && field != document.ClassName)
            {
                var node = new DesignNode(type, field);
                fieldToNode[field] = node;
            }
        }

        // Apply property assignments to nodes.
        foreach (var (field, property, value) in propertyAssignments)
        {
            if (field == document.ClassName)
            {
                ApplyRootProperty(document, rootNode, property, value);
                continue;
            }

            if (!fieldToNode.TryGetValue(field, out var node))
            {
                continue;
            }

            if ((property == "Controls" || property == "Items" || property == "DropDownItems") && value.Contains("Add"))
            {
                continue;
            }

            ApplyNodeProperty(node, property, value);
        }

        // Handle Name property: the field name is the control name unless overridden.
        foreach (var (field, node) in fieldToNode)
        {
            if (!node.Properties.Contains("Name"))
            {
                node.Properties.SetValue("Name", field);
            }
        }

        // Build hierarchy from Controls.Add calls.
        foreach (Match m in ControlsAddRegex.Matches(source))
        {
            AddChild(m.Groups[1].Value, m.Groups[2].Value);

            if (fieldToNode.TryGetValue(m.Groups[2].Value, out var addedChild))
            {
                if (m.Groups[3].Success && int.TryParse(m.Groups[3].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var column))
                {
                    addedChild.TableColumn = column;
                }

                if (m.Groups[4].Success && int.TryParse(m.Groups[4].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var row))
                {
                    addedChild.TableRow = row;
                }
            }
        }

        foreach (Match m in ControlsAddRangeRegex.Matches(source))
        {
            foreach (Match child in ChildReferenceRegex.Matches(m.Groups[2].Value))
            {
                AddChild(m.Groups[1].Value, child.Groups[1].Value);
            }
        }

        // Build hierarchy from ToolStrip Items.Add / DropDownItems.Add calls.
        foreach (Match m in ToolStripItemsAddRegex.Matches(source))
        {
            AddChild(m.Groups[1].Value, m.Groups[2].Value);
        }

        foreach (Match m in ToolStripItemsAddRangeRegex.Matches(source))
        {
            foreach (Match child in ChildReferenceRegex.Matches(m.Groups[2].Value))
            {
                AddChild(m.Groups[1].Value, child.Groups[1].Value);
            }
        }

        void AddChild(string parentField, string childField)
        {
            DesignNode parent;
            if (string.IsNullOrEmpty(parentField) || parentField == document.ClassName)
            {
                parent = document.Root;
            }
            else if (fieldToNode.TryGetValue(parentField, out var parentNode))
            {
                parent = parentNode;
            }
            else
            {
                return;
            }

            if (!fieldToNode.TryGetValue(childField, out var child))
            {
                return;
            }

            if (child.Parent is not null)
            {
                child.Parent.Children.Remove(child);
            }

            child.Parent = parent;
            parent.Children.Add(child);
        }

        // Parse event assignments.
        foreach (Match m in EventAssignRegex.Matches(source))
        {
            var field = m.Groups[1].Value;
            var eventName = m.Groups[2].Value;
            var handlerName = m.Groups[3].Value;

            DesignNode? node = null;
            if (field == document.ClassName)
            {
                node = document.Root;
            }
            else if (fieldToNode.TryGetValue(field, out var n))
            {
                node = n;
            }

            if (node is not null)
            {
                node.SetEvent(eventName, handlerName);
            }
        }

        // Ensure every node has a Name.
        foreach (var node in document.Root.SelfAndDescendants())
        {
            if (string.IsNullOrEmpty(node.Name))
            {
                node.Name = document.ClassName;
            }
        }

        return document;
    }

    private static string? FindDesignerFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase))
        {
            return filePath;
        }

        // Old/single-file forms: if the selected file itself contains designer code, parse it.
        if (File.Exists(filePath))
        {
            try
            {
                var source = File.ReadAllText(filePath);
                if (LooksLikeDesignerSource(source))
                {
                    return filePath;
                }
            }
            catch
            {
                // Ignore IO errors and fall through to paired designer lookup.
            }
        }

        var baseName = Path.GetFileNameWithoutExtension(filePath);
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var candidates = new[]
        {
            Path.Combine(directory, baseName + ".Designer.cs"),
            Path.Combine(directory, baseName + ".designer.cs"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static bool LooksLikeDesignerSource(string source)
    {
        return source.Contains("InitializeComponent", StringComparison.Ordinal)
            || source.Contains("Controls.Add", StringComparison.Ordinal)
            || source.Contains("Controls.AddRange", StringComparison.Ordinal)
            || source.Contains("new System.Windows.Forms.", StringComparison.Ordinal)
            || source.Contains("new System.ComponentModel.", StringComparison.Ordinal);
    }

    private static void ApplyRootProperty(DesignDocument document, DesignNode root, string property, string value)
    {
        switch (property)
        {
            case "ClientSize":
                if (TryParseSize(value, out var size))
                {
                    root.Properties.SetValue("ClientSize", size);
                }
                break;

            case "Text":
                if (TryParseString(value, out var text))
                {
                    root.Properties.SetValue("Text", text);
                }
                break;

            case "Name":
                if (TryParseString(value, out var name))
                {
                    root.Name = name;
                }
                break;

            case "AutoScaleMode":
                if (TryParseEnum(value, typeof(System.Windows.Forms.AutoScaleMode), out var mode))
                {
                    root.Properties.SetValue("AutoScaleMode", mode);
                }
                break;

            case "Font":
                if (TryParseFont(value, out var font))
                {
                    root.Properties.SetValue("Font", font);
                }
                break;

            case "BackColor":
            case "ForeColor":
                if (TryParseColor(value, out var color))
                {
                    root.Properties.SetValue(property, color);
                }
                break;

            default:
                ApplyGenericProperty(root, property, value);
                break;
        }
    }

    private static void ApplyNodeProperty(DesignNode node, string property, string value)
    {
        if (property == "Name")
        {
            if (TryParseString(value, out var name))
            {
                node.Name = name;
                node.Properties.SetValue("Name", name);
            }
            return;
        }

        if (property == "Location")
        {
            if (TryParsePoint(value, out var point))
            {
                node.Properties.SetValue("Location", point);
            }
            return;
        }

        if (property == "Size")
        {
            if (TryParseSize(value, out var size))
            {
                node.Properties.SetValue("Size", size);
            }
            return;
        }

        if (property == "TabIndex")
        {
            if (int.TryParse(value.TrimEnd('F', 'D', 'L', 'M'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                node.Properties.SetValue("TabIndex", index);
            }
            return;
        }

        ApplyGenericProperty(node, property, value);
    }

    private static void ApplyGenericProperty(DesignNode node, string property, string value)
    {
        var trimmed = value.Trim();
        var propertyType = ResolvePropertyType(node.ControlType, property);

        // String literal.
        if (TryParseString(trimmed, out var text))
        {
            if (propertyType == typeof(string))
            {
                node.Properties.SetValue(property, text);
            }

            return;
        }

        // Boolean.
        if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase))
        {
            node.Properties.SetValue(property, true);
            return;
        }

        if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase))
        {
            node.Properties.SetValue(property, false);
            return;
        }

        // Integer.
        if (int.TryParse(trimmed.TrimEnd('F', 'D', 'L', 'M'), NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
        {
            node.Properties.SetValue(property, intVal);
            return;
        }

        // Double.
        if (double.TryParse(trimmed.TrimEnd('F', 'D', 'L', 'M'), NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleVal))
        {
            node.Properties.SetValue(property, doubleVal);
            return;
        }

        // Point.
        if (TryParsePoint(trimmed, out var point))
        {
            node.Properties.SetValue(property, point);
            return;
        }

        // Size.
        if (TryParseSize(trimmed, out var size))
        {
            node.Properties.SetValue(property, size);
            return;
        }

        if (TryParseSizeF(trimmed, out var sizeF))
        {
            node.Properties.SetValue(property, sizeF);
            return;
        }

        if (TryParseFont(trimmed, out var font))
        {
            node.Properties.SetValue(property, font);
            return;
        }

        // Enum (e.g. System.Windows.Forms.DockStyle.Fill).
        if (propertyType is not null && propertyType.IsEnum)
        {
            if (TryParseEnum(trimmed, propertyType, out var enumVal))
            {
                node.Properties.SetValue(property, enumVal);
                return;
            }
        }

        // Known color.
        if (TryParseColor(trimmed, out var color))
        {
            node.Properties.SetValue(property, color);
            return;
        }

        // Reference to another control: this.otherField.
        var refMatch = Regex.Match(trimmed, @"^(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)$");
        if (refMatch.Success && propertyType is not null && typeof(Control).IsAssignableFrom(propertyType))
        {
            var refField = refMatch.Groups[1].Value;
            node.Properties.SetValue(property, refField);
            return;
        }
    }

    private static Type? ResolveType(string typeName)
    {
        var trimmed = typeName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return null;
        var clean = trimmed.Replace("global::", string.Empty, StringComparison.Ordinal)
            .Replace("[]", string.Empty, StringComparison.Ordinal)
            .Trim();

        var type = Type.GetType(clean);
        if (type is not null)
        {
            return type;
        }

        var shortName = clean.Contains('.') ? clean.Substring(clean.LastIndexOf('.') + 1) : clean;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var candidate = assembly.GetType(clean, throwOnError: false, ignoreCase: false)
                ?? assembly.GetType("System.Windows.Forms." + shortName, throwOnError: false, ignoreCase: false);
            if (candidate is not null)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Type? ResolvePropertyType(Type controlType, string propertyName)
    {
        var entry = PropertyCatalog.Find(controlType, propertyName);
        return entry?.ValueType;
    }

    private static bool TryParseString(string value, out string result)
    {
        result = string.Empty;
        var m = StringRegex.Match(value);
        if (!m.Success)
        {
            return false;
        }

        result = m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
        return true;
    }

    private static bool TryParsePoint(string value, out System.Drawing.Point result)
    {
        result = System.Drawing.Point.Empty;
        var m = PointRegex.Match(value);
        if (!m.Success)
        {
            return false;
        }

        result = new System.Drawing.Point(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                                          int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
        return true;
    }

    private static bool TryParseSize(string value, out System.Drawing.Size result)
    {
        result = System.Drawing.Size.Empty;
        var m = SizeRegex.Match(value);
        if (!m.Success)
        {
            return false;
        }

        result = new System.Drawing.Size(int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                                         int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
        return true;
    }

    private static bool TryParseSizeF(string value, out System.Drawing.SizeF result)
    {
        result = System.Drawing.SizeF.Empty;
        var m = SizeFRegex.Match(value);
        if (!m.Success)
        {
            return false;
        }

        result = new System.Drawing.SizeF(
            float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
            float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
        return true;
    }

    private static bool TryParseEnum(string value, Type enumType, out object result)
    {
        result = null!;
        var m = EnumRegex.Match(value);
        if (!m.Success)
        {
            return false;
        }

        var enumName = m.Groups[2].Value;
        try
        {
            result = Enum.Parse(enumType, enumName, ignoreCase: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseColor(string value, out System.Drawing.Color result)
    {
        result = System.Drawing.Color.Empty;

        var m = ColorSystemRegex.Match(value);
        if (m.Success)
        {
            var property = typeof(System.Drawing.SystemColors).GetProperty(m.Groups[1].Value);
            if (property is not null)
            {
                result = (System.Drawing.Color)property.GetValue(null)!;
                return true;
            }

            return false;
        }

        m = ColorKnownRegex.Match(value);
        if (m.Success)
        {
            try
            {
                result = System.Drawing.Color.FromName(m.Groups[1].Value);
                return !result.IsEmpty;
            }
            catch
            {
                return false;
            }
        }

        m = ColorArgbRegex.Match(value);
        if (m.Success)
        {
            try
            {
                result = System.Drawing.Color.FromArgb(
                    int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture));
                return true;
            }
            catch
            {
                return false;
            }
        }

        m = ColorArgb3Regex.Match(value);
        if (m.Success)
        {
            try
            {
                result = System.Drawing.Color.FromArgb(
                    int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture));
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static bool TryParseFont(string value, out System.Drawing.Font result)
    {
        result = null!;
        var m = FontRegex.Match(value);
        if (!m.Success)
        {
            return false;
        }

        try
        {
            var family = m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
            var size = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            var style = Enum.Parse<System.Drawing.FontStyle>(m.Groups[3].Value, ignoreCase: true);
            var unit = m.Groups[4].Success
                ? Enum.Parse<System.Drawing.GraphicsUnit>(m.Groups[4].Value, ignoreCase: true)
                : System.Drawing.GraphicsUnit.Point;
            result = new System.Drawing.Font(family, size, style, unit);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
