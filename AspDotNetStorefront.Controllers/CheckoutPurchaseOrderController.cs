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
	[SecureAccessFilter(forceHttps: true)]
	public class CheckoutPurchaseOrderController : Controller
	{
		readonly NoticeProvider NoticeProvider;
		readonly IPaymentOptionProvider PaymentOptionProvider;
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;

		public CheckoutPurchaseOrderController(
			NoticeProvider noticeProvider,
			IPaymentOptionProvider paymentOptionProvider,
			IPersistedCheckoutContextProvider persistedCheckoutContextProvider)
		{
			NoticeProvider = noticeProvider;
			PaymentOptionProvider = paymentOptionProvider;
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
		}

		[PageTypeFilter(PageTypes.Checkout)]
		[HttpGet, ImportModelStateFromTempData]
		public ActionResult PurchaseOrder()
		{
			var customer = HttpContext.GetCustomer();

			if(!PaymentOptionProvider.PaymentMethodSelectionIsValid(AppLogic.ro_PMPurchaseOrder, customer))
			{
				NoticeProvider.PushNotice(
					message: AppLogic.GetString("checkout.paymentmethodnotallowed"),
					type: NoticeType.Failure);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			var checkoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);

			var model = BuildPurchaseOrderViewModel(checkoutContext.PurchaseOrder);

			return View(model);
		}

		[HttpPost, ExportModelStateToTempData]
		public ActionResult PurchaseOrder(PurchaseOrderViewModel model)
		{
			if(!ModelState.IsValid)
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);

			var customer = HttpContext.GetCustomer();

			var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(PersistedCheckoutContextProvider.LoadCheckoutContext(customer))
				.WithPurchaseOrder(new PurchaseOrderDetails(model.PONumber))
				.WithoutAmazonPayments()
				.WithoutOffsiteRequiredBillingAddressId()
				.WithoutOffsiteRequiredShippingAddressId()
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);
			customer.UpdateCustomer(requestedPaymentMethod: AppLogic.ro_PMPurchaseOrder);

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}

		public ActionResult PurchaseOrderDetail()
		{
			var customer = HttpContext.GetCustomer();
			var checkoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);

			var model = BuildPurchaseOrderViewModel(checkoutContext.PurchaseOrder);

			return PartialView(ViewNames.PurchaseOrderDetailPartial, model);
		}

		PurchaseOrderViewModel BuildPurchaseOrderViewModel(PurchaseOrderDetails purchaseOrder)
		{
			return new PurchaseOrderViewModel
			{
				PONumber = purchaseOrder == null
					? string.Empty
					: purchaseOrder.Number
			};
		}
	}
}
