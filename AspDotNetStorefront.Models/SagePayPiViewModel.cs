// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.ComponentModel.DataAnnotations;
using AspDotNetStorefront.Validation.DataAttribute;

namespace AspDotNetStorefront.Models
{
	public class SagePayPiViewModel
	{
		[Display(Name = "creditCardDetails.cardName.label", Prompt = "creditCardDetails.cardName.example")]
		[Required(ErrorMessage = "creditCardDetails.cardName.required")]
		[HtmlAutocompleteType(HtmlAutocomplete.CcName)]
		[StringLength(100, ErrorMessage = "creditCardDetails.cardName.length")]
		public string Name { get; set; }

		[Display(Name = "creditCardDetails.cardNumber.label", Prompt = "creditCardDetails.cardNumber.example")]
		[Required(ErrorMessage = "creditCardDetails.cardNumber.required")]
		[HtmlAutocompleteType(HtmlAutocomplete.CcNumber)]
		[StringLength(300, ErrorMessage = "creditCardDetails.cardNumber.length")]
		public string Number { get; set; }

		[Display(Name = "creditCardDetails.cardExpirationDate.label", Prompt = "creditCardDetails.cardExpirationDate.example")]
		[Required(ErrorMessage = "creditCardDetails.cardExpirationDate.required")]
		[HtmlAutocompleteType(HtmlAutocomplete.CcExp)]
		[CreditCardFutureExpirationDate(ErrorMessage = "creditCardDetails.cardExpirationDate.inThePast")]
		[RegularExpression(@"\d{1,2}\s*[/-]\s*\d{2}", ErrorMessage = "creditCardDetails.cardExpirationDate.invalid")]
		[StringLength(7, ErrorMessage = "sagepaypi.cardExpirationDate.length")]
		public string ExpirationDate { get; set; }

		[Display(Name = "creditCardDetails.cardCvc.label", Prompt = "creditCardDetails.cardCvc.example")]
		[RequiredIfAppConfigFalse("CardExtraCodeIsOptional", ErrorMessage = "creditCardDetails.cardCvc.required")]
		[HtmlAutocompleteType(HtmlAutocomplete.CcCsc)]
		[StringLength(10, ErrorMessage = "creditCardDetails.cardCvc.length")]
		public string Cvv { get; set; }

		public string MerchantSessionKey { get; }
		public string ScriptUrl { get; }
		public bool ValidateCreditCardNumber { get; }

		public SagePayPiViewModel(
			string merchantSessionKey,
			string scriptUrl,
			bool validateCreditCardNumber)
		{
			MerchantSessionKey = merchantSessionKey;
			ScriptUrl = scriptUrl;
			ValidateCreditCardNumber = validateCreditCardNumber;
		}
	}

	public class SagePayPiThreeDSecureViewModel
	{
		public string PaReq { get; }
		public string TermUrl { get; }
		public string Md { get; }
		public string AcsUrl { get; }

		public SagePayPiThreeDSecureViewModel(
			string paReq,
			string termUrl,
			string md,
			string acsUrl)
		{
			PaReq = paReq;
			TermUrl = termUrl;
			Md = md;
			AcsUrl = acsUrl;
		}
	}
}
