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
	/// <summary>
	/// An <see cref="IClientScriptResourceHandler"/> for <see cref="ScriptReferenceClientResource"/>s.
	/// </summary>
	public class ScriptReferenceClientResourceHandler : IClientScriptResourceHandler
	{
		public IEnumerable<KeyedClientScriptResource> GetKeys(IEnumerable<ClientScriptResource> resources)
		{
			return resources
				.OfType<ScriptReferenceClientResource>()
				.Where(resource => !string.IsNullOrEmpty(resource.Path))
				.Select(resource => new KeyedClientScriptResource(
					key: resource.Path,
					resource: resource));
		}

		public IEnumerable<ClientScriptResource> Render(IEnumerable<ClientScriptResource> resources, RenderMode renderMode)
		{
			foreach(var resource in resources)
			{
				if(!(resource is ScriptReferenceClientResource))
				{
					yield return resource;
					continue;
				}

				var referenceResource = (ScriptReferenceClientResource)resource;

				if(string.IsNullOrWhiteSpace(referenceResource.Path))
					continue;

				var effectivePath = VirtualPathUtility.IsAppRelative(referenceResource.Path)
					? VirtualPathUtility.ToAbsolute(referenceResource.Path)
					: referenceResource.Path;

				var asyncAttribute = referenceResource.Async
					? @"async=""true"""
					: string.Empty;

				yield return new RenderedClientScriptResource(
					string.Format(
						@"<script src=""{0}"" {1}></script>",
						HttpUtility.HtmlAttributeEncode(effectivePath),
						asyncAttribute));
			}
		}
	}
}
