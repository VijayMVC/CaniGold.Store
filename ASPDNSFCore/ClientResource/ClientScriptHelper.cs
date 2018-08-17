// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;

namespace AspDotNetStorefront.ClientResource
{
	public static class ClientScriptHelper
	{
		public static IHtmlString RegisterInlineScript(this HtmlHelper htmlHelper, string content, string name = null, IEnumerable<string> dependencies = null)
		{
			var clientScriptRegistry = DependencyResolver.Current.GetService<IClientScriptRegistry>();

			var output = clientScriptRegistry.RegisterInlineScript(
				httpContext: htmlHelper.ViewContext.HttpContext,
				content: content,
				name: name,
				addScriptTag: false,
				dependencies: dependencies);

			return MvcHtmlString.Create(output);
		}

		public static IHtmlString RegisterInlineScript(this HtmlHelper htmlHelper, Func<object, HelperResult> content, string name = null, IEnumerable<string> dependencies = null)
		{
			var clientScriptRegistry = DependencyResolver.Current.GetService<IClientScriptRegistry>();

			var output = clientScriptRegistry.RegisterInlineScript(
				httpContext: htmlHelper.ViewContext.HttpContext,
				content: content(MvcHtmlString.Empty).ToString(),
				name: name,
				addScriptTag: false,
				dependencies: dependencies);

			return MvcHtmlString.Create(output);
		}

		public static IHtmlString RegisterScriptReference(this HtmlHelper htmlHelper, string url, bool async = false, IEnumerable<string> dependencies = null)
		{
			var clientScriptRegistry = DependencyResolver.Current.GetService<IClientScriptRegistry>();

			var output = clientScriptRegistry.RegisterScriptReference(
				htmlHelper.ViewContext.HttpContext,
				url,
				async,
				dependencies);

			return MvcHtmlString.Create(output);
		}

		public static IHtmlString RegisterScriptBundle(this HtmlHelper htmlHelper, string bundleUrl, IEnumerable<string> urls, IEnumerable<string> sharedDependencies = null)
		{
			var clientScriptRegistry = DependencyResolver.Current.GetService<IClientScriptRegistry>();

			var output = clientScriptRegistry.RegisterScriptBundle(
				htmlHelper.ViewContext.HttpContext,
				bundleUrl,
				urls,
				sharedDependencies);

			return MvcHtmlString.Create(output);
		}

		public static IHtmlString RegisterScriptBundle(this HtmlHelper htmlHelper, string bundleUrl, string url, IEnumerable<string> dependencies = null)
		{
			var clientScriptRegistry = DependencyResolver.Current.GetService<IClientScriptRegistry>();

			var output = clientScriptRegistry.RegisterScriptBundle(
				htmlHelper.ViewContext.HttpContext,
				bundleUrl,
				new[] { url },
				dependencies);

			return MvcHtmlString.Create(output);
		}

		public static IHtmlString RenderDeferredScripts(this HtmlHelper htmlHelper)
		{
			var clientScriptRegistry = DependencyResolver.Current.GetService<IClientScriptRegistry>();

			var output = clientScriptRegistry.RenderDeferredResources(htmlHelper.ViewContext.HttpContext);

			return MvcHtmlString.Create(string.Concat(output));
		}
	}
}
