using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectX12GameEngine.Shaders
{
    public static class SyntaxNodeExtensions
    {
        public static TRoot ReplaceType<TRoot>(this TRoot node, TypeSyntax type, TypeInfo typeInfo) where TRoot : SyntaxNode
        {
            TypeSyntax mappedType = type.GetMappedType(typeInfo);

            return mappedType.ToString() == type.ToString() ? node : node.ReplaceNode(type, type.GetMappedType(typeInfo).WithTriviaFrom(type));
        }

        public static TypeSyntax GetMappedType(this TypeSyntax type, TypeInfo typeInfo)
        {
            TypeSyntax mappedType = typeInfo.GetMappedType();

            return mappedType.ToString() == type.ToString() ? type : mappedType.WithTriviaFrom(type);
        }

        public static TypeSyntax GetMappedType(this TypeInfo typeInfo)
        {
            if (typeInfo.Type is null) throw new InvalidOperationException();

            string fullTypeName = typeInfo.Type.ToString();
            string value = HlslKnownTypes.GetMappedName(fullTypeName);

            return SyntaxFactory.ParseTypeName(value);
        }

        public static bool TryGetMappedMemberName(this MemberAccessExpressionSyntax node, SymbolInfo memberSymbolInfo, out ISymbol memberSymbol, out string? mappedName)
        {
            memberSymbol = memberSymbolInfo.Symbol ?? memberSymbolInfo.CandidateSymbols.FirstOrDefault() ?? throw new InvalidOperationException();
            INamedTypeSymbol? containingTypeSymbol = memberSymbol.ContainingType;

            mappedName = null;

            if (containingTypeSymbol != null)
            {
                string fullMemberName = containingTypeSymbol.ToString() + Type.Delimiter + node.Name.Identifier.ValueText;

                if (HlslKnownMethods.ContainsKey(fullMemberName))
                {
                    mappedName = HlslKnownMethods.GetMappedName(fullMemberName);
                    return true;
                }
            }

            return false;
        }
    }
}
