# DirectX12GameEngine

**DirectX12GameEngine** is a game engine completely written in C# utilizing the Direct3D 12 API for rendering graphics. It supports UWP either rendered directly to the `CoreWindow` or embedded in XAML with a `SwapChainPanel`. It also supports Win32 with WinForms. Some stand-out features are a shader generator that generates HLSL shaders out of .NET code, holographic rendering for HoloLens and Windows Mixed Reality and an editor made with UWP XAML.

![DirectX12GameEngine Editor](DirectX12GameEngine.Editor.png)

## Engine Projects

- **DirectX12GameEngine.Assets:** Assets classes for importing assets like textures, materials and models.
- **DirectX12GameEngine.Core.Assets:** Content manager, serialization of assets.
- **DirectX12GameEngine.Core:** Helper classes and extensions.
- **DirectX12GameEngine.Editor:** A UWP XAML editor for this game engine to handle scene manipulation and changing properties of components.
- **DirectX12GameEngine.Engine:** Main engine project with entity component system.
- **DirectX12GameEngine.Games:** Game base class, dependency injection and window handling.
- **DirectX12GameEngine.Graphics:** Direct3D 12 abstraction and Mixed Reality.
- **DirectX12GameEngine.Input:** Input manager that handles every platform.
- **DirectX12GameEngine.Rendering:** Everything rendering and material related including many classes to define your own custom materials and shaders.
- **DirectX12GameEngine.Shaders:** A .NET to HLSL compiler and HLSL to DXIL shader bytecode compiler.

## Sample Projects

- **DirectX12ComputeShaderSample:** Shows how to write compute shaders in C#.
- **DirectX12Game:** A simple sample scene containing some models and lights and a camera controller. This a library that gets referenced by the projects below where it is really executed.
- **DirectX12CoreWindowApp:** Runs `DirectX12Game` in a UWP app rendering directly to the `CoreWindow`.
- **DirectX12XamlApp:** Runs `DirectX12Game` in a UWP app rendering to a `SwapChainPanel` embedded in a XAML page.
- **DirectX12WinFormsApp:** Runs `DirectX12Game` in a .NET Core 3.0 WinForms app rendering to the Win32 window (`HWND`).

## Prerequisites

- [x] Visual Studio 2019 (with .NET Core preview enabled)
- [x] Windows 10 SDK, version 1903 (build 10.0.18362.0)
- [x] Latest .NET Core 3.0 SDK

Most projects are just using .NET Standard 2.0 or sometimes additionaly .NET Core 3.0 with the UWP SDK included through the NuGet package `Microsoft.Windows.SDK.Contracts`.

## Supported Platforms

- UWP, Windows 10 Fall Creators Update or higher (either `CoreWindow` or XAML)
- WinForms (WPF also supported with XAML Islands)

## Credits

- [Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows) (Managed DirectX bindings)
