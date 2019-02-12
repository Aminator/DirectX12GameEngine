using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DirectX12GameEngine.Rendering.Shaders
{
    public static class MemberInfoExtensions
    {
        public static IEnumerable<Type> GetNestedTypesInTypeHierarchy(this Type type, BindingFlags bindingAttr)
        {
            IEnumerable<Type> nestedTypes = type.GetNestedTypes(bindingAttr);

            Type parent = type.BaseType;

            while (parent != null)
            {
                nestedTypes = parent.GetNestedTypes(bindingAttr).Concat(nestedTypes);
                parent = parent.BaseType;
            }

            return nestedTypes;
        }

        public static IEnumerable<MemberInfo> GetMembersInTypeHierarchy(this Type type, BindingFlags bindingAttr)
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

            return type.GetMembers(bindingAttr).OrderByDescending(prop => lookup[prop.DeclaringType]);
        }

        public static IEnumerable<MemberInfo> GetMembersInOrder(this Type type, BindingFlags bindingAttr)
        {
            return type.GetMembersInTypeHierarchy(bindingAttr)
                .GroupBy(m => m.DeclaringType)
                .Select(g => g.OrderBy(m => m.GetCustomAttribute<ShaderResourceAttribute>()?.Order))
                .SelectMany(g => g);
        }

        public static Type? GetMemberType(this MemberInfo memberInfo, object obj) => memberInfo switch
        {
            FieldInfo fieldInfo => fieldInfo.GetValue(obj)?.GetType() ?? fieldInfo.FieldType,
            PropertyInfo propertyInfo => propertyInfo.GetValue(obj)?.GetType() ?? propertyInfo.PropertyType,
            _ => null
        };

        public static Type? GetMemberType(this MemberInfo memberInfo) => memberInfo switch
        {
            FieldInfo fieldInfo => fieldInfo.FieldType,
            PropertyInfo propertyInfo => propertyInfo.PropertyType,
            _ => null
        };

        public static ShaderResourceAttribute? GetResourceAttribute(this MemberInfo memberInfo, Type? memberType)
        {
            ShaderResourceAttribute? resourceType = memberInfo.GetCustomAttribute<ShaderResourceAttribute>();

            if (resourceType is null) return null;

            return resourceType.Override
                ? resourceType
                : memberType?.GetCustomAttribute<ShaderResourceAttribute>() ?? resourceType;
        }

        public static bool IsStatic(this MemberInfo memberInfo) => memberInfo switch
        {
            FieldInfo fieldInfo => fieldInfo.IsStatic,
            PropertyInfo propertyInfo => propertyInfo.GetAccessors(true)[0].IsStatic,
            _ => false
        };
    }
}
