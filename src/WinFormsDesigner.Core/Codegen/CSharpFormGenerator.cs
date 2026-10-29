using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Text;
using WinFormsDesigner.Core.Model;
using WinFormsDesigner.Core.Reflection;

namespace WinFormsDesigner.Core.Codegen;

/// <summary>
/// The result of a code generation run.
/// </summary>
public sealed class GeneratedForm
{
    /// <summary>Name of the generated form class.</summary>
    public string ClassName { get; init; } = "Form1";

    /// <summary>Namespace of the generated code.</summary>
    public string Namespace { get; init; } = "MyApp";

    /// <summary>Generated files, keyed by file name.</summary>
    public IReadOnlyDictionary<string, string> Files { get; init; } = new Dictionary<string, string>();

    /// <summary>Names of the controls that were generated.</summary>
    public IReadOnlyList<string> ControlNames { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Generates Visual Studio compatible C# source for a <see cref="DesignDocument"/>.
/// The output consists of a code behind file and a matching <c>.designer.cs</c> file that
/// compiles inside an ordinary WinForms project.
/// </summary>
public sealed class CSharpFormGenerator
{
    private static readonly string[] AlwaysWrittenProperties = ["Name", "TabIndex", "Location", "Size"];

    /// <summary>Generates the source files for the given document.</summary>
    public GeneratedForm Generate(DesignDocument document, string? originalSource = null)
    {
        var nodes = document.Root.SelfAndDescendants().ToList();
        var fieldNames = BuildFieldNames(nodes);
        var tabIndices = ComputeTabIndices(document.Root);

        var images = new List<(string Key, byte[] Bytes)>();
        var imageKeys = new Dictionary<string, string>(StringComparer.Ordinal);

        var designer = new StringBuilder();
        var codeBehind = new StringBuilder();

        WriteDesignerFile(designer, document, nodes, fieldNames, tabIndices, images, imageKeys);
        WriteCodeBehindFile(codeBehind, document, nodes, fieldNames, tabIndices);

        var generatedDesigner = designer.ToString();
        if (!string.IsNullOrWhiteSpace(originalSource))
        {
            generatedDesigner = PatchDesignerSource(originalSource!, document);
        }

        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [document.ClassName + ".cs"] = codeBehind.ToString(),
            [document.ClassName + ".designer.cs"] = generatedDesigner,
        };

        if (images.Count > 0)
        {
            files["EmbeddedResources.cs"] = WriteEmbeddedResources(document.Namespace, images);
        }

        return new GeneratedForm
        {
            ClassName = document.ClassName,
            Namespace = document.Namespace,
            Files = files,
            ControlNames = nodes.Where(n => n.Parent is not null).Select(n => fieldNames[n]).ToArray(),
        };
    }

    /// <summary>
    /// Generates only the statements that belong inside <c>InitializeComponent()</c>
    /// (control instantiation, property assignment and layout) for the given document.
    /// </summary>
    public static string GenerateInitializeComponentBody(DesignDocument document)
    {
        var full = new CSharpFormGenerator().Generate(document).Files[document.ClassName + ".designer.cs"];
        if (TryGetInitializeComponentSpan(full, out var open, out var close))
        {
            return full.Substring(open + 1, close - open - 1).Trim('\r', '\n').Trim();
        }

        return string.Empty;
    }
    public static string PatchDesignerSource(string originalSource, DesignDocument document)
    {
        if (string.IsNullOrWhiteSpace(originalSource))
        {
            return new CSharpFormGenerator().Generate(document).Files[document.ClassName + ".designer.cs"];
        }

        var newBody = GenerateInitializeComponentBody(document);
        if (string.IsNullOrWhiteSpace(newBody))
        {
            return originalSource;
        }

        newBody = Environment.NewLine + newBody.Trim() + Environment.NewLine;

        if (!TryGetInitializeComponentSpan(originalSource, out var originalOpen, out var originalClose))
        {
            return originalSource;
        }

        return originalSource.Substring(0, originalOpen + 1)
            + newBody
            + originalSource.Substring(originalClose);
    }

    /// <summary>
    /// Generates the source files for the given document, loading and patching the original
    /// designer file from disk when it already exists.
    /// </summary>
    public GeneratedForm GenerateFromFile(DesignDocument document, string originalFilePath)
    {
        string? originalSource = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(originalFilePath) && File.Exists(originalFilePath))
            {
                originalSource = File.ReadAllText(originalFilePath);
            }
        }
        catch
        {
            originalSource = null;
        }

