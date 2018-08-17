// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Linq;
using System.Web.Mvc;
using AspDotNetStorefront.Caching.ObjectCaching;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers
{
	[SecureAccessFilter(forceHttps: true)]
	public class CheckoutPaymentMethodController : Controller
	{
		readonly ICachedShoppingCartProvider CachedShoppingCartProvider;
		readonly IPaymentOptionProvider PaymentOptionProvider;
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;

		public CheckoutPaymentMethodController(
			ICachedShoppingCartProvider cachedShoppingCartProvider,
			IPaymentOptionProvider paymentOptionProvider,
			IPersistedCheckoutContextProvider persistedCheckoutContextProvider)
		{
			CachedShoppingCartProvider = cachedShoppingCartProvider;
			PaymentOptionProvider = paymentOptionProvider;
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
		}

		[HttpGet]
		public ActionResult PaymentMethod(bool? paymentMethodComplete)
		{
			var customer = HttpContext.GetCustomer();
			var cart = CachedShoppingCartProvider.Get(customer, CartTypeEnum.ShoppingCart, AppLogic.StoreID());

			var paymentOptions = PaymentOptionProvider.GetPaymentOptions(HttpContext, customer, cart)
				.Where(paymentOption => paymentOption.Available);

			var selectedPaymentMethod = PaymentOptionProvider.GetCustomerSelectedPaymentOption(paymentOptions, customer);

			var onSitePaymentOptions = paymentOptions
				.Where(paymentOption => !paymentOption.IsOffsiteForDisplay)
				.OrderBy(paymentOption => paymentOption.DisplayOrder);

			var model = new PaymentMethodRenderModel(
				onSitePaymentOptions: onSitePaymentOptions,
				selectedPaymentMethod: selectedPaymentMethod != null
					? selectedPaymentMethod.Info.Name
					: null,
				selectedPaymentMethodDisplayName: selectedPaymentMethod != null
					? selectedPaymentMethod.Info.DisplayName
					: null,
				paymentMethodComplete: paymentMethodComplete ?? false,
				editUrl: selectedPaymentMethod != null
					? selectedPaymentMethod.IsEditable
						? selectedPaymentMethod.EditUrl
						: null
					: null);

			return PartialView(ViewNames.PaymentMethodPartial, model);
		}

		[HttpGet]
		public ActionResult AlternativePaymentMethods(bool? paymentMethodComplete)
		{
			var customer = HttpContext.GetCustomer();
			var cart = new ShoppingCart(customer.SkinID, customer, CartTypeEnum.ShoppingCart, 0, false);

			var alternatePaymentOptions = PaymentOptionProvider.GetPaymentOptions(HttpContext, customer, cart)
				.Where(po => po.Available)
				.Where(po => po.IsOffsiteForDisplay)
				.OrderBy(po => po.DisplayOrder);

			var selectedPaymentMethod = PaymentOptionProvider.GetCustomerSelectedPaymentOption(alternatePaymentOptions, customer);

			var model = new PaymentMethodRenderModel(
				alternatePaymentOptions: alternatePaymentOptions,
				selectedPaymentMethod: selectedPaymentMethod != null
					? selectedPaymentMethod.Info.Name
					: null,
				selectedPaymentMethodDisplayName: selectedPaymentMethod != null
					? selectedPaymentMethod.Info.DisplayName
					: null,
				paymentMethodComplete: paymentMethodComplete ?? false,
				editUrl: selectedPaymentMethod != null
					? selectedPaymentMethod.EditUrl
					: null);

			return PartialView(ViewNames.AlternativePaymentMethodsPartial, model);
		}

		public ActionResult SetPaymentMethod(string selectedPaymentMethod = null)
		{
			var customer = HttpContext.GetCustomer();

			var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(PersistedCheckoutContextProvider.LoadCheckoutContext(customer))
				.WithoutOffsiteRequiredBillingAddressId()
				.WithoutOffsiteRequiredShippingAddressId()
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);

			//Update the customer record
			if(selectedPaymentMethod != null && selectedPaymentMethod != customer.RequestedPaymentMethod)
				customer.UpdateCustomer(requestedPaymentMethod: selectedPaymentMethod);

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}
	}
}
