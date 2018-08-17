// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web.Mvc;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Classes;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers
{
	[SecureAccessFilter(forceHttps: true)]
	public class CheckoutShippingEstimateController : Controller
	{
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;
		readonly AddressSelectListBuilder AddressSelectListBuilder;

		public CheckoutShippingEstimateController(
			IPersistedCheckoutContextProvider persistedCheckoutContextProvider,
			AddressSelectListBuilder addressSelectListBuilder)
		{
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
			AddressSelectListBuilder = addressSelectListBuilder;
		}

		[HttpGet, ImportModelStateFromTempData]
		public ActionResult ShippingEstimate(bool methodsWereReturned = false)
		{
			var customer = HttpContext.GetCustomer();
			var showShippingEstimator = AppLogic.AppConfigBool("ShowShippingEstimate")
				&& customer.PrimaryShippingAddressID == 0;

			if(!showShippingEstimator)
				return Content(string.Empty);

			var checkoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);

			var showNoRates = false;

			// We've entered an address and we did not get any rates back so lets dispay a generic error.
			if(!methodsWereReturned && checkoutContext.ShippingEstimate != null)
				showNoRates = true;

			return PartialView(ViewNames.ShippingEstimatePartial, BuildViewModel(checkoutContext.ShippingEstimate, showNoRates));
		}

		[HttpPost, ExportModelStateToTempData]
		public ActionResult ShippingEstimate(ShippingEstimateViewModel model)
		{
			if(!ModelState.IsValid)
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);

			// Add the estimate partial address to the checkout context so that we can use that later to display rates if there is no customer address
			var customer = HttpContext.GetCustomer();
			var shippingEstimateDetails = new ShippingEstimateDetails(
				country: model.Country,
				city: model.City,
				state: model.State,
				postalCode: model.PostalCode);

			var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(PersistedCheckoutContextProvider.LoadCheckoutContext(customer))
				.WithShippingEstimate(shippingEstimateDetails)
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}

		ShippingEstimateViewModel BuildViewModel(ShippingEstimateDetails shippingEstimateDetails, bool showNoRates)
		{
			if(shippingEstimateDetails == null)
				shippingEstimateDetails = new ShippingEstimateDetails(
					country: null,
					city: null,
					state: null,
					postalCode: null);

			var countries = AddressSelectListBuilder.BuildCountrySelectList(shippingEstimateDetails.Country);
			var states = AddressSelectListBuilder.BuildStateSelectList(countries.SelectedValue.ToString(), shippingEstimateDetails.State);

			return new ShippingEstimateViewModel
			{
				Country = shippingEstimateDetails.Country,
				Countries = countries,
				City = shippingEstimateDetails.City,
				State = shippingEstimateDetails.State,
				States = states,
				PostalCode = shippingEstimateDetails.PostalCode,
				ShowNoRates = showNoRates,
				ShippingCalculationRequiresCityAndState = Shipping.GetActiveShippingCalculationID() != Shipping.ShippingCalculationEnum.UseRealTimeRates
			};
		}
	}
}
