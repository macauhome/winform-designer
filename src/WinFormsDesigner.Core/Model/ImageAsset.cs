namespace WinFormsDesigner.Core.Model;

/// <summary>
/// An image that is used by one or more controls of a document.
/// </summary>
public sealed class ImageAsset
{
    public ImageAsset(string key, byte[] bytes, string? filePath = null)
    {
        Key = key;
        Bytes = bytes;
        FilePath = filePath;
    }

    /// <summary>Stable key used to reference the image from the generated code.</summary>
    public string Key { get; }

    /// <summary>The raw image bytes.</summary>
    public byte[] Bytes { get; }

    /// <summary>Original file the image was loaded from, if any.</summary>
    public string? FilePath { get; }

    /// <summary>Decodes the image. The caller owns the returned instance.</summary>
    public System.Drawing.Image Decode()
    {
        using var stream = new System.IO.MemoryStream(Bytes);
        using var image = System.Drawing.Image.FromStream(stream);
        return new System.Drawing.Bitmap(image);
    }
}
