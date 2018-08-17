// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web.Mvc;

namespace AspDotNetStorefrontCore
{
	class XSLTExtensions : XSLTExtensionBase
	{
		public XSLTExtensions(Customer cust, int skinId, HtmlHelper htmlHelper = null)
			: base(cust, skinId, htmlHelper)
		{
		}
	}
}
