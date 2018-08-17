// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using AcceptJs;
using AspDotNetStorefront.Caching.ObjectCaching;
using AspDotNetStorefrontCore;
using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers;
using Newtonsoft.Json;

namespace AspDotNetStorefrontGateways.Processors
{
	public class AcceptJs : GatewayProcessor
	{
		const string SolutionId = "AAA143870";

		readonly AppConfigProvider AppConfigProvider;
		readonly ICachedShoppingCartProvider CachedShoppingCartProvider;

		public AcceptJs()
		{
			AppConfigProvider = DependencyResolver.Current.GetService<AppConfigProvider>();
			CachedShoppingCartProvider = DependencyResolver.Current.GetService<ICachedShoppingCartProvider>();
		}

		public override string ProcessECheck(
			int orderNumber,
			int customerId,
			decimal orderTotal,
			bool useLiveTransactions,
			TransactionModeEnum transactionMode,
			Address useBillingAddress,
			Address useShippingAddress,
			out string avsResult,
			out string authorizationResult,
			out string authorizationCode,
			out string authorizationTransId,
			out string transactionCommandOut,
			out string transactionResponse)
		{
			return AcceptJsProcessCardOrECheck(
				customerId,
				orderTotal,
				useLiveTransactions,
				transactionMode,
				out avsResult,
				out authorizationResult,
				out authorizationCode,
				out authorizationTransId,
				out transactionCommandOut,
				out transactionResponse);
		}

		public override string ProcessCard(
			int orderNumber,
			int customerId,
			decimal orderTotal,
			bool useLiveTransactions,
			TransactionModeEnum transactionMode,
			AspDotNetStorefrontCore.Address billingAddress,
			string cardExtraCode,
			AspDotNetStorefrontCore.Address shippingAddress,
			string cavv,
			string eci,
			string xid,
			out string avsResult,
			out string authorizationResult,
			out string authorizationCode,
			out string authorizationTransId,
			out string transactionCommandOut,
			out string transactionResponse)
		{
			return AcceptJsProcessCardOrECheck(
				customerId,
				orderTotal,
				useLiveTransactions,
				transactionMode,
				out avsResult,
				out authorizationResult,
				out authorizationCode,
				out authorizationTransId,
				out transactionCommandOut,
				out transactionResponse);
		}

		string AcceptJsProcessCardOrECheck(
			int customerId,
			decimal orderTotal,
			bool useLiveTransactions,
			TransactionModeEnum transactionMode,
			out string avsResult,
			out string authorizationResult,
			out string authorizationCode,
			out string authorizationTransId,
			out string transactionCommandOut,
			out string transactionResponse)
		{
			authorizationCode = string.Empty;
			authorizationResult = string.Empty;
			authorizationTransId = string.Empty;
			avsResult = string.Empty;
			transactionCommandOut = string.Empty;
			transactionResponse = string.Empty;

			orderTotal.ValidateNumberOfDigits(15); // Accept.js limit

			//We don't display the address form in the lightbox, so use the billing address the customer entered onsite
			var customer = new AspDotNetStorefrontCore.Customer(customerId);
			var acceptJsBillingAddress = new customerAddressType
			{
				firstName = customer.FirstName
					.ToAlphaNumeric()
					.Truncate(50), // Accept.js limit
				lastName = customer.LastName
					.ToAlphaNumeric()
					.Truncate(50), // Accept.js limit
				address = customer.PrimaryBillingAddress.Address1
					.ToAlphaNumeric().
					Truncate(60), // Accept.js limit
				city = customer.PrimaryBillingAddress.City
					.ToAlphaNumeric()
					.Truncate(40), // Accept.js limit
				state = customer.PrimaryBillingAddress.State
					.ToAlphaNumeric()
					.Truncate(40), // Accept.js limit, but really it's only ever going to be 2 characters
				zip = customer.PrimaryBillingAddress.Zip
					.ToAlphaNumeric()
					.Truncate(20) // Accept.js limit
			};

			//Add line items to the order info
			var cart = CachedShoppingCartProvider.Get(customer, CartTypeEnum.ShoppingCart, AppLogic.StoreID());
			var lineItems = cart
				.CartItems
				.Take(30)  // Accept.js only accepts 30 line items.  Not very accepting of them.
				.Select(cartItem => new lineItemType
				{
					itemId = cartItem.ShoppingCartRecordID.ToString(),  // Accept.js limit of 31 which we can't get with an int
					name = cartItem.ProductName.Truncate(31), // Accept.js limit
					quantity = cartItem.Quantity,  // Accept.js limit of 4 decimal places which we can't get with an int
					unitPrice = Math.Round(cartItem.Price, 2)  // Accept.js limit
				})
				.ToArray();

			//Add the customer's payment info from the Accept.js form
			var opaqueData = new opaqueDataType
			{
				dataDescriptor = customer.ThisCustomerSession[AppLogic.AcceptJsDataDescriptor],
				dataValue = customer.ThisCustomerSession[AppLogic.AcceptJsDataValue]
			};

			var paymentType = new paymentType
			{
				Item = opaqueData
			};

			var transactionRequest = new transactionRequestType
			{
				transactionType = transactionMode == TransactionModeEnum.auth
					? transactionTypeEnum.authOnlyTransaction.ToString()
					: transactionTypeEnum.authCaptureTransaction.ToString(),
				amount = orderTotal,
				amountSpecified = true,
				payment = paymentType,
				billTo = acceptJsBillingAddress,
				lineItems = lineItems
			};

			if(useLiveTransactions)
				transactionRequest.solution = new solutionType
				{
					id = SolutionId
				};

			var request = new createTransactionRequest
			{
				transactionRequest = transactionRequest,
				merchantAuthentication = GetMerchantAuthentication(useLiveTransactions)
			};

			transactionCommandOut = JsonConvert.SerializeObject(request);

			var controller = new createTransactionController(request);
			controller.Execute(
				GetRunEnvironment(useLiveTransactions));

			var response = controller.GetApiResponse();

			if(response == null)
				return "NO RESPONSE FROM GATEWAY!";

			transactionResponse = JsonConvert.SerializeObject(response);

			if(response.messages.resultCode != messageTypeEnum.Ok)
				return response.transactionResponse?.errors?[0].errorText
					?? response.messages.message[0].text;

			if(response.transactionResponse.messages == null)
				return response.transactionResponse.errors?[0].errorText
					?? "Unspecified Error";

			authorizationCode = response.transactionResponse.authCode;
			authorizationResult = response.transactionResponse.messages[0].description;
			authorizationTransId = response.transactionResponse.transId;
			avsResult = response.transactionResponse.avsResultCode;

			return AppLogic.ro_OK;
		}

