using System.Runtime.CompilerServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class CscHostObject : ICscHostObject
    {
        private readonly Project project;

        public CscHostObject(Project project)
        {
            this.project = project;
        }

        public string? OutputAssembly { get; set; }

        public string? PdbFile { get; set; }

        public string? DocumentationFile { get; set; }

        public string? Win32Resource { get; set; }

        public bool SetProperty(object value, [CallerMemberName] string? propertyName = null)
        {
            return true;
        }

        public void BeginInitialization()
        {
        }

        public bool Compile()
        {
            if (OutputAssembly is null) return false;

            Compilation? compilation = project.GetCompilationAsync().Result;

            if (compilation is null) return false;

            EmitResult result = compilation.Emit(OutputAssembly, PdbFile, DocumentationFile, Win32Resource);

            return result.Success;
        }

        public bool EndInitialization(out string? errorMessage, out int errorCode)
        {
            errorMessage = null;
            errorCode = 0;

            return true;
        }

        public bool IsDesignTime()
        {
            return false;
        }

        public bool IsUpToDate()
        {
            return false;
        }

        public bool SetAdditionalLibPaths(string[] additionalLibPaths)
        {
            return SetProperty(additionalLibPaths);
        }

        public bool SetAddModules(string[] addModules)
        {
            return SetProperty(addModules);
        }

        public bool SetAllowUnsafeBlocks(bool allowUnsafeBlocks)
        {
            return SetProperty(allowUnsafeBlocks);
        }

        public bool SetBaseAddress(string baseAddress)
        {
            return SetProperty(baseAddress);
        }

        public bool SetCheckForOverflowUnderflow(bool checkForOverflowUnderflow)
        {
            return SetProperty(checkForOverflowUnderflow);
        }

        public bool SetCodePage(int codePage)
        {
            return SetProperty(codePage);
        }

        public bool SetDebugType(string debugType)
        {
            return SetProperty(debugType);
        }

        public bool SetDefineConstants(string defineConstants)
        {
            return SetProperty(defineConstants);
        }

        public bool SetDelaySign(bool delaySignExplicitlySet, bool delaySign)
        {
            return SetProperty(new[] { delaySignExplicitlySet, delaySign });
        }

        public bool SetDisabledWarnings(string disabledWarnings)
        {
            return SetProperty(disabledWarnings);
        }

        public bool SetDocumentationFile(string documentationFile)
        {
            DocumentationFile = documentationFile;

            return SetProperty(documentationFile);
        }

        public bool SetEmitDebugInformation(bool emitDebugInformation)
        {
            return SetProperty(emitDebugInformation);
        }

        public bool SetErrorReport(string errorReport)
        {
            return SetProperty(errorReport);
        }

        public bool SetFileAlignment(int fileAlignment)
        {
            return SetProperty(fileAlignment);
        }

        public bool SetGenerateFullPaths(bool generateFullPaths)
        {
            return SetProperty(generateFullPaths);
        }

        public bool SetKeyContainer(string keyContainer)
        {
            return SetProperty(keyContainer);
        }

        public bool SetKeyFile(string keyFile)
        {
            return SetProperty(keyFile);
        }

        public bool SetLangVersion(string langVersion)
        {
            return SetProperty(langVersion);
        }

        public bool SetLinkResources(ITaskItem[] linkResources)
        {
            return SetProperty(linkResources);
        }

        public bool SetMainEntryPoint(string targetType, string mainEntryPoint)
        {
            return SetProperty(new[] { targetType, mainEntryPoint });
        }

        public bool SetModuleAssemblyName(string moduleAssemblyName)
        {
            return SetProperty(moduleAssemblyName);
        }

        public bool SetNoConfig(bool noConfig)
        {
            return SetProperty(noConfig);
        }

        public bool SetNoStandardLib(bool noStandardLib)
        {
            return SetProperty(noStandardLib);
        }

        public bool SetOptimize(bool optimize)
        {
            return SetProperty(optimize);
        }

        public bool SetOutputAssembly(string outputAssembly)
        {
            OutputAssembly = outputAssembly;

            return SetProperty(outputAssembly);
        }

        public bool SetPdbFile(string pdbFile)
        {
            PdbFile = pdbFile;

            return SetProperty(pdbFile);
        }

        public bool SetPlatform(string platform)
        {
            return SetProperty(platform);
        }

        public bool SetReferences(ITaskItem[] references)
        {
            return SetProperty(references);
        }

        public bool SetResources(ITaskItem[] resources)
        {
            return SetProperty(resources);
        }

        public bool SetResponseFiles(ITaskItem[] responseFiles)
        {
            return SetProperty(responseFiles);
        }

        public bool SetSources(ITaskItem[] sources)
        {
            return SetProperty(sources);
        }

        public bool SetTargetType(string targetType)
        {
            return SetProperty(targetType);
        }

        public bool SetTreatWarningsAsErrors(bool treatWarningsAsErrors)
        {
            return SetProperty(treatWarningsAsErrors);
        }

        public bool SetWarningLevel(int warningLevel)
        {
            return SetProperty(warningLevel);
        }

        public bool SetWarningsAsErrors(string warningsAsErrors)
        {
            return SetProperty(warningsAsErrors);
        }

        public bool SetWarningsNotAsErrors(string warningsNotAsErrors)
        {
            return SetProperty(warningsNotAsErrors);
        }

        public bool SetWin32Icon(string win32Icon)
        {
            return SetProperty(win32Icon);
        }

        public bool SetWin32Resource(string win32Resource)
        {
            Win32Resource = win32Resource;

            return SetProperty(win32Resource);
        }
    }
}
