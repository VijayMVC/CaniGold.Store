// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using AspDotNetStorefront.Caching.ObjectCaching;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways;

namespace AspDotNetStorefront.Controllers
{
	public class PayPalExpressController : Controller
	{
		readonly ICachedShoppingCartProvider CachedShoppingCartProvider;
		readonly NoticeProvider NoticeProvider;
		readonly IPaymentOptionProvider PaymentOptionProvider;
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;

		public PayPalExpressController(
			ICachedShoppingCartProvider cachedShoppingCartProvider,
			NoticeProvider noticeProvider,
			IPaymentOptionProvider paymentOptionProvider,
			IPersistedCheckoutContextProvider persistedCheckoutContextProvider)
		{
			CachedShoppingCartProvider = cachedShoppingCartProvider;
			NoticeProvider = noticeProvider;
			PaymentOptionProvider = paymentOptionProvider;
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
		}

		[HttpGet]
		public ActionResult StartPayPalExpress(bool isPayPalCredit = false)
		{
			var customer = HttpContext.GetCustomer();

			if(!PaymentOptionProvider.PaymentMethodSelectionIsValid(AppLogic.ro_PMPayPalExpress, customer))
			{
				NoticeProvider.PushNotice(
					message: AppLogic.GetString("checkout.paymentmethodnotallowed"),
					type: NoticeType.Failure);

				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			var cart = CachedShoppingCartProvider.Get(customer, CartTypeEnum.ShoppingCart, AppLogic.StoreID());

			Address shippingAddress = null;
			if(customer.IsRegistered && customer.PrimaryShippingAddressID != 0)
			{
				shippingAddress = new Address();
				shippingAddress.LoadByCustomer(customer.CustomerID, customer.PrimaryShippingAddressID, AddressTypes.Shipping);
			}

			var checkoutOptions = new Dictionary<string, string>();
			if(isPayPalCredit)
				checkoutOptions.Add("UserSelectedFundingSource", "BML");

			var payPalExpressRedirectUrl = Gateway.StartExpressCheckout(
				cart: cart,
				boolBypassOrderReview: false,
				checkoutOptions: checkoutOptions);

			// PPE gets the redirect URL via an AJAX request, so we have to return it in the content.
			if(Request.IsAjaxRequest() && !isPayPalCredit)
				return Content(payPalExpressRedirectUrl);

			return Redirect(payPalExpressRedirectUrl);
		}

		public ActionResult PayPalExpressReturn(string token)
		{
			var customer = HttpContext.GetCustomer();
			var expressCheckoutDetails = Gateway.GetExpressCheckoutDetails(token, customer.CustomerID);

			if(string.IsNullOrEmpty(expressCheckoutDetails))
			{
				// If nothing returned, abort the transaction.
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			if(expressCheckoutDetails.Equals("AVSFAILED", StringComparison.OrdinalIgnoreCase))
			{
				NoticeProvider.PushNotice(AppLogic.GetString("paypal.express.avsconfirmedaddress.error"), NoticeType.Failure);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			if(!customer.IsRegistered && !customer.IsOver13)
			{
				// Set the Over13Checked flag since PayPal is permitting the order process, so they don't get rejected.
				DB.ExecuteSQL(
					"update Customer set Over13Checked = 1 where CustomerID = @customerId",
					new SqlParameter("customerId", customer.CustomerID));
			}

			if(customer.PrimaryBillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMPayPalEmbeddedCheckout)
			{
				var orderNumber = DB.GetSqlN(
					"select max(OrderNumber) N from dbo.Orders where CustomerID = @customerId",
					new SqlParameter("customerId", customer.CustomerID));

				return RedirectToAction(
					ActionNames.Confirmation,
					ControllerNames.CheckoutConfirmation,
					new { orderNumber = orderNumber });
			}

			customer.UpdateCustomer(requestedPaymentMethod: AppLogic.ro_PMPayPalExpress);

			var checkoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);

			// During our call for customer details from paypal above we set the email on the customer record if they are not signed in already. 
			// So let's pull that customer email into the checkout context
			var email = !string.IsNullOrEmpty(checkoutContext.Email)
				? checkoutContext.Email
				: customer.EMail;

			var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(PersistedCheckoutContextProvider.LoadCheckoutContext(customer))
				.WithPayPalExpress(new PayPalExpressDetails(
					token: token,
					payerId: expressCheckoutDetails))
				.WithoutAmazonPayments()
				.WithOffsiteRequiredBillingAddressId(customer.PrimaryBillingAddressID)      //This was set while processing the PayPal return info earlier
				.WithOffsiteRequiredShippingAddressId(customer.PrimaryShippingAddressID)    //This was set while processing the PayPal return info earlier
				.WithEmail(email)
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}
	}
}
