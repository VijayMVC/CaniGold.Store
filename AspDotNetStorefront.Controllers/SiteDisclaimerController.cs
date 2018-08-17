// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Web.Mvc;
using AspDotNetStorefront.Models;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers
{
	public class SiteDisclaimerController : Controller
	{
		readonly AppConfigProvider AppConfigProvider;

		public SiteDisclaimerController(AppConfigProvider appConfigProvider)
		{
			AppConfigProvider = appConfigProvider;
		}

		public ActionResult Index(string returnUrl)
		{
			var customer = HttpContext.GetCustomer();
			var siteDisclaimerTopic = new Topic(
				TopicID: Topic.GetTopicID("SiteDisclaimer", customer.LocaleSetting, customer.StoreID),
				LocaleSetting: customer.LocaleSetting,
				SkinID: customer.SkinID,
				UseParser: new Parser());

			var siteDisclaimerViewModel = new SiteDisclaimerViewModel
			{
				DisclaimerText = siteDisclaimerTopic.Contents,
				ReturnUrl = returnUrl,
			};

			return View(siteDisclaimerViewModel);
		}

		[HttpPost]
		public ActionResult Accept(string returnUrl)
		{
			AppLogic.SetCookie(
				"SiteDisclaimerAccepted",
				new Guid().ToString(),
				TimeSpan.FromDays(1));

			var defaultRedirectUrl = BuildDefaultRedirectUrl("SiteDisclaimerAgreedPage");
			return Redirect(Url.MakeSafeReturnUrl(returnUrl, defaultRedirectUrl));
		}

		[HttpPost]
		public ActionResult Decline()
		{
			var redirectUrl = BuildDefaultRedirectUrl("SiteDisclaimerNotAgreedURL");
			return Redirect(redirectUrl);
		}

		string BuildDefaultRedirectUrl(string defaultUrlAppConfigName)
		{
			var defaultUrl = AppConfigProvider.GetAppConfigValue(defaultUrlAppConfigName);

			Uri redirectUri;
			if(string.IsNullOrWhiteSpace(defaultUrl) || !Uri.TryCreate(defaultUrl, UriKind.RelativeOrAbsolute, out redirectUri))
				return "~/";

			// If the URL is absolute or rooted, use it as-is.
			if(redirectUri.IsAbsoluteUri
				|| redirectUri.OriginalString.StartsWith("/")
				|| redirectUri.OriginalString.StartsWith("~"))
				return defaultUrl;

			// If the URL relative and unrooted, it will be appended
			// to the current URL, which is undesirable. In that case,
			// prepend the virtual root.
			return $"~/{defaultUrl}";
		}
	}
}
