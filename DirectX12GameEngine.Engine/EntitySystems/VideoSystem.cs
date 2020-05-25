using DirectX12GameEngine.Games;
using DirectX12GameEngine.Graphics;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Playback;

namespace DirectX12GameEngine.Engine
{
    public class VideoSystem : EntitySystem<VideoComponent>
    {
        public VideoSystem(GraphicsDevice device)
        {
            GraphicsDevice = device;
        }

        public GraphicsDevice GraphicsDevice { get; }

        public override void Draw(GameTime gameTime)
        {
            foreach (VideoComponent videoComponent in Components)
            {
                MediaPlayer mediaPlayer = videoComponent.MediaPlayer;

                if (mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                {
                    //if (videoComponent.Target != null)
                    //{
                    //    using Vortice.Direct3D11.ID3D11On12Device device11On12 = ((Vortice.Direct3D11.ID3D11Device)GraphicsDevice.Direct3D11Device).QueryInterface<Vortice.Direct3D11.ID3D11On12Device>();

                    //    var d3D11RenderTarget = device11On12.CreateWrappedResource(
                    //        videoComponent.Target.NativeResource,
                    //        new Vortice.Direct3D11.ResourceFlags { BindFlags = (int)Direct3DBindings.ShaderResource },
                    //        (int)Vortice.Direct3D12.ResourceStates.CopyDestination,
                    //        (int)Vortice.Direct3D12.ResourceStates.CopyDestination);

                    //    using (Vortice.DXGI.IDXGISurface dxgiSurface = d3D11RenderTarget.QueryInterface<Vortice.DXGI.IDXGISurface>())
                    //    {
                    //        IDirect3DSurface surface = Direct3DInterop.CreateDirect3DSurface(dxgiSurface);
                    //        mediaPlayer.CopyFrameToVideoSurface(surface);
                    //    }

                    //    device11On12.ReleaseWrappedResources(d3D11RenderTarget);
                    //}
                }
            }
        }
    }
}
