// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Data.SqlClient;
using System.Text;
using System.Web.Mvc;
using AspDotNetStorefront.Caching.ObjectCaching;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefront.StringResource;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways;
using AspDotNetStorefrontGateways.Processors;
using Newtonsoft.Json.Linq;

namespace AspDotNetStorefront.Controllers
{
	public class ThreeDSecureController : Controller
	{
		readonly ICachedShoppingCartProvider CachedShoppingCartProvider;
		readonly NoticeProvider NoticeProvider;
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;
		readonly IStringResourceProvider StringResourceProvider;
		readonly AppConfigProvider AppConfigProvider;

		public ThreeDSecureController(
			ICachedShoppingCartProvider cachedShoppingCartProvider,
			NoticeProvider noticeProvider,
			IPersistedCheckoutContextProvider persistedCheckoutContextProvider,
			IStringResourceProvider stringResourceProvider,
			AppConfigProvider appConfigProvider)
		{
			CachedShoppingCartProvider = cachedShoppingCartProvider;
			NoticeProvider = noticeProvider;
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
			StringResourceProvider = stringResourceProvider;
			AppConfigProvider = appConfigProvider;
		}

		[HttpGet, ImportModelStateFromTempData]
		public ActionResult BraintreeThreeDSecureFail()
		{
			var customer = HttpContext.GetCustomer();
			var persistedCheckoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);

			var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(persistedCheckoutContext)
				.WithBraintree(new BraintreeDetails(
					nonce: persistedCheckoutContext.Braintree.Nonce,
					token: persistedCheckoutContext.Braintree.Token,
					paymentMethod: persistedCheckoutContext.Braintree.PaymentMethod,
					threeDSecureApproved: false))
				.WithoutOffsiteRequiredBillingAddressId()
				.WithoutOffsiteRequiredShippingAddressId()
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);

