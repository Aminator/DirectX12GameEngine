#if WINDOWS_UWP
using System;
using DirectX12GameEngine.Games;
using SharpDX;
using SharpDX.Direct3D12;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Playback;

namespace DirectX12GameEngine.Engine
{
    public class VideoSystem : EntitySystem<VideoComponent>
    {
        public VideoSystem(IServiceProvider services) : base(services)
        {
        }

        public override void Draw(GameTime gameTime)
        {
            foreach (VideoComponent videoComponent in Components)
            {
                MediaPlayer mediaPlayer = videoComponent.MediaPlayer;

                if (mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                {
                    if (videoComponent.Target != null)
                    {
                        //using (SharpDX.Direct3D11.Device11On12 device11On12 = GraphicsDevice.NativeDirect3D11Device.QueryInterface<SharpDX.Direct3D11.Device11On12>())
                        //{
                        //    device11On12.CreateWrappedResource(
                        //        videoComponent.Target.NativeResource,
                        //        new SharpDX.Direct3D11.D3D11ResourceFlags() { BindFlags = (int)Direct3DBindings.ShaderResource },
                        //        (int)ResourceStates.CopyDestination,
                        //        (int)ResourceStates.CopyDestination,
                        //        Utilities.GetGuidFromType(typeof(SharpDX.Direct3D11.Resource)),
                        //        out SharpDX.Direct3D11.Resource d3D11RenderTarget);

                        //    using (SharpDX.DXGI.Resource1 dxgiResource = d3D11RenderTarget.QueryInterface<SharpDX.DXGI.Resource1>())
                        //    using (SharpDX.DXGI.Surface2 dxgiSurface = new SharpDX.DXGI.Surface2(dxgiResource, 0))
                        //    {
                        //        IDirect3DSurface surface = GraphicsDevice.CreateDirect3DSurface(new SharpDX.DXGI.Surface2(dxgiResource, 0));
                        //        mediaPlayer.CopyFrameToVideoSurface(surface);
                        //    }

                        //    device11On12.ReleaseWrappedResources(new[] { d3D11RenderTarget }, 1);
                        //}

                        //mediaPlayer.PlaybackSession.NormalizedSourceRect = new Windows.Foundation.Rect(0.5, 0, 0.5, 1);
                        //mediaPlayer.CopyFrameToVideoSurface(graphicsPresenter.HolographicSurface);
                        //graphicsPresenter.Direct3D11Device.ImmediateContext.CopySubresourceRegion(graphicsPresenter.HolographicBackBuffer, 0, null, graphicsPresenter.HolographicBackBuffer, 1);
                        //mediaPlayer.PlaybackSession.NormalizedSourceRect = new Windows.Foundation.Rect(0, 0, 0.5, 1);
                        //mediaPlayer.CopyFrameToVideoSurface(graphicsPresenter.HolographicSurface);
                    }
                }
            }
        }
    }
}
#endif
