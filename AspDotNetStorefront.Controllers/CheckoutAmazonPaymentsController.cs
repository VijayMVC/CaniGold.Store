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
using AspDotNetStorefrontGateways.Processors;

namespace AspDotNetStorefront.Controllers
{
	[SecureAccessFilter(forceHttps: true)]
	public class CheckoutAmazonPaymentsController : Controller
	{
		readonly AmazonPaymentsApiProvider AmazonPaymentsApiProvider;
		readonly NoticeProvider NoticeProvider;
		readonly IPaymentOptionProvider PaymentOptionProvider;
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;
		readonly AddressSelectListBuilder SelectListBuilder;

		public CheckoutAmazonPaymentsController(
			AmazonPaymentsApiProvider amazonPaymentsApiProvider,
			NoticeProvider noticeProvider,
			IPaymentOptionProvider paymentOptionProvider,
			IPersistedCheckoutContextProvider persistedCheckoutContextProvider,
			AddressSelectListBuilder selectListBuilder)
		{
			AmazonPaymentsApiProvider = amazonPaymentsApiProvider;
			NoticeProvider = noticeProvider;
			PaymentOptionProvider = paymentOptionProvider;
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
			SelectListBuilder = selectListBuilder;
		}

		[PageTypeFilter(PageTypes.Checkout)]
		public ActionResult AmazonPayments(bool clearSession = false)
		{
			var customer = HttpContext.GetCustomer();

			if(!PaymentOptionProvider.PaymentMethodSelectionIsValid(AppLogic.ro_PMAmazonPayments, customer))
			{
				NoticeProvider.PushNotice(
					message: AppLogic.GetString("checkout.paymentmethodnotallowed"),
					type: NoticeType.Failure);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			var model = new AmazonPaymentsViewModel(
				residenceTypeOptions: SelectListBuilder.BuildResidenceTypeSelectList(ResidenceTypes.Residential.ToString()),
				clientId: AmazonPaymentsApiProvider.Configuration.ClientId,
				merchantId: AmazonPaymentsApiProvider.Configuration.MerchantId,
				scriptUrl: AmazonPaymentsApiProvider.Configuration.ScriptUrl);

			if(clearSession)
			{
				var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
					.From(PersistedCheckoutContextProvider.LoadCheckoutContext(customer))
					.WithoutAmazonPayments()
					.WithoutOffsiteRequiredBillingAddressId()
					.WithoutOffsiteRequiredShippingAddressId()
					.Build();

				PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);
				customer.UpdateCustomer(requestedPaymentMethod: string.Empty);
				return Redirect(Url.Action(ActionNames.Index, ControllerNames.Checkout));
			}

			return View(model);
		}

		public ActionResult AmazonPaymentsDetail()
		{
			return PartialView(ViewNames.AmazonPaymentsDetailPartial);
		}

		public ActionResult AmazonPaymentsCallback(string session, string access_token, string token_type, string expires_in, string scope)
		{
			// Get an email back from amazon and update the checkout context with it if we don't already have an email on the checkout context.
			var customer = HttpContext.GetCustomer();
			var persistedCheckoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);
			if(string.IsNullOrEmpty(persistedCheckoutContext.Email))
			{
				if(string.IsNullOrEmpty(access_token))
					return View(ViewNames.AmazonPayments, new { clearSession = true });

				var userProfile = AmazonPaymentsApiProvider.GetUserProfile(access_token);
				if(userProfile != null && !string.IsNullOrEmpty(userProfile.Email))
				{
					var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
						.From(persistedCheckoutContext)
						.WithEmail(userProfile.Email)
						.Build();

					PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);
				}
			}

			var residenceType = ResidenceTypes.Residential;
			if(customer.PrimaryShippingAddress != null
				&& customer.PrimaryShippingAddress.ResidenceType != ResidenceTypes.Unknown)
				residenceType = customer.PrimaryShippingAddress.ResidenceType;

			var model = new AmazonPaymentsViewModel(
				residenceTypeOptions: SelectListBuilder.BuildResidenceTypeSelectList(residenceType.ToString()),
				clientId: AmazonPaymentsApiProvider.Configuration.ClientId,
				merchantId: AmazonPaymentsApiProvider.Configuration.MerchantId,
				scriptUrl: AmazonPaymentsApiProvider.Configuration.ScriptUrl)
			{
				ResidenceType = residenceType,
				CheckoutStep = AmazonPaymentsCheckoutStep.SelectAddress
			};

			return View(ViewNames.AmazonPayments, model);
		}

		public ActionResult AmazonPaymentsComplete(AmazonPaymentsViewModel model)
		{
			var customer = HttpContext.GetCustomer();

			var orderDetails = AmazonPaymentsApiProvider
				.GetOrderDetails(model.AmazonOrderReferenceId)
				.GetOrderReferenceDetailsResult
				.OrderReferenceDetails;

			var shippingAddress = orderDetails
				.Destination
				.PhysicalDestination;

			var city = shippingAddress.City;
			var countryCode = shippingAddress.CountryCode;
			var countryName = AppLogic.GetCountryNameFromTwoLetterISOCode(countryCode);
			var stateName = shippingAddress.StateOrRegion ?? string.Empty;
			var stateAbbreviation = AppLogic.GetStateAbbreviation(stateName, countryName);
			var postalCode = shippingAddress.PostalCode;

			if(!ModelState.IsValid)
			{
				var newModel = new AmazonPaymentsViewModel(
					residenceTypeOptions: SelectListBuilder.BuildResidenceTypeSelectList(ResidenceTypes.Residential.ToString()),
					clientId: model.ClientId,
					merchantId: model.MerchantId,
					scriptUrl: model.ScriptUrl);

				return View(ViewNames.AmazonPayments, newModel);
			}

			var amazonAddress = Address.FindOrCreateOffSiteAddress(
				customerId: customer.CustomerID,
				city: city,
				stateAbbreviation: string.IsNullOrEmpty(stateAbbreviation)
					? stateName
					: stateAbbreviation,
				postalCode: postalCode,
				countryName: string.IsNullOrEmpty(countryName)
					? countryCode
					: countryName,
				offSiteSource: AppLogic.ro_PMAmazonPayments,
				residenceType: model.ResidenceType
				);

			customer.SetPrimaryAddress(amazonAddress.AddressID, AddressTypes.Billing);
			customer.SetPrimaryAddress(amazonAddress.AddressID, AddressTypes.Shipping);

			var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(PersistedCheckoutContextProvider.LoadCheckoutContext(customer))
				.WithAmazonPayments(new AmazonPaymentsDetails(model.AmazonOrderReferenceId))
				.WithOffsiteRequiredBillingAddressId(amazonAddress.AddressID)
				.WithOffsiteRequiredShippingAddressId(amazonAddress.AddressID)
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);
			customer.UpdateCustomer(requestedPaymentMethod: AppLogic.ro_PMAmazonPayments);

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}
	}
}
