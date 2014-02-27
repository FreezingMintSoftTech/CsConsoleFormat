﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Alba.CsConsoleFormat.Framework.Text;

// TODO Support more complex getter expressions
// TODO Support converters properly (see MS.Internal.Data.DefaultValueConverter)
namespace Alba.CsConsoleFormat.Markup
{
    public class GetExpression
    {
        private CultureInfo _effectiveCulture;

        public object Source { get; set; }
        public string Path { get; set; }
        public string Format { get; set; }
        public Func<object, object> Converter { get; set; }
        public CultureInfo Culture { get; set; }
        public Element TargetObject { get; set; }
        public Type TargetType { get; set; }

        private object EffectiveSource
        {
            get { return Source ?? TargetObject.DataContext; }
        }

        private CultureInfo EffectiveCulture
        {
            get { return _effectiveCulture ?? (_effectiveCulture = Culture ?? TargetObject.EffectiveCulture ?? Thread.CurrentThread.CurrentCulture); }
        }

        public object GetValue ()
        {
            object source = EffectiveSource;
            if (source == null)
                return null;
            if (Path.IsNullOrEmpty())
                return ConvertValue(source);

            object value = source;
            foreach (string propName in Path.Split('.')) {
                if (value == null)
                    return ConvertValue(null);
                PropertyInfo prop = value.GetType().GetProperty(propName);
                if (prop == null)
                    throw new InvalidOperationException("Cannot resolve property '{0}'.".Fmt(propName));
                value = prop.GetValue(value);
            }
            return ConvertValue(value);
        }

        private object ConvertValue (object value)
        {
            if (Converter != null)
                value = Converter(value);
            if (Format != null)
                value = string.Format(EffectiveCulture, Format, value);

            if (value == null) // TODO ???
                return null;
            // Check whether value can be assigned to the property
            Type valueType = value.GetType();
            if (TargetType == valueType || TargetType.IsAssignableFrom(valueType))
                return value;
            // Try converting using Convert class
            if (typeof(IConvertible).IsAssignableFrom(TargetType) && typeof(IConvertible).IsAssignableFrom(valueType))
                return Convert.ChangeType(value, TargetType);
            // Try converting with value's TypeConverter
            TypeConverter valueConverter = TypeDescriptor.GetConverter(valueType);
            if (valueConverter.CanConvertTo(TargetType))
                return valueConverter.ConvertTo(ValueConverterContext.Instance, EffectiveCulture, value, TargetType);
            // Try converting with target's TypeConverter
            TypeConverter targetConverter = TypeDescriptor.GetConverter(TargetType);
            if (targetConverter.CanConvertFrom(valueType))
                return targetConverter.ConvertFrom(ValueConverterContext.Instance, EffectiveCulture, value);

            throw new InvalidOperationException("Cannot convert from '{0}' to '{1}'.".Fmt(valueType, TargetType));
        }

        private class ValueConverterContext : ITypeDescriptorContext
        {
            public static readonly ValueConverterContext Instance = new ValueConverterContext();

            IContainer ITypeDescriptorContext.Container
            {
                get { return null; }
            }

            object ITypeDescriptorContext.Instance
            {
                get { return null; }
            }

            PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor
            {
                get { return null; }
            }

            bool ITypeDescriptorContext.OnComponentChanging ()
            {
                return false;
            }

            void ITypeDescriptorContext.OnComponentChanged ()
            {}

            object IServiceProvider.GetService (Type serviceType)
            {
                return null;
            }
        }
    }
}