using System.Text.RegularExpressions;

namespace WinFormsDesigner.App;

public static partial class DesignerFileLoader
{
    private static bool TryReplaceInitializeComponent(string source, string inner, out string result)
    {
        result = source;
        var nameIndex = source.IndexOf("void InitializeComponent", StringComparison.Ordinal);
        if (nameIndex < 0)
        {
            nameIndex = source.IndexOf("InitializeComponent", StringComparison.Ordinal);
        }
        if (nameIndex < 0)
        {
            return false;
        }

        var lineStart = source.LastIndexOf('\n', nameIndex);
        lineStart = lineStart < 0 ? 0 : lineStart + 1;

        while (lineStart > 0)
        {
            var previousLineEnd = source.LastIndexOf('\n', lineStart - 2);
            var previousLineStart = previousLineEnd < 0 ? 0 : previousLineEnd + 1;
            var previousLine = source.Substring(previousLineStart, lineStart - previousLineStart).Trim();
            if (previousLine.StartsWith("[", StringComparison.Ordinal))
            {
                lineStart = previousLineStart;
            }
            else
            {
                break;
            }
        }

        var openBrace = FindOpeningBrace(source, nameIndex);
        if (openBrace < 0)
        {
            return false;
        }

        var depth = 0;
        var closeBrace = -1;
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
                    break;
                }
            }
        }

        if (closeBrace < 0)
        {
            return false;
        }

        result = source.Substring(0, lineStart)
            + inner
            + source.Substring(closeBrace + 1);
        return true;
    }

    private static int FindOpeningBrace(string source, int startIndex)
    {
        for (var i = startIndex; i < source.Length; i++)
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
                return i;
            }
        }

        return -1;
    }
}
