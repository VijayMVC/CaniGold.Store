// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Web.Mvc;
using AspDotNetStorefront.Caching.ObjectCaching;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Classes;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Payment.Wallet;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways;

namespace AspDotNetStorefront.Controllers
{
	[SecureAccessFilter(forceHttps: true)]
	public class CheckoutCreditCardController : Controller
	{
		readonly ICachedShoppingCartProvider CachedShoppingCartProvider;
		readonly CreditCardTypeProvider CreditCardTypeProvider;
		readonly ICreditCardValidationProvider CreditCardValidationProvider;
		readonly NoticeProvider NoticeProvider;
		readonly IPaymentOptionProvider PaymentOptionProvider;
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;
		readonly IWalletProvider WalletProvider;

		public CheckoutCreditCardController(
			ICachedShoppingCartProvider cachedShoppingCartProvider,
			CreditCardTypeProvider creditCardTypeProvider,
			ICreditCardValidationProvider creditCardValidationProvider,
			NoticeProvider noticeProvider,
			IPaymentOptionProvider paymentOptionProvider,
			IPersistedCheckoutContextProvider persistedCheckoutContextProvider,
			IWalletProvider walletProvider)
		{
			CachedShoppingCartProvider = cachedShoppingCartProvider;
			CreditCardTypeProvider = creditCardTypeProvider;
			CreditCardValidationProvider = creditCardValidationProvider;
			NoticeProvider = noticeProvider;
			PaymentOptionProvider = paymentOptionProvider;
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
			WalletProvider = walletProvider;
		}

