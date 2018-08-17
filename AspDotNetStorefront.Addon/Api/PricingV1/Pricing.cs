// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;

namespace AspDotNetStorefront.Addon.Api.PricingV1
{
	class Pricing : IPricing
	{
		readonly Func<IProductPriceManager> ProductPriceManagerFactory;

		public Pricing(Func<IProductPriceManager> productPriceManagerFactory)
		{
			ProductPriceManagerFactory = productPriceManagerFactory;
		}

		public IProductPriceManager CreateProductPriceManager()
			=> ProductPriceManagerFactory();
	}
}
