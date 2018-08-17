// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefrontGateways
{
	public class MaxMindFraudCheck
	{
		const string SessionId = "aspdnsf"; // MaxMind requires this value to identify our cart, do not change

		public class MaxMindResult
		{
			public decimal FraudScore = -1M;
			public string FraudDetails = string.Empty;
			public bool FraudThresholdExceeded = false;
		}

		public Result<MaxMindResult> GetFraudScore(int orderNumber, Customer customer, Address billingAddress, Address shippingAddress, decimal orderAmount, string paymentMethod, int firstItemShippingAddressId = 0)
		{
			var maxMindResult = new MaxMindResult();

			// MaxMind Fraud Protection
			if(!AppLogic.AppConfigBool("MaxMind.Enabled"))
				return Result.Ok(maxMindResult);

			var firstShippingAddress = new Address();

			if(firstItemShippingAddressId != 0 && firstItemShippingAddressId != shippingAddress.AddressID)
				shippingAddress.LoadByCustomer(customer.CustomerID, firstItemShippingAddressId, AddressTypes.Shipping);

			var fraudScore = GetMaxMindFraudScore(
				orderNumber: orderNumber,
				customer: customer,
				billingAddress: billingAddress,
				shippingAddress: firstItemShippingAddressId != 0 ? firstShippingAddress : shippingAddress,
				orderAmount: orderAmount,
				paymentMethod: paymentMethod);

			maxMindResult = fraudScore.Value;

			if(maxMindResult.FraudScore >= AppLogic.AppConfigUSDecimal("MaxMind.FailScoreThreshold"))
				maxMindResult.FraudThresholdExceeded = true;

			return Result.Ok(maxMindResult);
		}

		// consult MaxMind documentation on Fraud Score Threshold Semantics. 0.0 = lowest risk. 10.0 = highest risk.
		Result<MaxMindResult> GetMaxMindFraudScore(int orderNumber, Customer customer, Address billingAddress, Address shippingAddress, decimal orderAmount, string paymentMethod)
		{
			var maxMindResult = new MaxMindResult();

			try
			{

				var email = !string.IsNullOrEmpty(billingAddress.EMail)
					? billingAddress.EMail.Trim()
					: !string.IsNullOrEmpty(customer.EMail)
					? customer.EMail.Trim()
					: string.Empty;

				var billingEMailDomain = string.Empty;
				if(email.Contains("@") && !email.EndsWith("@"))
					billingEMailDomain = email.Substring(email.IndexOf("@") + 1);

				string transactionType;
				switch(paymentMethod.ToUpper())
				{
					case "CREDITCARD":
						transactionType = "creditcard";
						break;
					case "PAYPALEXPRESS":
						transactionType = "paypal";
						break;
					default:
						transactionType = "other";
						break;
				}

				var thisIp = customer.LastIPAddress;
				if(string.IsNullOrEmpty(thisIp))
					thisIp = CommonLogic.CustomerIpAddress();

				var wsdl = AppLogic.AppConfig("MaxMind.SOAPURL").Trim();
				var endpointAddress = new System.ServiceModel.EndpointAddress(new Uri(wsdl));
				var binding = new System.ServiceModel.BasicHttpBinding();
				binding.Name = "minfraudWebServiceSoap";

				var cardNumber = string.Empty;
				if(billingAddress.CardNumber.Length > 6)
					cardNumber = billingAddress.CardNumber.Substring(0, 6);

				var request = new MaxMind.minfraud_soap14RequestBody
				{
					accept_language = customer.LocaleSetting,
					bin = cardNumber,
					city = billingAddress.City,
					country = billingAddress.Country,
					custPhone = billingAddress.Phone,
					domain = billingEMailDomain,
					emailMD5 = Security.GetMD5Hash(email),
					forwardedIP = CommonLogic.ServerVariables("HTTP_X_FORWARDED_FOR"),
					i = thisIp,
					license_key = AppLogic.AppConfig("MaxMind.LicenseKey"),
					requested_type = AppLogic.AppConfig("MaxMind.ServiceType"),
					order_amount = orderAmount.ToString(),
					order_currency = customer.CurrencySetting,
					postal = billingAddress.Zip,
					region = billingAddress.State,
					sessionID = SessionId, // MaxMind requires this value to identify our cart, do not change
					shipAddr = shippingAddress.Address1,
					shipCity = shippingAddress.City,
					shipCountry = shippingAddress.Country,
					shipPostal = shippingAddress.Zip,
					shipRegion = shippingAddress.State,
					txn_type = transactionType,
					txnID = orderNumber.ToString(),
					usernameMD5 = Security.GetMD5Hash(billingAddress.CardName.Trim().ToLowerInvariant())
				};

				MaxMind.minfraudWebServiceSoap mmind = new MaxMind.minfraudWebServiceSoapClient(binding, endpointAddress);
				MaxMind.MINFRAUD rsp = mmind.minfraud_soap14(new MaxMind.minfraud_soap14Request(request)).Body.minfraud_output;

				maxMindResult.FraudScore = Localization.ParseUSDecimal(rsp.riskScore);
				maxMindResult.FraudDetails = SerializeMaxMindResponse(rsp);
			}
			catch(Exception ex)
			{
				maxMindResult.FraudDetails = ex.Message;
			}

			return Result.Ok(maxMindResult); // don't let maxmind exception stop the order
		}

		public static string SerializeMaxMindResponse(MaxMind.MINFRAUD response)
		{
			try
			{
				var serializer = new XmlSerializer(response.GetType());
				using(var stream = new MemoryStream())
				{
					serializer.Serialize(stream, response);
					return Encoding.UTF8.GetString(stream.ToArray());
				}
			}
			catch
			{
				return string.Empty;
			}
		}
	}
}
