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

            if (!node.TryGetMappedMemberName(semanticModel, out ISymbol? containingSymbol, out _, out _))
            {
                if (!(containingSymbol is ITypeSymbol)) return;

                string fullTypeName = containingSymbol.ToDisplayString(
                    new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces))
                    + ", " + containingSymbol.ContainingAssembly.Identity.ToString();

                Type? type = Type.GetType(fullTypeName);

                if (type != null)
                {
                    shaderGenerator.AddType(type);
                }
            }
        }
    }
}
