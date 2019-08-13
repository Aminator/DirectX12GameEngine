using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectX12GameEngine.Shaders
{
    public class ShaderSyntaxRewriter : CSharpSyntaxRewriter
    {
        private readonly int depth;
        private readonly bool isTopLevel;
        private readonly Compilation compilation;
        private readonly ShaderGenerator shaderGenerator;

        public ShaderSyntaxRewriter(Compilation compilation, ShaderGenerator shaderGenerator, bool isTopLevel = false, int depth = 0)
        {
            this.compilation = compilation;
            this.shaderGenerator = shaderGenerator;
            this.isTopLevel = isTopLevel;
            this.depth = depth;
        }

        private SemanticModel GetSemanticModel(SyntaxNode node) => compilation.GetSemanticModel(node.SyntaxTree);

        public override SyntaxNode VisitCastExpression(CastExpressionSyntax node)
        {
            node = (CastExpressionSyntax)base.VisitCastExpression(node);
            return node.ReplaceType(node.Type);
        }

        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            node = (LocalDeclarationStatementSyntax)base.VisitLocalDeclarationStatement(node);
            return node.ReplaceType(node.Declaration.Type);
        }

        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            node = (ObjectCreationExpressionSyntax)base.VisitObjectCreationExpression(node);
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
            node = (DefaultExpressionSyntax)base.VisitDefaultExpression(node);
            node = node.ReplaceType(node.Type);
            return SyntaxFactory.CastExpression(node.Type, SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
        }

        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            MemberAccessExpressionSyntax newBaseNode = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node);

            SyntaxNode? newNode = null;

            if (node.Expression is BaseExpressionSyntax)
            {
                if (isTopLevel)
                {
                    newNode = SyntaxFactory.IdentifierName($"Base_{depth + 1}_{node.Name}");
                }
                else
                {
                    SymbolInfo memberSymbolInfo = GetSemanticModel(node.Name).GetSymbolInfo(node.Name);
                    ISymbol? memberSymbol = memberSymbolInfo.Symbol ?? memberSymbolInfo.CandidateSymbols.FirstOrDefault();

                    if (memberSymbol != null)
                    {
                        newNode = SyntaxFactory.IdentifierName($"{memberSymbol.ContainingType.Name}::{node.Name}");
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
            else
            {
                if (node.TryGetMappedMemberName(GetSemanticModel(node), out ISymbol? containingSymbol, out ISymbol? memberSymbol, out string? mappedName))
                {
                    if (memberSymbol is null || !memberSymbol.IsStatic)
                    {
                        newNode = SyntaxFactory.IdentifierName($"{newBaseNode.Expression}{mappedName}");
                    }
                    else
                    {
                        newNode = SyntaxFactory.IdentifierName(mappedName);
                    }
                }
                else
                {
                    if ((containingSymbol != null && containingSymbol.IsStatic) || (memberSymbol != null && memberSymbol.IsStatic))
                    {
                        newNode = SyntaxFactory.IdentifierName($"{newBaseNode.Expression}::{newBaseNode.Name}");
                    }
                }
            }

            return newNode?.WithTriviaFrom(newBaseNode) ?? newBaseNode;
        }
    }
}
