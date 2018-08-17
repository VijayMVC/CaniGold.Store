// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace AspDotNetStorefrontCore
{
	public class CreditCardValidationExpression
	{
		readonly IReadOnlyDictionary<CardType, string> ValidationExpressions = new Dictionary<CardType, string>
		{
			{ CardType.Amex, "^3[47][0-9]{13}$" },
			{ CardType.Visa, "^4[0-9]{12}(?:[0-9]{3})?$" },
			{ CardType.MasterCard, "^5[1-5]\\d{2}-?\\d{4}-?\\d{4}-?\\d{4}$|^2(?:2(?:2[1-9]|[3-9]\\d)|[3-6]\\d\\d|7(?:[01]\\d|20))-?\\d{4}-?\\d{4}-?\\d{4}$" },
			{ CardType.Discover, "^6(?:011|5[0-9]{2})[0-9]{12}$" }
		};

		public string GetExpression(CardType cardType)
		{
			if(!ValidationExpressions.ContainsKey(cardType))
				throw new InvalidOperationException("There are no validation expressions for the specified card type.");

			return ValidationExpressions[cardType];
		}
	}
}
