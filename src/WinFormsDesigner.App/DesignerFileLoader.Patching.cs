using System.Text;
using WinFormsDesigner.Core.Model;

namespace WinFormsDesigner.App;

public static partial class DesignerFileLoader
{
    public static string GenerateDesignerCode(DesignDocument document)
    {
        var generated = new Core.Codegen.CSharpFormGenerator().Generate(document);
        var key = document.ClassName + ".designer.cs";
        return generated.Files.TryGetValue(key, out var code) ? code : string.Empty;
    }

    public static string PatchSource(string originalSource, DesignDocument document)
    {
        if (string.IsNullOrEmpty(originalSource))
        {
            return GenerateDesignerCode(document);
        }

        var generatedDesigner = GenerateDesignerCode(document);
        var inner = ExtractDesignerInner(generatedDesigner);

        var originalDocument = TryLoadSource(originalSource);
        if (originalDocument is not null)
        {
            var originalGenerated = GenerateDesignerCode(originalDocument);
            if (string.Equals(originalGenerated, generatedDesigner, StringComparison.Ordinal))
            {
                return originalSource;
            }

            inner = MergeCustomLines(originalSource, inner, originalGenerated);
        }

        if (TryReplaceGeneratedRegion(originalSource, inner, out var regionResult))
        {
            return regionResult;
        }

        var withoutFields = RemoveDesignerFields(originalSource);
        if (TryReplaceInitializeComponent(withoutFields, inner, out var methodResult))
        {
            return methodResult;
        }

        return InsertBeforeLastClassBrace(withoutFields, inner);
    }

    public static string UpdateSource(string originalSource, DesignDocument document) => PatchSource(originalSource, document);

    public static string ApplyChanges(string originalSource, DesignDocument document) => PatchSource(originalSource, document);

    public static bool TryPatchFile(string filePath, DesignDocument document, out string? error)
    {
        error = null;
        try
        {
            if (!File.Exists(filePath))
            {
                error = "The source file does not exist.";
                return false;
            }

            var original = File.ReadAllText(filePath);
            var patched = PatchSource(original, document);
            File.WriteAllText(filePath, patched, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool Save(string filePath, DesignDocument document, out string? error) => TryPatchFile(filePath, document, out error);

    private static string ExtractDesignerInner(string designerSource)
    {
        var classBrace = designerSource.IndexOf("partial class", StringComparison.Ordinal);
        if (classBrace < 0)
        {
            classBrace = designerSource.IndexOf("class ", StringComparison.Ordinal);
        }

        var openBrace = designerSource.IndexOf('{', classBrace < 0 ? 0 : classBrace);
        if (openBrace < 0)
        {
            return designerSource;
        }

        var depth = 0;
        for (var i = openBrace; i < designerSource.Length; i++)
        {
            var c = designerSource[i];
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return designerSource.Substring(openBrace + 1, i - openBrace - 1).Trim('\r', '\n') + Environment.NewLine;
                }
            }
        }

        return designerSource;
    }
}