		[PageTypeFilter(PageTypes.Checkout)]
		[HttpGet]
		[ImportModelStateFromTempData]
		public ActionResult CreditCard()
		{
			var customer = HttpContext.GetCustomer();

			if(!PaymentOptionProvider.PaymentMethodSelectionIsValid(AppLogic.ro_PMCreditCard, customer))
			{
				NoticeProvider.PushNotice(
					message: AppLogic.GetString("checkout.paymentmethodnotallowed"),
					type: NoticeType.Failure);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			//Decide which form to display
			if(AppLogic.ActivePaymentGatewayCleaned() == Gateway.ro_GWBRAINTREE)
			{
				var processor = GatewayLoader.GetProcessor(Gateway.ro_GWBRAINTREE);

				var clientToken = processor.ObtainBraintreeToken();

				if(string.IsNullOrEmpty(clientToken))
				{
					NoticeProvider.PushNotice(AppLogic.GetString("braintree.creditcardunavailable"), NoticeType.Failure);
					return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
				}

				var braintreeModel = new BraintreeViewModel(token: clientToken,
					scriptUrl: AppLogic.AppConfig("Braintree.ScriptUrl"));

				return View(ViewNames.BraintreeCreditCard, braintreeModel);
			}
			else if(AppLogic.ActivePaymentGatewayCleaned() == Gateway.ro_GWACCEPTJS)
			{
				var liveMode = AppLogic.AppConfigBool("UseLiveTransactions");
				var cart = CachedShoppingCartProvider.Get(customer, CartTypeEnum.ShoppingCart, AppLogic.StoreID());

				var acceptJsModel = new AcceptJsViewModel(
					clientKey: liveMode
						? AppLogic.AppConfig("AcceptJs.Live.ClientKey")
						: AppLogic.AppConfig("AcceptJs.Test.ClientKey"),
					apiLoginId: liveMode
						? AppLogic.AppConfig("AcceptJs.Live.ApiLoginId")
						: AppLogic.AppConfig("AcceptJs.Test.ApiLoginId"),
					scriptUrlHostedForm: liveMode
						? AppLogic.AppConfig("AcceptJs.Form.Hosted.Live.Url")
						: AppLogic.AppConfig("AcceptJs.Form.Hosted.Test.Url"),
					scriptUrlOwnForm: liveMode
						? AppLogic.AppConfig("AcceptJs.Form.Own.Live.Url")
						: AppLogic.AppConfig("AcceptJs.Form.Own.Test.Url"));


				return View(ViewNames.AcceptJsCreditCard, acceptJsModel);
			}
			else if(AppLogic.ActivePaymentGatewayCleaned() == Gateway.ro_GWSAGEPAYPI)
			{
				var processor = (ISagePayPiGatewayProcessor)GatewayLoader.GetProcessor(Gateway.ro_GWSAGEPAYPI);

				var clientMerchantSessionKey = processor.ObtainSagePayPiMerchantSessionKey();

				if(string.IsNullOrEmpty(clientMerchantSessionKey))
				{
					NoticeProvider.PushNotice(AppLogic.GetString("sagepaypi.creditcardunavailable"), NoticeType.Failure);
					return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
				}

				var sagePayPiModel = new SagePayPiViewModel(merchantSessionKey: clientMerchantSessionKey,
					scriptUrl: AppLogic.AppConfigBool("UseLiveTransactions")
						? AppLogic.AppConfig("SagePayPi.LiveScriptUrl")
						: AppLogic.AppConfig("SagePayPi.TestScriptUrl"),
					validateCreditCardNumber: AppLogic.AppConfigBool("ValidateCreditCardNumbers"));

				return View(ViewNames.SagePayPiCreditCard, sagePayPiModel);
			}
			else
			{
				var ccModel = BuildCheckoutCreditCardViewModel(customer);
				return View(ViewNames.CreditCard, ccModel);
			}
		}

		[HttpPost]
		[ExportModelStateToTempData]
		public ActionResult CreditCard(CheckoutCreditCardViewModel model)
		{
			// Convert model fields into validatable values
			var customer = HttpContext.GetCustomer();
			var persistedCheckoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);

			var number = !string.IsNullOrEmpty(model.Number) && model.Number.StartsWith("•") && persistedCheckoutContext.CreditCard != null
				? persistedCheckoutContext.CreditCard.Number
				: model.Number.Replace(" ", "");
			var issueNumber = !string.IsNullOrEmpty(model.IssueNumber) && model.IssueNumber.StartsWith("•") && persistedCheckoutContext.CreditCard != null
				? persistedCheckoutContext.CreditCard.IssueNumber
				: model.IssueNumber;
			var expirationDate = ParseMonthYearString(model.ExpirationDate);
			var startDate = ParseMonthYearString(model.StartDate);
			var cvv = !string.IsNullOrEmpty(model.Cvv) && model.Cvv.StartsWith("•") && persistedCheckoutContext.CreditCard != null
				? persistedCheckoutContext.CreditCard.Cvv
				: model.Cvv;

			// Run server-side credit card validation
			var validationConfiguration = new CreditCardValidationConfiguration(
				validateCreditCardNumber: AppLogic.AppConfigBool("ValidateCreditCardNumbers"),
				showCardStartDateFields: AppLogic.AppConfigBool("ShowCardStartDateFields"),
				cardExtraCodeIsOptional: AppLogic.AppConfigBool("CardExtraCodeIsOptional"));

			var validationContext = new CreditCardValidationContext(
				cardType: model.CardType,
				number: number,
				issueNumber: issueNumber,
				expirationDate: expirationDate,
				startDate: startDate,
				cvv: cvv);

			var validationResult = CreditCardValidationProvider.ValidateCreditCard(validationConfiguration, validationContext);

			// Update the ModelState with any validation issues
			if(!validationResult.Valid)
				foreach(var field in validationResult.FieldErrors)
					foreach(var error in field)
						// This assumes that the model properties and the credit card validation field enum names match perfectly
						ModelState.AddModelError(field.Key.ToString(), error);

			// Use POST redirect GET if there are any issues
			if(!ModelState.IsValid)
				return RedirectToAction(ActionNames.CreditCard, ControllerNames.CheckoutCreditCard);

			// Save the validated credit card details into the persisted checkout state
			var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(persistedCheckoutContext)
				.WithCreditCard(new CreditCardDetails(
					name: model.Name,
					number: number,
					issueNumber: issueNumber,
					cardType: model.CardType,
					expirationDate: expirationDate,
					startDate: startDate,
					cvv: cvv))
				.WithoutAmazonPayments()
				.WithoutOffsiteRequiredBillingAddressId()
				.WithoutOffsiteRequiredShippingAddressId()
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);

