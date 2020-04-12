using System;
using System.Threading;
using DirectX12GameEngine.Graphics;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace DirectX12GameEngine.Engine
{
    [DefaultEntitySystem(typeof(VideoSystem))]
    public class VideoComponent : EntityComponent
    {
        private Uri? sourceUri;

        public VideoComponent()
        {
            MediaPlayer.VideoFrameAvailable += (s, e) => FrameEvent.Set();
        }

        public AutoResetEvent FrameEvent { get; } = new AutoResetEvent(false);

        public MediaPlayer MediaPlayer { get; } = new MediaPlayer { IsVideoFrameServerEnabled = true };

        public bool AutoPlay { get => MediaPlayer.AutoPlay; set => MediaPlayer.AutoPlay = value; }

        public Uri? SourceUri
        {
            get => sourceUri;
            set
            {
                sourceUri = value;
                MediaPlayer.Source = MediaSource.CreateFromUri(sourceUri);
            }
        }

        public Texture? Target { get; set; }
    }
}
