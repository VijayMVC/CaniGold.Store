// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web.Mvc;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers
{
	public class CaptchaController : Controller
	{
		readonly AppConfigProvider AppConfigProvider;

		public CaptchaController(AppConfigProvider appConfigProvider)
		{
			AppConfigProvider = appConfigProvider;
		}

		[ChildActionOnly]
		public ActionResult Index()
		{
			return PartialView(ViewNames.Captcha,
				new CaptchaViewModel(AppConfigProvider.GetAppConfigValue("reCAPTCHA.SiteKey")));
		}
	}
}
