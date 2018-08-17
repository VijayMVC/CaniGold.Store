// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using Autofac;

namespace AspDotNetStorefront.Addon.Api.OrderInfoV1
{
	public class OrderInfoModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder
				.Register(c => new OrderReader(
					localizedStringConverter: c.Resolve<ILocalizedStringConverter>()))
				.As<IOrderReader>();

			builder
				.Register(c => new OrderInfo(
					orderReaderFactory: c.Resolve<Func<IOrderReader>>()))
				.As<IOrderInfo>();
		}
	}
}
