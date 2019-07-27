using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DirectX12GameEngine.Editor.ViewModels.Properties
{
    public class NullPropertyViewModel : PropertyViewModel
    {
        public NullPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class EnumPropertyViewModel : PropertyViewModel<Enum>
    {
        public EnumPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
            Values = Enum.GetValues(Value.GetType());
        }

        public Array Values { get; }

        protected override void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            base.NotifyPropertyChanged(propertyName);

            if (propertyName == nameof(Value))
            {
            }
        }
    }

    public class CharPropertyViewModel : PropertyViewModel<char>
    {
        public CharPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class StringPropertyViewModel : PropertyViewModel<string>
    {
        public StringPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class BooleanPropertyViewModel : PropertyViewModel<bool>
    {
        public BooleanPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class DecimalPropertyViewModel : PropertyViewModel<decimal>
    {
        public DecimalPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class SinglePropertyViewModel : PropertyViewModel<float>
    {
        public SinglePropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class DoublePropertyViewModel : PropertyViewModel<double>
    {
        public DoublePropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class BytePropertyViewModel : PropertyViewModel<byte>
    {
        public BytePropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class SBytePropertyViewModel : PropertyViewModel<sbyte>
    {
        public SBytePropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class Int16PropertyViewModel : PropertyViewModel<short>
    {
        public Int16PropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class UInt16PropertyViewModel : PropertyViewModel<ushort>
    {
        public UInt16PropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class Int32PropertyViewModel : PropertyViewModel<int>
    {
        public Int32PropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class UInt32PropertyViewModel : PropertyViewModel<uint>
    {
        public UInt32PropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class Int64PropertyViewModel : PropertyViewModel<long>
    {
        public Int64PropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class UInt64PropertyViewModel : PropertyViewModel<ulong>
    {
        public UInt64PropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class Vector3PropertyViewModel : PropertyViewModel<Vector3>
    {
        public Vector3PropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }

        public float X
        {
            get => Value.X;
            set => Set(X, value, () => Value = new Vector3(value, Y, Z));
        }

        public float Y
        {
            get => Value.Y;
            set => Set(Y, value, () => Value = new Vector3(X, value, Z));
        }

        public float Z
        {
            get => Value.Z;
            set => Set(Z, value, () => Value = new Vector3(X, Y, value));
        }

        protected override void OnOwnerPropertyChanged()
        {
            base.OnOwnerPropertyChanged();

            NotifyPropertyChanged(nameof(X));
            NotifyPropertyChanged(nameof(Y));
            NotifyPropertyChanged(nameof(Z));
        }
    }

    public class Vector4PropertyViewModel : PropertyViewModel<Vector4>
    {
        public Vector4PropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }

        public float X
        {
            get => Value.X;
            set => Set(X, value, () => Value = new Vector4(value, Y, Z, W));
        }

        public float Y
        {
            get => Value.Y;
            set => Set(Y, value, () => Value = new Vector4(X, value, Z, W));
        }

        public float Z
        {
            get => Value.Z;
            set => Set(Z, value, () => Value = new Vector4(X, Y, value, W));
        }

        public float W
        {
            get => Value.W;
            set => Set(W, value, () => Value = new Vector4(X, Y, Z, value));
        }

        protected override void OnOwnerPropertyChanged()
        {
            base.OnOwnerPropertyChanged();

            NotifyPropertyChanged(nameof(X));
            NotifyPropertyChanged(nameof(Y));
            NotifyPropertyChanged(nameof(Z));
            NotifyPropertyChanged(nameof(W));
        }
    }

    public class QuaternionPropertyViewModel : PropertyViewModel<Quaternion>
    {
        public QuaternionPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }

        public float X
        {
            get => Value.X;
            set => Set(X, value, () => Value = new Quaternion(value, Y, Z, W));
        }

        public float Y
        {
            get => Value.Y;
            set => Set(Y, value, () => Value = new Quaternion(X, value, Z, W));
        }

        public float Z
        {
            get => Value.Z;
            set => Set(Z, value, () => Value = new Quaternion(X, Y, value, W));
        }

        public float W
        {
            get => Value.W;
            set => Set(W, value, () => Value = new Quaternion(X, Y, Z, value));
        }

        protected override void OnOwnerPropertyChanged()
        {
            base.OnOwnerPropertyChanged();

            NotifyPropertyChanged(nameof(X));
            NotifyPropertyChanged(nameof(Y));
            NotifyPropertyChanged(nameof(Z));
            NotifyPropertyChanged(nameof(W));
        }
    }
}
