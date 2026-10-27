using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using WinFormsDesigner.Core.Model;

namespace WinFormsDesigner.App;

public static partial class DesignerFileLoader
{
    private static DesignDocument? TryLoadSource(string source)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");
            File.WriteAllText(tempPath, source);
            var document = Load(tempPath);
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
            return document;
        }
        catch
        {
            return null;
        }
    }

    private static string MergeCustomLines(string originalSource, string newInner, string originalGenerated)
    {
        if (!TryGetInitializeComponentSpan(originalSource, out var origOpen, out var origClose))
        {
            return newInner;
        }

        if (!TryGetInitializeComponentSpan(originalGenerated, out var genOrigOpen, out var genOrigClose))
        {
            return newInner;
        }

        if (!TryGetInitializeComponentSpan(newInner, out var genNewOpen, out var genNewClose))
        {
            return newInner;
        }

        var originalBodyLines = originalSource.Substring(origOpen + 1, origClose - origOpen - 1)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .ToList();
        var generatedOriginalBodyLines = originalGenerated.Substring(genOrigOpen + 1, genOrigClose - genOrigOpen - 1)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .ToList();
        var newBodyLines = newInner.Substring(genNewOpen + 1, genNewClose - genNewOpen - 1)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .ToList();

        var generatedOriginalKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in generatedOriginalBodyLines)
        {
            generatedOriginalKeys.Add(GetStatementKey(line));
        }

        var customByAnchor = new Dictionary<string, List<string>>();
        string lastGeneratedKey = "\u0000";
        foreach (var line in originalBodyLines)
        {
            var key = GetStatementKey(line);
            if (generatedOriginalKeys.Contains(key))
            {
                lastGeneratedKey = key;
                continue;
            }

            if (!customByAnchor.TryGetValue(lastGeneratedKey, out var list))
            {
                list = new List<string>();
                customByAnchor[lastGeneratedKey] = list;
            }

            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                list.Add(string.Empty);
            }
            else
            {
                list.Add(new string(' ', 12) + trimmed);
            }
        }

        var result = new List<string>();
        if (customByAnchor.TryGetValue("\u0000", out var nullCustoms))
        {
            result.AddRange(nullCustoms);
            customByAnchor.Remove("\u0000");
        }

        foreach (var line in newBodyLines)
        {
            result.Add(line);
            var key = GetStatementKey(line);
            if (customByAnchor.TryGetValue(key, out var customs))
            {
                result.AddRange(customs);
                customByAnchor.Remove(key);
            }
        }

        foreach (var customs in customByAnchor.Values)
        {
            result.AddRange(customs);
        }

        var mergedBody = string.Join(Environment.NewLine, result);
        if (!mergedBody.EndsWith(Environment.NewLine, StringComparison.Ordinal))
        {
            mergedBody += Environment.NewLine;
        }

        return newInner.Substring(0, genNewOpen + 1) + Environment.NewLine + mergedBody + newInner.Substring(genNewClose);
    }

    private static bool TryGetInitializeComponentSpan(string source, out int openBrace, out int closeBrace)
    {
        openBrace = -1;
        closeBrace = -1;

        var nameIndex = source.IndexOf("void InitializeComponent", StringComparison.Ordinal);
        if (nameIndex < 0)
        {
            nameIndex = source.IndexOf("InitializeComponent", StringComparison.Ordinal);
        }
        if (nameIndex < 0)
        {
            return false;
        }

        openBrace = FindOpeningBrace(source, nameIndex);
        if (openBrace < 0)
        {
            return false;
        }

        var depth = 0;
        for (var i = openBrace; i < source.Length; i++)
        {
            var c = source[i];
            if (c == '"')
            {
                i++;
                while (i < source.Length)
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
            else if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
            {
                while (i < source.Length && source[i] != '\n')
                {
                    i++;
                }
            }
            else if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < source.Length && !(source[i] == '*' && source[i + 1] == '/'))
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

    private static string GetStatementKey(string line)
    {
        var t = line.Trim();
        if (t.Length == 0)
        {
            return "BLANK";
        }

        if (t.StartsWith("//", StringComparison.Ordinal) ||
            t.StartsWith("/*", StringComparison.Ordinal) ||
            t.StartsWith("*", StringComparison.Ordinal))
        {
            return "CUSTOM:" + t;
        }

        var m = Regex.Match(t, @"(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)\s*=\s*new\b");
        if (m.Success)
        {
            return "NEW:" + m.Groups[1].Value;
        }

        m = Regex.Match(t, @"(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)\.([A-Za-z_][A-Za-z0-9_]*)\s*=");
        if (m.Success)
        {
            return "PROP:" + m.Groups[1].Value + "." + m.Groups[2].Value;
        }

        m = Regex.Match(t, @"Controls\.Add\(\s*(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)");
        if (m.Success)
        {
            return "ADD:" + m.Groups[1].Value;
        }

        m = Regex.Match(t, @"(?:Items|DropDownItems)\.Add\(\s*(?:this\.)?([A-Za-z_][A-Za-z0-9_]*)");
        if (m.Success)
        {
            return "ADDITEM:" + m.Groups[1].Value;
        }

        if (t.Contains("SuspendLayout", StringComparison.Ordinal))
        {
            return "KW:SuspendLayout";
        }
        if (t.Contains("ResumeLayout", StringComparison.Ordinal))
        {
            return "KW:ResumeLayout";
        }
        if (t.Contains("PerformLayout", StringComparison.Ordinal))
        {
            return "KW:PerformLayout";
        }
        if (t.Contains("components", StringComparison.Ordinal))
        {
            return "KW:components";
        }

        return "LINE:" + t;
    }
}