			NoticeProvider.PushNotice(StringResourceProvider.GetString("braintree.liabilityshiftfailed"), NoticeType.Failure);

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}

		[HttpGet, ImportModelStateFromTempData]
		public ActionResult BraintreeThreeDSecurePass(string nonce)
		{
			var customer = HttpContext.GetCustomer();
			var checkoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);
			var cart = CachedShoppingCartProvider.Get(customer, CartTypeEnum.ShoppingCart, AppLogic.StoreID());
			var orderNumber = customer.ThisCustomerSession.SessionUSInt("3Dsecure.OrderNumber");

			var updatedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(checkoutContext)
				.WithBraintree(new BraintreeDetails(
					nonce: nonce,   //We got a new nonce after the 3dSecure request
					token: checkoutContext.Braintree.Token,
					paymentMethod: checkoutContext.Braintree.PaymentMethod,
					threeDSecureApproved: true))
				.WithoutOffsiteRequiredBillingAddressId()
				.WithoutOffsiteRequiredShippingAddressId()
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedCheckoutContext);

			customer.ThisCustomerSession[AppLogic.Braintree3dSecureKey] = "true";
			customer.ThisCustomerSession[AppLogic.BraintreeNonceKey] = nonce;
			customer.ThisCustomerSession[AppLogic.BraintreePaymentMethod] = checkoutContext.Braintree.PaymentMethod;

			var status = Gateway.MakeOrder(string.Empty, AppLogic.TransactionMode(), cart, orderNumber, string.Empty, string.Empty, string.Empty, string.Empty);
			ClearThreeDSecureSessionInfo(customer);

			if(status == AppLogic.ro_OK)
			{
				return RedirectToAction(
					ActionNames.Confirmation,
					ControllerNames.CheckoutConfirmation,
					new { orderNumber = orderNumber });
			}

			NoticeProvider.PushNotice(string.Format(StringResourceProvider.GetString("secureprocess.aspx.5"), status), NoticeType.Failure);
			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}

		[PageTypeFilter(PageTypes.Checkout)]
		[HttpGet, ImportModelStateFromTempData]
		public ActionResult ThreeDSecure()
		{
			//Braintree has its own 3dSecure form
			if(AppLogic.ActivePaymentGatewayCleaned() == Gateway.ro_GWBRAINTREE)
			{
				var customer = HttpContext.GetCustomer();
				var context = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);
				var cart = new ShoppingCart(customer.SkinID, customer, CartTypeEnum.ShoppingCart, 0, false);

				var braintreeModel = new BraintreeThreeDSecureViewModel(
					nonce: context.Braintree.Nonce,
					scriptUrl: AppConfigProvider.GetAppConfigValue("Braintree.ScriptUrl"),
					token: context.Braintree.Token,
					total: cart.Total(true).ToString());

				return View(ViewNames.BraintreeThreeDSecureForm, braintreeModel);
			}
			//Sage Pay PI has its own 3dSecure form
			if(AppLogic.ActivePaymentGatewayCleaned() == Gateway.ro_GWSAGEPAYPI)
			{
				var customer = HttpContext.GetCustomer();
				var sagePayPiThreeDSecureViewModel = new SagePayPiThreeDSecureViewModel(
						paReq: customer.ThisCustomerSession[AppLogic.SagePayPiPaReq],
						termUrl: customer.ThisCustomerSession[AppLogic.SagePayPiTermUrl],
						md: customer.ThisCustomerSession[AppLogic.SagePayPiMd],
						acsUrl: customer.ThisCustomerSession[AppLogic.SagePayPiAcsUrl]
					);

				return View(ViewNames.SagePayPiThreeDSecureForm, sagePayPiThreeDSecureViewModel);
			}

			var threeDSecureModel = new ThreeDSecureFrameViewModel
			{
				FrameUrl = Url.Action(ActionNames.ThreeDSecureForm, ControllerNames.ThreeDSecure, null, this.Request.Url.Scheme)
			};

			return View(threeDSecureModel);
		}

		[HttpPost]
		public ActionResult SagePayPiPaRes(string paRes, string mD)
		{
			var orderStatus = StringResourceProvider.GetString("sagepaypi.error.unknownerror");
			var sagePayPi = new SagePayPi();
			var customer = HttpContext.GetCustomer();
			var session = new CustomerSession(customer.CustomerID);
			var orderNumber = customer.ThisCustomerSession.SessionUSInt("3Dsecure.OrderNumber");
			var useLiveTransactions = AppConfigProvider.GetAppConfigValue<bool>("UseLiveTransactions");

			var transactionUrl = string.Format(
				"{0}transactions/{1}",
				useLiveTransactions
					? AppConfigProvider.GetAppConfigValue("SagePayPi.LiveUrl")
					: AppConfigProvider.GetAppConfigValue("SagePayPi.TestUrl"),
				session[AppLogic.SagePayPiMd]);

			var threeDSecureTransactionUrl = $"{transactionUrl}/3d-secure";

			var jsonObject = new JObject(
					new JProperty("paRes", paRes)
				);

			var formattedResponse = JObject.Parse(sagePayPi.SagePayPiApiCall(jsonObject.ToString(), threeDSecureTransactionUrl, "POST"));
			var transactionResponseHasError = sagePayPi.ResponseHasError(formattedResponse, "status", "authenticated")
				&& sagePayPi.ResponseHasError(formattedResponse, "status", "attemptonly");

			if(transactionResponseHasError)
			{
				if(AppConfigProvider.GetAppConfigValue<bool>("sagepaypi.customerfriendlyerrors"))
				{
					NoticeProvider.PushNotice(string.Format(
						"{0} {1}",
						StringResourceProvider.GetString("sagepaypi.threedsecure.didnotauthenticate"),
						StringResourceProvider.GetString("sagepaypi.error.reentercarddetails")), NoticeType.Failure);
				}
				else
				{
					orderStatus = sagePayPi.GetResponseError(formattedResponse, "status");

					if(orderStatus.EqualsIgnoreCase(StringResourceProvider.GetString("sagepaypi.error.unknownresponseerror")))
						orderStatus = sagePayPi.GetResponseError(formattedResponse, "statusDetail");
					else
						orderStatus = sagePayPi.GetThreeDSecureStatus(sagePayPi.GetResponseError(formattedResponse, "status"));

					if(orderStatus.EqualsIgnoreCase(StringResourceProvider.GetString("sagepaypi.error.unknownresponseerror")))
						orderStatus = sagePayPi.GetResponseError(formattedResponse, "description");

					//display error when 3-D secure does not authenticate
					NoticeProvider.PushNotice(string.Format(
						"{0} {1} {2} {3}.",
						StringResourceProvider.GetString("sagepaypi.threedsecure.didnotauthenticate"),
						StringResourceProvider.GetString("sagepaypi.error.reentercarddetails"),
						StringResourceProvider.GetString("sagepaypi.status.reason"),
						orderStatus.TrimEnd('.')), NoticeType.Failure);
				}

				if(orderNumber > 0)
					sagePayPi.LogFailedTransaction($"URL: {threeDSecureTransactionUrl}, Request: {jsonObject}", formattedResponse.ToString(), orderNumber);

				sagePayPi.ClearPaymentMethod(customer.CustomerID);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			//retrieve transaction
			var emptyObject = "{}";
			var formattedTransactionResponse = JObject.Parse(sagePayPi.SagePayPiApiCall(emptyObject.ToString(), transactionUrl, "GET"));
			var formattedTransactionResponseHasError = sagePayPi.ResponseHasError(formattedTransactionResponse, "status", "ok")
				&& sagePayPi.ResponseHasError(formattedTransactionResponse, "status", "attemptonly");

			//if the transaction did not submit properly, return early, display an error from sage pay and do not make the order
			if(formattedTransactionResponseHasError)
			{
				var threeDSecureStatus = sagePayPi.GetThreeDSecureStatus(string.Empty);

				if(formattedTransactionResponse != null && formattedTransactionResponse["3DSecure"] != null && formattedTransactionResponse["3DSecure"]["status"] != null)
					threeDSecureStatus = sagePayPi.GetThreeDSecureStatus(formattedTransactionResponse["3DSecure"]["status"].ToString());

				orderStatus = sagePayPi.GetResponseError(formattedTransactionResponse, "statusDetail");

				if(orderStatus.EqualsIgnoreCase(StringResourceProvider.GetString("sagepaypi.error.unknownresponseerror")))
					orderStatus = sagePayPi.GetResponseError(formattedTransactionResponse, "description");

				if(AppConfigProvider.GetAppConfigValue<bool>("sagepaypi.customerfriendlyerrors"))
					NoticeProvider.PushNotice(string.Format(
						"{0} {1}",
						StringResourceProvider.GetString("sagepaypi.threedsecure.didnotauthenticate"),
						StringResourceProvider.GetString("sagepaypi.error.reentercarddetails")), NoticeType.Failure);
				else
					NoticeProvider.PushNotice(string.Format(
						"{0}. {1} {2}. {3}",
						orderStatus.TrimEnd('.'),
						StringResourceProvider.GetString("sagepaypi.status.threedsecure"),
						threeDSecureStatus.TrimEnd('.'),
						StringResourceProvider.GetString("sagepaypi.error.reentercarddetails")), NoticeType.Failure);

				if(orderNumber > 0)
					sagePayPi.LogFailedTransaction($"GET Method - URL: {transactionUrl}", formattedTransactionResponse.ToString(), customer.ThisCustomerSession.SessionUSInt("3Dsecure.OrderNumber"));

				sagePayPi.ClearPaymentMethod(customer.CustomerID);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			var persistedCheckoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);
			var cart = CachedShoppingCartProvider.Get(customer, CartTypeEnum.ShoppingCart, AppLogic.StoreID());

			var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(persistedCheckoutContext)
				.WithCreditCard(new CreditCardDetails(
					name: persistedCheckoutContext.CreditCard.Name,
					number: persistedCheckoutContext.CreditCard.Number,
					issueNumber: persistedCheckoutContext.CreditCard.IssueNumber,
					cardType: formattedTransactionResponse["paymentMethod"]["card"]["cardType"].ToString(),
					expirationDate: persistedCheckoutContext.CreditCard.ExpirationDate,
					startDate: persistedCheckoutContext.CreditCard.StartDate,
					cvv: persistedCheckoutContext.CreditCard.Cvv))
				.WithSagePayPi(new SagePayPiDetails(
					cardIdentifier: persistedCheckoutContext.SagePayPi.CardIdentifier,
					merchantSessionId: persistedCheckoutContext.SagePayPi.MerchantSessionId,
					paymentMethod: persistedCheckoutContext.SagePayPi.PaymentMethod,  //This is the Sage Pay PI payment method, not ours	
					threeDSecureApproved: true))
				.WithoutOffsiteRequiredBillingAddressId()
				.WithoutOffsiteRequiredShippingAddressId()
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, updatedPersistedCheckoutContext);

			customer.ThisCustomerSession[AppLogic.SagePayPi3dSecureKey] = "true";
			customer.ThisCustomerSession[AppLogic.SagePayPiPaymentMethod] = persistedCheckoutContext.SagePayPi.PaymentMethod;

			orderStatus = Gateway.MakeOrder(string.Empty, AppLogic.TransactionMode(), cart, orderNumber, string.Empty, string.Empty, string.Empty, string.Empty);
			ClearThreeDSecureSessionInfo(customer);

			if(orderStatus == AppLogic.ro_OK)
			{
				return RedirectToAction(
					ActionNames.Confirmation,
					ControllerNames.CheckoutConfirmation,
					new { orderNumber = orderNumber });
			}

			//display error if we reach this point, we should have redirected by now
			if(AppConfigProvider.GetAppConfigValue<bool>("sagepaypi.customerfriendlyerrors"))
				NoticeProvider.PushNotice(string.Format(
					"{0} {1}",
					StringResourceProvider.GetString("sagepaypi.threedsecure.didnotauthenticate"),
					StringResourceProvider.GetString("sagepaypi.error.reentercarddetails")), NoticeType.Failure);
			else
				NoticeProvider.PushNotice(string.Format(
					"{0} {1}",
					string.Format(StringResourceProvider.GetString("secureprocess.aspx.5"), orderStatus.TrimEnd('.')),
					StringResourceProvider.GetString("sagepaypi.error.reentercarddetails")), NoticeType.Failure);

			if(orderNumber > 0)
				sagePayPi.LogFailedTransaction($"URL: {threeDSecureTransactionUrl}, Request: {jsonObject}", formattedResponse.ToString(), orderNumber);

			sagePayPi.ClearPaymentMethod(customer.CustomerID);
			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}

		[PageTypeFilter(PageTypes.Checkout)]
		[HttpGet, ImportModelStateFromTempData]
		public ActionResult ThreeDSecureForm()
		{
			var customer = HttpContext.GetCustomer();
			var useCardinal = AppConfigProvider.GetAppConfigValue<bool>("CardinalCommerce.Centinel.Enabled");

			var model = new ThreeDSecureViewModel
			{
				ACSUrl = useCardinal
					? customer.ThisCustomerSession["Cardinal.ACSURL"]
					: customer.ThisCustomerSession["3Dsecure.ACSURL"],
				PaReq = useCardinal
					? customer.ThisCustomerSession["Cardinal.Payload"]
					: customer.ThisCustomerSession["3Dsecure.paReq"],
				MD = useCardinal
					? "None"
					: customer.ThisCustomerSession["3DSecure.MD"],
				TermUrl = Url.Action(ActionNames.ThreeDSecureReturn, ControllerNames.ThreeDSecure, null, this.Request.Url.Scheme)
			};

			return View(model);
		}

		[HttpPost]
		public ActionResult ThreeDSecureReturn()
		{
			var customer = HttpContext.GetCustomer();
			var useCardinal = AppConfigProvider.GetAppConfigValue<bool>("CardinalCommerce.Centinel.Enabled");

			if(ShoppingCart.CartIsEmpty(customer.CustomerID, CartTypeEnum.ShoppingCart))
			{
				ClearThreeDSecureSessionInfo(customer);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			if(useCardinal)
				return Redirect(ProcessCardinalReturn(customer));
			else
				return Redirect(ProcessNativeThreeDSecureReturn(customer));
		}

		string ProcessCardinalReturn(Customer customer)
		{
			var cart = CachedShoppingCartProvider.Get(customer, CartTypeEnum.ShoppingCart, AppLogic.StoreID());
			var payload = customer.ThisCustomerSession["Cardinal.Payload"];
			var paRes = CommonLogic.FormCanBeDangerousContent("PaRes")
				.Replace(" ", "")
				.Replace("\r", "")
				.Replace("\n", "");
			var transactionId = customer.ThisCustomerSession["Cardinal.TransactionID"];
			var orderNumber = customer.ThisCustomerSession.SessionUSInt("Cardinal.OrderNumber");

			if(orderNumber == 0
				|| string.IsNullOrEmpty(payload)
				|| string.IsNullOrEmpty(transactionId))
			{
				NoticeProvider.PushNotice(StringResourceProvider.GetString("cardinal_process.aspx.2"), NoticeType.Failure);
				ClearThreeDSecureSessionInfo(customer);
				return Url.Action(ActionNames.Index, ControllerNames.Checkout);
			}

			var cardinalAuthenticateResult = string.Empty;
			var paResStatus = string.Empty;
			var signatureVerification = string.Empty;
			var errorNumber = string.Empty;
			var errorDescription = string.Empty;

			var AuthResult = Cardinal.PreChargeAuthenticate(orderNumber,
				paRes,
				transactionId,
				out paResStatus,
				out signatureVerification,
				out errorNumber,
				out errorDescription,
				out cardinalAuthenticateResult);

			customer.ThisCustomerSession["Cardinal.AuthenticateResult"] = cardinalAuthenticateResult;

			if(((paResStatus == "Y" || paResStatus == "A") && signatureVerification == "Y") //Great success
				|| (paResStatus == "U" && errorNumber == "0"))  //Signature verification failed but Cardinal says to take it anyway
			{
				var cardExtraCode = CommonLogic.ExtractToken(customer.ThisCustomerSession["Cardinal.AuthenticateResult"], "<Cavv>", "</Cavv>");
				var eciFlag = CommonLogic.ExtractToken(customer.ThisCustomerSession["Cardinal.AuthenticateResult"], "<EciFlag>", "</EciFlag>");
				var XID = CommonLogic.ExtractToken(customer.ThisCustomerSession["Cardinal.AuthenticateResult"], "<Xid>", "</Xid>");

				var billingAddress = new Address();
				billingAddress.LoadByCustomer(customer.CustomerID, customer.PrimaryBillingAddressID, AddressTypes.Billing);

				var status = Gateway.MakeOrder(string.Empty, AppLogic.TransactionMode(), cart, orderNumber, cardExtraCode, eciFlag, XID, string.Empty);

				if(status != AppLogic.ro_OK)
				{
					NoticeProvider.PushNotice(status, NoticeType.Failure);
					ClearThreeDSecureSessionInfo(customer);
					return Url.Action(ActionNames.Index, ControllerNames.Checkout);
				}

				DB.ExecuteSQL(string.Format("UPDATE Orders SET CardinalLookupResult = {0}, CardinalAuthenticateResult = {1} WHERE OrderNumber= {2}",
					DB.SQuote(customer.ThisCustomerSession["Cardinal.LookupResult"]),
					DB.SQuote(customer.ThisCustomerSession["Cardinal.AuthenticateResult"]),
					orderNumber));

				return Url.Action(
					ActionNames.Confirmation,
					ControllerNames.CheckoutConfirmation,
					new { orderNumber = orderNumber });
			}

			//If we made it this far, either something failed or Authorization or Signature Verification didn't pass on Cardinal's end
			NoticeProvider.PushNotice(StringResourceProvider.GetString("cardinal_process.aspx.3"), NoticeType.Failure);
			ClearThreeDSecureSessionInfo(customer);
			return Url.Action(ActionNames.Index, ControllerNames.Checkout);
		}

		string ProcessNativeThreeDSecureReturn(Customer customer)
		{
			var paReq = customer.ThisCustomerSession["3Dsecure.paReq"];
			var paRes = CommonLogic.FormCanBeDangerousContent("PaRes")
				.Replace(" ", "")
				.Replace("\r", "")
				.Replace("\n", "");
			var merchantData = CommonLogic.FormCanBeDangerousContent("MD");
			var transactionId = customer.ThisCustomerSession["3Dsecure.XID"];
			var orderNumber = customer.ThisCustomerSession.SessionUSInt("3Dsecure.OrderNumber");

			if(!string.IsNullOrEmpty(paRes))
				customer.ThisCustomerSession["3Dsecure.PaRes"] = paRes;

			if(merchantData != customer.ThisCustomerSession["3Dsecure.MD"]
				|| orderNumber == 0
				|| string.IsNullOrEmpty(paReq)
				|| string.IsNullOrEmpty(transactionId))
			{
				NoticeProvider.PushNotice(StringResourceProvider.GetString("secureprocess.aspx.1"), NoticeType.Failure);
				ClearThreeDSecureSessionInfo(customer);
				return Url.Action(ActionNames.Index, ControllerNames.Checkout);
			}

			var cart = CachedShoppingCartProvider.Get(customer, CartTypeEnum.ShoppingCart, AppLogic.StoreID());
			var status = Gateway.MakeOrder(string.Empty, AppLogic.TransactionMode(), cart, orderNumber, string.Empty, string.Empty, string.Empty, string.Empty);

			// The session may have changed in MakeOrder, so get the latest values from the DB
			CustomerSession cSession = new CustomerSession(customer.CustomerID);

			if(status == AppLogic.ro_OK)
			{
				if(!string.IsNullOrEmpty(cSession["3DSecure.LookupResult"]))
				{
					// the data in this session variable will be encoded, so decode it before saving to the database
					var decodedBytes = Convert.FromBase64String(cSession["3DSecure.LookupResult"]);
					var lookupResult = Encoding.UTF8.GetString(decodedBytes);

					DB.ExecuteSQL("UPDATE Orders SET CardinalLookupResult = @CardinalLookupResult WHERE OrderNumber = @OrderNumber",
						new SqlParameter[] {
							new SqlParameter("@CardinalLookupResult", lookupResult),
							new SqlParameter("@OrderNumber", orderNumber) });

					cSession["3DSecure.LookupResult"] = string.Empty;
				}

				ClearThreeDSecureSessionInfo(customer);
				return Url.Action(
					ActionNames.Confirmation,
					ControllerNames.CheckoutConfirmation,
					new { orderNumber = orderNumber });
			}

			NoticeProvider.PushNotice(string.Format(StringResourceProvider.GetString("secureprocess.aspx.5"), status), NoticeType.Failure);
			ClearThreeDSecureSessionInfo(customer);
			return Url.Action(ActionNames.Index, ControllerNames.Checkout);
		}

		void ClearThreeDSecureSessionInfo(Customer customer)
		{
			customer.ThisCustomerSession["3DSecure.CustomerID"] = string.Empty;
			customer.ThisCustomerSession["3DSecure.OrderNumber"] = string.Empty;
			customer.ThisCustomerSession["3DSecure.ACSUrl"] = string.Empty;
			customer.ThisCustomerSession["3DSecure.paReq"] = string.Empty;
			customer.ThisCustomerSession["3DSecure.XID"] = string.Empty;
			customer.ThisCustomerSession["3DSecure.MD"] = string.Empty;
			customer.ThisCustomerSession["3Dsecure.PaRes"] = string.Empty;
			customer.ThisCustomerSession["Cardinal.LookupResult"] = string.Empty;
			customer.ThisCustomerSession["Cardinal.AuthenticateResult"] = string.Empty;
			customer.ThisCustomerSession["Cardinal.ACSUrl"] = string.Empty;
			customer.ThisCustomerSession["Cardinal.Payload"] = string.Empty;
			customer.ThisCustomerSession["Cardinal.TransactionID"] = string.Empty;
			customer.ThisCustomerSession["Cardinal.OrderNumber"] = string.Empty;
			customer.ThisCustomerSession["Cardinal.LookupResult"] = string.Empty;
		}
	}
}
