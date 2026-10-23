namespace WinFormsDesigner.Core.Model;

/// <summary>
/// Formats colours for display in the property grid.
/// </summary>
public static class ColorText
{
    /// <summary>
    /// Formats a colour the way the Visual Studio property grid does. A named colour keeps
    /// its name, every other colour is shown as its components. The name of an unnamed
    /// colour is the hexadecimal value, which is not something a user wants to read.
    /// </summary>
    public static string Format(System.Drawing.Color color)
    {
        if (color.IsEmpty)
        {
            return "(System)";
        }

        if (color.IsSystemColor)
        {
            return color.Name;
        }

        if (color.IsKnownColor)
        {
            return color.Name;
        }

        return color.A == 255
            ? $"{color.R}, {color.G}, {color.B}"
            : $"{color.A}, {color.R}, {color.G}, {color.B}";
    }
}
