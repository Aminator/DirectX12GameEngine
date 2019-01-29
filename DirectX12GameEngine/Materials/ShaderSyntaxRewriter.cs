using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectX12GameEngine
{
    public class ShaderMethodDeclarationRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            return node.ReplaceType(node.ReturnType).WithModifiers(default);
        }
    }

    public class ShaderSyntaxRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitParameterList(ParameterListSyntax node)
        {
            return node.ReplaceNodes(node.Parameters, (oldNode, newNode) => newNode = (ParameterSyntax)Visit(oldNode));
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            return node.ReplaceType(node.Type);
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            ExpressionSyntax expression = node.Declaration.Variables.First().Initializer.Value;
            node = node.ReplaceNode(expression, Visit(expression));

            return node.ReplaceType(node.Declaration.Type);
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            return node.ReplaceNode(node.Expression, Visit(node.Expression));
        }

        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            node = node.ReplaceType(node.Type);

            if (node.ArgumentList.Arguments.Count == 0)
            {
                return SyntaxFactory.CastExpression(node.Type, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
            }
            else
            {
                return SyntaxFactory.InvocationExpression(node.Type, node.ArgumentList);
            }
        }

        public override SyntaxNode VisitDefaultExpression(DefaultExpressionSyntax node)
        {
            node = node.ReplaceType(node.Type);
            return SyntaxFactory.CastExpression(node.Type, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
        }

        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            return node.ReplaceMethod();
        }

        public override SyntaxNode VisitAttribute(AttributeSyntax node)
        {
            return node.ReplaceType(node.Name);
        }
    }
}
