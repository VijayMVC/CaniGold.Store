// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace AspDotNetStorefrontCore
{
	/// <summary>
	/// Converts strong types to and from AppConfig string values.
	/// </summary>
	public class AppConfigValueConverter
	{
		public T ConvertAppConfigValueToTypedValue<T>(string value, CultureInfo cultureInfo = null)
		{
			var typedValue = GetTypedValue<T>(value, cultureInfo);
			return typedValue is T
				? (T)typedValue
				: default(T);
		}

		public Tuple<AppConfigType, string> ConvertTypedValueToAppConfigValue<T>(T value)
		{
			var valueType = value.GetType();

			if(typeof(bool).IsAssignableFrom(valueType))
				return Tuple.Create(
					AppConfigType.boolean,
					value.ToString());

			if(typeof(int).IsAssignableFrom(valueType))
				return Tuple.Create(
					AppConfigType.integer,
					value.ToString());

			if(typeof(decimal).IsAssignableFrom(valueType))
				return Tuple.Create(
					AppConfigType.@decimal,
					value.ToString());

			if(typeof(double).IsAssignableFrom(valueType))
				return Tuple.Create(
					AppConfigType.@double,
					value.ToString());

			return Tuple.Create(
				AppConfigType.@string,
				value == null
					? string.Empty
					: value.ToString());
		}

		object GetTypedValue<T>(string value, CultureInfo cultureInfo)
		{
			var type = typeof(T);

			if(typeof(Enum).IsAssignableFrom(type))
				return GetEnumTypedValue<T>(value);

			if(typeof(bool).IsAssignableFrom(type))
				return GetBooleanTypedValue(value);

			if(typeof(int).IsAssignableFrom(type))
				return GetGenericTypedValue<int>(value, cultureInfo);

			if(typeof(decimal).IsAssignableFrom(type))
				return GetGenericTypedValue<decimal>(value, cultureInfo);

			if(typeof(double).IsAssignableFrom(type))
				return GetGenericTypedValue<double>(value, cultureInfo);

			throw new Exception(string.Format("Can't convert appconfig value '{0}' to a {1}.", value, typeof(T).Name));
		}

		object GetEnumTypedValue<T>(string value)
		{
			var type = typeof(T);

			var valueExists = Enum
				.GetNames(type)
				.Contains(value, StringComparer.OrdinalIgnoreCase);

			if(!valueExists)
				return null;

			return Enum.Parse(type, value, ignoreCase: true);
		}

		bool GetBooleanTypedValue(string value)
		{
			return new[]
				{ "true", "yes", "1" }
				.Contains(
					value,
					StringComparer.OrdinalIgnoreCase);
		}

		T GetGenericTypedValue<T>(string value, CultureInfo cultureInfo)
		{
			if(string.IsNullOrEmpty(value))
				return default(T);

			var converter = TypeDescriptor.GetConverter(typeof(T));

			if(!converter.CanConvertFrom(typeof(string)))
				throw new Exception(string.Format("Can't convert appconfig value '{0}' to a {1}.", value, typeof(T).Name));

			return (T)converter.ConvertFromString(null, cultureInfo, value);
		}
	}
}
