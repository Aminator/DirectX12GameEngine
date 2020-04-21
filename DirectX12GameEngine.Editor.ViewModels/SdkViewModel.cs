using System;
using Microsoft.Toolkit.Mvvm.ObjectModel;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class SdkViewModel : ObservableObject, IEquatable<SdkViewModel?>
    {
        private double downloadProgess;
        private double installProgess;

        public SdkViewModel(string version) : this(version, null)
        {
        }

        public SdkViewModel(string version, string? path)
        {
            Version = version;
            Path = path;
        }

        public string Version { get; }

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
