using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DirectX12GameEngine.Shaders
{
    public static class MemberInfoExtensions
    {
        public static Type GetElementOrDeclaredType(this Type type) => type.IsArray ? type.GetElementType() : type;

        public static bool IsOverride(this MethodInfo methodInfo)
        {
            return methodInfo != methodInfo.GetBaseDefinition();
        }

        public static IList<MethodInfo> GetBaseMethods(this MethodInfo methodInfo)
        {
            List<MethodInfo> methodInfos = new List<MethodInfo>();

            if (methodInfo.IsOverride())
            {
                MethodInfo? currentMethodInfo = methodInfo.DeclaringType.BaseType?.GetMethod(methodInfo.Name);

                while (currentMethodInfo != null)
                {
                    methodInfos.Add(currentMethodInfo);
                    currentMethodInfo = currentMethodInfo.DeclaringType.BaseType?.GetMethod(currentMethodInfo.Name);
                }
            }

            return methodInfos;
        }

        public static IList<Type> GetBaseTypes(this Type type)
        {
            List<Type> baseTypes = new List<Type>();

            Type baseType = type.BaseType;

            while (baseType != null && baseType != typeof(object) && baseType != typeof(ValueType))
            {
                baseTypes.Add(baseType);
                baseType = baseType.BaseType;
            }

            return baseTypes;
        }

        public static IEnumerable<Type> GetNestedTypesInTypeHierarchy(this Type type, BindingFlags bindingFlags)
        {
            IEnumerable<Type> nestedTypes = type.GetNestedTypes(bindingFlags);

            Type parent = type.BaseType;

            while (parent != null)
            {
                nestedTypes = parent.GetNestedTypes(bindingFlags).Concat(nestedTypes);
                parent = parent.BaseType;
            }

            return nestedTypes;
        }

        public static IEnumerable<MemberInfo> GetMembersInTypeHierarchy(this Type type, BindingFlags bindingFlags)
        {
            Dictionary<Type, int> lookup = new Dictionary<Type, int>();

            int index = 0;
            lookup[type] = index++;

            Type parent = type.BaseType;

            while (parent != null)
            {
                lookup[parent] = index;
                index++;
                parent = parent.BaseType;
            }

            return type.GetShaderMembers(bindingFlags).OrderByDescending(prop => lookup[prop.DeclaringType]);
        }

        public static IEnumerable<MemberInfo> GetMembersInTypeHierarchyInOrder(this Type type, BindingFlags bindingFlags)
        {
            return type.GetMembersInTypeHierarchy(bindingFlags)
                .GroupBy(m => m.DeclaringType)
                .Select(g => g.OrderBy(m => m.GetCustomAttribute<ShaderMemberAttribute>()?.Order))
                .SelectMany(m => m);
        }

        public static IEnumerable<MemberInfo> GetMembersInOrder(this Type type, BindingFlags bindingFlags)
        {
            IEnumerable<MemberInfo> members = type.GetShaderMembers(bindingFlags).OrderBy(m => m.GetCustomAttribute<ShaderMemberAttribute>()?.Order);

            if (type.IsDefined(typeof(ShaderContractAttribute)))
            {
                members = members.Where(m => m.IsDefined(typeof(ShaderMemberAttribute)));
            }

            return members;
        }

        private static IEnumerable<MemberInfo> GetShaderMembers(this Type type, BindingFlags bindingFlags)
        {
            IEnumerable<MemberInfo> members = type.GetMembers(bindingFlags)
                .Where(m => !m.IsDefined(typeof(IgnoreShaderMemberAttribute)))
                .Where(m => !(m as MethodInfo)?.IsSpecialName ?? true)
                .Where(m => !((m as PropertyInfo)?.GetIndexParameters().Length > 0));

            if (type.IsDefined(typeof(ShaderContractAttribute)))
            {
                members = members.Where(m => m.IsDefined(typeof(ShaderMemberAttribute)));
            }

            return members;
        }

        public static object? GetMemberValue(this MemberInfo memberInfo, object? obj) => memberInfo switch
        {
            FieldInfo fieldInfo => obj is null ? null : fieldInfo.GetValue(obj),
            PropertyInfo propertyInfo => obj is null ? null : propertyInfo.GetValue(obj),
            _ => null
        };

        public static Type? GetMemberType(this MemberInfo memberInfo, object? obj = null) => memberInfo switch
        {
            FieldInfo fieldInfo => obj is null ? fieldInfo.FieldType : fieldInfo.GetValue(obj)?.GetType() ?? fieldInfo.FieldType,
            PropertyInfo propertyInfo => obj is null ? propertyInfo.PropertyType : propertyInfo.GetValue(obj)?.GetType() ?? propertyInfo.PropertyType,
            _ => null
        };

        public static ShaderMemberAttribute? GetResourceAttribute(this MemberInfo memberInfo, Type? memberType)
        {
            ShaderMemberAttribute? resourceType = memberInfo.GetCustomAttribute<ShaderMemberAttribute>();

            return resourceType != null && resourceType.Override
                ? resourceType
                : memberType?.GetCustomAttribute<ShaderMemberAttribute>() ?? resourceType;
        }

        public static bool IsStatic(this MemberInfo memberInfo) => memberInfo switch
        {
            FieldInfo fieldInfo => fieldInfo.IsStatic,
            PropertyInfo propertyInfo => propertyInfo.GetAccessors(true)[0].IsStatic,
            _ => false
        };
    }
}
