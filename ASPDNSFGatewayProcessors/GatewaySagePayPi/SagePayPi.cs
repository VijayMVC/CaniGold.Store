// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.StringResource;
using AspDotNetStorefrontCore;
using Newtonsoft.Json.Linq;

namespace AspDotNetStorefrontGateways.Processors
{
	public class SagePayPi : GatewayProcessor, ISagePayPiGatewayProcessor
	{
		readonly IStringResourceProvider StringResourceProvider;
		readonly AppConfigProvider AppConfigProvider;

		public SagePayPi()
		{
			StringResourceProvider = DependencyResolver.Current.GetService<IStringResourceProvider>();
			AppConfigProvider = DependencyResolver.Current.GetService<AppConfigProvider>();
		}

		public string TransactionType { get; set; }

		public string TransactionId { get; set; }

		public int RecurringOrderNumberOriginal { get; set; }

		public override string CaptureOrder(Order order)
		{
			var result = AppLogic.ro_OK;
			var transactionId = LookupSagePayPiTransactionId(order.OrderNumber);

			if(string.IsNullOrEmpty(transactionId))
				return StringResourceProvider.GetString("admin.sagepaypi.notransactionid");

			var jsonObject = new JObject(
				new JProperty("instructionType", "release"),
				new JProperty("amount", GetSmallestCurrencyUnit(order.OrderBalance, Localization.StoreCurrency()))
			);

			var transactionUrl = string.Format(
				"{0}transactions/{1}",
				AppConfigProvider.GetAppConfigValue<bool>("UseLiveTransactions")
					? AppConfigProvider.GetAppConfigValue("SagePayPi.LiveUrl")
					: AppConfigProvider.GetAppConfigValue("SagePayPi.TestUrl"),
				transactionId);

			var instructionsTransactionUrl = $"{transactionUrl}/instructions";
			var apiResponse = SagePayPiApiCall(jsonObject.ToString(), instructionsTransactionUrl, "POST");
			var formattedTransactionResponse = JObject.Parse(apiResponse);
			var responseHasError = ResponseHasError(formattedTransactionResponse, "instructionType", "release");

			DB.ExecuteSQL("update orders set CaptureTXCommand = @CaptureTXCommand where OrderNumber = @OrderNumber",
				new SqlParameter("@CaptureTXCommand", $"{order.CaptureTXCommand}" +
													  $"{Environment.NewLine}" +
													  $"{Environment.NewLine}" +
													  $"POST Method - URL: {instructionsTransactionUrl}, " +
													  $"Request: {jsonObject}"),
				new SqlParameter("@OrderNumber", order.OrderNumber));

			DB.ExecuteSQL("update orders set CaptureTXResult = @CaptureTXResult where OrderNumber = @OrderNumber",
				new SqlParameter("@CaptureTXResult", $"{order.CaptureTXResult}" +
													 $"{Environment.NewLine}" +
													 $"{Environment.NewLine}" +
													 $"{formattedTransactionResponse}"),
				new SqlParameter("@OrderNumber", order.OrderNumber));

			if(responseHasError)
			{
				var error = GetResponseError(formattedTransactionResponse, "description");

				if(error.EqualsIgnoreCase(StringResourceProvider.GetString("sagepaypi.error.unknownresponseerror")))
					error = GetResponseError(formattedTransactionResponse, "errors");

				if(error.EqualsIgnoreCase("Transaction status not applicable"))
					error = StringResourceProvider.GetString("admin.sagepaypi.error.nolongercapturable");

				result = error;
			}
			return result;
		}

