using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using DirectX12GameEngine.Shaders.Numerics;
using Microsoft.CodeAnalysis;

namespace DirectX12GameEngine.Shaders
{
    internal static class HlslKnownTypes
    {
        private static readonly Dictionary<string, string> knownTypes = new Dictionary<string, string>()
        {
            { typeof(void).FullName, "void" },
            { typeof(bool).FullName, "bool" },
            { typeof(uint).FullName, "uint" },
            { typeof(int).FullName, "int" },
            { typeof(double).FullName, "double" },
            { typeof(float).FullName, "float" },
            { typeof(Vector2).FullName, "float2" },
            { typeof(Vector3).FullName, "float3" },
            { typeof(Vector4).FullName, "float4" },
            { typeof(GraphicsVector2).FullName, "float2" },
            { typeof(GraphicsVector3).FullName, "float3" },
            { typeof(GraphicsVector4).FullName, "float4" },
            { typeof(Int2).FullName, "int2" },
            { typeof(Int3).FullName, "int3" },
            { typeof(Int4).FullName, "int4" },
            { typeof(UInt2).FullName, "uint2" },
            { typeof(UInt3).FullName, "uint3" },
            { typeof(UInt4).FullName, "uint4" },
            { typeof(Matrix4x4).FullName, "float4x4" }
        };

        public static bool IsPrimitiveType(Type type)
        {
            type = type.GetElementType() ?? type;

            return type.IsEnum || knownTypes.ContainsKey(type.FullName);
        }

        public static bool ContainsKey(Type type)
        {
            type = type.GetElementType() ?? type;

            return !string.IsNullOrEmpty(GetTypeNameFromAttribute(type)) || knownTypes.ContainsKey(type.FullName);
        }

        public static bool ContainsKey(ITypeSymbol typeSymbol)
        {
            return !string.IsNullOrEmpty(GetTypeNameFromAttribute(typeSymbol)) || knownTypes.ContainsKey(typeSymbol.ToString());
        }

        public static string GetMappedName(Type type)
        {
            type = type.GetElementType() ?? type;

            string? typeNameFromAttribute = GetTypeNameFromAttribute(type);
            string fullTypeName = typeNameFromAttribute ?? type.FullName;

            string mappedName = knownTypes.TryGetValue(fullTypeName, out string mapped) ? mapped : fullTypeName.Replace(".", "::");

            return type.IsGenericType ? mappedName + $"<{string.Join(", ", type.GetGenericArguments().Select(t => GetMappedName(t)))}>" : mappedName;
        }

        public static string GetMappedName(ITypeSymbol typeSymbol)
        {
            string fullTypeName = typeSymbol.ToString();

            return GetTypeNameFromAttribute(typeSymbol) ?? (knownTypes.TryGetValue(fullTypeName, out string mapped) ? mapped : fullTypeName.Replace(".", "::"));
        }

        private static string? GetTypeNameFromAttribute(ITypeSymbol typeSymbol)
        {
            AttributeData? attributeData = typeSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToString() == typeof(ShaderTypeAttribute).FullName);
            return (string?)attributeData?.ConstructorArguments.First().Value;
        }

