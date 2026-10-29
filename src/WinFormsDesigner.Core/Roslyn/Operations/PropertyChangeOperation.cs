namespace WinFormsDesigner.Core.Roslyn.Operations;

public sealed class PropertyChangeOperation : IOperation
{
    public string Description => $"Change {ControlName}.{PropertyName}";
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string ControlName { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public Microsoft.CodeAnalysis.SyntaxNode OldValueSyntax { get; init; } = default!;
    public Microsoft.CodeAnalysis.SyntaxNode NewValueSyntax { get; init; } = default!;
}