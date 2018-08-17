// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Text;
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.ClientResource;

namespace AspDotNetStorefrontCore
{
	public class PayPalAd
	{
		public enum TargetPage
		{
			Home,
			Cart,
			Product,
			Entity
		}

		public readonly string ImageDimensions = string.Empty;

		public readonly string ImageMarkup = string.Empty;

		public PayPalAd(TargetPage placement)
		{
			var displayConfigName = string.Format("PayPal.Ads.ShowOn{0}Page", placement);
			var enabled = AppLogic.AppConfigBool(displayConfigName) && AppLogic.AppConfigBool("PayPal.Ads.TermsAndConditionsAgreement");
			if(!enabled)
				return;

			var dimensionsConfigName = string.Format("PayPal.Ads.{0}PageDimensions", placement);
			var dimensionsConfig = AppLogic.AppConfig(dimensionsConfigName);
			var publisherId = AppLogic.AppConfig("PayPal.Ads.PublisherId");
			var clientScriptRegistry = DependencyResolver.Current.GetService<IClientScriptRegistry>();

			ImageDimensions = string.IsNullOrEmpty(dimensionsConfig)
				? string.Empty
				: dimensionsConfig;

			ImageMarkup = !string.IsNullOrEmpty(dimensionsConfig) && ImageDimensions.Length > 0
				? GetAdMarkup(clientScriptRegistry, ImageDimensions, publisherId, dimensionsConfig)
				: string.Empty;
		}

		string GetAdMarkup(IClientScriptRegistry clientScriptRegistry, string dimensions, string publisherId, string dimensionsConfig)
		{
			if(string.IsNullOrEmpty(dimensionsConfig) || !dimensionsConfig.Contains(dimensions))
				throw new ArgumentException("Ad dimensions must be in the list of allowable dimensions.");

			var script = new StringBuilder();
			script.Append("<script type=\"text/javascript\">");
			script.Append("	(function (d, t) {\"use strict\";");
			script.Append("	var s = d.getElementsByTagName(t)[0], n = d.createElement(t);");
			script.Append("	n.src = \"//paypal.adtag.where.com/merchant.js\";");
			script.Append("	s.parentNode.insertBefore(n, s);");
			script.Append("}(document, \"script\"));");
			script.Append("</script>");

			var markup = new StringBuilder();
			markup.Append("<div class=\"paypal-banner-wrap\" >");
			markup.AppendFormat("<script type=\"text/javascript\" data-pp-pubid=\"{0}\" data-pp-placementtype=\"{1}\" data-pp-channel=\"vortx\"></script>", publisherId, dimensions);
			markup.Append(clientScriptRegistry.RegisterInlineScript(new HttpContextWrapper(HttpContext.Current), script.ToString()));
			markup.Append("</div>");

			return markup.ToString();
		}
	}
}
