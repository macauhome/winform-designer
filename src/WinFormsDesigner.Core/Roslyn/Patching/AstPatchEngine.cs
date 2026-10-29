using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WinFormsDesigner.Core.Roslyn.Operations;

namespace WinFormsDesigner.Core.Roslyn.Patching;

public static class AstPatchEngine
{
    public static SyntaxNode PatchProperty(SyntaxNode root, PropertyChangeOperation operation)
    {
        var assignment = root.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .FirstOrDefault(a =>
                a.Left is MemberAccessExpressionSyntax m &&
                m.Expression is IdentifierNameSyntax id &&
                id.Identifier.Text == operation.ControlName &&
                m.Name.Identifier.Text == operation.PropertyName);

        if (assignment is null)
        {
            return root;
        }

        return root.ReplaceNode(assignment.Right, operation.NewValueSyntax);
    }
}