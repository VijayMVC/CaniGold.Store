// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web.Mvc;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers
{
	public class AcceptJsController : Controller
	{
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;

		public AcceptJsController(IPersistedCheckoutContextProvider persistedCheckoutContextProvider)
		{
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
		}

		[HttpPost]
		[ExportModelStateToTempData]
		public ActionResult AcceptJsCreditCard(AcceptJsCreditCardViewModel model)
		{
			var customer = HttpContext.GetCustomer();

			// Save the validated credit card details into the persisted checkout state
			var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(PersistedCheckoutContextProvider.LoadCheckoutContext(customer))
				.WithCreditCard(
					creditCard: new CreditCardDetails(
					name: customer.Name,
					number: model.LastFour,     //Put this here so that it can display in the payment summary section of the checkout page
					issueNumber: null,
					cardType: null,
					expirationDate: null,
					startDate: null,
					cvv: null))
				.WithAcceptJsCreditCard(
					acceptJsCreditCard: new AcceptJsDetailsCreditCard(
					dataValue: model.DataValue,
					dataDescriptor: model.DataDescriptor,
					lastFour: model.LastFour,
					expirationMonth: model.ExpirationDate.Split('/')[0],    //No special handling to check for empty values here.
					expirationYear: model.ExpirationDate.Split('/')[1]))    // Better to explode here because expiration dates are required.
				.WithoutOffsiteRequiredBillingAddressId()
				.WithoutOffsiteRequiredShippingAddressId()
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);

			customer.UpdateCustomer(requestedPaymentMethod: AppLogic.ro_PMCreditCard);

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}

		[HttpPost]
		[ExportModelStateToTempData]
		public ActionResult AcceptJsECheck(AcceptJsEcheckPostModel model)
		{
			var customer = HttpContext.GetCustomer();

			// Save the validated eCheck details into the persisted checkout state.
			// ECheckDetails are null because we have already sent data to AcceptJs.
			// All we need now is the DataValue and DataDescriptor to complete the transaction.
			var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(PersistedCheckoutContextProvider.LoadCheckoutContext(customer))
				.WithECheck(
					eCheck: new ECheckDetails(
					nameOnAccount: null,
					accountNumber: null,
					routingNumber: null,
					accountType: null))
				.WithAcceptJsECheck(
					acceptJsECheck: new AcceptJsDetailsECheck(
					dataValue: model.DataValue,
					dataDescriptor: model.DataDescriptor,
					eCheckDisplayAccountNumberLastFour: model.ECheckDisplayAccountNumberLastFour,
					eCheckDisplayAccountType: model.ECheckDisplayAccountType))
				.WithoutOffsiteRequiredBillingAddressId()
				.WithoutOffsiteRequiredShippingAddressId()
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);

			customer.UpdateCustomer(requestedPaymentMethod: AppLogic.ro_PMECheck);

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}
	}
}
