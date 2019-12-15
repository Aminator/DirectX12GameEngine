using System;
using System.Linq;
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
            node = (CastExpressionSyntax)base.VisitCastExpression(node)!;
            return node.ReplaceType(node.Type);
        }

        public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            node = (LocalDeclarationStatementSyntax)base.VisitLocalDeclarationStatement(node)!;
            return node.ReplaceType(node.Declaration.Type);
        }

        public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            node = (ObjectCreationExpressionSyntax)base.VisitObjectCreationExpression(node)!;
            node = node.ReplaceType(node.Type);

            if (node.ArgumentList!.Arguments.Count == 0)
            {
                return SyntaxFactory.CastExpression(node.Type, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
            }
            else
            {
                return SyntaxFactory.InvocationExpression(node.Type, node.ArgumentList);
            }
        }

        public override SyntaxNode? VisitDefaultExpression(DefaultExpressionSyntax node)
        {
            node = (DefaultExpressionSyntax)base.VisitDefaultExpression(node)!;
            node = node.ReplaceType(node.Type);

            CastExpressionSyntax castExpressionSyntax = SyntaxFactory.CastExpression(node.Type, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
            return SyntaxFactory.ParenthesizedExpression(castExpressionSyntax);
        }

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            MemberAccessExpressionSyntax newBaseNode = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node)!;

            SyntaxNode? newNode = null;

            if (node.Expression is BaseExpressionSyntax)
            {
                SymbolInfo memberSymbolInfo = GetSemanticModel(node.Name).GetSymbolInfo(node.Name);
                ISymbol memberSymbol = memberSymbolInfo.Symbol ?? memberSymbolInfo.CandidateSymbols.FirstOrDefault() ?? throw new InvalidOperationException();

                newNode = SyntaxFactory.IdentifierName($"{memberSymbol.ContainingType.ToString().Replace(".", "::")}::{node.Name}");
            }
            else
            {
                if (node.TryGetMappedMemberName(GetSemanticModel(node), out ISymbol? memberSymbol, out string? mappedName))
                {
                    if (memberSymbol!.IsStatic)
                    {
                        newNode = SyntaxFactory.IdentifierName(mappedName);
                    }
                    else
                    {
                        newNode = SyntaxFactory.IdentifierName($"{newBaseNode.Expression}{mappedName}");
                    }
                }
                else if (memberSymbol != null)
                {
                    if (memberSymbol.IsStatic || memberSymbol.ContainingSymbol.IsStatic)
                    {
                        newNode = SyntaxFactory.IdentifierName($"{newBaseNode.Expression}::{newBaseNode.Name}");
                    }
                }
            }

            return newNode?.WithTriviaFrom(newBaseNode) ?? newBaseNode;
        }
    }
}