		public override string VoidOrder(int orderNumber)
		{
			var result = AppLogic.ro_OK;
			var transactionId = LookupSagePayPiTransactionId(orderNumber);

			if(string.IsNullOrEmpty(transactionId))
				return StringResourceProvider.GetString("admin.sagepaypi.notransactionid");

			var order = new Order(orderNumber);
			var orderStateIsCaptured = order.TransactionState == AppLogic.ro_TXStateCaptured;

			//If captured we will void, if authorized we will abort
			var jsonObject = new JObject(
					new JProperty("instructionType",
					orderStateIsCaptured
						? "void"
						: "abort")
				);

			var transactionUrl = string.Format(
				"{0}transactions/{1}",
				AppConfigProvider.GetAppConfigValue<bool>("UseLiveTransactions")
					? AppConfigProvider.GetAppConfigValue("SagePayPi.LiveUrl")
					: AppConfigProvider.GetAppConfigValue("SagePayPi.TestUrl"),
				transactionId);

			var instructionsTransactionUrl = $"{transactionUrl}/instructions";
			var apiResponse = SagePayPiApiCall(jsonObject.ToString(), instructionsTransactionUrl, "POST");
			var formattedTransactionResponse = JObject.Parse(apiResponse);
			var responseHasError = ResponseHasError(formattedTransactionResponse, "instructionType",
					orderStateIsCaptured
						? "void"
						: "abort");

			DB.ExecuteSQL("update orders set VoidTXCommand = @VoidTXCommand where OrderNumber = @OrderNumber",
				new SqlParameter("@VoidTXCommand", $"URL: {instructionsTransactionUrl}, Request: {jsonObject}"),
				new SqlParameter("@OrderNumber", orderNumber));
			DB.ExecuteSQL("update orders set VoidTXResult = @VoidTXResult where OrderNumber = @OrderNumber",
				new SqlParameter("@VoidTXResult", formattedTransactionResponse.ToString()),
				new SqlParameter("@OrderNumber", orderNumber));

			if(responseHasError)
			{
				var error = GetResponseError(formattedTransactionResponse, "description");

				if(error.EqualsIgnoreCase(StringResourceProvider.GetString("sagepaypi.error.unknownresponseerror")))
					error = GetResponseError(formattedTransactionResponse, "errors");

				if(error.EqualsIgnoreCase("Transaction status not applicable"))
					error = orderStateIsCaptured
						? StringResourceProvider.GetString("admin.sagepaypi.error.voiderrorsameday")
						: StringResourceProvider.GetString("admin.sagepaypi.error.aborterror");

				result = error;
			}
			return result;
		}

		public override string RefundOrder(int originalOrderNumber, int newOrderNumber, decimal refundAmount, string refundReason, Address useBillingAddress)
		{
			var result = AppLogic.ro_OK;
			var transactionId = LookupSagePayPiTransactionId(originalOrderNumber);

			if(string.IsNullOrEmpty(transactionId))
				return StringResourceProvider.GetString("admin.sagepaypi.notransactionid");

			var jsonObject = new JObject(
				new JProperty("transactionType", "Refund"),
				new JProperty("referenceTransactionId", transactionId),
				new JProperty("vendorTxCode", $"{originalOrderNumber}-{DateTime.Now.ToString("ddMMyyyyhhmmss")}"),
				new JProperty("amount", GetSmallestCurrencyUnit(refundAmount, Localization.StoreCurrency())),
				new JProperty("currency", Localization.StoreCurrency()),
				new JProperty("description", string.IsNullOrEmpty(refundReason)
					? StringResourceProvider.GetString("admin.refund.OrderWasRefunded")
					: refundReason)
			);

			var url = (AppConfigProvider.GetAppConfigValue<bool>("UseLiveTransactions")
				? $"{AppConfigProvider.GetAppConfigValue("SagePayPi.LiveUrl")}transactions"
				: $"{AppConfigProvider.GetAppConfigValue("SagePayPi.TestUrl")}transactions");

			var apiResponse = SagePayPiApiCall(jsonObject.ToString(), url, "POST");
			var formattedTransactionResponse = JObject.Parse(apiResponse);
			var responseHasError = ResponseHasError(formattedTransactionResponse, "status", "ok");

			DB.ExecuteSQL("update orders set RefundTXCommand = @RefundTXCommand where OrderNumber = @OrderNumber",
				new SqlParameter("@RefundTXCommand", $"URL: {url}, Request: {jsonObject}"),
				new SqlParameter("@OrderNumber", originalOrderNumber));
			DB.ExecuteSQL("update orders set RefundTXResult = @RefundTXResult where OrderNumber = @OrderNumber",
				new SqlParameter("@RefundTXResult", $"{formattedTransactionResponse} - Amount: {refundAmount}"),
				new SqlParameter("@OrderNumber", originalOrderNumber));

			if(responseHasError)
			{
				result = GetResponseError(formattedTransactionResponse, "description");

				if(newOrderNumber > 0)
					LogFailedTransaction($"URL: {url}, Request: {jsonObject}", formattedTransactionResponse.ToString(), newOrderNumber);
			}
			return result;
		}

