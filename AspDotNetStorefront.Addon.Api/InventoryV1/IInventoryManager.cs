// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
namespace AspDotNetStorefront.Addon.Api.InventoryV1
{
	public interface IInventoryManager
	{
		IResult SetInventory(int quantity, int variantId);

		IResult SetInventory(int quantity, int variantId, string locale, string size = null, string color = null);
	}
}
