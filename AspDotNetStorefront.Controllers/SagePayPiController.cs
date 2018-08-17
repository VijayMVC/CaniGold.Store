// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Linq;
using System.Web.Mvc;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Routing;
using AspDotNetStorefront.StringResource;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways;
using AspDotNetStorefrontGateways.Processors;

namespace AspDotNetStorefront.Controllers
{
	public class SagePayPiController : Controller
	{
		readonly NoticeProvider NoticeProvider;
		readonly IPaymentOptionProvider PaymentOptionProvider;
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;
		readonly IStringResourceProvider StringResourceProvider;
		readonly AppConfigProvider AppConfigProvider;

		public SagePayPiController(
			NoticeProvider noticeProvider,
			IPaymentOptionProvider paymentOptionProvider,
			IPersistedCheckoutContextProvider persistedCheckoutContextProvider,
			IStringResourceProvider stringResourceProvider,
			AppConfigProvider appConfigProvider)
		{
			NoticeProvider = noticeProvider;
			PaymentOptionProvider = paymentOptionProvider;
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
			StringResourceProvider = stringResourceProvider;
			AppConfigProvider = appConfigProvider;
		}

		[HttpPost]
		[ExportModelStateToTempData]
		public ActionResult SagePayPiCreditCard(FormCollection collection)
		{
			var cardErrorSegments = collection["sagePayPiCardError"]
				.ParseAsDelimitedList('|');

			if(cardErrorSegments.FirstOrDefault() == "ERROR")
			{
				var error = cardErrorSegments
					.Skip(1)
					.FirstOrDefault();

				if(string.IsNullOrEmpty(error) || error.Contains("\"httpErrorCode\":401"))
				{
					NoticeProvider.PushNotice(StringResourceProvider.GetString("sagepaypi.payment.addingdetailserror"), NoticeType.Failure);
					return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
				}

				var sagePayPi = new SagePayPi();
				var errorObject = Newtonsoft.Json.Linq.JObject.Parse(error);
				var errorDetails = sagePayPi.GetResponseError(errorObject, "errors");
				var errorMessage = string.Format("{0} {1}", StringResourceProvider.GetString("sagepaypi.payment.carderrorprompt"), errorDetails);

				NoticeProvider.PushNotice(errorMessage, NoticeType.Failure);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			var customer = HttpContext.GetCustomer();
			var session = new CustomerSession(customer.CustomerID);
			session[AppLogic.SagePayPiMerchantSessionKey] = collection["sagePayPiMerchantSessionKey"];

			var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(PersistedCheckoutContextProvider.LoadCheckoutContext(customer))
				.WithCreditCard(new CreditCardDetails(
					name: null,
					number: null,
					issueNumber: null,
					cardType: collection["sagePayPiCardType"],
					expirationDate: null,
					startDate: null,
					cvv: null))
				.WithSagePayPi(new SagePayPiDetails(
					cardIdentifier: collection["sagePayPiCardIdentifier"],
					merchantSessionId: collection["sagePayPiMerchantSessionKey"],
					paymentMethod: Gateway.SagePayPiCreditCardKey,  //This is the Sage Pay PI payment method, not ours	
					threeDSecureApproved: false))
				.WithoutOffsiteRequiredBillingAddressId()
				.WithoutOffsiteRequiredShippingAddressId()
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);

			customer.UpdateCustomer(requestedPaymentMethod: AppLogic.ro_PMCreditCard);

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}

		[HttpGet]
		public string MerchantSessionKey()
		{
			var processor = new SagePayPi();
			var key = processor.ObtainSagePayPiMerchantSessionKey();
			return key;
		}
	}
}
