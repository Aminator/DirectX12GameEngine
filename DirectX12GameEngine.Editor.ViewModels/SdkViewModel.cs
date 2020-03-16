using System;
using DirectX12GameEngine.Mvvm;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SdkViewModel : ViewModelBase, IEquatable<SdkViewModel?>
    {
        private double downloadProgess;
        private double installProgess;

        public SdkViewModel(Version version) : this(version, null)
        {
        }

        public SdkViewModel(Version version, string? path)
        {
            Version = version;
            Path = path;
        }

        public Version Version { get; }

        public string? Path { get; set; }

        public double DownloadProgess
        {
            get => downloadProgess;
            set => Set(ref downloadProgess, value);
        }

        public double InstallProgess
        {
            get => installProgess;
            set => Set(ref installProgess, value);
        }

        public override bool Equals(object obj)
        {
            if (obj is SdkViewModel entry)
            {
                return Equals(entry);
            }

            return false;
        }

        public bool Equals(SdkViewModel? other)
        {
            return Version == other?.Version;
        }

        public override int GetHashCode() => Version.GetHashCode();

        public override string ToString() => Version.ToString();

        public static bool operator ==(SdkViewModel? left, SdkViewModel? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(SdkViewModel? left, SdkViewModel? right) => !(left == right);
    }
}
