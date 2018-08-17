// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Checkout
{
	public class CreditCardValidationProvider : ICreditCardValidationProvider
	{
		readonly CreditCardTypeProvider CreditCardTypeProvider;

		public CreditCardValidationProvider(CreditCardTypeProvider creditCardTypeProvider)
		{
			CreditCardTypeProvider = creditCardTypeProvider;
		}

		/// <summary>
		/// Runs credit card details through a series of checks, returning a collection of field-level errors.
		/// </summary>
		/// <param name="configuration">AppConfigs and similar configuration.</param>
		/// <param name="context">Credit card details to be validated.</param>
		/// <returns>A <see cref="CreditCardValidationResult"/> containing any validation errors.</returns>
		public CreditCardValidationResult ValidateCreditCard(CreditCardValidationConfiguration configuration, CreditCardValidationContext context)
		{
			var fieldErrors = ValidateFields(configuration, context);
			return new CreditCardValidationResult(
				valid: !fieldErrors.Any(),
				fieldErrors: fieldErrors.ToLookup(
					kvp => kvp.Key,
					kvp => kvp.Value));
		}

		IEnumerable<KeyValuePair<CreditCardValidationField, string>> ValidateFields(CreditCardValidationConfiguration configuration, CreditCardValidationContext context)
		{
			var acceptedCardType = CreditCardTypeProvider
				.GetAcceptedCreditCardTypes()
				.Contains(context.CardType, StringComparer.OrdinalIgnoreCase);

			if(!acceptedCardType)
				yield return new KeyValuePair<CreditCardValidationField, string>(CreditCardValidationField.CardType, AppLogic.GetString("creditCardDetails.cardType.invalid"));

			var missingRequiredIssueNumber =
				CreditCardTypeProvider
					.GetCardTypesRequiringIssueNumber()
					.Contains(context.CardType, StringComparer.OrdinalIgnoreCase)
				&& string.IsNullOrEmpty(context.IssueNumber);

			if(missingRequiredIssueNumber)
				yield return new KeyValuePair<CreditCardValidationField, string>(CreditCardValidationField.IssueNumber, AppLogic.GetString("creditCardDetails.cardIssueNumber.required"));

			if(configuration.ValidateCreditCardNumber)
			{
				var cardType = CardType.Parse(context.CardType);
				if(cardType != null) // Only validate known card types
				{
					var creditCardValidator = new CreditCardValidator(context.Number, cardType);
					if(!creditCardValidator.Validate())
					{
						yield return new KeyValuePair<CreditCardValidationField, string>(CreditCardValidationField.Number, AppLogic.GetString("creditCardDetails.cardNumber.format"));
					}
				}
			}

			var missingExpirationDate = context.ExpirationDate == null;

			if(missingExpirationDate)
				yield return new KeyValuePair<CreditCardValidationField, string>(CreditCardValidationField.ExpirationDate, AppLogic.GetString("creditCardDetails.cardExpirationDate.required"));

			var missingRequiredCardExtraCode =
				!configuration.CardExtraCodeIsOptional
				&& string.IsNullOrEmpty(context.Cvv);

			if(missingRequiredCardExtraCode)
				yield return new KeyValuePair<CreditCardValidationField, string>(CreditCardValidationField.Cvv, AppLogic.GetString("address.cs.29"));
		}
	}
}
