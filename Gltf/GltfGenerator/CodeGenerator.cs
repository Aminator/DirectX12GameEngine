using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

namespace GltfGenerator
{
    public class CodeGenerator
    {
        private readonly string rootDirectory;
        private readonly string rootSchemaName;

        public CodeGenerator(string rootSchemaFilePath)
        {
            rootSchemaFilePath = Path.GetFullPath(rootSchemaFilePath);
            rootDirectory = Path.GetDirectoryName(rootSchemaFilePath);
            rootSchemaName = Path.GetFileName(rootSchemaFilePath);
        }

        public Dictionary<string, Schema> FileSchemas { get; private set; }

        public void ParseSchemas()
        {
            FileSchemas = new SchemaParser(rootDirectory).ParseSchemaTree(rootSchemaName);
        }

        public void ExpandSchemaReferences()
        {
            ExpandSchemaReferences(FileSchemas[rootSchemaName]);
        }

        private void ExpandSchemaReferences(Schema schema)
        {
            foreach (var typeReference in new TypeReferenceEnumerator(schema))
            {
                if (typeReference.IsReference)
                {
                    ExpandSchemaReferences(FileSchemas[typeReference.Name]);
                }
            }

            if (schema.Properties != null)
            {
                var keys = schema.Properties.Keys.ToArray();
                foreach (var key in keys)
                {
                    if (!string.IsNullOrEmpty(schema.Properties[key].ReferenceType))
                    {
                        schema.Properties[key] = FileSchemas[schema.Properties[key].ReferenceType];
                    }

                    ExpandSchemaReferences(schema.Properties[key]);
                }
            }

            if (schema.AdditionalProperties != null)
            {
                if (!string.IsNullOrEmpty(schema.AdditionalProperties.ReferenceType))
                {
                    schema.AdditionalProperties = FileSchemas[schema.AdditionalProperties.ReferenceType];
                }

                ExpandSchemaReferences(schema.AdditionalProperties);
            }

            if (schema.Items != null)
            {
                if (!string.IsNullOrEmpty(schema.Items.ReferenceType))
                {
                    schema.Items = FileSchemas[schema.Items.ReferenceType];
                }

                ExpandSchemaReferences(schema.Items);
            }
        }

        public void EvaluateInheritance()
        {
            EvaluateInheritance(FileSchemas[rootSchemaName]);
        }

        private void EvaluateInheritance(Schema schema)
        {
            foreach (var subSchema in new SchemaEnumerator(schema))
            {
                EvaluateInheritance(subSchema);
            }

            foreach (var typeReference in new TypeReferenceEnumerator(schema))
            {
                if (typeReference.IsReference)
                {
                    EvaluateInheritance(FileSchemas[typeReference.Name]);
                }
            }

            if (schema.AllOf == null) return;

            foreach (var typeRef in schema.AllOf)
            {
                var baseType = FileSchemas[typeRef.Name];

                if (schema.Properties != null && baseType.Properties != null)
                {
                    foreach (var property in baseType.Properties)
                    {
                        if (schema.Properties.TryGetValue(property.Key, out Schema value))
                        {
                            if (value.IsEmpty())
                            {
                                schema.Properties[property.Key] = property.Value;
                            }
                            else
                            {
                                throw new InvalidOperationException("Attempting to overwrite non-Default schema.");
                            }
                        }
                        else
                        {
                            schema.Properties.Add(property.Key, property.Value);
                        }
                    }
                }

                foreach (var property in baseType.GetType().GetProperties())
                {
                    if (!property.CanRead || !property.CanWrite) continue;

                    if (property.GetValue(schema) == null)
                    {
                        property.SetValue(schema, property.GetValue(baseType));
                    }
                }
            }

            schema.AllOf = null;
        }

        public void PostProcessSchema()
        {
            SetDefaults();
            EvaluateEnums();
            SetRequired();
        }

        private void SetDefaults()
        {
            foreach (var schema in FileSchemas.Values)
            {
                if (schema.Type == null)
                {
                    schema.Type = new[] { new TypeReference { IsReference = false, Name = "object" } };
                }
            }
        }

        /// <summary>
        /// In glTF 2.0 an enumeration is defined by a property that contains
        /// the "anyOf" object that contains an array containing multiple
        /// "enum" objects and a single "type" object.
        /// 
        ///   {
        ///     "properties" : {
        ///       "mimeType" : {
        ///         "anyOf" : [
        ///           { "enum" : [ "image/jpeg" ] },
        ///           { "enum" : [ "image/png" ] },
        ///           { "type" : "string" }
        ///         ]
        ///       }
        ///     }
        ///   }
        ///   
        /// Unlike the default Json Schema, each "enum" object array will
        /// contain only one element for glTF.
        /// 
        /// So if the property does not have a "type" object and it has an
        /// "anyOf" object, assume it is an enum and attept to set the
        /// appropriate schema properties.
        /// </summary>
        private void EvaluateEnums()
        {
            foreach (var schema in FileSchemas.Values)
            {
                if (schema.Properties != null)
                {
                    foreach (var property in schema.Properties)
                    {
                        if (!(property.Value.Type?.Count >= 1))
                        {
                            if (property.Value.AnyOf?.Count > 0)
                            {
                                // Set the type of the enum
                                property.Value.SetTypeFromAnyOf();

                                // Populate the values of the enum
                                property.Value.SetValuesFromAnyOf();
                            }
                        }
                    }
                }
            }
        }

