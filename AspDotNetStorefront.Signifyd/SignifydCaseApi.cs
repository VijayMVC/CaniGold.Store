// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.Routing;
using AspDotNetStorefront.Signifyd.Model;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Signifyd
{
	public class SignifydConfiguration
	{
		public readonly bool Enabled;
		public readonly string ApiUrl;
		public readonly string AccessToken;
		public readonly bool DeclineActionVoid;
		public readonly IEnumerable<string> SupportedGateways;

		public SignifydConfiguration(bool enabled, string apiUrl, string accessToken, bool declineActionVoid)
		{
			Enabled = enabled;
			AccessToken = accessToken;
			ApiUrl = apiUrl;
			DeclineActionVoid = declineActionVoid;

			// supported gateways must support delayed capture and on-site credit card processing
			SupportedGateways = new[]
				{
					"AuthorizeNet",
					"CyberSource",
					"eProcessingNetwork",
					"Moneris",
					"SagePayments",
					"USAePay",
					"SkipJack",
					"PayPalPro",
					"PayflowPro",
					"eWAY"
				};
		}
	}

	public class SignifydConfigurationProvider
	{
		public readonly AppConfigProvider AppConfigProvider;

		public SignifydConfigurationProvider(AppConfigProvider appConfigProvider)
		{
			AppConfigProvider = appConfigProvider;
		}

		public SignifydConfiguration Create()
		{
			var enabled = AppConfigProvider.GetAppConfigValue<bool>("Signifyd.Enabled");
			var apiUrl = AppConfigProvider.GetAppConfigValue("Signifyd.API.Url");
			var accessToken = AppConfigProvider.GetAppConfigValue("Signifyd.Team.Key");
			var declineActionVoid = AppConfigProvider.GetAppConfigValue<bool>("Signifyd.DeclineAction.Void");

			if(enabled
				&& (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(accessToken)))
				throw new Exception("Signifyd is not configured properly - please see documentation.");

			return new SignifydConfiguration(
				enabled: enabled,
				apiUrl: apiUrl,
				accessToken: accessToken,
				declineActionVoid: declineActionVoid);
		}
	}

	public class SignifydCasePayload
	{
		public string Gateway
		{ get; set; }
		public Customer Customer
		{ get; set; }
		public AspDotNetStorefrontCore.Address BillingAddress
		{ get; set; }
		public AspDotNetStorefrontCore.Address ShippingAddress
		{ get; set; }
		public int OrderNumber
		{ get; set; }
		public decimal OrderTotal
		{ get; set; }
		public string TransactionId
		{ get; set; }
		public string CAVVResponseCode
		{ get; set; }
		public string AVSResult
		{ get; set; }
	}

	public class SignifydCaseApi
	{
		public void ValidateConfiguration(SignifydCasePayload signifydCasePayload, SignifydConfiguration signifydConfiguration)
		{
			// validate gateway. Since this is passed into MakeOrder we have to check here instead of during configuration create
			if(!signifydConfiguration.SupportedGateways
				.Any(s => s.Equals(signifydCasePayload.Gateway, StringComparison.OrdinalIgnoreCase)))
				throw new Exception($"Signifyd does not support the {signifydCasePayload.Gateway} gateway - please see documentation.");
		}

		public Result<bool> CreateCaseAndGuarantee(SignifydCasePayload signifydCasePayload, SignifydConfiguration signifydConfiguration)
		{
			ValidateConfiguration(signifydCasePayload, signifydConfiguration);

			// check for/create needed response webhook
			CreateWebhooks(signifydConfiguration);

			var deviceId = HttpContext.Current.Request.Cookies["signifydDeviceId"]?.Value;
			var lastOrderId = DB.GetSqlN("select top 1 OrderNumber N from Orders where CustomerID = @customerId order by OrderDate desc",
				new SqlParameter("@customerId", signifydCasePayload.Customer.CustomerID));
			var aggregateOrderCount = DB.GetSqlN("select count(*) N from Orders where CustomerID = @customerId",
				new SqlParameter("@customerId", signifydCasePayload.Customer.CustomerID)) + 1;
			var aggregateOrderAmount = DB.GetSqlNDecimal("select sum(OrderTotal) N from Orders where CustomerID = @customerId",
				new SqlParameter("@customerId", signifydCasePayload.Customer.CustomerID)) + signifydCasePayload.OrderTotal;
			var lastAccountUpdate = Convert.ToDateTime(DB.GetSqlS("select CONVERT(nvarchar(20), UpdatedOn) S from Customer where CustomerID = @customerId",
				new SqlParameter("@customerId", signifydCasePayload.Customer.CustomerID)));

			// issue the case/guarantee
			var signifydCaseAPI = new Api.CasesApi();
			signifydCaseAPI.Configuration.Username = signifydConfiguration.AccessToken; // the user name is the Signifyd Team Key

			var bin = signifydCasePayload.BillingAddress.CardNumber;
			if(bin?.Length > 6)
				bin = bin.Substring(0, 6);

			var response = signifydCaseAPI.CreateACase(new CaseCreation
			(
				Purchase: new Purchase(
					BrowserIpAddress: signifydCasePayload.Customer.LastIPAddress,
					OrderId: signifydCasePayload.OrderNumber.ToString(),
					CreatedAt: new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)).ToString(signifydCaseAPI.Configuration.DateTimeFormat),
					AvsResponseCode: signifydCasePayload.AVSResult,
					CvvResponseCode: signifydCasePayload.CAVVResponseCode,
					TotalPrice: (double)signifydCasePayload.OrderTotal,
					OrderSessionId: deviceId,
					DiscountCodes: null,
					Shipments: null,
					Products: null,
					PaymentGateway: signifydCasePayload.Gateway,
					PaymentMethod: PaymentMethod.CREDITCARD,
					TransactionId: signifydCasePayload.TransactionId,
					Currency: signifydCasePayload.Customer.CurrencySetting,
					OrderChannel: OrderChannel.WEB,
					ReceivedBy: null
				),
				Recipient: new Recipient(
					FullName: signifydCasePayload.Customer.FullName(),
					ConfirmationEmail: signifydCasePayload.Customer.EMail,
					DeliveryAddress: new DeliveryAddress(
						StreetAddress: signifydCasePayload.ShippingAddress.Address1,
						City: signifydCasePayload.ShippingAddress.City,
						PostalCode: signifydCasePayload.ShippingAddress.Zip,
						CountryCode: AppLogic.GetCountryTwoLetterISOCode(signifydCasePayload.ShippingAddress.Country)),
					ConfirmationPhone: signifydCasePayload.Customer.Phone,
					Organization: signifydCasePayload.ShippingAddress.Company,
					AgeRange: null
				),
				Card: new Card(
					CardHolderName: signifydCasePayload.Customer.FullName(),
					BillingAddress: new Model.Address(
						StreetAddress: signifydCasePayload.BillingAddress.Address1,
						City: signifydCasePayload.BillingAddress.City,
						PostalCode: signifydCasePayload.BillingAddress.Zip,
						CountryCode: AppLogic.GetCountryTwoLetterISOCode(signifydCasePayload.BillingAddress.Country)),
					Bin: Convert.ToDouble(bin),
					Last4: AppLogic.SafeDisplayCardNumberLast4(signifydCasePayload.BillingAddress.CardNumber, "Address", signifydCasePayload.BillingAddress.AddressID),
					ExpiryMonth: Convert.ToDouble(signifydCasePayload.BillingAddress.CardExpirationMonth),
					ExpiryYear: Convert.ToDouble(signifydCasePayload.BillingAddress.CardExpirationYear)
				),
				UserAccount: new UserAccount(
					Email: signifydCasePayload.Customer.EMail,
					Username: signifydCasePayload.Customer.EMail,
					Phone: signifydCasePayload.Customer.Phone,
					CreatedDate: signifydCasePayload.Customer.CreatedOn.ToString(signifydCaseAPI.Configuration.DateTimeFormat),
					AccountNumber: signifydCasePayload.Customer.CustomerID.ToString(),
					LastOrderId: lastOrderId.ToString(),
					AggregateOrderCount: aggregateOrderCount,
					AggregateOrderDollars: (double)aggregateOrderAmount,
					LastUpdateDate: lastAccountUpdate.ToString(signifydCaseAPI.Configuration.DateTimeFormat)
				)
			));

			// create order status record with Pending status
			DB.ExecuteSQL("insert SignifydOrderStatus ([OrderNumber],[GuaranteedStatus],[InvestigationID]) VALUES(@orderNumber, @guaranteedStatus, @investigationId)",
				new[]
				{
					new SqlParameter("@orderNumber", signifydCasePayload.OrderNumber),
					new SqlParameter("@guaranteedStatus", GuaranteeDisposition.PENDING.ToString()),
					new SqlParameter("@investigationId", response.InvestigationId)
				});

			return Result.Ok(true);
		}

		void CreateWebhooks(SignifydConfiguration signifydConfiguration)
		{
			var webHooksApi = new Api.WebhooksApi();
			webHooksApi.Configuration.Username = signifydConfiguration.AccessToken;

			// check for the existance of the GuaranteeCompletion webhook (the only one we use), and add it if it's not there
			if(!webHooksApi.List().Where(s => s.EventType == WebhookEventType.GUARANTEECOMPLETION).Any())
			{
				// Signifyd currently doesn't support web hooks via HTTPS
				var webHookUrl = new UrlHelper(HttpContext.Current.Request.RequestContext).Action(ActionNames.Webhook, ControllerNames.Signifyd, routeValues: null, protocol: Uri.UriSchemeHttp);
				var webHookRequests = new List<WebhookRequest> { new WebhookRequest(WebhookEventType.GUARANTEECOMPLETION, Url: webHookUrl) };

				webHooksApi.Create(new WebhookRequestContainer(webHookRequests));
			}
		}
	}
}
