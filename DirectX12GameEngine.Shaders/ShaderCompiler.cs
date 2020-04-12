using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vortice.Dxc;

namespace DirectX12GameEngine.Shaders
{
    public static class ShaderCompiler
    {
        public static byte[] Compile(ShaderStage shaderStage, string source, string entryPoint, string sourceName = "")
        {
            return Compile(shaderStage, source, entryPoint, sourceName, ShaderModel.Model6_1);
        }

        public static byte[] Compile(ShaderStage shaderStage, string source, string entryPoint, string sourceName, ShaderModel shaderModel)
        {
            return Compile(shaderStage, source, entryPoint, sourceName, new DxcCompilerOptions { ShaderModel = Unsafe.As<ShaderModel, DxcShaderModel>(ref shaderModel), PackMatrixInRowMajor = true });
        }

        private static byte[] Compile(ShaderStage shaderStage, string source, string entryPoint, string sourceName, DxcCompilerOptions options)
        {
            //IDxcOperationResult result = DxcCompiler.Compile((DxcShaderStage)shaderStage, source, entryPoint, sourceName, options);

            string shaderProfile = GetShaderProfile(shaderStage, options.ShaderModel);

            List<string> arguments = new List<string>();

            if (options.PackMatrixInColumnMajor)
            {
                arguments.Add("-Zpc");
            }
            else if (options.PackMatrixInRowMajor)
            {
                arguments.Add("-Zpr");
            }

            IDxcLibrary library = Dxc.CreateDxcLibrary();
            IDxcBlobEncoding sourceBlob = Dxc.CreateBlobForText(library, source);
            IDxcIncludeHandler includeHandler = library.CreateIncludeHandler();

            IDxcCompiler compiler = Dxc.CreateDxcCompiler();
            IDxcOperationResult result = compiler.Compile(
                sourceBlob,
                sourceName,
                entryPoint,
                shaderProfile,
                arguments.ToArray(),
                arguments.Count,
                null,
                0,
                includeHandler);

            if (result.GetStatus() == 0)
            {
                IDxcBlob blob = result.GetResult();
                return Dxc.GetBytesFromBlob(blob);
            }
            else
            {
                string resultText = Dxc.GetStringFromBlob(library, result.GetErrors());
                throw new Exception(resultText);
            }
        }

        private static string GetShaderProfile(ShaderStage shaderStage, DxcShaderModel shaderModel)
        {
            string shaderProfile = shaderStage switch
            {
                ShaderStage.VertexShader => "vs",
                ShaderStage.PixelShader => "ps",
                ShaderStage.GeometryShader => "gs",
                ShaderStage.HullShader => "hs",
                ShaderStage.DomainShader => "ds",
                ShaderStage.ComputeShader => "cs",
                ShaderStage.Library => "lib",
                _ => ""
            };

            shaderProfile += $"_{shaderModel.Major}_{shaderModel.Minor}";

            return shaderProfile;
        }
    }
}
