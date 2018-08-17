// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Net.Http;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers.Classes
{
	public class CaptchaVerificationProvider
	{
		readonly AppConfigProvider AppConfigProvider;
		public readonly string RecaptchaFormKey = "g-recaptcha-response";

		public CaptchaVerificationProvider(AppConfigProvider appConfigProvider)
		{
			AppConfigProvider = appConfigProvider;
		}

		public Result ValidateCaptchaResponse(string token, string customerIpAddress)
		{
			if(string.IsNullOrEmpty(token))
				return Result.Fail(new CaptchaException(AppLogic.GetString("Global.CaptchaInvalid")));

			using(var client = new HttpClient())
			{
				try
				{
					var content = new FormUrlEncodedContent(new Dictionary<string, string>
					{
						{ "secret", AppConfigProvider.GetAppConfigValue("reCAPTCHA.SecretKey") },
						{ "response", token },
						{ "remoteip", customerIpAddress }
					});

					var response = client.PostAsync(AppConfigProvider.GetAppConfigValue("reCAPTCHA.VerifyURL"), content).Result;

					if(!response.IsSuccessStatusCode)
					{
						SysLog.LogMessage("reCAPTCHA validation call failed.", response.ReasonPhrase, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
						return Result.Fail(new CaptchaException(AppLogic.GetString("Global.CaptchaValidationFailed")));
					}

					var responseObject = Newtonsoft.Json
						.JsonConvert
						.DeserializeObject<ReCaptchaResponse>(response.Content.ReadAsStringAsync().Result.Replace('-', '_'));   //Google's response object has weird property names

					if(!responseObject.Success)
					{
						SysLog.LogMessage("reCAPTCHA validation failed.", responseObject.Error_Codes.ToDelimitedString(), MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
						return Result.Fail(new CaptchaException(AppLogic.GetString("Global.CaptchaInvalid")));
					}

					return Result.Ok();
				}
				catch(Exception exception)
				{
					SysLog.LogException(exception, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);
					return Result.Fail(new CaptchaException(exception.Message));
				}
			}
		}
	}

	public class ReCaptchaResponse
	{
		//These names have to match what comes from Google, with - replaced by _
		public bool Success { get; set; }
		public string Challenge_Ts { get; set; }
		public string HostName { get; set; }
		public string[] Error_Codes { get; set; }
	}

	public class CaptchaException : Exception
	{
		public CaptchaException(string message) : base(message)
		{ }
	}
}
