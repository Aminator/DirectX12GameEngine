using System;
using System.CodeDom;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GltfGenerator
{
    public class SingleValueCodegenTypeFactory
    {
        public static CodegenType MakeCodegenType(string name, Schema schema)
        {
            CodegenType returnType = new CodegenType();
            EnforceRestrictionsOnSetValues(returnType, name, schema);

            if (schema.Format == "uriref")
            {
                switch (schema.UriType)
                {
                    case UriType.Application:
                    case UriType.Image:
                    case UriType.Text:
                        returnType.CodeType = new CodeTypeReference(typeof(string));
                        returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                        break;
                    case UriType.None:
                        throw new InvalidDataException("UriType must be specified in the schema");
                }

                return returnType;
            }

            if (schema.Type.Count > 1)
            {
                returnType.CodeType = new CodeTypeReference(typeof(object));
                returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                return returnType;
            }

            var typeRef = schema.Type[0];
            if (typeRef.IsReference)
            {
                throw new NotImplementedException();
            }

            if (typeRef.Name == "object")
            {
                if (schema.Enum != null || schema.Default != null)
                {
                    throw new NotImplementedException();
                }

                if (schema.Title != null)
                {
                    returnType.CodeType = new CodeTypeReference(Helpers.ToPascalCase(schema.Title));
                    returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                    return returnType;
                }
                throw new NotImplementedException();
            }

            if (typeRef.Name == "number")
            {
                if (schema.Enum != null)
                {
                    throw new NotImplementedException();
                }

                if (schema.Default != null)
                {
                    returnType.DefaultValue = new CodePrimitiveExpression(((JsonElement)schema.Default).GetSingle());
                    returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, returnType.DefaultValue));
                }
                else if (!schema.IsRequired)
                {
                    returnType.CodeType = new CodeTypeReference(typeof(float?));
                    returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                    return returnType;
                }

                returnType.CodeType = new CodeTypeReference(typeof(float));
                return returnType;
            }

            if (typeRef.Name == "string")
            {
                if (schema.Enum != null && !((string)schema.Enum[0]).Contains('/'))
                {
                    returnType.Attributes.Add(
                        new CodeAttributeDeclaration("System.Text.Json.Serialization.JsonConverterAttribute",
                        new[] { new CodeAttributeArgument(new CodeTypeOfExpression(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))) }));
                    var enumType = GenStringEnumType(name, schema);
                    returnType.AdditionalMembers.Add(enumType);

                    if (schema.Default != null)
                    {
                        returnType.CodeType = new CodeTypeReference(enumType.Name);
                        for (var i = 0; i < enumType.Members.Count; i++)
                        {
                            if (enumType.Members[i].Name.Equals(((JsonElement)schema.Default).GetString(), StringComparison.OrdinalIgnoreCase))
                            {
                                returnType.DefaultValue = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(enumType.Name), enumType.Members[i].Name);
                                returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, returnType.DefaultValue));
                                return returnType;
                            }
                        }
                        throw new InvalidDataException("The default value is not in the enum list");
                    }
                    // TODO: System.Text.Json.Serialization.JsonStringEnumConverter cannot handle nullable enums.
                    else if (!schema.IsRequired)
                    {
                        returnType.CodeType = new CodeTypeReference(typeof(Nullable<>));
                        returnType.CodeType.TypeArguments.Add(new CodeTypeReference(enumType.Name));
                        returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                        return returnType;
                    }

                    returnType.CodeType = new CodeTypeReference(enumType.Name);
                    return returnType;
                }

                if (schema.Default != null)
                {
                    returnType.DefaultValue = new CodePrimitiveExpression(((JsonElement)schema.Default).GetString());
                    returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, returnType.DefaultValue));
                }
                else
                {
                    returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                }
                returnType.CodeType = new CodeTypeReference(typeof(string));
                return returnType;
            }

            if (typeRef.Name == "integer")
            {
                if (schema.Enum != null)
                {
                    var enumType = GenIntEnumType(name, schema);
                    returnType.AdditionalMembers.Add(enumType);

                    if (schema.Default != null)
                    {
                        returnType.DefaultValue = GetEnumField(enumType, ((JsonElement)schema.Default).GetInt32());
                        returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, returnType.DefaultValue));
                    }
                    else if (!schema.IsRequired)
                    {
                        returnType.CodeType = new CodeTypeReference(typeof(Nullable<>));
                        returnType.CodeType.TypeArguments.Add(new CodeTypeReference(enumType.Name));
                        returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                        return returnType;
                    }

                    returnType.CodeType = new CodeTypeReference(enumType.Name);
                    return returnType;
                }

                if (schema.Default != null)
                {
                    returnType.DefaultValue = new CodePrimitiveExpression(((JsonElement)schema.Default).GetInt32());
                    returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, returnType.DefaultValue));
                }
                else if (!schema.IsRequired)
                {
                    returnType.CodeType = new CodeTypeReference(typeof(int?));
                    returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                    return returnType;
                }

                returnType.CodeType = new CodeTypeReference(typeof(int));
                return returnType;
            }

            if (typeRef.Name == "boolean")
            {
                if (schema.Enum != null)
                {
                    throw new NotImplementedException();
                }

                if (schema.Default != null)
                {
                    returnType.DefaultValue = new CodePrimitiveExpression(((JsonElement)schema.Default).GetBoolean());
                    returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, returnType.DefaultValue));
                }
                else if (!schema.IsRequired)
                {
                    returnType.CodeType = new CodeTypeReference(typeof(bool?));
                    returnType.AdditionalMembers.Add(Helpers.CreateMethodThatChecksIfTheValueOfAMemberIsNotEqualToAnotherExpression(name, new CodePrimitiveExpression(null)));
                    return returnType;
                }
                returnType.CodeType = new CodeTypeReference(typeof(bool));
                return returnType;
            }

            // other types: array, null

            throw new NotImplementedException(typeRef.Name);
        }

        private static void EnforceRestrictionsOnSetValues(CodegenType returnType, string name, Schema schema)
        {
            if (schema.Minimum != null)
            {
                returnType.SetStatements.Add(new CodeConditionStatement
                {
                    Condition = new CodeBinaryOperatorExpression
                    {
                        Left = new CodePropertySetValueReferenceExpression(),
                        Operator = schema.ExclusiveMinimum ? CodeBinaryOperatorType.LessThanOrEqual : CodeBinaryOperatorType.LessThan,
                        Right = new CodePrimitiveExpression(schema.Minimum)
                    },
                    TrueStatements =
                    {
                        new CodeThrowExceptionStatement
                        {
                            ToThrow = new CodeObjectCreateExpression
                            {
                                CreateType = new CodeTypeReference(typeof(ArgumentOutOfRangeException)),
                                Parameters =
                                {
                                    new CodePrimitiveExpression(name),
                                    new CodePropertySetValueReferenceExpression(),
                                    new CodePrimitiveExpression(
                                        schema.ExclusiveMinimum ?
                                            $"Expected value to be greater than {schema.Minimum}" :
                                            $"Expected value to be greater than or equal to {schema.Minimum}")
                                }
                            }
                        }
                    }
                });
            }

            if (schema.Maximum != null)
            {
                returnType.SetStatements.Add(new CodeConditionStatement
                {
                    Condition = new CodeBinaryOperatorExpression
                    {
                        Left = new CodePropertySetValueReferenceExpression(),
                        Operator = schema.ExclusiveMaximum ? CodeBinaryOperatorType.GreaterThanOrEqual : CodeBinaryOperatorType.GreaterThan,
                        Right = new CodePrimitiveExpression(schema.Maximum)
                    },
                    TrueStatements =
                    {
                        new CodeThrowExceptionStatement
                        {
                            ToThrow = new CodeObjectCreateExpression
                            {
                                CreateType = new CodeTypeReference(typeof(ArgumentOutOfRangeException)),
                                Parameters =
                                {
                                    new CodePrimitiveExpression(name),
                                    new CodePropertySetValueReferenceExpression(),
                                    new CodePrimitiveExpression(
                                        schema.ExclusiveMaximum ?
                                        $"Expected value to be less than {schema.Maximum}" :
                                        $"Expected value to be less than or equal to {schema.Maximum}")
                                }
                            }
                        }
                    }
                });
            }

            if (schema.MinItems != null)
            {
                throw new NotImplementedException();
            }

            if (schema.MinLength != null)
            {
                throw new NotImplementedException();
            }

            if (schema.MaxItems != null)
            {
                throw new NotImplementedException();
            }

            if (schema.MaxLength != null)
            {
                throw new NotImplementedException();
            }
        }

        public static CodeTypeDeclaration GenStringEnumType(string name, Schema schema)
        {
            var enumName = $"Gltf{name}";
            var enumType = new CodeTypeDeclaration()
            {
                IsEnum = true,
                Attributes = MemberAttributes.Public,
                Name = enumName
            };

            foreach (string value in schema.Enum)
            {
                enumType.Members.Add(new CodeMemberField(enumName, Helpers.ToPascalCase(value)));
            }

            return enumType;
        }

        public static CodeTypeDeclaration GenIntEnumType(string name, Schema schema)
        {
            var enumName = $"Gltf{name}";
            var enumType = new CodeTypeDeclaration()
            {
                IsEnum = true,
                Attributes = MemberAttributes.Public,
                Name = enumName
            };

            if (schema.EnumNames == null || schema.Enum.Count != schema.EnumNames.Count)
            {
                throw new InvalidOperationException("Enum names must be defined for each integer enum");
            }

            foreach (var index in Enumerable.Range(0, schema.EnumNames.Count))
            {
                var value = (int)schema.Enum[index];
                enumType.Members.Add(new CodeMemberField(enumName, Helpers.ToPascalCase(schema.EnumNames[index]))
                {
                    InitExpression = new CodePrimitiveExpression(value)
                });
            }

            return enumType;
        }

        public static CodeFieldReferenceExpression GetEnumField(CodeTypeDeclaration enumType, int value)
        {
            var defaultMember = enumType.Members.Cast<CodeMemberField>().FirstOrDefault(m => (int)((CodePrimitiveExpression)m.InitExpression).Value == value);

            if (defaultMember == null)
            {
                throw new InvalidDataException("The default value is not in the enum list");
            }

            return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(enumType.Name), defaultMember.Name);
        }
    }
}
