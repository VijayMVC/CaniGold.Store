// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using Autofac;

namespace AspDotNetStorefront.Addon.Api.ShippingV1
{
	public class ShippingModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder
				.Register(c => new OrderShipmentManager())
				.As<IOrderShipmentManager>();

			builder
				.Register(c => new Shipping(
					orderShipmentManagerFactory: c.Resolve<Func<IOrderShipmentManager>>()))
				.As<IShipping>();
		}
	}
}
