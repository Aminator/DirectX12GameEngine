using System;
using Vortice.Dxc;

namespace DirectX12GameEngine.Shaders
{
    public static class ShaderCompiler
    {
        private static readonly IDxcLibrary library = Dxc.CreateDxcLibrary();

        public static byte[] Compile(DxcShaderStage shaderStage, string source, string entryPoint, string sourceName = "")
        {
            return Compile(shaderStage, source, entryPoint, sourceName, DxcShaderModel.Model6_2);
        }

        public static byte[] Compile(DxcShaderStage shaderStage, string source, string entryPoint, string sourceName, DxcShaderModel shaderModel)
        {
            return Compile(shaderStage, source, entryPoint, sourceName, new DxcCompilerOptions { ShaderModel = shaderModel });
        }

        public static byte[] Compile(DxcShaderStage shaderStage, string source, string entryPoint, string sourceName, DxcCompilerOptions options)
        {
            // TODO: Temporary fix because of bug in Vortice.Dxc
            options.PackMatricesInRowMajor = false;

            IDxcOperationResult result = DxcCompiler.Compile(shaderStage, source, entryPoint, sourceName, options);

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
    }
}
