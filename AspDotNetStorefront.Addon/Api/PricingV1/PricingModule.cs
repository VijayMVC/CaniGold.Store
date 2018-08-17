// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using Autofac;

namespace AspDotNetStorefront.Addon.Api.PricingV1
{
	public class PricingModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder
				.Register(c => new ProductPriceManager())
				.As<IProductPriceManager>();

			builder
				.Register(c => new Pricing(c.Resolve<Func<IProductPriceManager>>()))
				.As<IPricing>();
		}
	}
}
