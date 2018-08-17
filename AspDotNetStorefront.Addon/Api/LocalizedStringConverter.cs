// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace AspDotNetStorefront.Addon.Api
{
	interface ILocalizedStringConverter
	{
		LocalizedString ParseMlData(string mlData);
		string BuildMlData(LocalizedString localizedString, IEnumerable<string> locales);
	}

	class LocalizedStringConverter : ILocalizedStringConverter
	{
		public LocalizedString ParseMlData(string mlData)
		{
			if(string.IsNullOrEmpty(mlData))
				return LocalizedString.Empty;

			// If there is unlocalized data in the string, flag it as such
			if(!mlData.StartsWith("<ml", StringComparison.OrdinalIgnoreCase))
				return new LocalizedString(
					new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
					{
						[string.Empty] = mlData
					},
					isLocalized: false);

			XElement mlElement;
			try
			{
				mlElement = XElement.Parse(mlData);
			}
			catch
			{
				// Mangled ML XML data is simply lost
				return LocalizedString.Empty;
			}

			return new LocalizedString(
				mlElement
					.Elements("locale")
					.ToDictionary(
						localeElement => (string)localeElement.Attribute("name"),
						localeElement => (string)localeElement,
						StringComparer.OrdinalIgnoreCase),
				isLocalized: true);
		}

		public string BuildMlData(LocalizedString localizedString, IEnumerable<string> locales)
		{
			if(localizedString == null)
				return null;

			IEnumerable<XElement> localeElements;
			if(localizedString.IsLocalized)
			{
				// If the localized string was built from localized data, only pull in the locales that match.
				// Other locales should have empty data.
				localeElements = locales
					.Select(locale => new XElement("locale",
						new XAttribute("name", locale),
						localizedString.Locales.Contains(locale, StringComparer.OrdinalIgnoreCase)
							? localizedString[locale]
							: string.Empty));
			}
			else
			{
				// If the localized string was built from unlocalized data, multiplex the data into every locale
				localeElements = locales
					.Select(locale => new XElement("locale",
						new XAttribute("name", locale),
						localizedString[string.Empty]));
			}

			return new XElement("ml", localeElements).ToString();
		}
	}
}
