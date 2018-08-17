// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;

namespace AspDotNetStorefront.ClientResource
{
	/// <summary>
	/// Provides the keys and rendering for specific <see cref="ClientScriptResource"/> types.
	/// </summary>
	public interface IClientScriptResourceHandler
	{
		IEnumerable<KeyedClientScriptResource> GetKeys(IEnumerable<ClientScriptResource> resources);

		IEnumerable<ClientScriptResource> Render(IEnumerable<ClientScriptResource> resources, RenderMode renderMode);
	}

	public enum RenderMode
	{
		Immediate,
		Deferred,
	}
}