        return Generate(document, originalSource);
    }

    /// <summary>
    /// Locates the opening and closing braces of the <c>InitializeComponent</c> method,
    /// skipping strings and comments. Returns false when the method cannot be found.
    /// </summary>
    private static bool TryGetInitializeComponentSpan(string source, out int openBrace, out int closeBrace)
    {
        openBrace = -1;
        closeBrace = -1;

        var nameIndex = source.IndexOf("InitializeComponent", StringComparison.Ordinal);
        if (nameIndex < 0)
        {
            return false;
        }

        var length = source.Length;
        for (var i = nameIndex; i < length; i++)
        {
            var c = source[i];
            if (c == '"')
            {
                i++;
                while (i < length)
                {
                    if (source[i] == '\\')
                    {
                        i += 2;
                    }
                    else if (source[i] == '"')
                    {
                        i++;
                        break;
                    }
                    else
                    {
                        i++;
                    }
                }
                i--;
            }
            else if (c == '/' && i + 1 < length && source[i + 1] == '/')
            {
                while (i < length && source[i] != '\n')
                {
                    i++;
                }
            }
            else if (c == '/' && i + 1 < length && source[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < length && !(source[i] == '*' && source[i + 1] == '/'))
                {
                    i++;
                }
                i += 2;
            }
            else if (c == '{')
            {
                openBrace = i;
                break;
            }
        }

        if (openBrace < 0)
        {
            return false;
        }

        var depth = 0;
        for (var i = openBrace; i < length; i++)
        {
            var c = source[i];
            if (c == '"')
            {
                i++;
                while (i < length)
                {
                    if (source[i] == '\\')
                    {
                        i += 2;
                    }
                    else if (source[i] == '"')
                    {
                        i++;
                        break;
                    }
                    else
                    {
                        i++;
                    }
                }
                i--;
            }
            else if (c == '/' && i + 1 < length && source[i + 1] == '/')
            {
                while (i < length && source[i] != '\n')
                {
                    i++;
                }
            }
            else if (c == '/' && i + 1 < length && source[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < length && !(source[i] == '*' && source[i + 1] == '/'))
                {
                    i++;
                }
                i += 2;
            }
            else if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    closeBrace = i;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Generates a complete, runnable WinForms project around the designed form.
    /// </summary>
    public GeneratedForm GenerateProject(DesignDocument document)
    {
        var generated = Generate(document);
        var files = new Dictionary<string, string>(generated.Files, StringComparer.OrdinalIgnoreCase);

        files[document.ProjectName + ".csproj"] = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <OutputType>WinExe</OutputType>
                <TargetFramework>net10.0-windows</TargetFramework>
                <UseWindowsForms>true</UseWindowsForms>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <RootNamespace>{document.Namespace}</RootNamespace>
                <AssemblyName>{document.ProjectName}</AssemblyName>
              </PropertyGroup>

            </Project>
            """;

        files["Program.cs"] = $$"""
            namespace {{document.Namespace}};

            internal static class Program
            {
                [STAThread]
                private static void Main()
                {
                    ApplicationConfiguration.Initialize();
                    Application.Run(new {{document.ClassName}}());
                }
            }
            """;

        return new GeneratedForm
        {
            ClassName = generated.ClassName,
            Namespace = generated.Namespace,
            Files = files,
            ControlNames = generated.ControlNames,
        };
    }

    private static Dictionary<DesignNode, string> BuildFieldNames(List<DesignNode> nodes)
    {
        var result = new Dictionary<DesignNode, string>();
        var used = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in nodes)
        {
            var name = CSharpValueFormatter.ToIdentifier(node.Name);
            if (!used.Add(name))
            {
                var index = 2;
                string candidate;
                do
                {
                    candidate = name + index.ToString(CultureInfo.InvariantCulture);
                    index++;
                }
                while (!used.Add(candidate));

                name = candidate;
            }

            result[node] = name;
        }

        return result;
    }

    /// <summary>
    /// Computes the effective tab index of every child. Controls that were never given an
    /// explicit tab index follow the Visual Studio convention of numbering them in the order
    /// in which they were created, which is the reverse of the z-order.
    /// </summary>
    private static Dictionary<DesignNode, int> ComputeTabIndices(DesignNode root)
    {
        var result = new Dictionary<DesignNode, int>();
        var parents = root.SelfAndDescendants().ToList();

        foreach (var parent in parents)
        {
            var children = parent.Children;
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child.Properties.TryGetValue("TabIndex", out var raw) && raw is int explicitIndex)
                {
                    result[child] = explicitIndex;
                }
                else
                {
                    result[child] = children.Count - 1 - i;
                }
            }
        }

        return result;
    }

    private void WriteDesignerFile(
        StringBuilder sb,
        DesignDocument document,
        List<DesignNode> nodes,
        Dictionary<DesignNode, string> fieldNames,
        Dictionary<DesignNode, int> tabIndices,
        List<(string Key, byte[] Bytes)> images,
        Dictionary<string, string> imageKeys)
    {
        sb.AppendLine("namespace " + document.Namespace);
        sb.AppendLine("{");
        sb.AppendLine("    partial class " + document.ClassName);
        sb.AppendLine("    {");
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Required method for Designer support - do not modify");
        sb.AppendLine("        /// using code generator.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        private void InitializeComponent()");
        sb.AppendLine("        {");

        var hasComponents = nodes.Any(n => n.Parent is not null && IsContainerComponent(n.ControlType));

        // The component container is created first so that components can be handed to it.
        if (hasComponents)
        {
            sb.AppendLine("            this.components = new System.ComponentModel.Container();");
        }

        // Instantiate every control, parents before children. The form itself is "this"
        // and therefore does not need a field.
        foreach (var node in nodes.Where(n => n.Parent is not null))
        {
            var field = fieldNames[node];
            var typeName = TypeName(node.ControlType);

            if (IsContainerComponent(node.ControlType) && HasContainerConstructor(node.ControlType))
            {
                sb.AppendLine($"            this.{field} = new {typeName}(this.components);");
            }
            else
            {
                sb.AppendLine($"            this.{field} = new {typeName}();");
            }
        }

        // Suspend layout for every container that hosts children. The root form is suspended
        // separately because it is addressed through "this" rather than a field.
        var layoutContainers = nodes.Where(n => n.Parent is not null && n.Children.Count > 0 && typeof(System.Windows.Forms.Control).IsAssignableFrom(n.ControlType)).ToList();
        sb.AppendLine("            this.SuspendLayout();");
        foreach (var container in layoutContainers)
        {
            sb.AppendLine($"            this.{fieldNames[container]}.SuspendLayout();");
        }

        // One block per control, deepest containers first so the code reads top down.
        foreach (var node in nodes.Where(n => n.Parent is not null))
        {
            WriteControlBlock(sb, node, document, fieldNames, tabIndices, images, imageKeys);
        }

        WriteFormBlock(sb, document, nodes, fieldNames, tabIndices, images, imageKeys);

        // Resume layout in the reverse order of the suspend calls.
        for (var i = layoutContainers.Count - 1; i >= 0; i--)
        {
            sb.AppendLine($"            this.{fieldNames[layoutContainers[i]]}.ResumeLayout(false);");
        }

        sb.AppendLine("            this.ResumeLayout(false);");
        sb.AppendLine("            this.PerformLayout();");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Field declarations.
        if (hasComponents)
        {
            sb.AppendLine("        private System.ComponentModel.IContainer components = null;");
        }

        foreach (var node in nodes.Where(n => n.Parent is not null))
        {
            sb.AppendLine($"        private {TypeName(node.ControlType)} {fieldNames[node]};");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    private void WriteControlBlock(
        StringBuilder sb,
        DesignNode node,
        DesignDocument document,
        Dictionary<DesignNode, string> fieldNames,
        Dictionary<DesignNode, int> tabIndices,
        List<(string Key, byte[] Bytes)> images,
        Dictionary<string, string> imageKeys)
    {
        var field = fieldNames[node];
        var lines = new List<(string SortKey, string Code)>();
        var isStripItem = IsToolStripItem(node.ControlType);

        // Child controls are added first, exactly like the Visual Studio designer does.
        foreach (var child in node.Children)
        {
            if (!IsAddedToControls(child.ControlType))
            {
                // Components and strip items are attached differently.
                continue;
            }

            if (typeof(System.Windows.Forms.TableLayoutPanel).IsAssignableFrom(node.ControlType)
                && (child.TableColumn.HasValue || child.TableRow.HasValue))
            {
                lines.Add(("Controls", $"            this.{field}.Controls.Add(this.{fieldNames[child]}, {child.TableColumn ?? 0}, {child.TableRow ?? 0});"));
            }
            else
            {
                lines.Add(("Controls", $"            this.{field}.Controls.Add(this.{fieldNames[child]});"));
            }
        }

        var stripItems = node.Children.Where(c => IsToolStripItem(c.ControlType)).ToList();
        if (stripItems.Count > 0)
        {
            var collectionName = typeof(System.Windows.Forms.ToolStripDropDownItem).IsAssignableFrom(node.ControlType)
                ? "DropDownItems"
                : "Items";
            lines.Add(("Controls", $"            this.{field}.{collectionName}.AddRange(new System.Windows.Forms.ToolStripItem[] {{"));
            foreach (var child in stripItems)
            {
                lines.Add(("Controls", $"                this.{fieldNames[child]},"));
            }

            lines.Add(("Controls", "            });"));
        }

        // Components that cannot be handed to the container in their constructor are
        // registered with it explicitly.
        if (IsContainerComponent(node.ControlType) && !HasContainerConstructor(node.ControlType))
        {
            lines.Add(("Components", $"            this.components.Add(this.{field});"));
        }

        foreach (var property in node.Properties.Values.Keys)
        {
            if (AlwaysWrittenProperties.Contains(property, StringComparer.Ordinal)
                || property is "Items" or "Columns")
            {
                continue;
            }

            if (!node.Properties.TryGetValue(property, out var value))
            {
                continue;
            }

            if (!IsWritableProperty(node.ControlType, property))
            {
                // Read only properties such as the size of a tool strip item cannot be assigned.
                continue;
            }

            var code = FormatPropertyValue(document, node, field, property, value, fieldNames, images, imageKeys);
            if (code is null)
            {
                continue;
            }

            if (IsDefaultValue(node.ControlType, property, value))
            {
                continue;
            }

            lines.Add((property, $"            this.{field}.{property} = {code};"));
        }

        WriteCollectionBlocks(node, field, lines);

        // Location, Name, Size and TabIndex are always written, matching the designer.
        // Tool strip items have neither a location nor a tab index, and components have
        // no geometry at all.
        var isControl = typeof(System.Windows.Forms.Control).IsAssignableFrom(node.ControlType);
        if (isControl && !isStripItem)
        {
            var location = GetPoint(node, "Location") ?? System.Drawing.Point.Empty;
            var size = GetSize(node, "Size") ?? GetDefaultSize(node.ControlType);

            lines.Add(("Location", $"            this.{field}.Location = new System.Drawing.Point({location.X.ToString(CultureInfo.InvariantCulture)}, {location.Y.ToString(CultureInfo.InvariantCulture)});"));
            lines.Add(("Size", $"            this.{field}.Size = new System.Drawing.Size({size.Width.ToString(CultureInfo.InvariantCulture)}, {size.Height.ToString(CultureInfo.InvariantCulture)});"));
            lines.Add(("TabIndex", $"            this.{field}.TabIndex = {tabIndices[node].ToString(CultureInfo.InvariantCulture)};"));
        }

        if (IsWritableProperty(node.ControlType, "Name"))
        {
            lines.Add(("Name", $"            this.{field}.Name = {CSharpValueFormatter.Quote(node.Name)};"));
        }

        // Container specific helper: the designer disables tab stops on plain containers.
        if (node.Children.Count == 0 && IsContainer(node.ControlType) && !node.Properties.Contains("TabStop"))
        {
            lines.Add(("TabStop", $"            this.{field}.TabStop = false;"));
        }

        // Event handlers.
        foreach (var (eventName, handlerName) in node.Events.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            var descriptor = DescribeEvent(node.ControlType, eventName);
            if (descriptor is null)
            {
                continue;
            }

            lines.Add(($"zzEvent{eventName}", $"            this.{field}.{eventName} += new {descriptor.DelegateType}(this.{handlerName});"));
        }

        if (lines.Count == 0)
        {
            return;
        }

        sb.AppendLine("            // ");
        sb.AppendLine($"            // {node.Name}");
        sb.AppendLine("            // ");

        foreach (var (_, code) in lines.OrderBy(l => l.SortKey, StringComparer.Ordinal))
        {
            sb.AppendLine(code);
        }
    }

    /// <summary>
    /// Writes the child collection contents of a control, e.g. the items of a list box
    /// or the columns of a list view.
    /// </summary>
    private static void WriteCollectionBlocks(
        DesignNode node,
        string field,
        List<(string SortKey, string Code)> lines)
    {
        if (node.Properties.TryGetValue("Items", out var items) && items is System.Collections.IEnumerable itemList)
        {
            var values = ToStringList(itemList);
            if (values.Count > 0)
            {
                lines.Add(("Items", $"            this.{field}.Items.AddRange(new string[] {{"));
                foreach (var value in values)
                {
                    lines.Add(("Items", $"                {CSharpValueFormatter.Quote(value)},"));
                }

                lines.Add(("Items", "            });"));
            }
        }

        if (node.Properties.TryGetValue("Columns", out var columns) && columns is System.Collections.IEnumerable columnList)
        {
            var values = ToStringList(columnList);
            if (values.Count > 0)
            {
                lines.Add(("Columns", $"            this.{field}.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {{"));
                foreach (var value in values)
                {
                    lines.Add(("Columns", $"                new System.Windows.Forms.ColumnHeader({CSharpValueFormatter.Quote(value)}),"));
                }

                lines.Add(("Columns", "            });"));
            }
        }
    }

    private static List<string> ToStringList(System.Collections.IEnumerable source)
    {
        var result = new List<string>();
        foreach (var item in source)
        {
            if (item is not null)
            {
                result.Add(item.ToString() ?? string.Empty);
            }
        }

        return result;
    }

    /// <summary>
    /// Determines whether a property points at another control of the same document.
    /// </summary>
    private static bool IsReferenceProperty(Type type)
        => type != typeof(string)
            && (typeof(System.Windows.Forms.Control).IsAssignableFrom(type)
                || typeof(System.Windows.Forms.IButtonControl).IsAssignableFrom(type));

    private void WriteFormBlock(
        StringBuilder sb,
        DesignDocument document,
        List<DesignNode> nodes,
        Dictionary<DesignNode, string> fieldNames,
        Dictionary<DesignNode, int> tabIndices,
        List<(string Key, byte[] Bytes)> images,
        Dictionary<string, string> imageKeys)
    {
        var root = document.Root;
        var lines = new List<(string SortKey, string Code)>();

        foreach (var property in root.Properties.Values.Keys)
        {
            if (AlwaysWrittenProperties.Contains(property, StringComparer.Ordinal)
                || property is "Items" or "Columns" or "AutoScaleDimensions" or "AutoScaleMode"
                || property is "ClientSize" or "Text")
            {
                // These are written explicitly further down.
                continue;
            }

            if (!root.Properties.TryGetValue(property, out var value))
            {
                continue;
            }

            var code = FormatPropertyValue(document, root, document.ClassName, property, value, fieldNames, images, imageKeys);
            if (code is null || IsDefaultValue(root.ControlType, property, value))
            {
                continue;
            }

            lines.Add((property, $"            this.{property} = {code};"));
        }

        var font = GetFont(root);
        var autoScaleMode = GetAutoScaleMode(root);
        if (autoScaleMode == System.Windows.Forms.AutoScaleMode.Font)
        {
            var dimensions = ComputeAutoScaleDimensions(font);
            lines.Add(("AutoScaleDimensions", $"            this.AutoScaleDimensions = new System.Drawing.SizeF({dimensions.Width.ToString("R", CultureInfo.InvariantCulture)}F, {dimensions.Height.ToString("R", CultureInfo.InvariantCulture)}F);"));
        }

        if (autoScaleMode != System.Windows.Forms.AutoScaleMode.None)
        {
            lines.Add(("AutoScaleMode", $"            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.{autoScaleMode};"));
        }

        var clientSize = GetSize(root, "ClientSize") ?? new System.Drawing.Size(800, 450);
        lines.Add(("ClientSize", $"            this.ClientSize = new System.Drawing.Size({clientSize.Width.ToString(CultureInfo.InvariantCulture)}, {clientSize.Height.ToString(CultureInfo.InvariantCulture)});"));

        foreach (var child in root.Children)
        {
            if (!IsAddedToControls(child.ControlType))
            {
                // Components live in the component container, not in the control collection.
                continue;
            }

            lines.Add(("Controls", $"            this.Controls.Add(this.{fieldNames[child]});"));
        }

        lines.Add(("Name", $"            this.Name = {CSharpValueFormatter.Quote(document.ClassName)};"));

        var text = root.Properties.TryGetValue("Text", out var caption) && caption is string captionText
            ? captionText
            : document.ClassName;
        lines.Add(("Text", $"            this.Text = {CSharpValueFormatter.Quote(text)};"));

        foreach (var (eventName, handlerName) in root.Events.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            var descriptor = DescribeEvent(root.ControlType, eventName);
            if (descriptor is null)
            {
                continue;
            }

            lines.Add(($"zzEvent{eventName}", $"            this.{eventName} += new {descriptor.DelegateType}(this.{handlerName});"));
        }

        sb.AppendLine("            // ");
        sb.AppendLine($"            // {document.ClassName}");
        sb.AppendLine("            // ");

        foreach (var (_, code) in lines.OrderBy(l => l.SortKey, StringComparer.Ordinal))
        {
            sb.AppendLine(code);
        }
    }

    private void WriteCodeBehindFile(
        StringBuilder sb,
        DesignDocument document,
        List<DesignNode> nodes,
        Dictionary<DesignNode, string> fieldNames,
        Dictionary<DesignNode, int> tabIndices)
    {
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Windows.Forms;");
        sb.AppendLine();
        sb.AppendLine("namespace " + document.Namespace);
        sb.AppendLine("{");
        sb.AppendLine("    public partial class " + document.ClassName + " : Form");
        sb.AppendLine("    {");
        sb.AppendLine("        public " + document.ClassName + "()");
        sb.AppendLine("        {");
        sb.AppendLine("            InitializeComponent();");
        sb.AppendLine("        }");

        if (nodes.Any(n => n.Parent is not null && IsContainerComponent(n.ControlType)))
        {
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Releases the resources owned by the form.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        protected override void Dispose(bool disposing)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (disposing && (components != null))");
            sb.AppendLine("            {");
            sb.AppendLine("                components.Dispose();");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            base.Dispose(disposing);");
            sb.AppendLine("        }");
        }

        var handlers = new List<string>();

        foreach (var node in nodes)
        {
            foreach (var (eventName, handlerName) in node.Events.OrderBy(e => e.Key, StringComparer.Ordinal))
            {
                var descriptor = DescribeEvent(node.ControlType, eventName);
                if (descriptor is null)
                {
                    continue;
                }

                handlers.Add($"        private void {handlerName}({descriptor.Parameters})");
                handlers.Add("        {");
                handlers.Add("        }");
                handlers.Add(string.Empty);
            }
        }

        if (handlers.Count > 0)
        {
            sb.AppendLine();
            foreach (var handler in handlers)
            {
                sb.AppendLine(handler);
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    private string WriteEmbeddedResources(string ns, List<(string Key, byte[] Bytes)> images)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace " + ns);
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Provides the images used by the generated form.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    internal static class EmbeddedResources");
        sb.AppendLine("    {");
        sb.AppendLine("        private static readonly System.Collections.Generic.Dictionary<string, byte[]> Data = new()");
        sb.AppendLine("        {");
        foreach (var (key, bytes) in images)
        {
            sb.AppendLine($"            [\"{key}\"] = System.Convert.FromBase64String(\"{System.Convert.ToBase64String(bytes)}\"),");
        }

        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        /// <summary>Returns the image registered under the given key.</summary>");
        sb.AppendLine("        public static System.Drawing.Image? GetImage(string key)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!Data.TryGetValue(key, out var bytes))");
        sb.AppendLine("            {");
        sb.AppendLine("                return null;");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            using var stream = new System.IO.MemoryStream(bytes);");
        sb.AppendLine("            using var image = System.Drawing.Image.FromStream(stream);");
        sb.AppendLine("            return new System.Drawing.Bitmap(image);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private string? FormatPropertyValue(
        DesignDocument document,
        DesignNode node,
        string field,
        string property,
        object? value,
        Dictionary<DesignNode, string> fieldNames,
        List<(string Key, byte[] Bytes)> images,
        Dictionary<string, string> imageKeys)
    {
        if (value is string referenceName)
        {
            var entry = PropertyCatalog.Find(node.ControlType, property);
            if (entry is not null && IsReferenceProperty(entry.ValueType))
            {
                var target = document.FindNode(referenceName);
                if (target is not null && fieldNames.TryGetValue(target, out var targetField))
                {
                    return $"this.{targetField}";
                }
            }
        }

        if (value is ImageAsset asset)
        {
            var key = asset.Key;
            if (!imageKeys.ContainsKey(key))
            {
                imageKeys[key] = key;
                images.Add((key, asset.Bytes));
            }

            return $"global::{document.Namespace}.EmbeddedResources.GetImage(\"{key}\")";
        }

        if (value is System.Drawing.Image image)
        {
            using var buffer = new System.IO.MemoryStream();
            image.Save(buffer, System.Drawing.Imaging.ImageFormat.Png);
            var key = field + "_" + property;
            if (!imageKeys.ContainsKey(key))
            {
                imageKeys[key] = key;
                images.Add((key, buffer.ToArray()));
            }

            return $"global::{document.Namespace}.EmbeddedResources.GetImage(\"{key}\")";
        }

        return CSharpValueFormatter.Format(value);
    }

    private static System.Drawing.Point? GetPoint(DesignNode node, string property)
        => node.Properties.TryGetValue(property, out var value) ? value as System.Drawing.Point? : null;

    private static System.Drawing.Size? GetSize(DesignNode node, string property)
        => node.Properties.TryGetValue(property, out var value) ? value as System.Drawing.Size? : null;

    private static System.Drawing.Size GetDefaultSize(Type type)
    {
        try
        {
            if (ControlFactory.GetDefault(type) is System.Windows.Forms.Control instance)
            {
                return instance.Size;
            }

            return new System.Drawing.Size(100, 25);
        }
        catch (Exception)
        {
            return new System.Drawing.Size(100, 25);
        }
    }

    private static System.Drawing.Font? GetFont(DesignNode node)
        => node.Properties.TryGetValue("Font", out var value) ? value as System.Drawing.Font : null;

    private static System.Windows.Forms.AutoScaleMode GetAutoScaleMode(DesignNode node)
    {
        if (node.Properties.TryGetValue("AutoScaleMode", out var value) && value is System.Windows.Forms.AutoScaleMode mode)
        {
            return mode;
        }

        return System.Windows.Forms.AutoScaleMode.Font;
    }

    /// <summary>
    /// Computes the auto scale dimensions the way the Windows Forms runtime does, so the
    /// generated value matches what the Visual Studio designer produces.
    /// </summary>
    public static System.Drawing.SizeF ComputeAutoScaleDimensions(System.Drawing.Font? font)
    {
        try
        {
            using var form = new System.Windows.Forms.Form
            {
                AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font,
            };

            if (font is not null)
            {
                form.Font = font;
            }

            form.PerformAutoScale();
            return form.CurrentAutoScaleDimensions;
        }
        catch (Exception)
        {
            return new System.Drawing.SizeF(7F, 15F);
        }
    }

    private static bool IsDefaultValue(Type controlType, string property, object? value)
    {
        try
        {
            var entry = PropertyCatalog.Find(controlType, property);
            if (entry is null)
            {
                return false;
            }

            var defaultValue = entry.GetValue(ControlFactory.GetDefault(controlType));
            return Equals(defaultValue, value);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool IsContainer(Type type)
        => typeof(System.Windows.Forms.ContainerControl).IsAssignableFrom(type)
            || type == typeof(System.Windows.Forms.Panel)
            || type == typeof(System.Windows.Forms.GroupBox)
            || type == typeof(System.Windows.Forms.TabPage);

    /// <summary>Determines whether the type is an item of a tool strip.</summary>
    private static bool IsToolStripItem(Type type)
        => typeof(System.Windows.Forms.ToolStripItem).IsAssignableFrom(type);

    /// <summary>Determines whether the type is a context menu strip.</summary>
    private static bool IsContextMenuStrip(Type type)
        => typeof(System.Windows.Forms.ContextMenuStrip).IsAssignableFrom(type);

    /// <summary>
    /// Determines whether the type belongs into the component container of the form instead
    /// of the control collection of its parent.
    /// </summary>
    private static bool IsContainerComponent(Type type)
        => !IsToolStripItem(type)
            && (IsContextMenuStrip(type)
                || (!typeof(System.Windows.Forms.Control).IsAssignableFrom(type)
                    && typeof(System.ComponentModel.Component).IsAssignableFrom(type)));

    /// <summary>Determines whether the control is added to the control collection of its parent.</summary>
    private static bool IsAddedToControls(Type type)
        => typeof(System.Windows.Forms.Control).IsAssignableFrom(type)
            && !IsContextMenuStrip(type)
            && !IsToolStripItem(type);

    /// <summary>Determines whether the type accepts a component container in its constructor.</summary>
    private static bool HasContainerConstructor(Type type)
        => type.GetConstructor(new[] { typeof(System.ComponentModel.IContainer) }) is not null;

    /// <summary>Determines whether a property can be assigned in the generated code.</summary>
    private static bool IsWritableProperty(Type type, string property)
    {
        var entry = PropertyCatalog.Find(type, property);
        if (entry is not null)
        {
            return entry.Info.CanWrite;
        }

        var info = type.GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
        return info is not null && info.CanWrite;
    }

    /// <summary>Describes the delegate of an event so the generated code matches its signature.</summary>
    private static EventDescriptor? DescribeEvent(Type controlType, string eventName)
    {
        var eventInfo = controlType.GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);
        if (eventInfo?.EventHandlerType is null)
        {
            return null;
        }

        var invoke = eventInfo.EventHandlerType.GetMethod("Invoke");
        if (invoke is null)
        {
            return null;
        }

        var parameters = invoke.GetParameters();
        var parameterList = string.Join(", ", parameters.Select(p => $"{ParameterTypeName(p.ParameterType)} {p.Name}"));
        var delegateType = TypeName(eventInfo.EventHandlerType);

        return new EventDescriptor(delegateType, parameterList);
    }

    private sealed record EventDescriptor(string DelegateType, string Parameters);

    /// <summary>
    /// Renders a parameter type using the short name when the namespace is already imported
    /// by the generated code behind file.
    /// </summary>
    private static string ParameterTypeName(Type type)
    {
        if (type == typeof(object))
        {
            return "object";
        }

        if (type == typeof(string))
        {
            return "string";
        }

        if (type == typeof(int))
        {
            return "int";
        }

        if (type == typeof(bool))
        {
            return "bool";
        }

        if (type == typeof(double))
        {
            return "double";
        }

        var ns = type.Namespace;
        if (ns is "System" or "System.Windows.Forms" or "System.Drawing")
        {
            return type.Name;
        }

        return TypeName(type);
    }

    private static string TypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var name = type.FullName ?? type.Name;
            var tick = name.IndexOf('`');
            if (tick > 0)
            {
                name = name[..tick];
            }

            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
        }

        return type.FullName ?? type.Name;
    }
}
