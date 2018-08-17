// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web;
using System.Web.Mvc;

namespace AspDotNetStorefront.ClientResource
{
	public static class BundledResourceRendererHelper
	{
		public static IHtmlString RenderStyleBundle(this HtmlHelper htmlHelper, string bundlePath, string[] filePaths, bool inline = false)
		{
			var bundledResourceProvider = DependencyResolver.Current.GetService<IBundledResourceProvider>();
			return MvcHtmlString.Create(bundledResourceProvider.RenderStyleBundle(bundlePath, filePaths, inline));
		}

		public static IHtmlString RenderScriptBundle(this HtmlHelper htmlHelper, string bundlePath, string[] filePaths)
		{
			var bundledResourceProvider = DependencyResolver.Current.GetService<IBundledResourceProvider>();
			return MvcHtmlString.Create(bundledResourceProvider.RenderScriptBundle(bundlePath, filePaths));
		}
	}
}
