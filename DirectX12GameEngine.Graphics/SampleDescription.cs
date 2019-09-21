namespace DirectX12GameEngine.Graphics
{
    public struct SampleDescription
    {
        public int Count;

        public int Quality;

        public SampleDescription(int count, int quality)
        {
            Count = count;
            Quality = quality;
        }
    }
}
