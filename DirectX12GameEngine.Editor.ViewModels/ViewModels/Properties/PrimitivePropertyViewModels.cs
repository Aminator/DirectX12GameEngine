using System;
using System.Reflection;

namespace DirectX12GameEngine.Editor.ViewModels.Properties
{
    public class NullPropertyViewModel : PropertyViewModel
    {
        public NullPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
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

    public class DecimalPropertyViewModel : PropertyViewModel<decimal>
    {
        public DecimalPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
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

    public class EnumPropertyViewModel : PropertyViewModel<Enum>
    {
        private Array? values;

        public EnumPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }

        public Array Values => values ?? (values = Enum.GetValues(Type));
    }

    public class GuidPropertyViewModel : PropertyViewModel<Guid>
    {
        public GuidPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class TimeSpanPropertyViewModel : PropertyViewModel<TimeSpan>
    {
        public TimeSpanPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class DateTimePropertyViewModel : PropertyViewModel<DateTime>
    {
        public DateTimePropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }

    public class DateTimeOffsetPropertyViewModel : PropertyViewModel<DateTimeOffset>
    {
        public DateTimeOffsetPropertyViewModel(object model, PropertyInfo propertyInfo) : base(model, propertyInfo)
        {
        }
    }
}
