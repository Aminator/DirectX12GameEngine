using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace DirectX12GameEngine.Shaders
{
    public class ShaderGenerator
    {
        public const string AnonymousMethodEntryPointName = "Main";
        public const string AnonymousMethodDeclaringTypeName = "Shader";

        public const BindingFlags DefaultBindingFlagsWithContract = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        public const BindingFlags DefaultBindingFlagsWithoutContract = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;

        private static readonly Regex AnonymousMethodNameRegex = new Regex(@"<\w+>", RegexOptions.Compiled);
        private static readonly Regex AnonymousMethodIndexRegex = new Regex(@"\d+$", RegexOptions.Compiled);

        private readonly object shader;
        private readonly Type shaderType;
        private readonly Delegate? action;

        private readonly List<ShaderTypeDefinition> collectedTypes = new List<ShaderTypeDefinition>();
        private readonly HashSet<Type> registeredTypes = new HashSet<Type>();
        private readonly HlslBindingTracker bindingTracker = new HlslBindingTracker();

        private readonly StringWriter stringWriter = new StringWriter();
        private readonly IndentedTextWriter writer;

        private ShaderGeneratorResult? result;

        public ShaderGenerator(object shader, params Attribute[] entryPointAttributes) : this(shader, new ShaderGeneratorSettings(entryPointAttributes))
        {
        }

        public ShaderGenerator(object shader, ShaderGeneratorSettings settings)
        {
            this.shader = shader;
            shaderType = shader.GetType();

            EntryPointAttributes = settings.EntryPointAttributes;

            BindingFlagsWithContract = settings.BindingFlagsWithContract;
            BindingFlagsWithoutContract = settings.BindingFlagsWithoutContract;

            writer = new IndentedTextWriter(stringWriter);
        }

        public ShaderGenerator(Delegate action, params Attribute[] entryPointAttributes) : this(action, new ShaderGeneratorSettings(entryPointAttributes))
        {
        }

        public ShaderGenerator(Delegate action, ShaderGeneratorSettings settings) : this(action.Target, settings)
        {
            this.action = action;
        }

        public bool IsGenerated => result != null;

        public IEnumerable<Attribute> EntryPointAttributes { get; }

        public BindingFlags BindingFlagsWithContract { get; }

        public BindingFlags BindingFlagsWithoutContract { get; }

        public void AddType(Type type)
        {
            CollectStructure(type, null);
        }

        public BindingFlags GetBindingFlagsForType(Type type)
        {
            return type.IsDefined(typeof(ShaderContractAttribute)) || type.IsAssignableFrom(action?.Target.GetType()) ? BindingFlagsWithContract : BindingFlagsWithoutContract;
        }

        public ShaderGeneratorResult GenerateShader()
        {
            if (result != null) return result;

            CollectStructure(shaderType, shader);

            WriteTopLevelStructure();

            result = new ShaderGeneratorResult(stringWriter.ToString());

            GetEntryPoints(result, shaderType, GetBindingFlagsForType(shaderType));

            ShaderAttribute? shaderAttribute = EntryPointAttributes.OfType<ShaderAttribute>().FirstOrDefault();

            if (shaderAttribute != null && action != null && !action.Method.IsDefined(typeof(ShaderMemberAttribute)))
            {
                result.EntryPoints[shaderAttribute.Name] = AnonymousMethodEntryPointName;
            }

            return result;
        }

        public static void GetEntryPoints(ShaderGeneratorResult result, Type shaderType, BindingFlags bindingFlags = DefaultBindingFlagsWithContract)
        {
            foreach (MethodInfo shaderMethodInfo in shaderType.GetMethods(bindingFlags)
                .Where(m => m.IsDefined(typeof(ShaderAttribute)))
                .OrderBy(m => m.GetCustomAttribute<ShaderMemberAttribute>()?.Order))
            {
                ShaderAttribute shaderAttribute = shaderMethodInfo.GetCustomAttribute<ShaderAttribute>();
                result.EntryPoints[shaderAttribute.Name] = shaderMethodInfo.Name;
            }
        }

        private void CollectStructure(Type type, object? obj)
        {
            type = type.GetElementOrDeclaredType();

            if (HlslKnownTypes.ContainsKey(type) || !registeredTypes.Add(type)) return;

            ShaderTypeDefinition shaderTypeDefinition = new ShaderTypeDefinition(type, obj);

            if (type.IsEnum)
            {
                collectedTypes.Add(shaderTypeDefinition);
                return;
            }

            foreach (Type baseType in type.GetBaseTypes())
            {
                CollectStructure(baseType, obj);
            }

            foreach (Type interfaceType in type.GetInterfaces())
            {
                CollectStructure(interfaceType, obj);
            }

            var memberInfos = type.GetMembersInOrder(GetBindingFlagsForType(type) | BindingFlags.DeclaredOnly);

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(obj);
                object? memberValue = memberInfo.GetMemberValue(obj);

                ShaderMemberAttribute? shaderMemberAttribute = memberInfo.GetShaderMemberAttribute(memberType);

                if (memberInfo is MethodInfo methodInfo && CanWriteMethod(methodInfo))
                {
                    CollectMethod(methodInfo);
                }
                else if (memberValue is Delegate memberDelegate)
                {
                    CollectMethod(memberDelegate.Method);
                }
                else if (memberType != null && memberType != type)
                {
                    CollectStructure(memberType, memberValue);

                    if (shaderMemberAttribute != null)
                    {
                        shaderTypeDefinition.ResourceDefinitions.Add(new ResourceDefinition(memberType, shaderMemberAttribute));
                    }
                }
            }

            if (!type.IsAssignableFrom(shaderType))
            {
                collectedTypes.Add(shaderTypeDefinition);
            }
        }

        private void WriteTopLevelStructure()
        {
            var allMemberInfos = shaderType.GetMembersInTypeHierarchyInOrder(GetBindingFlagsForType(shaderType));
            var memberInfos = allMemberInfos.Where(m => !(m is MethodInfo));

            foreach (ShaderTypeDefinition type in collectedTypes)
            {
                WriteStructure(type.Type, type.Instance);
            }

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(shader);
                ShaderMemberAttribute? shaderMemberAttribute = memberInfo.GetShaderMemberAttribute(memberType);

                if (memberType != null && shaderMemberAttribute != null)
                {
                    WriteResource(memberInfo.Name, memberType, shaderMemberAttribute);
                }
            }

            foreach (Type type in shaderType.GetBaseTypes().Reverse())
            {
                WriteStructure(type, shader);
            }

            foreach (MemberInfo memberInfo in shaderType.GetMembersInOrder(GetBindingFlagsForType(shaderType)))
            {
                Type? memberType = memberInfo.GetMemberType(shader);
                object? memberValue = memberInfo.GetMemberValue(shader);

                if (memberInfo is MethodInfo methodInfo && CanWriteMethod(methodInfo))
                {
                    WriteMethod(methodInfo, true);
                }
                else if (memberValue is Delegate memberDelegate)
                {
                    WriteMethod(memberDelegate.Method, true, memberInfo.Name);
                }
            }

            if (action != null && !action.Method.IsDefined(typeof(ShaderMemberAttribute)))
            {
                WriteMethod(action.Method, true, AnonymousMethodEntryPointName, EntryPointAttributes);
            }

            stringWriter.GetStringBuilder().TrimEnd().AppendLine();
        }

        private void WriteStructure(Type type, object? obj, string? explicitTypeName = null)
        {
            string typeName = explicitTypeName ?? type.Name;

            string[] namespaces = type.Namespace.Split('.');

            if (type.IsAssignableFrom(shaderType))
            {
                namespaces = namespaces.Concat(new[] { typeName }).ToArray();
            }

            for (int i = 0; i < namespaces.Length - 1; i++)
            {
                writer.Write($"namespace {namespaces[i]} {{ ");
            }

            writer.WriteLine($"namespace {namespaces[namespaces.Length - 1]}");
            writer.WriteLine("{");
            writer.Indent++;

            if (!type.IsAssignableFrom(shaderType))
            {
                if (type.IsEnum)
                {
                    writer.WriteLine($"enum class {typeName}");
                }
                else if (type.IsInterface)
                {
                    writer.WriteLine($"interface {typeName}");
                }
                else
                {
                    writer.Write($"struct {typeName}");

                    bool trim = false;

                    if (type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType))
                    {
                        writer.Write($" : {HlslKnownTypes.GetMappedName(type.BaseType)}, ");
                        trim = true;
                    }

                    // NOTE: Types might no expose every interface method.
                    //Type[] interfaces = type.GetInterfaces();

                    //if (interfaces.Length > 0)
                    //{
                    //    foreach (Type interfaceType in interfaces)
                    //    {
                    //        writer.Write(interfaceType.Name + ", ");
                    //    }

                    //    trim = true;
                    //}

                    if (trim)
                    {
                        stringWriter.GetStringBuilder().Length -= 2;
                    }

                    writer.WriteLine();
                }

                writer.WriteLine("{");
                writer.Indent++;
            }

            BindingFlags fieldBindingFlags = GetBindingFlagsForType(type) | BindingFlags.DeclaredOnly;

            if (type.IsEnum)
            {
                fieldBindingFlags &= ~BindingFlags.Instance;
            }

            var fieldAndPropertyInfos = type.GetMembersInOrder(fieldBindingFlags).Where(m => m is FieldInfo || m is PropertyInfo);
            var methodInfos = type.GetMembersInTypeHierarchyInOrder(GetBindingFlagsForType(type)).Where(m => m is MethodInfo);
            var memberInfos = type.IsAssignableFrom(shaderType) ? methodInfos : fieldAndPropertyInfos.Concat(methodInfos);

            foreach (MemberInfo memberInfo in memberInfos)
            {
                Type? memberType = memberInfo.GetMemberType(obj);
                object? memberValue = memberInfo.GetMemberValue(obj);

                if (memberInfo is MethodInfo methodInfo && CanWriteMethod(methodInfo))
                {
                    WriteMethod(methodInfo);
                }
                else if (memberValue is Delegate memberDelegate)
                {
                    WriteMethod(memberDelegate.Method, false, memberInfo.Name);
                }
                else if (memberType != null)
                {
                    if (type.IsEnum)
                    {
                        writer.Write(memberInfo.Name);
                        writer.WriteLine(",");
                    }
                    else
                    {
                        WriteStructureField(memberInfo, memberType);
                    }
                }
            }

            stringWriter.GetStringBuilder().TrimEnd().AppendLine();

            if (!type.IsAssignableFrom(shaderType))
            {
                writer.Indent--;
                writer.WriteLine("};");
            }

            writer.Indent--;

            for (int i = 0; i < namespaces.Length - 1; i++)
            {
                writer.Write("}");
            }

            writer.WriteLine("}");
            writer.WriteLine();

            if (type.IsEnum) return;

            foreach (MemberInfo memberInfo in memberInfos.Where(m => m.IsStatic()))
            {
                Type? memberType = memberInfo.GetMemberType(obj);

                if (memberType != null)
                {
                    WriteStaticStructureField(memberInfo, memberType);
                }
            }
        }

        private void WriteStructureField(MemberInfo memberInfo, Type memberType)
        {
            if (memberInfo.IsStatic())
            {
                writer.Write("static");
                writer.Write(" ");
            }

            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberInfo.Name}");

            if (memberType.IsArray) writer.Write("[2]");

            writer.Write(GetHlslSemantic(memberInfo.GetCustomAttribute<ShaderSemanticAttribute>()));
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private void WriteStaticStructureField(MemberInfo memberInfo, Type memberType)
        {
            string declaringType = HlslKnownTypes.GetMappedName(memberInfo.DeclaringType);
            writer.WriteLine($"static {HlslKnownTypes.GetMappedName(memberType)} {declaringType}::{memberInfo.Name};");
            writer.WriteLine();
        }

        private void WriteResource(string memberName, Type memberType, ShaderMemberAttribute shaderMemberAttribute)
        {
            switch (shaderMemberAttribute)
            {
                case ConstantBufferViewAttribute _:
                    WriteConstantBufferView(memberName, memberType, bindingTracker.ConstantBuffer++);
                    break;
                case ShaderResourceViewAttribute _:
                    WriteShaderResourceView(memberName, memberType, bindingTracker.ShaderResourceView++);
                    break;
                case UnorderedAccessViewAttribute _:
                    WriteUnorderedAccessView(memberName, memberType, bindingTracker.UnorderedAccessView++);
                    break;
                case SamplerAttribute _:
                    WriteSampler(memberName, memberType, bindingTracker.Sampler++);
                    break;
                case StaticResourceAttribute _:
                    WriteStaticResource(memberName, memberType);
                    break;
                default:
                    throw new NotSupportedException("This shader resource type is not supported.");
            }
        }

        private void WriteConstantBufferView(string memberName, Type memberType, int binding)
        {
            writer.Write($"cbuffer {memberName}Buffer");
            writer.WriteLine($" : register(b{binding})");
            writer.WriteLine("{");
            writer.Indent++;
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberName}");
            if (memberType.IsArray) writer.Write("[2]");
            writer.WriteLine(";");
            writer.Indent--;
            writer.WriteLine("}");
            writer.WriteLine();
        }

        private void WriteShaderResourceView(string memberName, Type memberType, int binding)
        {
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberName}");
            writer.Write($" : register(t{binding})");
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private void WriteUnorderedAccessView(string memberName, Type memberType, int binding)
        {
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberName}");
            writer.Write($" : register(u{binding})");
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private void WriteSampler(string memberName, Type memberType, int binding)
        {
            writer.Write($"{HlslKnownTypes.GetMappedName(memberType)} {memberName}");
            writer.Write($" : register(s{binding})");
            writer.WriteLine(";");
            writer.WriteLine();
        }

        private void WriteStaticResource(string memberName, Type memberType, IEnumerable<string>? resourceNames = null)
        {
            if (resourceNames is null)
            {
                List<string> generatedMemberNames = new List<string>();

                foreach (ResourceDefinition resourceDefinition in collectedTypes.First(d => d.Type == memberType).ResourceDefinitions)
                {
                    string generatedMemberName = $"__Generated__{bindingTracker.StaticResource++}__";
                    generatedMemberNames.Add(generatedMemberName);

                    WriteResource(generatedMemberName, resourceDefinition.MemberType, resourceDefinition.ShaderMemberAttribute);
                }

                resourceNames = generatedMemberNames;
            }

            writer.Write($"static {HlslKnownTypes.GetMappedName(memberType)} {memberName}");

            if (resourceNames.Count() > 0)
            {
                writer.Write(" = { ");

                foreach (string resourceName in resourceNames)
                {
                    writer.Write(resourceName);
                    writer.Write(", ");
                }

                stringWriter.GetStringBuilder().Length -= 2;

                writer.Write(" }");
            }

            writer.WriteLine(";");
            writer.WriteLine();
        }

        private static string GetHlslSemantic(ShaderSemanticAttribute? semanticAttribute)
        {
            if (semanticAttribute is null) return "";

            Type semanticType = semanticAttribute.GetType();

            if (HlslKnownSemantics.ContainsKey(semanticType))
            {
                string semanticName = HlslKnownSemantics.GetMappedName(semanticType);

                return semanticAttribute is ShaderSemanticWithIndexAttribute semanticAttributeWithIndex
                    ? " : " + semanticName + semanticAttributeWithIndex.Index
                    : " : " + semanticName;
            }

            throw new NotSupportedException();
        }

        private bool CanWriteMethod(MethodInfo methodInfo)
        {
            return methodInfo.IsDefined(typeof(ShaderMemberAttribute)) || methodInfo == action?.Method /*|| methodInfo.DeclaringType.IsInterface*/;
        }

        private void CollectMethod(MethodInfo methodInfo)
        {
            ShaderMethodAttribute? shaderMethodAttribute = GetShaderMethodAttribute(methodInfo);

            IEnumerable<Type> dependentTypes = shaderMethodAttribute?.DependentTypes != null
                ? shaderMethodAttribute.DependentTypes
                : ShaderMethodGenerator.GetDependentTypes(methodInfo).Select(t => Type.GetType(ShaderMethodGenerator.GetFullTypeName(t)));

            dependentTypes = dependentTypes.Concat(methodInfo.GetParameters().Select(p => p.ParameterType).Concat(new[] { methodInfo.ReturnType })).Distinct();

            foreach (Type type in dependentTypes)
            {
                AddType(type);
            }
        }

        private void WriteMethod(MethodInfo methodInfo, bool isTopLevel = false, string ? explicitMethodName = null, IEnumerable<Attribute>? attributes = null)
        {
            bool isStatic = methodInfo.IsStatic;

            var methodAttributes = methodInfo.GetCustomAttributes();

            if (attributes != null)
            {
                methodAttributes = methodAttributes.Concat(attributes);
            }

            foreach (Attribute attribute in methodAttributes)
            {
                if (isTopLevel || !(attribute is ShaderAttribute))
                {
                    WriteAttribute(attribute);
                }
            }

            if (isStatic) writer.Write("static ");

            string methodName = explicitMethodName ?? methodInfo.Name;

            writer.Write(HlslKnownTypes.GetMappedName(methodInfo.ReturnType));
            writer.Write(" ");
            writer.Write(methodName);

            WriteParameters(methodInfo);

            if (methodInfo.ReturnTypeCustomAttributes.GetCustomAttributes(typeof(ShaderSemanticAttribute), false).FirstOrDefault(a => HlslKnownSemantics.ContainsKey(a.GetType())) is ShaderSemanticAttribute returnTypeAttribute)
            {
                writer.Write(GetHlslSemantic(returnTypeAttribute));
            }

            if (methodInfo.GetMethodBody() != null)
            {
                string methodBody = GetMethodBody(methodInfo);

                writer.WriteLine();
                writer.WriteLine("{");
                writer.Indent++;
                writer.WriteLine(methodBody);
                writer.Indent--;
                writer.WriteLine("}");
            }
            else
            {
                writer.Write(";");
            }

            writer.WriteLine();
        }

        private string GetMethodBody(MethodInfo methodInfo)
        {
            string shaderSource = GetShaderMethodAttribute(methodInfo)?.ShaderSource ?? ShaderMethodGenerator.GetMethodBody(methodInfo);

            // Indent every line
            StringBuilder indent = new StringBuilder();

            for (int i = 0; i < writer.Indent + 1; i++)
            {
                indent.Append(IndentedTextWriter.DefaultTabString);
            }

            return shaderSource.Replace(Environment.NewLine, Environment.NewLine + indent.ToString()).Trim();
        }

        private static ShaderMethodAttribute? GetShaderMethodAttribute(MethodInfo methodInfo)
        {
            if (methodInfo.IsDefined(typeof(CompilerGeneratedAttribute)) || methodInfo.DeclaringType.IsDefined(typeof(CompilerGeneratedAttribute)))
            {
                string methodName = AnonymousMethodNameRegex.Match(methodInfo.Name).Value.Trim('<', '>');
                int anonymousMethodIndex = int.Parse(AnonymousMethodIndexRegex.Match(methodInfo.Name).Value, CultureInfo.InvariantCulture);

                MethodInfo? containingMethodInfo = methodInfo.DeclaringType.GetMethod(methodName, DefaultBindingFlagsWithContract)
                    ?? methodInfo.DeclaringType.DeclaringType.GetMethod(methodName, DefaultBindingFlagsWithContract);

                if (containingMethodInfo is null) throw new InvalidOperationException($"The containing method {methodName} of the anonymous method {methodInfo.Name} was not found.");

                var shaderMethodAttributes = containingMethodInfo.GetCustomAttributes<AnonymousShaderMethodAttribute>();

                foreach (AnonymousShaderMethodAttribute shaderMethodAttribute in shaderMethodAttributes)
                {
                    if (shaderMethodAttribute.AnonymousMethodIndex == anonymousMethodIndex)
                    {
                        if (shaderMethodAttribute.ShaderSource != null)
                        {
                            return shaderMethodAttribute;
                        }
                        else
                        {
                            var globalAttributes = containingMethodInfo.DeclaringType.Assembly.GetCustomAttributes<GlobalAnonymousShaderMethodAttribute>();

                            foreach (GlobalAnonymousShaderMethodAttribute globalAttribute in globalAttributes)
                            {
                                if (globalAttribute.AnonymousMethodIndex == shaderMethodAttribute.AnonymousMethodIndex
                                    && globalAttribute.DeclaringType == containingMethodInfo.DeclaringType
                                    && containingMethodInfo.ReflectedType.GetMethod(globalAttribute.MethodName, DefaultBindingFlagsWithContract, null, globalAttribute.ParameterTypes, null) == containingMethodInfo)
                                {
                                    return globalAttribute;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                ShaderMethodAttribute? shaderMethodAttribute = methodInfo.GetCustomAttribute<ShaderMethodAttribute?>();

                if (shaderMethodAttribute != null)
                {
                    if (shaderMethodAttribute.ShaderSource != null)
                    {
                        return shaderMethodAttribute;
                    }
                    else
                    {
                        var globalAttributes = methodInfo.DeclaringType.Assembly.GetCustomAttributes<GlobalShaderMethodAttribute>();

                        foreach (GlobalShaderMethodAttribute globalAttribute in globalAttributes)
                        {
                            if (globalAttribute.DeclaringType == methodInfo.DeclaringType
                                && methodInfo.ReflectedType.GetMethod(globalAttribute.MethodName, DefaultBindingFlagsWithContract, null, globalAttribute.ParameterTypes, null) == methodInfo)
                            {
                                return globalAttribute;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private void WriteAttribute(Attribute attribute)
        {
            Type attributeType = attribute.GetType();

            if (HlslKnownAttributes.ContainsKey(attributeType))
            {
                writer.Write("[");
                writer.Write(HlslKnownAttributes.GetMappedName(attributeType));

                var fieldAndPropertyInfos = attributeType.GetMembersInOrder(GetBindingFlagsForType(attributeType) | BindingFlags.DeclaredOnly).Where(m => m is FieldInfo || m is PropertyInfo);
                IEnumerable<object> attributeMemberValues = fieldAndPropertyInfos.Where(m => m.GetMemberValue(attribute) != null).Select(m => m.GetMemberValue(attribute))!;

                if (attributeMemberValues.Count() > 0)
                {
                    writer.Write("(");

                    foreach (object memberValue in attributeMemberValues)
                    {
                        string valueString = memberValue is string ? $"\"{memberValue}\"" : memberValue.ToString();

                        writer.Write(valueString);
                        writer.Write(", ");
                    }

                    stringWriter.GetStringBuilder().Length -= 2;

                    writer.Write(")");
                }

                writer.WriteLine("]");
            }
        }

        private void WriteParameters(MethodInfo methodInfo)
        {
            writer.Write("(");

            ParameterInfo[] parameterInfos = methodInfo.GetParameters();

            if (parameterInfos.Length > 0)
            {
                foreach (ParameterInfo parameterInfo in parameterInfos)
                {
                    if (parameterInfo.ParameterType.IsByRef)
                    {
                        string refString = parameterInfo.IsIn ? "in" : parameterInfo.IsOut ? "out" : "inout";
                        writer.Write(refString);
                        writer.Write(" ");
                    }

                    writer.Write($"{HlslKnownTypes.GetMappedName(parameterInfo.ParameterType)} {parameterInfo.Name}");

                    if (parameterInfo.GetCustomAttributes<ShaderSemanticAttribute>().FirstOrDefault(a => HlslKnownSemantics.ContainsKey(a.GetType())) is ShaderSemanticAttribute parameterAttribute)
                    {
                        writer.Write(GetHlslSemantic(parameterAttribute));
                    }

                    writer.Write(", ");
                }

                stringWriter.GetStringBuilder().Length -= 2;
            }

            writer.Write(")");
        }

        private class HlslBindingTracker
        {
            public int ConstantBuffer { get; set; }

            public int Sampler { get; set; }

            public int ShaderResourceView { get; set; }

            public int UnorderedAccessView { get; set; }

            public int StaticResource { get; set; }
        }

        private class ShaderTypeDefinition
        {
            public ShaderTypeDefinition(Type type, object? instance)
            {
                Type = type;
                Instance = instance;
            }

            public object? Instance { get; }

            public Type Type { get; }

            public List<ResourceDefinition> ResourceDefinitions { get; } = new List<ResourceDefinition>();
        }

        private class ResourceDefinition
        {
            public ResourceDefinition(Type memberType, ShaderMemberAttribute shaderMemberAttribute)
            {
                MemberType = memberType;
                ShaderMemberAttribute = shaderMemberAttribute;
            }

            public Type MemberType { get; }

            public ShaderMemberAttribute ShaderMemberAttribute { get; }
        }
    }
}
