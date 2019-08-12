using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectX12GameEngine.Shaders
{
    public static class SyntaxNodeExtensions
    {
        public static TRoot ReplaceType<TRoot>(this TRoot node, TypeSyntax type) where TRoot : SyntaxNode
        {
            string value = ShaderGenerator.HlslKnownTypes.GetMappedName(type.ToString());

            if (value == type.ToString())
            {
                return node;
            }
            else
            {
                return node.ReplaceNode(type, SyntaxFactory.ParseTypeName(value).WithTriviaFrom(type));
            }
        }

        public static SyntaxNode ReplaceMember(this MemberAccessExpressionSyntax node, SemanticModel semanticModel)
        {
            SymbolInfo containingMemberSymbolInfo = semanticModel.GetSymbolInfo(node.Expression);
            SymbolInfo memberSymbolInfo = semanticModel.GetSymbolInfo(node.Name);

            ISymbol? memberSymbol = memberSymbolInfo.Symbol ?? memberSymbolInfo.CandidateSymbols.FirstOrDefault();

            if (memberSymbol is null)
            {
                ISymbol? containingMemberSymbol = containingMemberSymbolInfo.Symbol ?? containingMemberSymbolInfo.CandidateSymbols.FirstOrDefault();

                ITypeSymbol? typeSymbol = containingMemberSymbol switch
                {
                    IFieldSymbol fieldSymbol => fieldSymbol.Type,
                    IPropertySymbol propertySymbol => propertySymbol.Type,
                    IMethodSymbol methodSymbol => methodSymbol.ContainingType,
                    _ => null
                };

                if (typeSymbol != null)
                {
                    string memberName = typeSymbol.ToString() + Type.Delimiter + node.Name.Identifier.ValueText;

                    if (ShaderGenerator.HlslKnownMethods.ContainsKey(memberName))
                    {
                        string mappedName = containingMemberSymbol.Name + ShaderGenerator.HlslKnownMethods.GetMappedName(memberName);
                        return SyntaxFactory.IdentifierName(mappedName).WithTriviaFrom(node);
                    }
                }
            }

            if (memberSymbol is null || containingMemberSymbolInfo.Symbol is null || (memberSymbol.Kind != SymbolKind.Field && memberSymbol.Kind != SymbolKind.Property && memberSymbol.Kind != SymbolKind.Method))
            {
                return node;
            }

            string? value = ShaderGenerator.HlslKnownMethods.GetMappedName(containingMemberSymbolInfo.Symbol, memberSymbol);

            if (value is null)
            {
                return node;
            }
            else
            {
                return SyntaxFactory.IdentifierName(value).WithTriviaFrom(node);
            }
        }
    }
}