        private static string? GetTypeNameFromAttribute(Type type)
        {
            return type.GetCustomAttribute<ShaderTypeAttribute>()?.TypeName;
        }
    }

    internal static class HlslKnownMethods
    {
        private static readonly Dictionary<string, string> knownMethods = new Dictionary<string, string>()
        {
            { "System.Math.Cos", "cos" },
            { "System.MathF.Cos", "cos" },
            { "System.Math.Exp", "exp" },
            { "System.MathF.Exp", "exp" },
            { "System.Math.Max", "max" },
            { "System.Math.Pow", "pow" },
            { "System.MathF.Pow", "pow" },
            { "System.Math.Sin", "sin" },
            { "System.MathF.Sin", "sin" },
            { "System.Math.PI", "3.1415926535897931" },
            { "System.MathF.PI", "3.14159274f" },

            { "DirectX12GameEngine.Shaders.Numerics.GraphicsVector2.Length", "length" },
            { "DirectX12GameEngine.Shaders.Numerics.GraphicsVector3.Length", "length" },
            { "DirectX12GameEngine.Shaders.Numerics.GraphicsVector4.Length", "length" },

            { "DirectX12GameEngine.Shaders.Numerics.Int2.X", ".x" },
            { "DirectX12GameEngine.Shaders.Numerics.Int2.Y", ".y" },

            { "DirectX12GameEngine.Shaders.Numerics.Int3.X", ".x" },
            { "DirectX12GameEngine.Shaders.Numerics.Int3.Y", ".y" },
            { "DirectX12GameEngine.Shaders.Numerics.Int3.Z", ".z" },

            { "DirectX12GameEngine.Shaders.Numerics.Int4.X", ".x" },
            { "DirectX12GameEngine.Shaders.Numerics.Int4.Y", ".y" },
            { "DirectX12GameEngine.Shaders.Numerics.Int4.Z", ".z" },
            { "DirectX12GameEngine.Shaders.Numerics.Int4.W", ".w" },

            { "DirectX12GameEngine.Shaders.Numerics.UInt2.X", ".x" },
            { "DirectX12GameEngine.Shaders.Numerics.UInt2.Y", ".y" },

            { "DirectX12GameEngine.Shaders.Numerics.UInt3.X", ".x" },
            { "DirectX12GameEngine.Shaders.Numerics.UInt3.Y", ".y" },
            { "DirectX12GameEngine.Shaders.Numerics.UInt3.Z", ".z" },

            { "DirectX12GameEngine.Shaders.Numerics.UInt4.X", ".x" },
            { "DirectX12GameEngine.Shaders.Numerics.UInt4.Y", ".y" },
            { "DirectX12GameEngine.Shaders.Numerics.UInt4.Z", ".z" },
            { "DirectX12GameEngine.Shaders.Numerics.UInt4.W", ".w" },

            { "System.Numerics.Vector2.X", ".x" },
            { "System.Numerics.Vector2.Y", ".y" },

            { "System.Numerics.Vector3.X", ".x" },
            { "System.Numerics.Vector3.Y", ".y" },
            { "System.Numerics.Vector3.Z", ".z" },
            { "System.Numerics.Vector3.Cross", "cross" },
            { "System.Numerics.Vector3.Dot", "dot" },
            { "System.Numerics.Vector3.Lerp", "lerp" },
            { "System.Numerics.Vector3.Transform", "mul" },
            { "System.Numerics.Vector3.TransformNormal", "mul" },
            { "System.Numerics.Vector3.Normalize", "normalize" },
            { "System.Numerics.Vector3.Zero", "(float3)0" },
            { "System.Numerics.Vector3.One", "float3(1.0f, 1.0f, 1.0f)" },
            { "System.Numerics.Vector3.UnitX", "float3(1.0f, 0.0f, 0.0f)" },
            { "System.Numerics.Vector3.UnitY", "float3(0.0f, 1.0f, 0.0f)" },
            { "System.Numerics.Vector3.UnitZ", "float3(0.0f, 0.0f, 1.0f)" },

            { "System.Numerics.Vector4.X", ".x" },
            { "System.Numerics.Vector4.Y", ".y" },
            { "System.Numerics.Vector4.Z", ".z" },
            { "System.Numerics.Vector4.W", ".w" },
            { "System.Numerics.Vector4.Lerp", "lerp" },
            { "System.Numerics.Vector4.Transform", "mul" },
            { "System.Numerics.Vector4.Normalize", "normalize" },
            { "System.Numerics.Vector4.Zero", "(float4)0" },
            { "System.Numerics.Vector4.One", "float4(1.0f, 1.0f, 1.0f, 1.0f)" },

            { "System.Numerics.Matrix4x4.Multiply", "mul" },
            { "System.Numerics.Matrix4x4.Transpose", "transpose" },
            { "System.Numerics.Matrix4x4.Translation", "[3].xyz" },
            { "System.Numerics.Matrix4x4.M11", "[0][0]" },
            { "System.Numerics.Matrix4x4.M12", "[0][1]" },
            { "System.Numerics.Matrix4x4.M13", "[0][2]" },
            { "System.Numerics.Matrix4x4.M14", "[0][3]" },
            { "System.Numerics.Matrix4x4.M21", "[1][0]" },
            { "System.Numerics.Matrix4x4.M22", "[1][1]" },
            { "System.Numerics.Matrix4x4.M23", "[1][2]" },
            { "System.Numerics.Matrix4x4.M24", "[1][3]" },
            { "System.Numerics.Matrix4x4.M31", "[2][0]" },
            { "System.Numerics.Matrix4x4.M32", "[2][1]" },
            { "System.Numerics.Matrix4x4.M33", "[2][2]" },
            { "System.Numerics.Matrix4x4.M34", "[2][3]" },
            { "System.Numerics.Matrix4x4.M41", "[3][0]" },
            { "System.Numerics.Matrix4x4.M42", "[3][1]" },
            { "System.Numerics.Matrix4x4.M43", "[3][2]" },
            { "System.Numerics.Matrix4x4.M44", "[3][3]" }
        };

        public static bool ContainsKey(string name)
        {
            return knownMethods.ContainsKey(name);
        }

        public static string GetMappedName(string name)
        {
            return knownMethods[name];
        }
    }
}
