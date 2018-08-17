// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
namespace AspDotNetStorefront.ClientResource
{
	public class KeyedClientScriptResource
	{
		public readonly string Key;
		public readonly ClientScriptResource Resource;

		public KeyedClientScriptResource(string key, ClientScriptResource resource)
		{
			Key = key;
			Resource = resource;
		}
	}
}
