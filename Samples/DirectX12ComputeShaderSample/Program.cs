using System.Threading.Tasks;
using DirectX12GameEngine.Graphics;

namespace DirectX12ComputeShaderSample
{
    public class Program
    {
        private static async Task Main()
        {
            using GraphicsDevice device = new GraphicsDevice(FeatureLevel.Level11_0);

            await Test1.RunAsync(device);
            await Test2.RunAsync(device);
        }
    }
}
