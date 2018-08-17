// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Web.Mvc;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Classes
{
	public class AddressSelectListBuilder
	{
		const string SelectListValueField = "Value";
		const string SelectListDataField = "Text";

		public SelectList BuildResidenceTypeSelectList(string selectedValue)
		{
			return new SelectList(
				items: Enum.GetNames(typeof(ResidenceTypes))
					.Where(residenceType => residenceType != ResidenceTypes.Unknown.ToString())
					.Select(residenceType => new SelectListItem
					{
						Text = residenceType,
						Value = residenceType
					}),
				dataValueField: SelectListValueField,
				dataTextField: SelectListDataField,
				selectedValue: selectedValue);
		}

		public SelectList BuildStateSelectList(string country, string selectedValue = null)
		{
			if(string.IsNullOrEmpty(country))
				throw new ArgumentException("Country can not be null");

			return new SelectList(
				items: State
					.GetAllStatesForCountry(AppLogic.GetCountryID(country))
					.Select(state => new SelectListItem
					{
						Text = state.Name,
						Value = state.Abbreviation
					}),
				dataValueField: SelectListValueField,
				dataTextField: SelectListDataField,
				selectedValue: selectedValue);
		}

		public SelectList BuildCountrySelectList(string selectedValue = null)
		{
			var countries = Country.GetAll(
				published: true);

			if(countries.Count == 0)
				throw new ArgumentException("No published countries found");

			if(string.IsNullOrEmpty(selectedValue))
			{
				var defaultCountry = AppLogic.AppConfig("Address.Country.Default");
				selectedValue = countries
				  .Where(c => StringComparer.OrdinalIgnoreCase.Equals(c.TwoLetterIsoCode, defaultCountry))
				  .Select(c => c.Name)
				  .FirstOrDefault()
				  ?? countries
					.Select(c => c.Name)
					.FirstOrDefault();
			}
			else
			{
				selectedValue = countries
				  .Where(c => StringComparer.OrdinalIgnoreCase.Equals(c.Name, selectedValue))
				  .Select(c => c.Name)
				  .FirstOrDefault()
				  ?? countries
					.Select(c => c.Name)
					.FirstOrDefault();
			}

			return new SelectList(
				items: countries
					.Select(country => new SelectListItem
					{
						Text = country.Name,
						Value = country.Name
					}),
				dataValueField: SelectListValueField,
				dataTextField: SelectListDataField,
				selectedValue: selectedValue);
		}
	}
}
