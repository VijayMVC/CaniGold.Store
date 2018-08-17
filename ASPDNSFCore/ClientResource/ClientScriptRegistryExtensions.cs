// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AspDotNetStorefront.ClientResource
{
	public static class ClientScriptRegistryExtensions
	{
		public static string RegisterInlineScript(
			this IClientScriptRegistry clientScriptRegistry,
			HttpContextBase httpContext,
			string content,
			string name = null,
			bool addScriptTag = false,
			IEnumerable<string> dependencies = null)
		{
			if(string.IsNullOrWhiteSpace(content))
				return string.Empty;

			var resource = new InlineScriptClientResource(
				content: content,
				name: name,
				addScriptTag: addScriptTag,
				requirements: dependencies);

			var output = clientScriptRegistry.Register(
				httpContext,
				new[] { resource });

			return string.Concat(output);
		}

		public static string RegisterScriptReference(
			this IClientScriptRegistry clientScriptRegistry,
			HttpContextBase httpContext,
			string url,
			bool async = false,
			IEnumerable<string> dependencies = null)
		{
			if(string.IsNullOrWhiteSpace(url))
				return string.Empty;

			var resource = new ScriptReferenceClientResource(url, async, dependencies);

			var output = clientScriptRegistry.Register(
				httpContext,
				new[] { resource });

			return string.Concat(output);
		}

		public static string RegisterScriptBundle(
			this IClientScriptRegistry clientScriptRegistry,
			HttpContextBase httpContext,
			string bundleUrl,
			IEnumerable<string> urls,
			IEnumerable<string> sharedDependencies = null)
		{
			if(urls == null || !urls.Any())
				return string.Empty;

			// Enforce the provided ordering within a registration
			// by making each item in the bundle depend on the previous.
			var resources = new List<ClientScriptResource>();
			string lastUrl = null;
			foreach(var url in urls)
			{
				resources.Add(new BundledScriptReferenceClientResource(
					url,
					bundleUrl,
					new[] { lastUrl }.Concat(sharedDependencies ?? Enumerable.Empty<string>())));

				lastUrl = url;
			}

			var output = clientScriptRegistry.Register(httpContext, resources);

			return string.Concat(output);
		}
	}
}