		public override string ProcessCard(int orderNumber, int customerID, Decimal orderTotal, bool useLiveTransactions, TransactionModeEnum transactionMode, Address useBillingAddress, string cardExtraCode, Address useShippingAddress, string cavv, string eci, string xid, out string avsResult, out string authorizationResult, out string authorizationCode, out string authorizationTransID, out string transactionCommandOut, out string transactionResponse)
		{
			var result = AppLogic.ro_OK;
			var session = new CustomerSession(customerID);
			var customer = new Customer(customerID);
			var shippingTwoLetterCountryCode = AppLogic.GetCountryTwoLetterISOCode(customer.PrimaryShippingAddress.Country);
			var billingTwoLetterCountryCode = AppLogic.GetCountryTwoLetterISOCode(customer.PrimaryBillingAddress.Country);
			authorizationCode = string.Empty;
			authorizationResult = string.Empty;
			authorizationTransID = string.Empty;
			avsResult = string.Empty;
			transactionCommandOut = string.Empty;
			transactionResponse = string.Empty;
			var threeDSecureEnabled = AppConfigProvider.GetAppConfigValue<bool>("SagePayPi.3dSecureEnabled");
			var isRecurringOrder = TransactionType != null && TransactionType.EqualsIgnoreCase("repeat");

			if(isRecurringOrder)
			{
				//process recurring order charge and set the result
				if(TransactionId != null)
				{
					var repeatJsonObject = new JObject(
						new JProperty("transactionType", "Repeat"),
						new JProperty("referenceTransactionId", TransactionId),
						new JProperty("vendorTxCode", orderNumber.ToString()),
						new JProperty("amount", GetSmallestCurrencyUnit(orderTotal, Localization.StoreCurrency())),
						new JProperty("currency", Localization.StoreCurrency()),
						new JProperty("description", $"{HttpContext.Current.Server.UrlEncode(AppConfigProvider.GetAppConfigValue("StoreName"))} Order {orderNumber}"),
						new JProperty("shippingDetails",
							new JObject(
								new JProperty("recipientFirstName", customer.PrimaryShippingAddress.FirstName),
								new JProperty("recipientLastName", customer.PrimaryShippingAddress.LastName),
								new JProperty("shippingAddress1", customer.PrimaryShippingAddress.Address1),
								new JProperty("shippingAddress2", customer.PrimaryShippingAddress.Address2),
								new JProperty("shippingCity", customer.PrimaryShippingAddress.City),
								new JProperty("shippingPostalCode", shippingTwoLetterCountryCode.EqualsIgnoreCase("IE")
									? string.Empty
									: customer.PrimaryShippingAddress.Zip),
								new JProperty("shippingCountry", AppLogic.GetCountryTwoLetterISOCode(customer.PrimaryShippingAddress.Country)),
								new JProperty("shippingState", shippingTwoLetterCountryCode.EqualsIgnoreCase("US")
									? GetStateAbbreviationById(customer.PrimaryShippingAddress.StateID)
									: string.Empty)
							)
						)
					);
					var url = (useLiveTransactions
						? $"{AppConfigProvider.GetAppConfigValue("SagePayPi.LiveUrl")}transactions"
						: $"{AppConfigProvider.GetAppConfigValue("SagePayPi.TestUrl")}transactions");

					var apiResponse = SagePayPiApiCall(repeatJsonObject.ToString(), url, "POST");
					var formattedResponse = JObject.Parse(apiResponse);
					var responseHasError = ResponseHasError(formattedResponse, "status", "ok");

					transactionCommandOut = $"URL: {url}, Request: {repeatJsonObject}";
					transactionResponse = formattedResponse.ToString();

					if(responseHasError)
					{
						result = GetResponseError(formattedResponse, "statusDetail", customerID);

						if(result.EqualsIgnoreCase(StringResourceProvider.GetString("sagepaypi.error.unknownresponseerror")))
							result = GetResponseError(formattedResponse, "description", customerID);

						if(result.EqualsIgnoreCase(StringResourceProvider.GetString("sagepaypi.error.unknownresponseerror")))
							result = GetResponseError(formattedResponse, "errors", customerID);

						return $"{StringResourceProvider.GetString("admin.sagepaypi.transaction.repeaterror")} {result}";
					}
					else
					{
						//Get the checkout context so we can use the shipping method the customer chose
						authorizationTransID = formattedResponse["transactionId"].ToString() ?? string.Empty;
						authorizationCode = formattedResponse["bankAuthorisationCode"].ToString() ?? string.Empty;
						authorizationResult = formattedResponse["statusDetail"].ToString() ?? string.Empty;
						avsResult = string.Empty;

						useBillingAddress.CardType = formattedResponse["paymentMethod"]["card"]["cardType"].ToString();
						useBillingAddress.CardNumber = formattedResponse["paymentMethod"]["card"]["lastFourDigits"].ToString();
						useBillingAddress.UpdateDB();

						//Update the transaction id on the original order. This needs freshened up because the Sage Pay gateway archives all 
						//transactions that are older than 2 years old.
						if(RecurringOrderNumberOriginal > 0 && !string.IsNullOrEmpty(authorizationTransID))
							UpdateOriginalTransactionID(RecurringOrderNumberOriginal, authorizationTransID);
					}
				}
				else
				{
					result = $"{StringResourceProvider.GetString("admin.sagepaypi.transaction.repeaterror")} {StringResourceProvider.GetString("admin.sagepaypi.transaction.repeatnotransactionid")}";
				}
			}
			else
			{
				//Make a transaction, if 3d secure is required it will be returned here
				if((threeDSecureEnabled
					&& session[AppLogic.SagePayPi3dSecureKey].EqualsIgnoreCase("false")
					&& session[AppLogic.SagePayPiPaymentMethod].EqualsIgnoreCase("creditcard"))
					|| (!threeDSecureEnabled))
				{
					var jsonObject = new JObject(
						new JProperty("transactionType", AppLogic.TransactionModeIsAuthOnly()
															? "Deferred"
															: "Payment"),
						new JProperty("paymentMethod",
							new JObject(
								new JProperty("card",
									new JObject(
										new JProperty("merchantSessionKey", session[AppLogic.SagePayPiMerchantSessionKey]),
										new JProperty("cardIdentifier", session[AppLogic.SagePayPiCardIdentifier]),
										new JProperty("save", true)
									)
								)
							)
						),
						new JProperty("vendorTxCode", orderNumber.ToString()),
						new JProperty("amount", GetSmallestCurrencyUnit(orderTotal, Localization.StoreCurrency())),
						new JProperty("currency", Localization.StoreCurrency()),
						new JProperty("description", $"{HttpContext.Current.Server.UrlEncode(AppConfigProvider.GetAppConfigValue("StoreName"))} Order {orderNumber}"),
						new JProperty("apply3DSecure", "UseMSPSetting"),
						new JProperty("applyAvsCvcCheck", "UseMSPSetting"),
						new JProperty("customerFirstName", customer.PrimaryBillingAddress.FirstName),
						new JProperty("customerLastName", customer.PrimaryBillingAddress.LastName),
						new JProperty("customerEmail", customer.EMail),
						new JProperty("customerPhone", customer.Phone),
						new JProperty("billingAddress",
							new JObject(
							new JProperty("address1", customer.PrimaryBillingAddress.Address1),
							new JProperty("address2", customer.PrimaryBillingAddress.Address2),
							new JProperty("city", customer.PrimaryBillingAddress.City),
							new JProperty("postalCode", billingTwoLetterCountryCode.EqualsIgnoreCase("IE")
								? string.Empty
								: customer.PrimaryBillingAddress.Zip),
							new JProperty("country", AppLogic.GetCountryTwoLetterISOCode(customer.PrimaryBillingAddress.Country)),
							new JProperty("state", billingTwoLetterCountryCode.EqualsIgnoreCase("US")
								? GetStateAbbreviationById(customer.PrimaryBillingAddress.StateID)
								: string.Empty)
							)
						),
						new JProperty("shippingDetails",
							new JObject(
							new JProperty("recipientFirstName", customer.PrimaryShippingAddress.FirstName),
							new JProperty("recipientLastName", customer.PrimaryShippingAddress.LastName),
							new JProperty("shippingAddress1", customer.PrimaryShippingAddress.Address1),
							new JProperty("shippingAddress2", customer.PrimaryShippingAddress.Address2),
							new JProperty("shippingCity", customer.PrimaryShippingAddress.City),
							new JProperty("shippingPostalCode", shippingTwoLetterCountryCode.EqualsIgnoreCase("IE")
								? string.Empty
								: customer.PrimaryShippingAddress.Zip),
							new JProperty("shippingCountry", AppLogic.GetCountryTwoLetterISOCode(customer.PrimaryShippingAddress.Country)),
							new JProperty("shippingState", shippingTwoLetterCountryCode.EqualsIgnoreCase("US")
								? GetStateAbbreviationById(customer.PrimaryShippingAddress.StateID)
								: string.Empty)
							)
						),
						new JProperty("entryMethod", "Ecommerce")
					);

					var url = (useLiveTransactions
						? $"{AppConfigProvider.GetAppConfigValue("SagePayPi.LiveUrl")}transactions"
						: $"{AppConfigProvider.GetAppConfigValue("SagePayPi.TestUrl")}transactions");

					var apiResponse = SagePayPiApiCall(jsonObject.ToString(), url, "POST");
					var formattedResponse = JObject.Parse(apiResponse);
					var responseHasError = ResponseHasError(formattedResponse, "status", "ok");

					if(responseHasError)
					{
						transactionCommandOut = $"URL: {url}, Request: {jsonObject}";
						transactionResponse = formattedResponse.ToString();

						if(AppConfigProvider.GetAppConfigValue<bool>("SagePayPI.CustomerFriendlyErrors"))
						{
							ClearPaymentMethod(customerID);
							return $"{StringResourceProvider.GetString("sagepaypi.transaction.didnotprocess")} {StringResourceProvider.GetString("sagepaypi.error.reentercarddetails")}";
						}

						result = GetResponseError(formattedResponse, "statusDetail", customerID);

						if(result.EqualsIgnoreCase(StringResourceProvider.GetString("sagepaypi.error.unknownresponseerror")))
							result = GetResponseError(formattedResponse, "description", customerID);

						if(result.EqualsIgnoreCase(StringResourceProvider.GetString("sagepaypi.error.unknownresponseerror")))
							result = GetResponseError(formattedResponse, "errors", customerID);

						ClearPaymentMethod(customerID);
						return $"{StringResourceProvider.GetString("sagepaypi.transaction.paymenterror")} {result.TrimEnd('.')}. {StringResourceProvider.GetString("sagepaypi.error.reentercarddetails")}";
					}
					else
					{
						session[AppLogic.SagePayPiMd] = formattedResponse["transactionId"].ToString();

						//Implement 3-d secure logic if it is required per the transaction response
						if(threeDSecureEnabled
						&& session[AppLogic.SagePayPi3dSecureKey].EqualsIgnoreCase("false")
						&& session[AppLogic.SagePayPiPaymentMethod].EqualsIgnoreCase("creditcard")
						&& formattedResponse["status"].ToString().EqualsIgnoreCase("3dauth")) //Do a 3DSecure redirect
						{
							session[AppLogic.SagePayPiPaReq] = formattedResponse["paReq"].ToString();
							session[AppLogic.SagePayPiTermUrl] = string.Format("{0}{1}",
								HttpContext
								.Current
								.Request
								.Url
								.AbsoluteUri
								.Replace(
									HttpContext
									.Current
									.Request
									.Url
									.LocalPath,
									string.Empty),
								AppConfigProvider.GetAppConfigValue("SagePayPi.3DSecureTermUrl"));
							session[AppLogic.SagePayPiAcsUrl] = formattedResponse["acsUrl"].ToString();
							var sagePayPiThreeDSecureViewModel = new SagePayPiThreeDSecureViewModel(
								paReq: formattedResponse["paReq"].ToString(),
								termUrl: string.Format("{0}{1}",
									HttpContext
									.Current
									.Request
									.Url
									.AbsoluteUri
									.Replace(
										HttpContext
										.Current
										.Request
										.Url
										.LocalPath,
										string.Empty),
									AppConfigProvider.GetAppConfigValue("SagePayPi.3DSecureTermUrl")),
								md: formattedResponse["transactionId"].ToString(),
								acsUrl: formattedResponse["acsUrl"].ToString()
							);
							session["3DSecure.OrderNumber"] = orderNumber.ToString();
							result = AppLogic.ro_3DSecure;
							return result;
						}
					}
				}

				//Do a final retrieval of the completed transaction and record some of the results
				var emptyObject = new JObject();
				var transactionUrl = (useLiveTransactions
					? $"{AppConfigProvider.GetAppConfigValue("SagePayPi.LiveUrl")}transactions/{session[AppLogic.SagePayPiMd]}"
					: $"{AppConfigProvider.GetAppConfigValue("SagePayPi.TestUrl")}transactions/{session[AppLogic.SagePayPiMd]}");

				var apiTransactionResponse = SagePayPiApiCall(emptyObject.ToString(), transactionUrl, "GET");
				var formattedTransactionResponse = JObject.Parse(apiTransactionResponse);
				var transactionResponseError = ResponseHasError(formattedTransactionResponse, "status", "ok");

				transactionCommandOut = $"GET Method - URL: {transactionUrl}";
				transactionResponse = formattedTransactionResponse.ToString();

				if(transactionResponseError)
				{
					if(AppConfigProvider.GetAppConfigValue<bool>("SagePayPI.CustomerFriendlyErrors"))
						result = $"{StringResourceProvider.GetString("sagepaypi.transaction.didnotprocess")} {StringResourceProvider.GetString("sagepaypi.error.reentercarddetails")}";
					else
						result = string.Format(
							"{0} 3-D Secure status: {1}.",
							GetResponseError(formattedTransactionResponse, "statusDetail", customerID),
							GetThreeDSecureStatus(formattedTransactionResponse["3DSecure"]["status"].ToString()));
				}
				else
				{
					var threeDSecureStatus = formattedTransactionResponse["3DSecure"]["status"].ToString();

					if(formattedTransactionResponse["status"].ToString().EqualsIgnoreCase("ok"))
					{
						authorizationTransID = formattedTransactionResponse["transactionId"].ToString() ?? string.Empty;
						authorizationCode = formattedTransactionResponse["bankAuthorisationCode"].ToString() ?? string.Empty;
						authorizationResult = formattedTransactionResponse["statusDetail"].ToString() ?? string.Empty;
						avsResult = string.Empty;

						useBillingAddress.CardType = formattedTransactionResponse["paymentMethod"]["card"]["cardType"].ToString();
						useBillingAddress.CardNumber = formattedTransactionResponse["paymentMethod"]["card"]["lastFourDigits"].ToString();
						useBillingAddress.UpdateDB();
					}
					else
						//if the transaction did not retrieve properly, put an error from sage pay into result
						result = formattedTransactionResponse["statusDetail"].ToString();

					if(threeDSecureEnabled && threeDSecureStatus.ToString().EqualsIgnoreCase("Error"))
					{
						//We need to stop processing the order if 3d secure is required but had an error
						result = $"{StringResourceProvider.GetString("sagepaypi.threedsecure.error")} {StringResourceProvider.GetString("sagepaypi.error.reentercarddetails")}";

						ClearPaymentMethod(customerID);
					}
				}
			}
			return result;
		}

