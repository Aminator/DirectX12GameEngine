using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DirectX12GameEngine.Shaders
{
    public class ShaderSyntaxCollector : CSharpSyntaxWalker
    {
        private readonly SemanticModel semanticModel;
        private readonly ShaderGenerator shaderGenerator;

        public ShaderSyntaxCollector(ShaderGenerator shaderGenerator, SemanticModel semanticModel)
        {
            this.shaderGenerator = shaderGenerator;
            this.semanticModel = semanticModel;
        }

        public override void VisitParameter(ParameterSyntax node)
        {
            base.VisitParameter(node);

            TypeInfo typeInfo = semanticModel.GetTypeInfo(node.Type);

            string fullTypeName = typeInfo.Type.ToDisplayString(
                new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces))
                + ", " + typeInfo.Type.ContainingAssembly.Identity.ToString();

            shaderGenerator.AddType(Type.GetType(fullTypeName));
        }

        public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            base.VisitMemberAccessExpression(node);

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
                        string fullTypeName = typeSymbol.ToDisplayString(
                            new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces))
                            + ", " + typeSymbol.ContainingAssembly.Identity.ToString();

                        shaderGenerator.AddType(Type.GetType(fullTypeName));
                        return;
                    }
                }
            }

            if (memberSymbol is null || containingMemberSymbolInfo.Symbol is null || (memberSymbol.Kind != SymbolKind.Field && memberSymbol.Kind != SymbolKind.Property && memberSymbol.Kind != SymbolKind.Method))
            {
                return;
            }

            if (!ShaderGenerator.HlslKnownMethods.Contains(containingMemberSymbolInfo.Symbol, memberSymbol))
            {
                ITypeSymbol? symbol = containingMemberSymbolInfo.Symbol.IsStatic ? containingMemberSymbolInfo.Symbol as ITypeSymbol : memberSymbol.ContainingType;

                if (symbol is null) return;

                string fullTypeName = symbol.ToDisplayString(
                    new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces))
                    + ", " + symbol.ContainingAssembly.Identity.ToString();

                shaderGenerator.AddType(Type.GetType(fullTypeName));
            }
        }
    }
}
