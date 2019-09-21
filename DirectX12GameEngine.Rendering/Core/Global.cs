namespace DirectX12GameEngine.Rendering.Core
{
    public static class Global
    {
        public static float ElapsedTime;

        public static float TotalTime;
    }

    public struct GlobalBuffer
    {
        public float ElapsedTime { get; set; }

        public float TotalTime { get; set; }
    }
}
