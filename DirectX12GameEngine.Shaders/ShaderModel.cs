using System;

namespace DirectX12GameEngine.Shaders
{
    public readonly struct ShaderModel : IEquatable<ShaderModel>
    {
        public static readonly ShaderModel Model6_0 = new ShaderModel(6, 0);
        public static readonly ShaderModel Model6_1 = new ShaderModel(6, 1);
        public static readonly ShaderModel Model6_2 = new ShaderModel(6, 2);
        public static readonly ShaderModel Model6_3 = new ShaderModel(6, 3);
        public static readonly ShaderModel Model6_4 = new ShaderModel(6, 4);
        public static readonly ShaderModel Model6_5 = new ShaderModel(6, 5);

        public readonly int Major;
        public readonly int Minor;

        public ShaderModel(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ShaderModel))
            {
                return false;
            }

            return Equals((ShaderModel)obj);
        }

        public bool Equals(ShaderModel other)
        {
            return Major == other.Major && Minor == other.Minor;
        }

        public static bool operator ==(ShaderModel left, ShaderModel right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ShaderModel left, ShaderModel right)
        {
            return !(left == right);
        }

		public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Major.GetHashCode();
                hashCode = (hashCode * 397) ^ Minor.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"Major={Major}, Minor={Minor}";
        }
    }
}
