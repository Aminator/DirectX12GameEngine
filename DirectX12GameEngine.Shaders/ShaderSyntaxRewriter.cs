using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectX12GameEngine.Shaders
{
    public class ShaderSyntaxRewriter : CSharpSyntaxRewriter
    {
        private readonly Compilation compilation;

        public ShaderSyntaxRewriter(Compilation compilation)
        {
            this.compilation = compilation;
        }

        private SemanticModel GetSemanticModel(SyntaxNode node) => compilation.GetSemanticModel(node.SyntaxTree);

        public override SyntaxNode? VisitCastExpression(CastExpressionSyntax node)
        {
            CastExpressionSyntax newNode = (CastExpressionSyntax)base.VisitCastExpression(node)!;
            return newNode.ReplaceType(newNode.Type, GetSemanticModel(node).GetTypeInfo(node.Type));
        }

        public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            LocalDeclarationStatementSyntax newNode = (LocalDeclarationStatementSyntax)base.VisitLocalDeclarationStatement(node)!;
            return newNode.ReplaceType(newNode.Declaration.Type, GetSemanticModel(node).GetTypeInfo(node.Declaration.Type));
        }

        public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            ObjectCreationExpressionSyntax newNode = (ObjectCreationExpressionSyntax)base.VisitObjectCreationExpression(node)!;
            TypeSyntax mappedType = newNode.Type.GetMappedType(GetSemanticModel(node).GetTypeInfo(node));

            if (newNode.ArgumentList!.Arguments.Count == 0)
            {
                return SyntaxFactory.CastExpression(mappedType, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
            }
            else
            {
                return SyntaxFactory.InvocationExpression(mappedType, newNode.ArgumentList);
            }
        }

        public override SyntaxNode? VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (node.IsKind(SyntaxKind.DefaultLiteralExpression))
            {
                TypeSyntax mappedType = GetSemanticModel(node).GetTypeInfo(node).GetMappedType();

                CastExpressionSyntax castExpressionSyntax = SyntaxFactory.CastExpression(mappedType, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
                return SyntaxFactory.ParenthesizedExpression(castExpressionSyntax);
            }

            return base.VisitLiteralExpression(node);
        }

        public override SyntaxNode? VisitDefaultExpression(DefaultExpressionSyntax node)
        {
            DefaultExpressionSyntax newNode = (DefaultExpressionSyntax)base.VisitDefaultExpression(node)!;
            TypeSyntax mappedType = newNode.Type.GetMappedType(GetSemanticModel(node).GetTypeInfo(node));

            CastExpressionSyntax castExpressionSyntax = SyntaxFactory.CastExpression(mappedType, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
            return SyntaxFactory.ParenthesizedExpression(castExpressionSyntax);
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            MemberAccessExpressionSyntax newBaseNode = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node)!;

            SyntaxNode newNode;

            if (node.TryGetMappedMemberName(GetSemanticModel(node).GetSymbolInfo(node.Name), out ISymbol memberSymbol, out string? mappedName))
            {
                if (memberSymbol.IsStatic)
                {
                    newNode = SyntaxFactory.IdentifierName(mappedName);
                }
                else
                {
                    newNode = SyntaxFactory.IdentifierName($"{newBaseNode.Expression}{mappedName}");
                }
            }
            else
            {
                if (node.Expression is BaseExpressionSyntax || memberSymbol.IsStatic && memberSymbol.ContainingType != null)
                {
                    newNode = SyntaxFactory.IdentifierName($"{memberSymbol.ContainingType.ToString().Replace(".", "::")}::{node.Name}");
                }
                else
                {
                    return newBaseNode;
                }
            }

            return newNode.WithTriviaFrom(newBaseNode);
        }
    }
}
