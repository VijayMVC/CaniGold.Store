// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using Autofac;

namespace AspDotNetStorefront.Addon.Api.InventoryV1
{
	public class InventoryModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder
				.Register(c => new InventoryManager(
					localizedStringConverter: c.Resolve<ILocalizedStringConverter>()))
				.As<IInventoryManager>();

			builder
				.Register(c => new Inventory(
					inventoryManagerFactory: c.Resolve<Func<IInventoryManager>>()))
				.As<IInventory>();
		}
	}
}
