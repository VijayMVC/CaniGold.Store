// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Web.Mvc;

namespace AspDotNetStorefrontCore
{
	public class ECheckAccountTypeProvider
	{
		public SelectList GetECheckAccountTypesSelectList(string selectedValue = null)
		{
			return new SelectList(
				items: new Dictionary<string, string>
					{
						{ "Checking", "checking" },
						{ "Savings", "savings" },
						{ "Business Checking", "businessChecking" }
					},
				dataValueField: "Value",
				dataTextField: "Key",
				selectedValue: selectedValue);
		}
	}
}
