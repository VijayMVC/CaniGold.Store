// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;

namespace AspDotNetStorefront.Addon.Api
{
	public class LocalizedString
	{
		public static readonly LocalizedString Empty = new LocalizedString(
			new Dictionary<string, string>
			{
				[string.Empty] = string.Empty
			},
			isLocalized: false);

		public bool IsLocalized { get; }
		public IEnumerable<string> Locales
			=> LocalizedValues.Keys;

		readonly IReadOnlyDictionary<string, string> LocalizedValues;

		public string this[string locale]
		{
			get
			{
				if(string.IsNullOrEmpty(locale))
					return null;

				if(!IsLocalized)
					return LocalizedValues[string.Empty];

				if(LocalizedValues.ContainsKey(locale))
					return LocalizedValues[locale];

				return null;
			}
		}

		public LocalizedString(IReadOnlyDictionary<string, string> localizedValues, bool isLocalized)
		{
			LocalizedValues = localizedValues;
			IsLocalized = isLocalized;
		}
	}
}
