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
	/// Manages a collection of all client resources registered for the current request.
	/// </summary>
	public interface IClientScriptRegistry
	{
		IEnumerable<string> Register(HttpContextBase httpContext, IEnumerable<ClientScriptResource> resource);
		string RenderDeferredResources(HttpContextBase httpContext);
	}

	public abstract class ClientScriptResource
	{
		public readonly IEnumerable<string> Dependencies;

		public ClientScriptResource(IEnumerable<string> dependencies)
		{
			Dependencies = dependencies ?? Enumerable.Empty<string>();
		}
	}
}
