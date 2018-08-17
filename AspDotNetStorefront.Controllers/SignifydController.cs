// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using AspDotNetStorefront.Signifyd;
using AspDotNetStorefront.Signifyd.Model;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways;

namespace AspDotNetStorefront.Controllers
{
	public class SignifydController : Controller
	{
		const string RequestHeaderSignifydTopic = "X-SIGNIFYD-TOPIC";
		const string RequestHeaderSignifydHash = "X-SIGNIFYD-SEC-HMAC-SHA256";
		const string SignifydTestHashKey = "ABCDE";

		const string TopicCaseCreation = "cases/creation";
		const string TopicCaseRescore = "cases/rescore";
		const string TopicCaseReview = "cases/review";
		const string TopicGuaranteeCompletion = "guarantees/completion";
		const string TopicClaimReviewed = "claim/reviewed";
		const string TopicTest = "cases/test";

		readonly AppConfigProvider AppConfigProvider;
		readonly SignifydCaseApi SignifydApi;
		readonly SignifydConfigurationProvider SignifydConfigurationProvider;

		public SignifydController(SignifydCaseApi signifydApi, SignifydConfigurationProvider signifydConfigurationProvider, AppConfigProvider appConfigProvider)
		{
			AppConfigProvider = appConfigProvider;
			SignifydApi = signifydApi;
			SignifydConfigurationProvider = signifydConfigurationProvider;
		}

