using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace DirectX12GameEngine.Serialization
{
    [GlobalTypeConverter(typeof(Vector2))]
    public class Vector2Converter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text)
            {
                float[] vector = Regex.Replace(text, @"\s+", "").Split(',').Select(n => float.Parse(n, culture)).ToArray();

                if (vector.Length == 2)
                {
                    return new Vector2(vector[0], vector[1]);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Vector2 vector)
            {
                FormattableString formattableString = $"{vector.X},{vector.Y}";
                return formattableString.ToString(culture);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [GlobalTypeConverter(typeof(Vector3))]
    public class Vector3Converter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text)
            {
                float[] vector = Regex.Replace(text, @"\s+", "").Split(',').Select(n => float.Parse(n, culture)).ToArray();

                if (vector.Length == 3)
                {
                    return new Vector3(vector[0], vector[1], vector[2]);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Vector3 vector)
            {
                FormattableString formattableString = $"{vector.X},{vector.Y},{vector.Z}";
                return formattableString.ToString(culture);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [GlobalTypeConverter(typeof(Vector4))]
    public class Vector4Converter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text)
            {
                float[] vector = Regex.Replace(text, @"\s+", "").Split(',').Select(n => float.Parse(n, culture)).ToArray();

                if (vector.Length == 4)
                {
                    return new Vector4(vector[0], vector[1], vector[2], vector[3]);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Vector4 vector)
            {
                FormattableString formattableString = $"{vector.X},{vector.Y},{vector.Z},{vector.W}";
                return formattableString.ToString(culture);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [GlobalTypeConverter(typeof(Quaternion))]
    public class QuaternionConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text)
            {
                float[] vector = Regex.Replace(text, @"\s+", "").Split(',').Select(n => float.Parse(n, culture)).ToArray();

                if (vector.Length == 4)
                {
                    return new Quaternion(vector[0], vector[1], vector[2], vector[3]);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Quaternion vector)
            {
                FormattableString formattableString = $"{vector.X},{vector.Y},{vector.Z},{vector.W}";
                return formattableString.ToString(culture);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [GlobalTypeConverter(typeof(Matrix4x4))]
    public class Matrix4x4Converter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text)
            {
                float[] matrix = Regex.Replace(text, @"\s+", "").Split(',').Select(n => float.Parse(n, culture)).ToArray();

                if (matrix.Length == 16)
                {
                    return new Matrix4x4(
                        matrix[0], matrix[1], matrix[2], matrix[3],
                        matrix[4], matrix[5], matrix[6], matrix[7],
                        matrix[8], matrix[9], matrix[10], matrix[11],
                        matrix[12], matrix[13], matrix[14], matrix[15]);
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Matrix4x4 matrix)
            {
                FormattableString formattableString = $"{matrix.M11},{matrix.M12},{matrix.M13},{matrix.M14},{matrix.M21},{matrix.M22},{matrix.M23},{matrix.M24},{matrix.M31},{matrix.M32},{matrix.M33},{matrix.M34},{matrix.M41},{matrix.M42},{matrix.M43},{matrix.M44}";
                return formattableString.ToString(culture);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