		public override string CaptureOrder(AspDotNetStorefrontCore.Order order)
		{
			var useLiveTransactions = AppConfigProvider.GetAppConfigValue<bool>("UseLiveTransactions");

			var orderTotal = order.Total();
			orderTotal.ValidateNumberOfDigits(15); // Accept.js limit

			var transactionRequest = new transactionRequestType
			{
				transactionType = transactionTypeEnum.priorAuthCaptureTransaction.ToString(),
				amount = orderTotal,
				amountSpecified = true,
				refTransId = order.AuthorizationPNREF,
			};

			if(useLiveTransactions)
				transactionRequest.solution = new solutionType
				{
					id = SolutionId
				};

			var request = new createTransactionRequest
			{
				transactionRequest = transactionRequest,
				merchantAuthentication = GetMerchantAuthentication(useLiveTransactions)
			};

			//Save the command we're sending
			order.CaptureTXCommand = JsonConvert.SerializeObject(request);

			var controller = new createTransactionController(request);
			controller.Execute(
				GetRunEnvironment(useLiveTransactions));

			var response = controller.GetApiResponse();

			if(response == null)
				return "NO RESPONSE FROM GATEWAY!";

			//Save the response
			order.CaptureTXResult = JsonConvert.SerializeObject(response);

			if(response.messages.resultCode != messageTypeEnum.Ok)
				return response.transactionResponse?.errors?[0].errorText
					?? response.messages.message[0].text;

			if(response.transactionResponse.messages == null)
				return response.transactionResponse.errors?[0].errorText
					?? "Unspecified Error";

			return AppLogic.ro_OK;
		}

		public override string VoidOrder(int orderNumber)
		{
			var useLiveTransactions = AppConfigProvider.GetAppConfigValue<bool>("UseLiveTransactions");

			var originalOrder = new AspDotNetStorefrontCore.Order(orderNumber);

			var transactionRequest = new transactionRequestType
			{
				transactionType = transactionTypeEnum.voidTransaction.ToString(),
				payment = GetPreviousOrderPaymentInfo(orderNumber),
				refTransId = originalOrder.AuthorizationPNREF
			};

			if(useLiveTransactions)
				transactionRequest.solution = new solutionType
				{
					id = SolutionId
				};

			var request = new createTransactionRequest
			{
				transactionRequest = transactionRequest,
				merchantAuthentication = GetMerchantAuthentication(useLiveTransactions)
			};

			//Save the command we're sending
			DB.ExecuteSQL("UPDATE Orders SET VoidTXCommand = @command WHERE OrderNumber = @orderNumber",
				new SqlParameter("@command", JsonConvert.SerializeObject(request)),
				new SqlParameter("@orderNumber", orderNumber));

			var controller = new createTransactionController(request);
			controller.Execute(
				GetRunEnvironment(useLiveTransactions));

			var response = controller.GetApiResponse();

			if(response == null)
				return "NO RESPONSE FROM GATEWAY!";

			//Save the response
			DB.ExecuteSQL("UPDATE Orders SET VoidTXResult = @result WHERE OrderNumber = @orderNumber",
				new SqlParameter("@result", JsonConvert.SerializeObject(response)),
				new SqlParameter("@orderNumber", orderNumber));

			if(response.messages.resultCode != messageTypeEnum.Ok)
				return response.transactionResponse?.errors?[0].errorText
					?? response.messages.message[0].text;

			if(response.transactionResponse.messages == null)
				return response.transactionResponse.errors?[0].errorText
					?? "Unspecified Error";

			return AppLogic.ro_OK;
		}