        private void SetRequired()
        {
            foreach (var schema in FileSchemas.Values)
            {
                if (schema.Required != null && schema.Required.Count > 0)
                {
                    foreach (var prop in schema.Required)
                    {
                        schema.Properties[prop].IsRequired = true;
                    }
                }
            }
        }

        public Dictionary<string, CodeTypeDeclaration> GeneratedClasses { get; set; }

        private static readonly Regex GltfReplacementRegex = new Regex("gltf", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public CodeCompileUnit RawClass(string fileName, out string className)
        {
            var root = FileSchemas[fileName];
            var schemaFile = new CodeCompileUnit();
            var schemaNamespace = new CodeNamespace("GltfLoader.Schema");
            schemaNamespace.Imports.Add(new CodeNamespaceImport("System.Linq"));
            schemaNamespace.Imports.Add(new CodeNamespaceImport("System.Runtime.Serialization"));

            className = Helpers.ToPascalCase(GltfReplacementRegex.Replace(root.Title, "Gltf"));

            var schemaClass = new CodeTypeDeclaration(className)
            {
                Attributes = MemberAttributes.Public
            };

            if (root.AllOf != null)
            {
                foreach (var typeRef in root.AllOf)
                {
                    if (typeRef.IsReference)
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            if (root.Properties != null)
            {
                foreach (var property in root.Properties)
                {
                    AddProperty(schemaClass, property.Key, property.Value);
                }
            }

            GeneratedClasses[fileName] = schemaClass;
            schemaNamespace.Types.Add(schemaClass);
            schemaFile.Namespaces.Add(schemaNamespace);
            return schemaFile;
        }

        private void AddProperty(CodeTypeDeclaration target, string rawName, Schema schema)
        {
            var propertyName = Helpers.ToPascalCase(rawName);
            var fieldName = Helpers.GetFieldName(propertyName);
            var codegenType = CodegenTypeFactory.MakeCodegenType(rawName, schema);
            target.Members.AddRange(codegenType.AdditionalMembers);

            var propertyBackingVariable = new CodeMemberField
            {
                Type = codegenType.CodeType,
                Name = fieldName,
                Comments = { new CodeCommentStatement("<summary>", true), new CodeCommentStatement($"Backing field for {propertyName}.", true), new CodeCommentStatement("</summary>", true) },
                InitExpression = codegenType.DefaultValue
            };

            target.Members.Add(propertyBackingVariable);

            var setStatements = codegenType.SetStatements ?? new CodeStatementCollection();
            setStatements.Add(new CodeAssignStatement()
            {
                Left = new CodeFieldReferenceExpression
                {
                    FieldName = fieldName,
                    TargetObject = new CodeThisReferenceExpression()
                },
                Right = new CodePropertySetValueReferenceExpression()
            });

            var property = new CodeMemberProperty
            {
                Type = codegenType.CodeType,
                Name = propertyName,
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                HasGet = true,
                GetStatements = { new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName)) },
                HasSet = true,
                Comments = { new CodeCommentStatement("<summary>", true), new CodeCommentStatement(schema.Description, true), new CodeCommentStatement("</summary>", true) },
                CustomAttributes = codegenType.Attributes
            };
            property.SetStatements.AddRange(setStatements);

            target.Members.Add(property);
        }

        public static CodeTypeReference GetCodegenType(CodeTypeDeclaration target, Schema schema, string name, out CodeAttributeDeclarationCollection attributes, out CodeExpression defaultValue)
        {
            var codegenType = CodegenTypeFactory.MakeCodegenType(name, schema);
            attributes = codegenType.Attributes;
            defaultValue = codegenType.DefaultValue;
            target.Members.AddRange(codegenType.AdditionalMembers);

            return codegenType.CodeType;
        }

        public void CSharpCodeGen(string outputDirectory)
        {
            // make sure the output directory exists
            Directory.CreateDirectory(outputDirectory);

            GeneratedClasses = new Dictionary<string, CodeTypeDeclaration>();
            foreach (var schema in FileSchemas)
            {
                if (schema.Value.Type != null && schema.Value.Type[0].Name == "object")
                {
                    CodeGenClass(schema.Key, outputDirectory);
                }
            }
        }

        private void CodeGenClass(string fileName, string outputDirectory)
        {
            var schemaFile = RawClass(fileName, out string className);
            using CSharpCodeProvider csharpcodeprovider = new CSharpCodeProvider();
            var sourceFile = Path.Combine(outputDirectory, className + "." + csharpcodeprovider.FileExtension);

            IndentedTextWriter tw1 = new IndentedTextWriter(new StreamWriter(sourceFile, false), "    ");
            csharpcodeprovider.GenerateCodeFromCompileUnit(schemaFile, tw1, new CodeGeneratorOptions { BracingStyle = "C", IndentString = "    " });
            tw1.Close();
        }
    }
}
