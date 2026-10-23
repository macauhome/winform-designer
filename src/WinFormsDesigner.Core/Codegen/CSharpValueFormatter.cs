using System.Globalization;
using System.Text;

namespace WinFormsDesigner.Core.Codegen;

/// <summary>
/// Renders runtime values as C# source literals, using the same conventions the Visual Studio
/// designer uses so the generated code looks native.
/// </summary>
public static class CSharpValueFormatter
{
    /// <summary>
    /// Formats a value as a C# expression. Returns <c>null</c> when the value has no
    /// representable literal and the property should be skipped instead.
    /// </summary>
    public static string? Format(object? value, Type? declaredType = null)
    {
        switch (value)
        {
            case null:
                return "null";

            case string s:
                return Quote(s);

            case char c:
                return $"'{Escape(c.ToString(), quote: false)}'";

            case bool b:
                return b ? "true" : "false";

            case byte or sbyte or short or ushort or int or uint or long or ulong:
                return Convert.ToString(value, CultureInfo.InvariantCulture)!;

            case float f:
                return f.ToString("R", CultureInfo.InvariantCulture) + "F";

            case double d:
                return d.ToString("R", CultureInfo.InvariantCulture) + "D";

            case decimal m:
                return m.ToString(CultureInfo.InvariantCulture) + "M";

            case System.Drawing.Color color:
                return FormatColor(color);

            case System.Drawing.Font font:
                return FormatFont(font);

            case System.Drawing.Point point:
                return $"new System.Drawing.Point({point.X.ToString(CultureInfo.InvariantCulture)}, {point.Y.ToString(CultureInfo.InvariantCulture)})";

            case System.Drawing.Size size:
                return $"new System.Drawing.Size({size.Width.ToString(CultureInfo.InvariantCulture)}, {size.Height.ToString(CultureInfo.InvariantCulture)})";

            case System.Drawing.SizeF sizeF:
                return $"new System.Drawing.SizeF({sizeF.Width.ToString("R", CultureInfo.InvariantCulture)}F, {sizeF.Height.ToString("R", CultureInfo.InvariantCulture)}F)";

            case System.Drawing.PointF pointF:
                return $"new System.Drawing.PointF({pointF.X.ToString("R", CultureInfo.InvariantCulture)}F, {pointF.Y.ToString("R", CultureInfo.InvariantCulture)}F)";

            case System.Drawing.Rectangle rect:
                return $"new System.Drawing.Rectangle({rect.X.ToString(CultureInfo.InvariantCulture)}, {rect.Y.ToString(CultureInfo.InvariantCulture)}, {rect.Width.ToString(CultureInfo.InvariantCulture)}, {rect.Height.ToString(CultureInfo.InvariantCulture)})";

            case System.Windows.Forms.Padding padding:
                return $"new System.Windows.Forms.Padding({padding.Left.ToString(CultureInfo.InvariantCulture)}, {padding.Top.ToString(CultureInfo.InvariantCulture)}, {padding.Right.ToString(CultureInfo.InvariantCulture)}, {padding.Bottom.ToString(CultureInfo.InvariantCulture)})";

            case System.Windows.Forms.Cursor:
                // Cursors cannot be expressed as a literal; the designer keeps the default.
                return null;

            case Enum e:
                return FormatEnum(e);
        }

        if (value is System.Collections.IEnumerable && value is not string)
        {
            return null;
        }

        var target = declaredType ?? value.GetType();
        if (target.IsEnum)
        {
            return FormatEnum((Enum)value);
        }

        return null;
    }

    /// <summary>Formats an enumeration value, expanding flag combinations.</summary>
    public static string FormatEnum(Enum value)
    {
        var type = value.GetType();
        var typeName = type.FullName ?? type.Name;

        if (!type.IsDefined(typeof(System.FlagsAttribute), inherit: false))
        {
            return $"{typeName}.{value}";
        }

        var names = new List<string>();
        foreach (var candidate in Enum.GetValues(type))
        {
            var flag = (Enum)candidate;
            if (flag.GetHashCode() == 0)
            {
                continue;
            }

            if (value.HasFlag(flag))
            {
                names.Add($"{typeName}.{flag}");
            }
        }

        if (names.Count == 0)
        {
            return $"{typeName}.{value}";
        }

        return string.Join(" | ", names);
    }

    private static string FormatColor(System.Drawing.Color color)
    {
        if (color.IsEmpty)
        {
            return "System.Drawing.Color.Empty";
        }

        if (color.A == 255 && color.IsSystemColor)
        {
            return $"System.Drawing.SystemColors.{color.Name}";
        }

        if (color.A == 255 && color.IsKnownColor)
        {
            return $"System.Drawing.Color.{color.Name}";
        }

        return $"System.Drawing.Color.FromArgb({color.A.ToString(CultureInfo.InvariantCulture)}, {color.R.ToString(CultureInfo.InvariantCulture)}, {color.G.ToString(CultureInfo.InvariantCulture)}, {color.B.ToString(CultureInfo.InvariantCulture)})";
    }

    private static string FormatFont(System.Drawing.Font font)
    {
        var family = font.FontFamily.Name.Replace("\"", "\\\"");
        var size = font.Size.ToString("R", CultureInfo.InvariantCulture) + "F";
        var style = FormatEnum(font.Style);
        var unit = "System.Drawing.GraphicsUnit." + font.Unit;
        return $"new System.Drawing.Font(\"{family}\", {size}, {style}, {unit}, ((System.Byte)({font.GdiCharSet.ToString(CultureInfo.InvariantCulture)})))";
    }

    /// <summary>Quotes and escapes a string literal.</summary>
    public static string Quote(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        builder.Append(Escape(value, quote: true));
        builder.Append('"');
        return builder.ToString();
    }

    private static string Escape(string value, bool quote)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"' when quote:
                    builder.Append("\\\"");
                    break;
                case '\'':
                    builder.Append("\\'");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\0':
                    builder.Append("\\0");
                    break;
                default:
                    if (char.IsControl(c))
                    {
                        builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>Turns an arbitrary identifier into a valid C# identifier.</summary>
    public static string ToIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "control";
        }

        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            var valid = char.IsLetter(c) || c == '_' || (i > 0 && char.IsDigit(c));
            builder.Append(valid ? c : '_');
        }

        var result = builder.ToString();
        if (char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        return result;
    }
}
