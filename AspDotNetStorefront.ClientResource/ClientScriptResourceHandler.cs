// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspDotNetStorefront.ClientResource
{
	/// <summary>
	/// Provides a facade that wraps all of the registered <see cref="IClientScriptResourceHandler"/>.
	/// </summary>
	public class ClientScriptResourceHandler
	{
		readonly IEnumerable<IClientScriptResourceHandler> ClientScriptResourceHandlers;

		public ClientScriptResourceHandler(IEnumerable<IClientScriptResourceHandler> clientScriptResourceHandlers)
		{
			ClientScriptResourceHandlers = clientScriptResourceHandlers;
		}

		public IEnumerable<KeyedClientScriptResource> GetKeys(IEnumerable<ClientScriptResource> resources)
		{
			foreach(var handler in ClientScriptResourceHandlers)
				foreach(var result in handler.GetKeys(resources))
					yield return result;
		}

		public IEnumerable<string> Render(IEnumerable<ClientScriptResource> resources, RenderMode renderMode)
		{
			// Build a chain of all of the handler Render() methods, where the input of each method is the output of the next.
			var chain = new Func<IEnumerable<ClientScriptResource>, IEnumerable<ClientScriptResource>>(r => r);
			foreach(var handler in ClientScriptResourceHandlers)
			{
				var previousLink = new Func<IEnumerable<ClientScriptResource>, IEnumerable<ClientScriptResource>>(chain);
				chain = new Func<IEnumerable<ClientScriptResource>, IEnumerable<ClientScriptResource>>(r => handler.Render(previousLink(r), renderMode));
			}

			// Execute the chained method, grabbing out all of the rendered resource contents.
			return chain(resources)
				.OfType<RenderedClientScriptResource>()
				.Select(r => r.Content);
		}
	}
}
