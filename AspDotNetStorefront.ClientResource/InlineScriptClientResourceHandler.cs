// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.ClientResource
{
	/// <summary>
	/// An <see cref="IClientScriptResourceHandler"/> for <see cref="InlineScriptClientResource"/>s.
	/// </summary>
	public class InlineScriptClientResourceHandler : IClientScriptResourceHandler
	{
		public IEnumerable<KeyedClientScriptResource> GetKeys(IEnumerable<ClientScriptResource> resources)
		{
			return resources
				.OfType<InlineScriptClientResource>()
				.Where(resource => !string.IsNullOrEmpty(resource.Content))
				.Select(resource => new KeyedClientScriptResource(
					key: string.IsNullOrWhiteSpace(resource.Name)
						? MD5
							.Create()
							.ComputeHash(Encoding.UTF8.GetBytes(resource.Content))
							.ToString(delimiter: string.Empty)
						: resource.Name,
					resource: resource));
		}

		public IEnumerable<ClientScriptResource> Render(IEnumerable<ClientScriptResource> resources, RenderMode renderMode)
		{
			foreach(var resource in resources)
			{
				if(!(resource is InlineScriptClientResource))
				{
					yield return resource;
					continue;
				}

				var inlineResource = (InlineScriptClientResource)resource;

				if(string.IsNullOrWhiteSpace(inlineResource.Content))
					continue;

				yield return new RenderedClientScriptResource(
					string.Format(
						inlineResource.AddScriptTag
							? "<script>{0}</script>{1}"
							: "{0}{1}",
						inlineResource.Content,
						Environment.NewLine));
			}
		}
	}
}
