// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.Auth;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontCore.Tokens;
using AspDotNetStorefrontCore.Tokens.DynamicHandlers;
using AspDotNetStorefrontGateways;

namespace AspDotNetStorefront.Controllers
{
	[SecureAccessFilter(forceHttps: true)]
	public class CheckoutConfirmationController : Controller
	{
		readonly ICheckoutAccountStatusProvider CheckoutAccountStatusProvider;
		readonly NoticeProvider NoticeProvider;
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;

		public CheckoutConfirmationController(
			ICheckoutAccountStatusProvider checkoutAccountStatusProvider,
			NoticeProvider noticeProvider,
			IPersistedCheckoutContextProvider persistedCheckoutContextProvider)
		{
			CheckoutAccountStatusProvider = checkoutAccountStatusProvider;
			NoticeProvider = noticeProvider;
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
		}

		[PageTypeFilter(PageTypes.OrderConfirmation)]
		public ActionResult Confirmation(int orderNumber)
		{
			var customer = HttpContext.GetCustomer();
			var checkoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);
			var order = new Order(orderNumber, customer.LocaleSetting);

			//Missing info
			if(customer.CustomerID == 0 || orderNumber == 0)
			{
				NoticeProvider.PushNotice(AppLogic.GetString("orderconfirmation.Invalid"), NoticeType.Failure);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			//No such order
			if(order.IsEmpty)
			{
				NoticeProvider.PushNotice(AppLogic.GetString("orderconfirmation.aspx.19"), NoticeType.Failure);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			//Wrong customer
			if(customer.CustomerID != order.CustomerID)
			{
				return RedirectToAction(ActionNames.Detail, ControllerNames.Topic, new { @name = "ordernotfound" });
			}

			if(customer.ThisCustomerSession["3DSecure.LookupResult"].Length > 0)
			{
				var sqlParams = new SqlParameter[]
				{
					new SqlParameter("@LookupResult", customer.ThisCustomerSession["3DSecure.LookupResult"]),
					new SqlParameter("@OrderNumber", orderNumber)
				};

				DB.ExecuteSQL("UPDATE Orders SET CardinalLookupResult = @LookupResult WHERE OrderNumber = @OrderNumber", sqlParams);
			}

			//Order cleanup
			if(!order.AlreadyConfirmed)
			{
				ViewBag.OrderAlreadyConfirmed = false; // Adding a variable to the viewbag so that xmlpackages can tell the order has not yet been confirmed
				var paymentMethod = AppLogic.CleanPaymentMethod(order.PaymentMethod);

				DB.ExecuteSQL("update Customer set OrderOptions=NULL, OrderNotes=NULL, FinalizationData=NULL where CustomerID=" + customer.CustomerID.ToString());

				//New order notification
				AppLogic.SendOrderEMail(customer, orderNumber, false, paymentMethod, true);

				//Low inventory notification
				if(AppLogic.AppConfigBool("SendLowStockWarnings") && order.TransactionIsCaptured()) //If delayed capture, we'll check this when the order is captured
				{
					List<int> purchasedVariants = new List<int>();
					foreach(CartItem ci in order.CartItems)
					{
						purchasedVariants.Add(ci.VariantID);
					}

					AppLogic.LowInventoryWarning(purchasedVariants);
				}

				//Handle impersonation
				var impersonationValue = customer.ThisCustomerSession[AppLogic.ImpersonationSessionKey];
				if(!string.IsNullOrEmpty(impersonationValue))
				{
					int impersonatorId = 0;

					if(int.TryParse(impersonationValue, out impersonatorId))
					{
						var impersonator = new Customer(impersonatorId);
						var impersonationSql = "UPDATE Orders SET Notes = Notes + @ImpersonationNote WHERE OrderNumber = @OrderNumber";
						var impersonationSqlParams = new SqlParameter[]
						{
							new SqlParameter("@OrderNumber", orderNumber),
							new SqlParameter("@ImpersonationNote", string.Format(AppLogic.GetString("admin.order.ImpersonationNote"), impersonator.EMail))
						};

						DB.ExecuteSQL(impersonationSql, impersonationSqlParams);
						customer.ThisCustomerSession.ClearVal(AppLogic.ImpersonationSessionKey);
					}

				}

				//Braintree cleanup
				if(order.PaymentGateway == Gateway.ro_GWBRAINTREE)
				{
					//Clear out some session values we don't need anymore
					customer.ThisCustomerSession.ClearVal(AppLogic.Braintree3dSecureKey);
					customer.ThisCustomerSession.ClearVal(AppLogic.BraintreeNonceKey);
					customer.ThisCustomerSession.ClearVal(AppLogic.BraintreePaymentMethod);
				}

				//SagePayPi cleanup
				if(order.PaymentGateway == Gateway.ro_GWSAGEPAYPI)
				{
					//Clear out some session values we don't need anymore
					customer.ThisCustomerSession.ClearVal(AppLogic.SagePayPi3dSecureKey);
					customer.ThisCustomerSession.ClearVal(AppLogic.SagePayPiCardIdentifier);
					customer.ThisCustomerSession.ClearVal(AppLogic.SagePayPiPaymentMethod);
				}

				//Make sure we don't do this again
				DB.ExecuteSQL("UPDATE Orders SET AlreadyConfirmed = 1 WHERE OrderNumber = @OrderNumber", new SqlParameter[] { new SqlParameter("@OrderNumber", orderNumber) });
			}

			//Build the return model
			string googleTrackingCode = null;
			if(!order.AlreadyConfirmed && AppLogic.AppConfigBool("IncludeGoogleTrackingCode"))
				googleTrackingCode = GetTrackingCodeTopicContents("GoogleTrackingCode", customer, orderNumber, order.Total());

			string generalTrackingCode = null;
			if(!order.AlreadyConfirmed)
				generalTrackingCode = GetTrackingCodeTopicContents("ConfirmationTracking", customer, orderNumber, order.Total());

			var showGoogleCustomerReviews = AppLogic.AppConfigBool("GoogleCustomerReviewsEnabled")
				&& !order.AlreadyConfirmed
				&& !string.IsNullOrWhiteSpace(AppLogic.AppConfig("GoogleCustomerReviewsMerchantID"));

			var xmlPackage = AppLogic.AppConfig("XmlPackage.OrderConfirmationPage");
			if(string.IsNullOrEmpty(xmlPackage))
				xmlPackage = "page.orderconfirmation.xml.config";

			var body = AppLogic.RunXmlPackage(xmlPackage, new Parser(), customer, customer.SkinID, string.Empty, "OrderNumber=" + orderNumber.ToString(), true, true);

			var model = new OrderConfirmationViewModel(
				orderNumber: orderNumber,
				body: body,
				googleTrackingCode: googleTrackingCode,
				generalTrackingCode: generalTrackingCode,
				showGoogleTrackingCode: !string.IsNullOrWhiteSpace(googleTrackingCode),
				showGeneralTrackingCode: !string.IsNullOrWhiteSpace(generalTrackingCode),
				showGoogleCustomerReviews: showGoogleCustomerReviews,
				addPayPalExpressCheckoutScript: order.PaymentMethod == AppLogic.ro_PMPayPalExpress
					&& !order.AlreadyConfirmed,
				addBuySafeScript: AppLogic.GlobalConfigBool("BuySafe.Enabled")
					&& !string.IsNullOrEmpty(AppLogic.GlobalConfig("BuySafe.Hash"))
					&& !order.AlreadyConfirmed);

			//Get rid of old data - do this at the very end so we have all the info we need for order processing and building the model above
			ClearSensitiveOrderData(customer);

			if(!customer.IsRegistered || AppLogic.AppConfigBool("ForceSignoutOnOrderCompletion"))
				ClearCustomerSession(customer);

			return View(model);
		}

		string GetTrackingCodeTopicContents(string topicName, Customer customer, int orderNumber, decimal orderTotal)
		{
			// Get the topic without parsing tokens
			var topic = new Topic(
				topicName,
				customer.LocaleSetting,
				customer.SkinID,
				UseParser: null);

			// Run the parser on the topic with custom tokens
			var parser = new Parser();
			return parser.ReplaceTokens(
				HttpContext,
				customer,
				topic.Contents,
				new ParserOptions(
					additionalTokenHandlers: new ITokenHandler[]
					{
						new Literal("OrderTotal", Localization.CurrencyStringForGatewayWithoutExchangeRate(orderTotal)),
						new Literal("OrderNumber", orderNumber.ToString())
					}));
		}

		public ActionResult BuySafeGuarantee(int orderNumber)
		{
			if(!AppLogic.GlobalConfigBool("BuySafe.Enabled") || string.IsNullOrEmpty(AppLogic.GlobalConfig("BuySafe.Hash")))
				return Content(string.Empty);


			var customer = HttpContext.GetCustomer();
			var order = new Order(orderNumber, customer.LocaleSetting);

			var model = new BuySafeGuaranteeViewModel(
				orderNumber: orderNumber,
				jsLocation: AppLogic.GlobalConfig("BuySafe.RollOverJSLocation"),
				hash: AppLogic.GlobalConfig("BuySafe.Hash"),
				email: order.EMail,
				total: order.Total());

			return PartialView(ViewNames.BuySafeGuaranteePartial, model);
		}

		void ClearSensitiveOrderData(Customer customer)
		{
			Address billingAddress = new Address();

			//Clear anything that should not be stored except for immediate usage:
			billingAddress.LoadByCustomer(customer.CustomerID, customer.PrimaryBillingAddressID, AddressTypes.Billing);
			billingAddress.PONumber = String.Empty;
			if(!customer.MasterShouldWeStoreCreditCardInfo)
			{
				billingAddress.ClearCCInfo();
			}
			billingAddress.UpdateDB();

			//Clear out the payment method so it isn't automatically set on the next checkout
			customer.UpdateCustomer(requestedPaymentMethod: string.Empty);

			//Clear session data
			PersistedCheckoutContextProvider.ClearCheckoutContext(customer);
			AppLogic.ClearCardExtraCodeInSession(customer);
		}

		void ClearCustomerSession(Customer customer)
		{
			if(AppLogic.AppConfigBool("SiteDisclaimerRequired"))
				HttpContext.Profile.SetPropertyValue("SiteDisclaimerAccepted", string.Empty);

			Session.Clear();
			Session.Abandon();

			Request
				.GetOwinContext()
				.Authentication
				.SignOut(AuthValues.CookiesAuthenticationType);

			customer.Logout();
		}
	}
}