			// Save the StoreCCInDB setting if it was shown to the customer & their choice changed
			var siteIsStoringCCs = AppLogic.AppConfigBool("StoreCCInDB");

			if(siteIsStoringCCs && model.SaveCreditCardNumber != customer.StoreCCInDB)
				customer.UpdateCustomer(storeCreditCardInDb: siteIsStoringCCs && model.SaveCreditCardNumber);

			// Update the customer record
			if(customer.RequestedPaymentMethod != AppLogic.ro_PMCreditCard)
				customer.UpdateCustomer(requestedPaymentMethod: AppLogic.ro_PMCreditCard);

			try
			{
				if(WalletProvider.WalletsAreEnabled() && model.SaveToWallet)
					WalletProvider.CreatePaymentProfile(
						customer: customer,
						billingAddress: customer.PrimaryBillingAddress,
						cardType: model.CardType,
						number: number,
						cvv: cvv,
						expirationDate: expirationDate.Value);
			}
			catch(WalletException walletException)
			{
				ModelState.AddModelError("SaveToWallet", walletException.Message);
				return RedirectToAction(ActionNames.CreditCard, ControllerNames.CheckoutCreditCard);
			}

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}

		public ActionResult CreditCardDetail()
		{
			var customer = HttpContext.GetCustomer();

			var showCardStartDateFields = AppLogic.AppConfigBool("ShowCardStartDateFields");

			var walletsAreEnabled = customer.IsRegistered && WalletProvider.WalletsAreEnabled();
			var displayWalletCards = walletsAreEnabled && WalletProvider.GetPaymentProfiles(customer).Any();

			string name = customer.FullName(),
				number = null,
				cardType = null,
				expirationDate = null,
				startDate = null;

			var checkoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);
			if(checkoutContext.CreditCard != null)
			{
				name = checkoutContext.CreditCard.Name ?? customer.FullName();

				number = GetCreditCardNumberForDisplay(checkoutContext.CreditCard.Number);

				cardType = checkoutContext.CreditCard.CardType;

				expirationDate = GetCreditCardDateForDisplay(checkoutContext.CreditCard.ExpirationDate);

				startDate = GetCreditCardDateForDisplay(checkoutContext.CreditCard.StartDate);
			}

