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
        private readonly SemanticModel semanticModel;
        private readonly ShaderGenerator shaderGenerator;

        public ShaderSyntaxRewriter(ShaderGenerator shaderGenerator, SemanticModel semanticModel, bool isTopLevel = false, int depth = 0)
        {
            this.shaderGenerator = shaderGenerator;
            this.semanticModel = semanticModel;
            this.isTopLevel = isTopLevel;
            this.depth = depth;
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node);

            if (isTopLevel && depth > 0)
            {
                node = node.ReplaceToken(node.Identifier, SyntaxFactory.Identifier($"Base_{depth}_{node.Identifier.ValueText}"));
            }

            SyntaxTriviaList trivia = node.Modifiers.FirstOrDefault().LeadingTrivia;

            SyntaxTokenList modifiers = new SyntaxTokenList();

            if (node.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                modifiers = modifiers.Add(SyntaxFactory.Token(default, SyntaxKind.StaticKeyword, SyntaxFactory.TriviaList(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " "))));
            }

            if (modifiers.Count == 0)
            {
                node = node.WithReturnType(node.ReturnType.WithLeadingTrivia(trivia));
            }

            node = node.ReplaceType(node.ReturnType).WithModifiers(modifiers);

            return node;
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            string? attributeName = node.AttributeLists.FirstOrDefault()?.Attributes.FirstOrDefault()?.Name.ToString();

            node = (ParameterSyntax)base.VisitParameter(node);
            node = node.WithAttributeLists(default);
            node = node.ReplaceType(node.Type);

            if (attributeName != null)
            {
                node = node.ReplaceToken(node.Identifier, SyntaxFactory.Identifier($"{node.Identifier.ValueText} : {ShaderGenerator.HlslKnownSemantics.GetMappedName(attributeName + "Attribute")}"));
            }

            return node;
        }

        public override SyntaxNode VisitAttribute(AttributeSyntax node)
        {
            node = (AttributeSyntax)base.VisitAttribute(node);
            return node.ReplaceType(node.Name);
        }

        public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
        {
            var knownAttributes = node.Attributes.Where(n => ShaderGenerator.HlslKnownAttributes.ContainsKey(n.Name + "Attribute"));

            if (knownAttributes.Count() == 0) return null;

            SeparatedSyntaxList<AttributeSyntax> list = new SeparatedSyntaxList<AttributeSyntax>();

            foreach (AttributeSyntax attribute in knownAttributes)
            {
                list = list.Add(attribute.WithName(SyntaxFactory.ParseName(ShaderGenerator.HlslKnownAttributes.GetMappedName(attribute.Name + "Attribute"))));
            }

            node = node.WithAttributes(list);

            return base.VisitAttributeList(node);
        }

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
                    SymbolInfo memberSymbolInfo = semanticModel.GetSymbolInfo(node.Name);
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
                if (node.TryGetMappedMemberName(semanticModel, out ISymbol? containingSymbol, out ISymbol? memberSymbol, out string? mappedName))
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
