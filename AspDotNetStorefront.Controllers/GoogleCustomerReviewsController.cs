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
	public class GoogleCustomerReviewsController : Controller
	{
		readonly AppConfigProvider AppConfigProvider;

		public GoogleCustomerReviewsController(AppConfigProvider appConfigProvider)
		{
			AppConfigProvider = appConfigProvider;
		}

		public ActionResult GoogleCustomerReviewsOptInSurvey(int orderNumber)
		{
			if(!AppConfigProvider.GetAppConfigValue<bool>("GoogleCustomerReviewsEnabled")
				|| string.IsNullOrEmpty(AppConfigProvider.GetAppConfigValue("GoogleCustomerReviewsMerchantID")))
				return Content(string.Empty);

			long number;
			var customer = HttpContext.GetCustomer();
			var order = new Order(orderNumber, customer.LocaleSetting);

			var model = new GoogleCustomerReviewsOptInSurveyViewModel(
				orderNumber: orderNumber,
				email: !string.IsNullOrEmpty(customer.EMail)
					? customer.EMail
					: "anonymous@anonymous.com",
				countryCode: AppLogic.GetCountryTwoLetterISOCode(order.ShippingAddress.m_Country),
				deliveryDate: DateTime
					.Now
					.AddDays(AppConfigProvider.GetAppConfigValue<int>("GoogleCustomerReviewsDeliveryLeadTime"))
					.ToString("yyyy-MM-dd"),
				badgeEnabled: AppConfigProvider.GetAppConfigValue<bool>("GoogleCustomerReviewsBadgeEnabled"),
				merchantId: long.TryParse(AppConfigProvider.GetAppConfigValue("GoogleCustomerReviewsMerchantID"), out number)
					? number
					: 0,
				language: AppConfigProvider.GetAppConfigValue("GoogleCustomerReviewsLanguage"),
				surveyPosition: AppConfigProvider.GetAppConfigValue("GoogleCustomerReviewsOptInSurveyPosition"));

			return PartialView(ViewNames.GoogleCustomerReviewsOptInSurveyPartial, model);
		}

		public ActionResult GoogleCustomerReviewsBadge()
		{
			if(!AppConfigProvider.GetAppConfigValue<bool>("GoogleCustomerReviewsEnabled")
				|| string.IsNullOrEmpty(AppConfigProvider.GetAppConfigValue("GoogleCustomerReviewsMerchantID"))
				|| !AppConfigProvider.GetAppConfigValue<bool>("GoogleCustomerReviewsBadgeEnabled"))
				return Content(string.Empty);

			long number;
			var model = new GoogleCustomerReviewsBadgeViewModel(
				merchantId: long.TryParse(AppConfigProvider.GetAppConfigValue("GoogleCustomerReviewsMerchantID"), out number)
					? number
					: 0,
				badgePosition: AppConfigProvider.GetAppConfigValue("GoogleCustomerReviewsBadgePosition"),
				language: AppConfigProvider.GetAppConfigValue("GoogleCustomerReviewsLanguage"));

			return PartialView(ViewNames.GoogleCustomerReviewsBadgePartial, model);
		}
	}
}