			return PartialView(ViewNames.CreditCardDetailPartial, new CheckoutCreditCardViewModel
			{
				Name = name,
				Number = number,
				CardType = cardType,
				ExpirationDate = expirationDate,
				StartDate = startDate,
				ShowStartDate = showCardStartDateFields,
				WalletsAreEnabled = walletsAreEnabled,
				DisplayWalletCards = displayWalletCards,
				LastFour = (!string.IsNullOrEmpty(number) && number.Length > 4)
					? number.Substring(number.Length - 4)
					: null,
				CardImage = !string.IsNullOrEmpty(cardType)
					? DisplayTools.GetCardImage(
						imagePath: Url.SkinUrl("images/"),
						cardName: cardType)
					: Url.SkinUrl("images/genericCard.gif")
			});
		}

		string GetCreditCardNumberForDisplay(string cardNumber)
		{
			return !string.IsNullOrEmpty(cardNumber)
				? string.Format(
					"•••• •••• •••• {0}",
					cardNumber.Substring(
						Math.Max(0, cardNumber.Length - 4),
						Math.Min(4, cardNumber.Length)))
				: null;
		}

		string GetCreditCardDateForDisplay(DateTime? date)
		{
			return date != null
				? string.Format(
					"{0} / {1}",
					date.Value.Month,
					date.Value.ToString("yy"))
				: null;
		}

		/// <summary>
		/// Converts a mm/yy formatted string to a DateTime?
		/// </summary>
		/// <param name="monthYear">A string formatted as mm/yy or mm/yyyy</param>
		/// <returns>A DateTime if the value could be parsed, or null otherwise</returns>
		DateTime? ParseMonthYearString(string monthYear)
		{
			if(string.IsNullOrEmpty(monthYear))
				return null;

			var splitValues = monthYear
				.Split(
					new[] { '/', '-' },
					StringSplitOptions.RemoveEmptyEntries)
				.Select(value => value.Trim())
				.ToArray();

			if(splitValues.Length != 2)
				return null;

			int month;
			if(!int.TryParse(splitValues[0], out month))
				return null;

			int year;
			if(!int.TryParse(splitValues[1], out year))
				return null;

			// If it's a two digit year, assume it's this century.
			// Probable Y2.1K bug.
			var yearOffset = year < 100
				? 2000
				: 0;

			try
			{
				return new DateTime(yearOffset + year, month, 1);
			}
			catch
			{
				return null;
			}
		}

		CheckoutCreditCardViewModel BuildCheckoutCreditCardViewModel(Customer customer)
		{
			var walletsAreEnabled = customer.IsRegistered && WalletProvider.WalletsAreEnabled();
			var displayWalletCards = walletsAreEnabled && WalletProvider.GetPaymentProfiles(customer).Any();

			var creditCardTypeListItems = CreditCardTypeProvider
				.GetAcceptedCreditCardTypes()
				.Select(creditCardType => new SelectListItem
				{
					Text = creditCardType,
					Value = creditCardType.ToUpper(),
				});

			var showIssueNumber = CreditCardTypeProvider
				.GetAcceptedCreditCardTypes()
				.Intersect(
					CreditCardTypeProvider.GetCardTypesRequiringIssueNumber(),
					StringComparer.OrdinalIgnoreCase)
				.Any();


			string name = customer.FullName(),
				number = null,
				cardType = null,
				issueNumber = null,
				expirationDate = null,
				startDate = null,
				cvv = null;

			var checkoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);
			if(checkoutContext.CreditCard != null)
			{
				name = checkoutContext.CreditCard.Name ?? customer.FullName();

				number = GetCreditCardNumberForDisplay(checkoutContext.CreditCard.Number);

				cardType = checkoutContext.CreditCard.CardType;

				issueNumber = !string.IsNullOrEmpty(checkoutContext.CreditCard.IssueNumber)
					? "••••"
					: null;

				expirationDate = GetCreditCardDateForDisplay(checkoutContext.CreditCard.ExpirationDate);

				startDate = GetCreditCardDateForDisplay(checkoutContext.CreditCard.StartDate);

				cvv = !string.IsNullOrEmpty(checkoutContext.CreditCard.Cvv)
					? "•••"
					: null;
			}

			return new CheckoutCreditCardViewModel
			{
				Name = name,
				Number = number,
				CardType = cardType,
				IssueNumber = issueNumber,
				ExpirationDate = expirationDate,
				StartDate = startDate,
				Cvv = cvv,
				CardTypes = creditCardTypeListItems,
				ShowStartDate = AppLogic.AppConfigBool("ShowCardStartDateFields"),
				ShowIssueNumber = showIssueNumber,
				ShowSaveCreditCardNumber = AppLogic.AppConfigBool("StoreCCInDB"),
				SaveCreditCardNumber = customer.StoreCCInDB,
				ValidateCreditCardNumber = AppLogic.AppConfigBool("ValidateCreditCardNumbers"),
				WalletsAreEnabled = walletsAreEnabled,
				DisplayWalletCards = displayWalletCards,
			};
		}
	}
}
