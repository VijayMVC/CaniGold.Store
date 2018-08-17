// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Web.Mvc;
using AspDotNetStorefrontCore;
using eWAY.Rapid;
using eWAY.Rapid.Enums;
using eWAY.Rapid.Models;
using GatewayEWay;

namespace AspDotNetStorefrontGateways.Processors
{
	public class EWay : GatewayProcessor
	{
		readonly AppConfigProvider AppConfigProvider;

		public EWay()
		{
			AppConfigProvider = DependencyResolver.Current.GetService<AppConfigProvider>();
		}

		public override string ProcessCard(
			int orderNumber,
			int customerId,
			decimal orderTotal,
			bool useLiveTransactions,
			TransactionModeEnum transactionMode,
			AspDotNetStorefrontCore.Address billingAddress,
			string cardExtraCode,
			AspDotNetStorefrontCore.Address useShippingAddress,
			string CAVV,
			string ECI,
			string XID,
			out string AVSResult,
			out string authorizationResult,
			out string authorizationCode,
			out string authorizationTransId,
			out string transactionCommandOut,
			out string transactionResponse)
		{
			AVSResult = string.Empty;
			authorizationResult = string.Empty;
			authorizationCode = string.Empty;
			authorizationTransId = string.Empty;
			transactionCommandOut = string.Empty;
			transactionResponse = string.Empty;

			try
			{
				if(AppConfigProvider.GetAppConfigValue("Localization.StoreCurrency") != "AUD"
					&& AppConfigProvider.GetAppConfigValue("Localization.StoreCurrency") != "NZD")
				{
					return "eWAY requires that the store currency be either AUD or NZD.";
				}

				var cardDetails = new CardDetails()
				{
					Name = $"{billingAddress.FirstName} {billingAddress.LastName}",
					Number = billingAddress.CardNumber,
					ExpiryMonth = FormatExpiryMonth(billingAddress.CardExpirationMonth),
					ExpiryYear = FormatExpiryYear(billingAddress.CardExpirationYear),
					CVN = cardExtraCode
				};

				var address = new eWAY.Rapid.Models.Address()
				{
					Street1 = billingAddress.Address1,
					Street2 = billingAddress.Address2,
					City = billingAddress.City,
					State = billingAddress.State,
					PostalCode = billingAddress.Zip,
					Country = AppLogic.GetCountryTwoLetterISOCode(billingAddress.Country),
				};

				var customer = new eWAY.Rapid.Models.Customer()
				{
					FirstName = billingAddress.FirstName,
					LastName = billingAddress.LastName,
					Email = billingAddress.EMail,
					Phone = billingAddress.Phone,
					CardDetails = cardDetails,
					Address = address
				};

				var paymentDetails = new PaymentDetails()
				{
					TotalAmount = DecimalToFixedPoint(orderTotal),
					InvoiceNumber = orderNumber.ToString(),
					CurrencyCode = Currency.GetDefaultCurrency()
				};

				var transaction = new Transaction()
				{
					Customer = customer,
					PaymentDetails = paymentDetails,
					TransactionType = TransactionTypes.Purchase,
					Capture = transactionMode == TransactionModeEnum.authcapture
						? true
						: false
				};

				transactionCommandOut = SanitizeTransaction(
					XmlCommon.SerializeObject(transaction, transaction.GetType()),
					new Dictionary<string, string>()
					{
						{ $">{billingAddress.CardNumber}</", $">{AppLogic.SafeDisplayCardNumber(billingAddress.CardNumber, "Orders", 0)}</" },
						{ $">{cardExtraCode}</", ">***</" },
					});

				var response = GetRapidClient()
					.Create(PaymentMethod.Direct, transaction);

				if(response.TransactionStatus == null)
				{
					return "Error calling payment gateway.";
				}

				transactionResponse = XmlCommon.SerializeObject(response, response.GetType());

				authorizationResult = response.TransactionStatus.ProcessingDetails?.ResponseMessage;

				if((response.TransactionStatus.Status ?? false) == false)
				{
					var errorCode = string.IsNullOrWhiteSpace(authorizationResult)
						&& response.Errors != null
						? string.Join(" ", response.Errors)
						: authorizationResult;

					return $"There was a problem processing the credit card transaction.  Error code: {errorCode}";
				}

				AVSResult = response.TransactionStatus.VerificationResult?.CVN.ToString();
				authorizationCode = response.TransactionStatus.ProcessingDetails.AuthorisationCode;
				authorizationTransId = response.TransactionStatus.TransactionID.ToString();
			}
			catch(Exception exception)
			{
				SysLog.LogException(exception, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
				return "Error calling payment gateway.";
			}

			return AppLogic.ro_OK;
		}

		public override string CaptureOrder(Order order)
		{
			try
			{
				var payment = new Payment()
				{
					TotalAmount = DecimalToFixedPoint(order.OrderBalance),
					InvoiceNumber = order.OrderNumber.ToString(),
					CurrencyCode = Currency.GetDefaultCurrency()
				};

				var capturePaymentRequest = new CapturePaymentRequest()
				{
					TransactionId = order.AuthorizationPNREF,
					Payment = payment
				};

				order.CaptureTXCommand = XmlCommon.SerializeObject(capturePaymentRequest, capturePaymentRequest.GetType());

				var response = GetRapidClient()
					.CapturePayment(capturePaymentRequest);

				order.CaptureTXResult = XmlCommon.SerializeObject(response, response.GetType());

				if(!response.TransactionStatus)
				{
					var errorCode = string.IsNullOrWhiteSpace(response.ResponseMessage)
						&& response.Errors != null
							? string.Join(" ", response.Errors)
							: response.ResponseMessage;
					return $"There was a problem capuring the credit card transaction.  Error code: {errorCode}.";
				}

				DB.ExecuteSQL(@"update orders set authorizationpnref = @transactionId where ordernumber = @orderNumber",
					new SqlParameter("@transactionId", response.TransactionID),
					new SqlParameter("@orderNumber", order.OrderNumber));
			}
			catch(Exception exception)
			{
				SysLog.LogException(exception, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
				return "Error calling payment gateway.";
			}

			return AppLogic.ro_OK;
		}

		public override string VoidOrder(int orderNumber)
		{
			try
			{
				using(var connection = DB.dbConn())
				{
					connection.Open();

					DB.ExecuteSQL("update orders set voidtxcommand = null, voidtxresult = null where ordernumber = @orderNumber",
						connection,
						new SqlParameter("@orderNumber", orderNumber));

					var authorizationPnref = DB.GetSqlS("select authorizationpnref S from orders with (nolock) where ordernumber = @orderNumber",
						new SqlParameter("@orderNumber", orderNumber));

					var cancel = new CancelAuthorisationRequest()
					{
						TransactionId = authorizationPnref
					};

					DB.ExecuteSQL("update orders set voidtxcommand = @voidTxCommand where ordernumber = @orderNumber",
						connection,
						new SqlParameter("@voidTxCommand", XmlCommon.SerializeObject(cancel, cancel.GetType())),
						new SqlParameter("@orderNumber", orderNumber));

					var response = GetRapidClient()
						.CancelAuthorisation(cancel);

					if(!response.TransactionStatus)
					{
						var errorCode = string.IsNullOrWhiteSpace(response.ResponseMessage)
							&& response.Errors != null
								? string.Join(" ", response.Errors)
								: response.ResponseMessage;
						return $"There was a problem voiding the credit card transaction.  Error code: {errorCode}.";
					}

					DB.ExecuteSQL("update orders set voidtxresult = @voidTxResult where ordernumber = @orderNumber",
						connection,
						new SqlParameter("@voidTxResult", XmlCommon.SerializeObject(response, response.GetType())),
						new SqlParameter("@orderNumber", orderNumber));
				}
			}
			catch(Exception exception)
			{
				SysLog.LogException(exception, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
				return "Error calling payment gateway.";
			}

			return AppLogic.ro_OK;
		}

		public override string RefundOrder(
			int orderNumber,
			int newOrderNumber,
			decimal refundAmount,
			string refundReason,
			AspDotNetStorefrontCore.Address billingAddress)
		{
			try
			{
				using(var connection = DB.dbConn())
				{
					connection.Open();

					DB.ExecuteSQL("update orders set refundtxcommand = null, refundtxresult = null where ordernumber = @orderNumber",
						connection,
						new SqlParameter("@orderNumber", orderNumber));

					var authorizationPnref = string.Empty;
					var totalAmount = refundAmount;

					using(var command = connection.CreateCommand())
					{
						command.CommandText = "select authorizationpnref, ordertotal from orders with (nolock) where ordernumber = @orderNumber";
						command.Parameters.AddWithValue("@orderNumber", orderNumber);

						using(var rs = command.ExecuteReader())
						{
							if(rs.Read())
							{
								authorizationPnref = DB.RSField(rs, "AuthorizationPNREF");
								var orderTotal = DB.RSFieldDecimal(rs, "OrderTotal");
								totalAmount = refundAmount > orderTotal ? orderTotal : refundAmount;
							}
						}
					}

					if(orderNumber == 0)
					{
						return $"There was a problem refunding the credit card transaction.  Order {orderNumber} not found.";
					}

					var refundDetails = new RefundDetails();
					refundDetails.InvoiceNumber = orderNumber.ToString();
					refundDetails.OriginalTransactionID = Convert.ToInt32(authorizationPnref);
					refundDetails.TotalAmount = DecimalToFixedPoint(totalAmount);
					refundDetails.CurrencyCode = Currency.GetDefaultCurrency();

					var refund = new Refund();
					refund.InvoiceDescription = refundReason;
					refund.RefundDetails = refundDetails;

					DB.ExecuteSQL("update orders set refundtxcommand = @refundTxCommand where ordernumber = @orderNumber",
						connection,
						new SqlParameter("@refundTxCommand", XmlCommon.SerializeObject(refund, refund.GetType())),
						new SqlParameter("@orderNumber", orderNumber));

					var response = GetRapidClient()
						.Refund(refund);

					if(!(response.TransactionStatus ?? false))
					{
						var errorCode = string.IsNullOrWhiteSpace(response.ResponseMessage)
							&& response.Errors != null
								? string.Join(" ", response.Errors)
								: response.ResponseMessage;
						return $"There was a problem refunding the credit card transaction.  Error code: { errorCode }.";
					}

					DB.ExecuteSQL("update orders set RefundTXResult = @refundTxResult where OrderNumber = @orderNumber",
						connection,
						new SqlParameter("@refundTxResult", XmlCommon.SerializeObject(response, response.GetType())),
						new SqlParameter("@orderNumber", orderNumber));
				}
			}
			catch(Exception exception)
			{
				SysLog.LogException(exception, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
				return "Error calling payment gateway.";
			}

			return AppLogic.ro_OK;
		}

		IRapidClient GetRapidClient()
		{
			if(string.IsNullOrEmpty(AppConfigProvider.GetAppConfigValue("eWAY.APIKey"))
				|| string.IsNullOrEmpty(AppConfigProvider.GetAppConfigValue("eWAY.APIPassword")))
			{
				throw new Exceptions.MissingConfigurationFailure("Missing eWAY configuration settings.");
			}

			return RapidClientFactory.NewRapidClient(
				apiKey: AppConfigProvider.GetAppConfigValue("eWAY.APIKey"),
				password: AppConfigProvider.GetAppConfigValue("eWAY.APIPassword"),
				rapidEndpoint: AppConfigProvider.GetAppConfigValue<bool>("UseLiveTransactions")
					? "Production"
					: "Sandbox");
		}

		string SanitizeTransaction(string transaction, Dictionary<string, string> replacements)
		{
			var sanitizedTransaction = new StringBuilder(transaction);
			foreach(var replacement in replacements)
			{
				if(!transaction.Contains(replacement.Key))
				{
					return "Could not safely sanitize transaction.";
				}

				sanitizedTransaction.Replace(replacement.Key, replacement.Value);
			}

			return sanitizedTransaction.ToString();
		}

		void LogFailedTransaction(
			AspDotNetStorefrontCore.Customer customer,
			int originalRecurringOrderNumber,
			string paymentMethod,
			string command,
			string result,
			string recurringSubscriptionId)
		{
			DB.ExecuteSQL(
				@"insert into FailedTransaction(
					CustomerID, 
					OrderNumber, 
					IPAddress, 
					OrderDate, 
					PaymentGateway, 
					PaymentMethod, 
					TransactionCommand, 
					TransactionResult, 
					CustomerEMailed, 
					RecurringSubscriptionID) 
				values(
					@customerId, 
					@originalOrderNumber, 
					@ipAddress, 
					getdate(), 
					@gateway, 
					@paymentMethod, 
					@command, 
					@status, 
					0, 
					@subscriptionId)",
				new SqlParameter("@customerId", customer.CustomerID),
				new SqlParameter("@originalOrderNumber", originalRecurringOrderNumber),
				new SqlParameter("@ipAddress", customer.LastIPAddress),
				new SqlParameter("@gateway", Gateway.eWAYGatewayName),
				new SqlParameter("@paymentMethod", paymentMethod),
				new SqlParameter("@command", command),
				new SqlParameter("@status", result),
				new SqlParameter("@subscriptionId", recurringSubscriptionId));
		}

		public override RecurringSupportType RecurringSupportType()
		{
			return Processors.RecurringSupportType.Normal;
		}

		public override string DisplayName(string LocaleSetting)
		{
			return "eWAY";
		}

		int DecimalToFixedPoint(decimal @decimal)
		{
			return Convert.ToInt32(decimal.Round(@decimal, 2, MidpointRounding.AwayFromZero) * 100);
		}

		string FormatExpiryMonth(string month)
		{
			return month.PadLeft(2, '0');
		}

		string FormatExpiryYear(string year)
		{
			switch(year.Length)
			{
				case 2:
					return year;
				case 4:
					return year.Substring(2);
				default:
					throw new Exception("Invalid credit card expiration year");
			}
		}
	}
}
