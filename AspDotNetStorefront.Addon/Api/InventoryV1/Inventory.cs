// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;

namespace AspDotNetStorefront.Addon.Api.InventoryV1
{
	class Inventory : IInventory
	{
		readonly Func<IInventoryManager> InventoryManagerFactory;

		public Inventory(Func<IInventoryManager> inventoryManagerFactory)
		{
			InventoryManagerFactory = inventoryManagerFactory;
		}

		public IInventoryManager CreateInventoryManager()
			=> InventoryManagerFactory();
	}
}
