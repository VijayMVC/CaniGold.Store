// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Classes
{
	public class CaptchaSettings
	{
		public readonly string ReCaptchaSiteKey;
		public readonly string ReCaptchaSecretKey;
		public readonly bool RequireCaptchaOnLogin;
		public readonly bool RequireCaptchaOnCreateAccount;
		public readonly bool RequireCaptchaOnCheckout;
		public readonly bool RequireCaptchaOnContactForm;

		public CaptchaSettings()
		{
			ReCaptchaSiteKey = AppLogic.AppConfig("reCAPTCHA.SiteKey");
			ReCaptchaSecretKey = AppLogic.AppConfig("reCAPTCHA.SecretKey");
			RequireCaptchaOnLogin = AppLogic.AppConfigBool("reCAPTCHA.RequiredOnStoreLogin");
			RequireCaptchaOnCreateAccount = AppLogic.AppConfigBool("reCAPTCHA.RequiredOnCreateAccount");
			RequireCaptchaOnCheckout = AppLogic.AppConfigBool("reCAPTCHA.RequiredOnCheckout");
			RequireCaptchaOnContactForm = AppLogic.AppConfigBool("reCAPTCHA.RequiredOnContactForm");
		}

		public bool CaptchaIsConfigured()
		{
			return !string.IsNullOrEmpty(ReCaptchaSiteKey)
				&& !string.IsNullOrEmpty(ReCaptchaSecretKey);
		}
	}
}
