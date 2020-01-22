using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GltfGenerator
{
    public static class CodegenTypeFactory
    {
        public static CodegenType MakeCodegenType(string name, Schema schema)
        {
            var codegenType = InternalMakeCodegenType(Helpers.ToPascalCase(name), schema);

            if (schema.IsRequired)
            {
                //codegenType.Attributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(JsonRequiredAttribute))));
            }

            codegenType.Attributes.Add(new CodeAttributeDeclaration(new CodeTypeReference(typeof(JsonPropertyNameAttribute)), new[] { new CodeAttributeArgument(new CodePrimitiveExpression(name)) }));

            return codegenType;
        }

        private static CodegenType InternalMakeCodegenType(string name, Schema schema)
        {
            if (!string.IsNullOrWhiteSpace(schema.ReferenceType))
            {
                throw new InvalidOperationException("We don't support de-referencing here.");
            }

            if (!(schema.Type?.Count >= 1))
            {
                throw new InvalidOperationException("This Schema does not represent a type");
            }

            if (schema.AdditionalProperties == null)
            {
                if (schema.Type.Count == 1 && !schema.Type[0].IsReference && schema.Type[0].Name == "array")
                {
                    return ArrayValueCodegenTypeFactory.MakeCodegenType(name, schema);
                }

                return SingleValueCodegenTypeFactory.MakeCodegenType(name, schema);
            }

            if (schema.Type.Count == 1 && schema.Type[0].Name == "object")
            {
                return MakeDictionaryType(name, schema);
            }

            throw new InvalidOperationException();
        }

        private static CodegenType MakeDictionaryType(string name, Schema schema)
        {
            var returnType = new CodegenType();

            if (schema.AdditionalProperties.Type.Count > 1)
            {
                returnType.CodeType = new CodeTypeReference(typeof(Dictionary<string, object>));
                returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                return returnType;
            }

            if (schema.Default != null)
            {
                throw new NotImplementedException("Defaults for dictionaries are not yet supported");
            }

            if (schema.AdditionalProperties.Type[0].Name == "object")
            {
                if (schema.AdditionalProperties.Title != null)
                {
                    returnType.CodeType = new CodeTypeReference($"System.Collections.Generic.Dictionary<string, {Helpers.ToPascalCase(schema.AdditionalProperties.Title)}>");
                    returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                    return returnType;
                }
                returnType.CodeType = new CodeTypeReference(typeof(Dictionary<string, object>));
                returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                return returnType;
            }

            if (schema.AdditionalProperties.Type[0].Name == "string")
            {
                returnType.CodeType = new CodeTypeReference(typeof(Dictionary<string, string>));
                returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                return returnType;
            }

            if (schema.AdditionalProperties.Type[0].Name == "integer")
            {
                returnType.CodeType = new CodeTypeReference(typeof(Dictionary<string, int>));
                returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                return returnType;
            }

            throw new NotImplementedException($"Dictionary<string,{schema.AdditionalProperties.Type[0].Name}> not yet implemented.");
        }
    }
}