		[HttpPost]
		[AllowAnonymous]
		public ActionResult Webhook(WebhookPayload webhookPayload)
		{
			if(AppConfigProvider.GetAppConfigValue<bool>("Signifyd.Log.Enabled"))
				SysLog.LogMessage(
					message: "Received a webhook call from Signifyd.",
					details: webhookPayload.ToString(),
					messageType: MessageTypeEnum.Informational,
					messageSeverity: MessageSeverityEnum.Alert);

			var signifydConfiguration = SignifydConfigurationProvider.Create();

			if(!VerifyAuthenticity(signifydConfiguration))
				return null;

			// Get current order status
			var orderStatus = DB.GetSqlS("select GuaranteedStatus S from [SignifydOrderStatus] where OrderNumber = @orderNumber", new SqlParameter("@orderNumber", (int)webhookPayload.OrderId));

			// if not found, report error and bail
			if(string.IsNullOrEmpty(orderStatus))
			{
				if(AppConfigProvider.GetAppConfigValue<bool>("Signifyd.Log.Enabled"))
					SysLog.LogException(new Exception($"Signifyd called Webhook for non-existent order {webhookPayload.OrderId}"), MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
				return null;
			}

			var orderDisposition = (GuaranteeDisposition)Enum.Parse(typeof(GuaranteeDisposition), orderStatus, true);

			switch(Request.Headers[RequestHeaderSignifydTopic])
			{
				case TopicGuaranteeCompletion:

					var order = new Order((int)webhookPayload.OrderId);

					if(webhookPayload.GuaranteeDisposition == GuaranteeDisposition.APPROVED
						&& (orderDisposition == GuaranteeDisposition.PENDING
						|| orderDisposition == GuaranteeDisposition.INREVIEW))
						OrderApproved(order, AppLogic.TransactionModeIsAuthCapture());

					if(webhookPayload.GuaranteeDisposition == GuaranteeDisposition.DECLINED
						&& (orderDisposition == GuaranteeDisposition.PENDING
						|| orderDisposition == GuaranteeDisposition.INREVIEW))
						OrderDeclined(order, signifydConfiguration.DeclineActionVoid);

					DB.ExecuteSQL("update [SignifydOrderStatus] set GuaranteedStatus = @guaranteeStatus where OrderNumber = @orderNumber",
						new[]
						{
							new SqlParameter("@guaranteeStatus", webhookPayload.GuaranteeDisposition.ToString()),
							new SqlParameter("@orderNumber", (int)webhookPayload.OrderId)
						});

					break;

				case TopicCaseCreation:
					break;

				case TopicCaseRescore:
					break;

				case TopicCaseReview:
					break;

				case TopicClaimReviewed:
					break;

				case TopicTest:
					break;

				default:
					SysLog.LogException(new Exception($"Unrecognized Signifyd webhook topic: {Request.Headers[RequestHeaderSignifydTopic]}"), MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
					break;
			}

			return null;
		}

		bool VerifyAuthenticity(SignifydConfiguration signifydConfiguration)
		{
			// Check the hash of this message to make sure it came from Signifyd

			// If this is the Test topic, check if it was hashed with their test code
			// This happens if someone clicks 'Test' in the web console for our webhook
			if(Request.Headers[RequestHeaderSignifydTopic] == TopicTest)
			{
				using(var hash = new HMACSHA256(Encoding.UTF8.GetBytes(SignifydTestHashKey)))
				{
					Request.InputStream.Position = 0;
					var localEncodedJsonBodyHash = Convert.ToBase64String(hash.ComputeHash(Request.InputStream));

					if(localEncodedJsonBodyHash == Request.Headers[RequestHeaderSignifydHash])  // This Test topic was hashed using their Test hash key
					{
						if(AppConfigProvider.GetAppConfigValue<bool>("Signifyd.Log.Enabled"))
							SysLog.LogMessage(
								message: "Signifyd test post received.",
								details: "Signifyd test post received.",
								messageType: MessageTypeEnum.Informational,
								messageSeverity: MessageSeverityEnum.Message);

						return false;
					}
				}
			}

			// This message should be hashed with our API key; let's verify the message authenticity
			using(var hash = new HMACSHA256(Encoding.UTF8.GetBytes(signifydConfiguration.AccessToken)))
			{
				Request.InputStream.Position = 0;
				var localEncodedJsonBodyHash = Convert.ToBase64String(hash.ComputeHash(Request.InputStream));

				if(localEncodedJsonBodyHash != Request.Headers[RequestHeaderSignifydHash])
				{
					if(AppConfigProvider.GetAppConfigValue<bool>("Signifyd.Log.Enabled"))
						SysLog.LogException(
							ex: new Exception($"Signifyd hash does not match locally calculated hash; could not validate authenticity."),
							messageType: MessageTypeEnum.GeneralException,
							messageSeverity: MessageSeverityEnum.Error);

					return false;
				}
			}

			return true;
		}

		void OrderApproved(Order order, bool captureOrder)
		{
			// this is the status that gets returned if configuration doesn't allow capture of the order; otherwise it's the gateway's return status
			var status = "Not captured because of configuration setting - manual capture required.";

			if(captureOrder)
				status = Gateway.DispatchCapture(AppLogic.CleanPaymentGateway(order.PaymentGateway), order.OrderNumber);

			// notify admin, only if there was a problem during capture (or manual capture required)
			if(status != AppLogic.ro_OK)
				SendEmailsAndLog(
					message: $"Signifyd guarantee APPROVED for order {order.OrderNumber}.",
					detail: $"Capture result: {status}");
		}

		void OrderDeclined(Order order, bool voidOrder)
		{
			// this is the status that gets returned if configuration doesn't allow an actual void of the order; otherwise it's the gateway's return status
			var status = "Not voided because of configuration setting - manual void required.";

			if(voidOrder)
				status = Gateway.OrderManagement_DoVoid(order);

			SendEmailsAndLog(
				message: $"Signifyd guarantee DECLINED for order {order.OrderNumber}.",
				detail: $"Void result: {status}");
		}

		void SendEmailsAndLog(string message, string detail)
		{
			if(AppConfigProvider.GetAppConfigValue<bool>("Signifyd.Log.Enabled"))
				SysLog.LogMessage(
					message: message,
					details: detail,
					messageType: MessageTypeEnum.Informational,
					messageSeverity: MessageSeverityEnum.Message);

			if(AppConfigProvider.GetAppConfigValue<bool>("TurnOffStoreAdminEMailNotifications"))
				return;

			var sendToList = AppLogic.AppConfig("GotOrderEMailTo").Replace(",", ";");
			if(string.IsNullOrEmpty(sendToList))
				return;

			foreach(var address in sendToList.Split(';'))
			{
				try
				{
					AppLogic.SendMail(
						subject: message,
						body: detail,
						useHtml: true,
						fromAddress: AppConfigProvider.GetAppConfigValue("GotOrderEMailFrom"),
						fromName: AppConfigProvider.GetAppConfigValue("GotOrderEMailFromName"),
						toAddress: address,
						toName: address);
				}
				// swallow any exceptions from sending the notification email
				catch { }
			}
		}
	}
}
