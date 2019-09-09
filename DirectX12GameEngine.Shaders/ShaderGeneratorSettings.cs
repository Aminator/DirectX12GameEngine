using System;
using System.Collections.Generic;
using System.Reflection;

namespace DirectX12GameEngine.Shaders
{
    public class ShaderGeneratorSettings
    {
        public ShaderGeneratorSettings(params Attribute[] entryPointAttributes)
        {
            EntryPointAttributes = entryPointAttributes;
        }

        public IEnumerable<Attribute> EntryPointAttributes { get; set; }

        public BindingFlags BindingFlagsWithContract { get; set; } = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        public BindingFlags BindingFlagsWithoutContract { get; set; } = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
    }
}
