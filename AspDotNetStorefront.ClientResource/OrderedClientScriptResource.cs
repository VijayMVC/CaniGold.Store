// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
namespace AspDotNetStorefront.ClientResource
{
	public class OrderedClientScriptResource
	{
		public readonly int Order;
		public readonly ClientScriptResource Resource;

		public OrderedClientScriptResource(int order, ClientScriptResource resource)
		{
			Order = order;
			Resource = resource;
		}
	}
}
