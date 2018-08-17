// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Web.Mvc;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers
{
	public class RecentAdditionsController : Controller
	{
		public ActionResult Index()
		{
			var xmlpackage = new XmlPackage("page.recentadditions.xml.config");

			var customer = ControllerContext.HttpContext.GetCustomer();
			var packageOutput = AppLogic.RunXmlPackage(xmlpackage, null, customer, customer.SkinID, true, false);

			var pageTitle = AppLogic.GetString("recentadditions.aspx.1", customer.SkinID, customer.LocaleSetting);
			if(!String.IsNullOrEmpty(xmlpackage.SectionTitle))
				pageTitle = xmlpackage.SectionTitle;

			var simplePageViewModel = new SimplePageViewModel
			{
				MetaTitle = xmlpackage.SETitle,
				MetaDescription = xmlpackage.SEDescription,
				MetaKeywords = xmlpackage.SEKeywords,
				PageTitle = pageTitle,
				PageContent = packageOutput,
			};
			return View(ViewNames.SimplePage, simplePageViewModel);
		}
	}
}
