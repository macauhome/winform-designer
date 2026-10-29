namespace WinFormsDesigner.Core.Roslyn.Operations;

public interface IOperation
{
    string Description { get; }
    DateTime Timestamp { get; }
}