		public override string DisplayName(string LocaleSetting)
		{
			return StringResourceProvider.GetString("sagepaypi.displayname");
		}

		public override bool SupportsAdHocOrders()
		{
			return true;
		}

		public string ObtainSagePayPiMerchantSessionKey()
		{
			var customer = HttpContext.Current.GetCustomer();
			var session = new CustomerSession(customer.CustomerID);
			var useLiveTransactions = AppConfigProvider.GetAppConfigValue<bool>("UseLiveTransactions");
			var url = (useLiveTransactions
				? $"{AppConfigProvider.GetAppConfigValue("SagePayPi.LiveUrl")}merchant-session-keys"
				: $"{AppConfigProvider.GetAppConfigValue("SagePayPi.TestUrl")}merchant-session-keys");
			var jsonObject = new JObject(
					new JProperty("vendorName", AppConfigProvider.GetAppConfigValue("SagePayPi.VendorName"))
				);

			var apiResponse = SagePayPiApiCall(jsonObject.ToString(), url, "POST");
			var formattedResponse = JObject.Parse(apiResponse);
			var responseHasError = ResponseHasError(formattedResponse, "merchantSessionKey", string.Empty);

			if(responseHasError)
				return null;

			var formattedResponseString = formattedResponse
				.Children()
				.Last()
				.Last()
				.ToString()
				.Replace("\"", string.Empty)
				.Replace("\"", "'")
				.ToString();
			session[AppLogic.SagePayPiMerchantSessionKey] = formattedResponseString;

			return formattedResponseString;
		}

