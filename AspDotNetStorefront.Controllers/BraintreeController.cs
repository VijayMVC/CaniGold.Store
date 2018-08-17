// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web.Mvc;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways;

namespace AspDotNetStorefront.Controllers
{
	public class BraintreeController : Controller
	{
		readonly NoticeProvider NoticeProvider;
		readonly IPaymentOptionProvider PaymentOptionProvider;
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;

		public BraintreeController(
			NoticeProvider noticeProvider,
			IPaymentOptionProvider paymentOptionProvider,
			IPersistedCheckoutContextProvider persistedCheckoutContextProvider)
		{
			NoticeProvider = noticeProvider;
			PaymentOptionProvider = paymentOptionProvider;
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
		}

		[HttpPost]
		[ExportModelStateToTempData]
		public ActionResult BraintreeCreditCard(FormCollection collection)
		{
			var customer = HttpContext.GetCustomer();

			var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(PersistedCheckoutContextProvider.LoadCheckoutContext(customer))
				.WithCreditCard(new CreditCardDetails(
					name: customer.Name,
					number: null,
					issueNumber: null,
					cardType: collection["braintreeCardType"],
					expirationDate: null,
					startDate: null,
					cvv: null))
				.WithBraintree(new BraintreeDetails(
					nonce: collection["braintreeNonce"],
					token: collection["braintreeToken"],
					paymentMethod: Gateway.BraintreeCreditCardKey,  //This is the Braintree payment method, not ours	
					threeDSecureApproved: false))
				.WithoutOffsiteRequiredBillingAddressId()
				.WithoutOffsiteRequiredShippingAddressId()
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);

			customer.UpdateCustomer(requestedPaymentMethod: AppLogic.ro_PMCreditCard);

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}
	}
}