		public override string RefundOrder(
			int originalOrderNumber,
			int newOrderNumber,
			decimal refundAmount,
			string refundReason,
			AspDotNetStorefrontCore.Address billingAddress)
		{
			var useLiveTransactions = AppConfigProvider.GetAppConfigValue<bool>("UseLiveTransactions");

			var originalOrder = new AspDotNetStorefrontCore.Order(originalOrderNumber);

			refundAmount.ValidateNumberOfDigits(15);

			var transactionRequest = new transactionRequestType
			{
				payment = GetPreviousOrderPaymentInfo(originalOrderNumber),
				transactionType = transactionTypeEnum.refundTransaction.ToString(),
				amount = refundAmount,
				amountSpecified = true,
				refTransId = originalOrder.AuthorizationPNREF
			};

			if(useLiveTransactions)
				transactionRequest.solution = new solutionType
				{
					id = SolutionId
				};

			var request = new createTransactionRequest
			{
				transactionRequest = transactionRequest,
				merchantAuthentication = GetMerchantAuthentication(useLiveTransactions)
			};

			//Save the command we're sending
			originalOrder.RefundTXCommand = JsonConvert.SerializeObject(request);

			var controller = new createTransactionController(request);
			controller.Execute(
				GetRunEnvironment(useLiveTransactions));

			var response = controller.GetApiResponse();

			if(response == null)
				return "NO RESPONSE FROM GATEWAY!";

			//Save the response
			originalOrder.RefundTXResult = JsonConvert.SerializeObject(response);

			if(response.messages.resultCode != messageTypeEnum.Ok)
				return response.transactionResponse?.errors?[0].errorText
					?? response.messages.message[0].text;

			if(response.transactionResponse.messages == null)
				return response.transactionResponse.errors?[0].errorText
					?? "Unspecified Error";

			return AppLogic.ro_OK;
		}

		paymentType GetPreviousOrderPaymentInfo(int orderNumber)
		{
			var cardNumber = DB.GetSqlS("SELECT Last4 S FROM Orders WITH (NOLOCK) WHERE OrderNumber = @orderNumber",
				new SqlParameter("@orderNumber", orderNumber));

			var creditCard = new creditCardType
			{
				cardNumber = cardNumber,
				expirationDate = "XXXX"
			};

			return new paymentType
			{
				Item = creditCard
			};
		}

		AuthorizeNet.Environment GetRunEnvironment(bool liveMode)
		{
			return liveMode
				? AuthorizeNet.Environment.PRODUCTION
				: AuthorizeNet.Environment.SANDBOX;
		}

		merchantAuthenticationType GetMerchantAuthentication(bool liveMode)
		{
			return new merchantAuthenticationType()
			{
				name = liveMode
					? AppConfigProvider.GetAppConfigValue("AcceptJs.Live.ApiLoginId")
					: AppConfigProvider.GetAppConfigValue("AcceptJs.Test.ApiLoginId"),
				ItemElementName = ItemChoiceType.transactionKey,
				Item = liveMode
					? AppConfigProvider.GetAppConfigValue("AcceptJs.Live.TransactionKey")
					: AppConfigProvider.GetAppConfigValue("AcceptJs.Test.TransactionKey"),
			};
		}
	}
}

namespace AcceptJs
{
	public static class Extensions
	{
		public static string Truncate(this string @string, int length)
		{
			return (@string.Length > length
					? @string.Substring(0, length)
					: @string)
					.Trim();
		}

		public static string ToAlphaNumeric(this string @string)
		{
			return new Regex("[^A-Za-z0-9]+")
				.Replace(@string, " ")
				.Trim();
		}

		// Not friendly, but I think it's legit to hard stop on amounts above 12 billion, not matter what the currency.
		public static void ValidateNumberOfDigits(this decimal @decimal, int digits)
		{
			if(@decimal.ToString()
				.Replace(".", string.Empty)
				.Length > digits)
			{
				throw new Exception($"Amount {@decimal} contains too many digits.");
			}
		}
	}
}

