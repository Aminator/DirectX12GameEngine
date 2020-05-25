using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DirectX12GameEngine.Shaders
{
    internal static class MemberInfoExtensions
    {
        public static IEnumerable<Type> GetBaseTypes(this Type type)
        {
            Type baseType = type.BaseType;

            while (baseType != null && baseType != typeof(object) && baseType != typeof(ValueType))
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
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

        public static object? GetMemberValue(this MemberInfo memberInfo, object? obj)
        {
            ShaderResourceAttribute? shaderResourceAttribute = memberInfo.GetCustomAttribute<ShaderResourceAttribute>();

            return shaderResourceAttribute?.ResourceType ?? memberInfo switch
            {
                FieldInfo fieldInfo => obj is null ? null : fieldInfo.GetValue(obj),
                PropertyInfo propertyInfo => obj is null ? null : propertyInfo.GetValue(obj),
                _ => null
            };
        }

        public static Type? GetMemberType(this MemberInfo memberInfo, object? obj = null)
        {
            ShaderResourceAttribute? shaderResourceAttribute = memberInfo.GetCustomAttribute<ShaderResourceAttribute>();

            return shaderResourceAttribute?.ResourceType ?? memberInfo switch
            {
                FieldInfo fieldInfo => obj is null ? fieldInfo.FieldType : fieldInfo.GetValue(obj)?.GetType() ?? fieldInfo.FieldType,
                PropertyInfo propertyInfo => obj is null ? propertyInfo.PropertyType : propertyInfo.GetValue(obj)?.GetType() ?? propertyInfo.PropertyType,
                _ => null
            };
        }

        public static ShaderResourceAttribute? GetShaderResourceAttribute(this MemberInfo memberInfo, Type? memberType)
        {
            ShaderResourceAttribute? shaderResourceAttribute = memberInfo.GetCustomAttribute<ShaderResourceAttribute>();

            return shaderResourceAttribute != null && shaderResourceAttribute.Override
                ? shaderResourceAttribute
                : memberType?.GetCustomAttribute<ShaderResourceAttribute>() ?? shaderResourceAttribute;
        }

        public static bool IsStatic(this MemberInfo memberInfo) => memberInfo switch
        {
            FieldInfo fieldInfo => fieldInfo.IsStatic,
            PropertyInfo propertyInfo => propertyInfo.GetAccessors(true)[0].IsStatic,
            _ => false
        };
    }
}
