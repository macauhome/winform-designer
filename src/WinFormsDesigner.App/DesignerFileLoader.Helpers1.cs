using System.Text;
using System.Text.RegularExpressions;
using WinFormsDesigner.Core.Model;

namespace WinFormsDesigner.App;

public static partial class DesignerFileLoader
{
    private static bool TryReplaceGeneratedRegion(string source, string inner, out string result)
    {
        result = source;
        var match = Regex.Match(source, @"#\s*region\b[^\r\n]*", RegexOptions.IgnoreCase);
        while (match.Success)
        {
            if (match.Value.IndexOf("generated", StringComparison.OrdinalIgnoreCase) >= 0
                || match.Value.IndexOf("designer", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var endMarker = source.IndexOf("#endregion", match.Index + match.Length, StringComparison.Ordinal);
                if (endMarker >= 0)
                {
                    var before = source.Substring(0, match.Index + match.Length);
                    var after = source.Substring(endMarker);
                    result = before + Environment.NewLine + inner + after;
                    return true;
                }
            }

            match = match.NextMatch();
        }

        return false;
    }

    private static string RemoveDesignerFields(string source)
    {
        var lines = source.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            var match = FieldDeclRegex.Match(trimmed);
            if (match.Success && match.Value.Trim() == trimmed)
            {
                var typeName = match.Groups[1].Value.Trim();
                var type = ResolveType(typeName);
                if (type is not null
                    && (typeof(System.Windows.Forms.Control).IsAssignableFrom(type)
                        || typeof(System.ComponentModel.Component).IsAssignableFrom(type)))
                {
                    continue;
                }
            }

            if (trimmed.Contains("components", StringComparison.Ordinal)
                && (trimmed.Contains("IContainer", StringComparison.Ordinal)
                    || trimmed.Contains("Container", StringComparison.Ordinal))
                && trimmed.EndsWith(";", StringComparison.Ordinal))
            {
                continue;
            }

            builder.AppendLine(line);
        }

        return builder.ToString();
    }

    private static string InsertBeforeLastClassBrace(string source, string inner)
    {
        var lastBrace = source.LastIndexOf('}');
        if (lastBrace < 0)
        {
            return source + Environment.NewLine + inner;
        }

        return source.Substring(0, lastBrace) + inner + source.Substring(lastBrace);
    }
}
