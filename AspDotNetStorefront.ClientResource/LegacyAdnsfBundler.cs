// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.ClientResource;

namespace AspDotNetStorefront.Optimization
{
	public static class AdnsfBundler
	{
		[Obsolete("The AdnsfBundler class is obsolete. Use @Html.RenderStyleBundle() from Razor or IBundledResourceProvider.RenderStyleBundle from code.")]
		public static IHtmlString RenderStyleBundle(string bundlePath, string[] filePaths)
		{
			return new MvcHtmlString(DependencyResolver
				.Current
				.GetService<IBundledResourceProvider>()
				.RenderStyleBundle(bundlePath, filePaths));
		}

		[Obsolete("The AdnsfBundler class is obsolete. Use @Html.RenderScriptBundle() from Razor or IBundledResourceProvider.RenderScriptBundle from code.")]
		public static IHtmlString RenderScriptBundle(string bundlePath, string[] filePaths)
		{
			return new MvcHtmlString(DependencyResolver
				.Current
				.GetService<IBundledResourceProvider>()
				.RenderScriptBundle(bundlePath, filePaths));
		}
	}
}