		public string SagePayPiApiCall(string dataObject, string url, string callMethod)
		{
			var data = new ASCIIEncoding().GetBytes(dataObject);
			var integrationKey = AppConfigProvider.GetAppConfigValue("SagePayPi.IntegrationKey");
			var integrationPassword = AppConfigProvider.GetAppConfigValue("SagePayPi.IntegrationPassword");
			var encoded = Convert.ToBase64String(Encoding
				.GetEncoding("ISO-8859-1")
				.GetBytes($"{integrationKey}:{integrationPassword}"));

			// Prepare API request
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Headers.Add("Authorization", string.Concat("Basic ", encoded));
			request.Method = callMethod;
			request.ContentType = "application/json";

			// If this is a Post Method, send the content body data and its length
			if(callMethod.EqualsIgnoreCase("post"))
			{
				// Add body length
				request.ContentLength = data.Length;
				// Send body data
				using(var newStream = request.GetRequestStream())
				{
					newStream.Write(data, 0, data.Length);
				}
			}

			// Get the response
			try
			{
				var rawResponseString = string.Empty;
				using(var response = request.GetResponse())
				{
					using(var sr = new StreamReader(response.GetResponseStream()))
					{
						rawResponseString = sr.ReadToEnd();
					}
				}
				return rawResponseString;
			}
			catch(WebException webEx)
			{
				var errorResponse = "{\"status\": \"Error\",  \"statusDetail\": \"An error occured\"}";
				if(webEx.Status == WebExceptionStatus.ProtocolError)
				{
					using(var sr = new StreamReader(webEx.Response.GetResponseStream()))
					{
						errorResponse = sr.ReadToEnd();
					}
				}
				return errorResponse;
			}
			catch(Exception ex)
			{
				SysLog.LogMessage(StringResourceProvider.GetString("sagepaypi.connectionerror"), ex.Message, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
				return "{\"status\": \"Error\",  \"statusDetail\": \"An unknown error occured\"}";
			}
		}

		int GetSmallestCurrencyUnit(decimal amount, string currencyCode)
		{
			var amountToSmallestUnit = 100;
			using(var dbconn = DB.dbConn())
			{
				dbconn.Open();
				using(var rs = DB.GetRS("SELECT AmountToSmallestUnit FROM CurrencyExceptions WHERE CurrencyCode = @CurrencyCode", dbconn,
					new SqlParameter("@CurrencyCode", currencyCode)))
				{
					if(rs.Read())
					{
						if(!string.IsNullOrEmpty(DB.RSField(rs, "AmountToSmallestUnit")))
							amountToSmallestUnit = DB.RSFieldInt(rs, "AmountToSmallestUnit");
					}
				}
			}
			//using ceiling to round up any amount that might come in with a 4 decimal place value
			return (int)Math.Ceiling((amount * amountToSmallestUnit));
		}

		string GetStateAbbreviationById(int stateId)
		{
			var stateAbbreviation = string.Empty;
			using(var dbconn = DB.dbConn())
			{
				dbconn.Open();
				using(var rs = DB.GetRS("SELECT Abbreviation FROM State WHERE StateID = @StateID", dbconn,
					new SqlParameter("@StateID", stateId)))
				{
					if(rs.Read())
					{
						if(!string.IsNullOrEmpty(DB.RSField(rs, "Abbreviation")))
							stateAbbreviation = DB.RSField(rs, "Abbreviation");
					}
				}
			}
			return stateAbbreviation;
		}

		string LookupSagePayPiTransactionId(int orderNumber)
		{
			return DB.GetSqlS("SELECT AuthorizationPNREF AS S FROM Orders with (NOLOCK) WHERE OrderNumber = @OrderNumber",
				new SqlParameter("@OrderNumber", orderNumber));
		}

		public bool ResponseHasError(JObject responseObject, string statusIdentifier, string successIdentifier)
		{
			var isAnError = false;

			if(responseObject[statusIdentifier] == null)
				isAnError = true;
			else if(responseObject[statusIdentifier].ToString().EqualsIgnoreCase("error"))
				isAnError = true;
			else if(!responseObject[statusIdentifier].ToString().EqualsIgnoreCase(successIdentifier)
				&& !responseObject[statusIdentifier].ToString().EqualsIgnoreCase("3dauth")
				&& !(statusIdentifier.EqualsIgnoreCase("merchantSessionKey") && responseObject[statusIdentifier].ToString() != string.Empty))
				isAnError = true;

			return isAnError;
		}

		public string GetResponseError(JObject responseObject, string errorIdentifier, int customerID = 0)
		{
			var errorResult = StringResourceProvider.GetString("sagepaypi.error.unknownresponseerror");
			var error = responseObject[errorIdentifier];

			if(error != null && error.ToString() != string.Empty)
			{
				if(errorIdentifier.EqualsIgnoreCase("errors"))
				{
					var errorDescriptions = new Dictionary<string, string>();
					var errorArray = JArray.Parse(error.ToString());
					var errorObjectsCount = 1;
					var errorDescriptionsCount = 1;
					var formattedErrorMessage = string.Empty;

					foreach(JObject errorObject in errorArray.Children<JObject>())
					{
						foreach(JProperty errorProperty in errorObject.Properties())
						{
							var name = string.Concat(errorObjectsCount, errorProperty.Name);
							var value = (string)errorProperty.Value;

							if(name.EqualsIgnoreCase(string.Concat(errorObjectsCount, "description")))
								errorDescriptions.Add(name, value);
							else if(name.EqualsIgnoreCase(string.Concat(errorObjectsCount, "message")))
								errorDescriptions.Add(name, value);
						}
						errorObjectsCount++;
					}

					foreach(var item in errorDescriptions)
					{
						formattedErrorMessage += errorDescriptions.Count() > 1
							? $"{errorDescriptionsCount}: {item.Value}. "
							: $"{item.Value}. ";
						errorDescriptionsCount++;
					}

					if(!string.IsNullOrEmpty(formattedErrorMessage))
						errorResult = formattedErrorMessage;
				}
				else
				{
					errorResult = error.ToString();
				}
			}

			//If the card identifier or the merchant session key is invalid, we need to clear the payment details to force re-entry
			if(!IsKeyOrIdentifierValid(errorResult) && customerID != 0)
			{
				ClearPaymentMethod(customerID);
				errorResult = ($"{errorResult}. {StringResourceProvider.GetString("sagepaypi.error.reentercarddetails")}").Replace("..", ".");
			}

			return errorResult;
		}

		public bool IsKeyOrIdentifierValid(string status)
		{
			var valid = true;

			switch(status.ToLower())
			{
				case "merchant session key or card identifier invalid":
					valid = false;
					break;
				case "merchant session key not found":
					valid = false;
					break;
			}

			return valid;
		}

		public string GetThreeDSecureStatus(string status)
		{
			var statusCleaned = "N/A";

			switch(status.ToLower())
			{
				case "authenticated":
					statusCleaned = "Authenticated";
					break;
				case "notchecked":
					statusCleaned = "Not Checked";
					break;
				case "notauthenticated":
					statusCleaned = "Not Authenticated";
					break;
				case "error":
					statusCleaned = "Error";
					break;
				case "cardnotenrolled":
					statusCleaned = "Card Not Enrolled";
					break;
				case "issuernotenrolled":
					statusCleaned = "Issuer Not Enrolled";
					break;
				case "malformedorinvalid ":
					statusCleaned = "Malformed or Invalid";
					break;
				case "attemptonly":
					statusCleaned = "Attempt Only";
					break;
				case "incomplete":
					statusCleaned = "Incomplete";
					break;
			}

			return statusCleaned;
		}

		public void ClearPaymentMethod(int customerID)
		{
			DB.ExecuteSQL("UPDATE Customer SET RequestedPaymentMethod = NULL WHERE CustomerID = @CustomerID",
				new SqlParameter("@CustomerID", customerID));
		}

		public void LogFailedTransaction(string transactionCommand, string rawResponseString, int orderNumber)
		{
			// Log failure
			var TransactionCommandOut = transactionCommand.ToString();
			var ThisCustomer = HttpContext.Current.GetCustomer();
			DB.ExecuteSQL(@"insert into FailedTransaction(
				CustomerID,
				OrderNumber,
				IPAddress,
				OrderDate,
				PaymentGateway,
				PaymentMethod,
				TransactionCommand,
				TransactionResult,
				CustomerEMailed
				) values(
				@CustomerID,
				@OrderNumber,
				@IPAddress,
				getdate(),
				@PaymentGateway,
				@PaymentMethod,
				@TransactionCommand,
				@TransactionResult,
				0)",
				new SqlParameter("@CustomerID", ThisCustomer.CustomerID.ToString()),
				new SqlParameter("@OrderNumber", orderNumber.ToString()),
				new SqlParameter("@IPAddress", ThisCustomer.LastIPAddress),
				new SqlParameter("@PaymentGateway", Gateway.ro_GWSAGEPAYPI),
				new SqlParameter("@PaymentMethod", AppLogic.ro_PMCreditCard),
				new SqlParameter("@TransactionCommand", TransactionCommandOut),
				new SqlParameter("@TransactionResult", rawResponseString));
		}

		public void UpdateOriginalTransactionID(int originalRecurringOrderNumber, string newTransactionId)
		{
			DB.ExecuteSQL("UPDATE Orders SET AuthorizationPNREF = @NewTransactionId WHERE OrderNumber = @OriginalRecurringOrderNumber",
				new SqlParameter("@NewTransactionId", newTransactionId),
				new SqlParameter("@OriginalRecurringOrderNumber", originalRecurringOrderNumber));
		}
	}
}
