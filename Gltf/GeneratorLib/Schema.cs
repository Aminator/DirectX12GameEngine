using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GeneratorLib
{
    public class SchemaEnumerator : IEnumerable<Schema>
    {
        private readonly Schema schema;

        public SchemaEnumerator(Schema schema)
        {
            this.schema = schema;
        }

        public IEnumerator<Schema> GetEnumerator()
        {
            return (schema.Properties != null ? schema.Properties.Values : Enumerable.Empty<Schema>())
                .Concat(schema.PatternProperties != null ? schema.PatternProperties.Values : Enumerable.Empty<Schema>())
                .Concat(schema.AdditionalItems != null ? new[] { schema.AdditionalItems } : Enumerable.Empty<Schema>())
                .Concat(schema.AdditionalProperties != null ? new[] { schema.AdditionalProperties } : Enumerable.Empty<Schema>())
                .Concat(schema.Items != null ? new[] { schema.Items } : Enumerable.Empty<Schema>())
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class TypeReferenceEnumerator : IEnumerable<TypeReference>
    {
        private readonly Schema schema;

        public TypeReferenceEnumerator(Schema schema)
        {
            this.schema = schema;
        }

        public IEnumerator<TypeReference> GetEnumerator()
        {
            return (schema.Type ?? Enumerable.Empty<TypeReference>())
                .Concat(schema.AllOf ?? Enumerable.Empty<TypeReference>())
                .Concat(!string.IsNullOrWhiteSpace(schema.ReferenceType) ? new[] { new TypeReference() { IsReference = true, Name = schema.ReferenceType } } : Enumerable.Empty<TypeReference>())
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class AnyOfType
    {
        public IList<object> Enum { get; set; }

        public string Description { get; set; }

        public string Type { get; set; }
    }

    // Based on http://json-schema.org/latest/json-schema-validation.html#rfc.section.5 and http://json-schema.org/draft-04/schema
    public class Schema
    {
        public Schema AdditionalItems { get; set; }

        // TODO implement this for glTF 2.0
        // Example: Dependencies: a: [ b ], c: [ b ]
        public Dictionary<string, IList<string>> Dependencies { get; set; }

        public object Default { get; set; }

        public string Description { get; set; }

        // TODO implement this for glTF 2.0
        [JsonPropertyName("gltf_detailedDescription")]
        public string DetailedDescription { get; set; }

        public Schema AdditionalProperties { get; set; }

        // TODO implement this for glTF 2.0
        // Used by Schema.Disallowed
        // Example: Not : AnyOf : [ required: [a, b], required: [a, c], required: [a, d] ]
        // TypeReferenceConverter
        public Schema Not { get; set; }

        // TODO implement this for glTF 2.0
        public uint? MultipleOf { get; set; }

        [JsonPropertyName("allOf")]
        [JsonConverter(typeof(ArrayOfTypeReferencesConverter))]
        public IList<TypeReference> AllOf { get; set; }

        // Handled by CodeGenerator.EvaluteEnums
        public IList<AnyOfType> AnyOf { get; set; }

        // TODO implement this for glTF 2.0
        // Example: OneOf : [ required: a, required: b ]
        // TypeReferenceConverter
        //public IList<IDictionary<string, IList<string>>> OneOf { get; set; }

        public IList<object> Enum { get; set; }

        [JsonPropertyName("gltf_enumNames")]
        public IList<string> EnumNames { get; set; }

        public bool ExclusiveMaximum { get; set; } = false;

        public bool ExclusiveMinimum { get; set; } = false;

        public string Format { get; set; }

        // Technically could be an array, but glTF only uses it for a schema
        public Schema Items { get; set; }

        public uint? MaxItems { get; set; }

        // Not used in glTF 2.0
        public uint? MaxLength { get; set; }

        // Not used in glTF 2.0
        public uint? MaxProperties { get; set; }

        public float? Maximum { get; set; }

        public uint? MinItems { get; set; }

        // Not used in glTF 2.0
        public uint? MinLength { get; set; }

        // TODO implement this for glTF 2.0
        // Example: minProperties: 1
        public uint? MinProperties { get; set; }

        public float? Minimum { get; set; }

        public Dictionary<string, Schema> PatternProperties { get; set; }

        // TODO implement this for glTF 2.0
        public string Pattern { get; set; }

        public Dictionary<string, Schema> Properties { get; set; }

        [JsonPropertyName("$ref")]
        public string ReferenceType { get; set; }

        // Handled by CodeGenerator.SetRequired
        public IList<string> Required { get; set; }
        
        public string Title { get; set; }

        [JsonConverter(typeof(ArrayOfTypeReferencesConverter))]
        public IList<TypeReference> Type { get; set; }

        // TODO implement this for glTF 2.0
        // Example: UniqueItems: true
        public bool UniqueItems { get; set; } = false;

        [JsonPropertyName("gltf_uriType")]
        public UriType UriType { get; set; } = UriType.None;

        // TODO implement this for glTF 2.0
        [JsonPropertyName("gltf_webgl")]
        public string WebGL { get; set; }

        private static readonly Schema empty = new Schema();
        public bool IsEmpty()
        {
            return this.GetType().GetProperties().All(property => Object.Equals(property.GetValue(this), property.GetValue(empty)));
        }

        /// <summary>
        /// Json schema properties that contain an "anyOf" object are used as
        /// enumerations in glTF 2.0.
        /// 
        /// This method searches the array of dictionaries in the "anyOf"
        /// object for the dictionary with the key "type" and extracts the type
        /// string from its value.
        /// </summary>
        internal void SetTypeFromAnyOf()
        {
            foreach (var dict in this.AnyOf)
            {
                if (!string.IsNullOrEmpty(dict.Type))
                {
                    if (this.Type == null)
                    {
                        this.Type = new List<TypeReference>();
                    }
                    this.Type.Add(new TypeReference { IsReference = false, Name = dict.Type });
                    break;
                }
            }
        }

        /// <summary>
        /// Json schema properties that contain an "anyOf" object are used as
        /// enumerations in glTF 2.0. 
        /// 
        /// This method requires that the type of the enumeration has been set.
        /// 
        /// For each dictionary in the "anyOf" array, this method extracts the
        /// list of enumerations and appends the first element of the "enum"
        /// array to the Schema enum list.
        /// 
        /// Additionally, when the enumeration is of type integer, the
        /// "description" object value is appended to the Schema enum name list.
        /// </summary>
        internal void SetValuesFromAnyOf()
        {
            if (this.Enum == null)
            {
                this.Enum = new List<object>();
            }
            if (this.Type?[0].Name == "integer" && this.EnumNames == null)
            {
                this.EnumNames = new List<string>();
            }

            foreach (var dict in this.AnyOf)
            {
                if (dict.Enum != null && dict.Enum.Count > 0)
                {
                    JsonElement enumElement = (JsonElement)dict.Enum[0];

                    if (this.Type?[0].Name == "integer")
                    {
                        this.Enum.Add(enumElement.GetInt32());
                        this.EnumNames.Add(dict.Description);
                    }
                    else if (this.Type?[0].Name == "string")
                    {
                        this.Enum.Add(enumElement.GetString());
                    }
                    else
                    {
                        throw new NotImplementedException("Enum of " + this.Type?[0].Name);
                    }
                }
            }
        }

        internal bool IsRequired { get; set; } = false;
    }

    public class TypeReference
    {
        public bool IsReference { get; set; }

        public string Name { get; set; }
    }

    public enum UriType
    {
        None,
        Application,
        Text,
        Image
    }
}
