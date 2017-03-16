﻿using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using static Alba.CsConsoleFormat.TypeConverterUtils;

namespace Alba.CsConsoleFormat
{
    /// <summary>
    /// Converts <see cref="Thickness"/> to and from <see cref="string"/> and numeric types:
    /// <list type="bullet">
    /// <item>"1 2 3 4" - <c>new Thickness(1, 2, 3, 4)</c></item>
    /// <item>"1 2" - <c>new Thickness(1, 2)</c> (<c>new Thickness(1, 2, 1, 2)</c>)</item>
    /// <item>"1", 1 - <c>new Thickness(1)</c> (<c>new Thickness(1, 1, 1, 1)</c>)</item>
    /// </list> 
    /// Separator can be " " or ",".
    /// </summary>
    public class ThicknessConverter : TypeConverter
    {
        private static readonly Lazy<ConstructorInfo> ThicknessConstructor = new Lazy<ConstructorInfo>(() =>
            typeof(Thickness).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(int) }));

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
            base.CanConvertFrom(context, sourceType) || IsTypeStringOrNumeric(sourceType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
            base.CanConvertTo(context, destinationType) || destinationType == typeof(InstanceDescriptor);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            switch (value) {
                case string str:
                    return FromString(str);
                case object number when number.IsTypeNumeric():
                    return new Thickness(ToInt(number));
                default:
                    return base.ConvertFrom(context, culture, value);
            }

            Thickness FromString(string str)
            {
                string[] parts = SplitNumbers(str, 4);
                switch (parts.Length) {
                    case 1:
                        return new Thickness(ParseInt(parts[0]));
                    case 2:
                        return new Thickness(ParseInt(parts[0]), ParseInt(parts[1]));
                    case 4:
                        return new Thickness(ParseInt(parts[0]), ParseInt(parts[1]), ParseInt(parts[2]), ParseInt(parts[3]));
                    default:
                        throw new FormatException($"Invalid Thickness format: '{str}'.");
                }
            }
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (!(value is Thickness th))
                throw GetConvertToException(value, destinationType);

            if (destinationType == typeof(string))
                return th.ToString();
            else if (destinationType == typeof(InstanceDescriptor))
                return new InstanceDescriptor(ThicknessConstructor.Value, new object[] { th.Left, th.Top, th.Right, th.Bottom }, true);
            else
                return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}