// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;

namespace AspDotNetStorefront.Addon.Api.OrderInfoV1
{
	public class OrderItemKitDetail
	{
		public int OrderNumber { get; }
		public int KitCartRecId { get; }
		public int CustomerId { get; }
		public int ShoppingCartRecId { get; }
		public int ProductId { get; }
		public int VariantId { get; }
		public LocalizedString ProductName { get; }
		public LocalizedString VariantName { get; }
		public int KitGroupId { get; }
		public LocalizedString KitGroupName { get; }
		public bool KitGroupIsRequired { get; }
		public int KitItemId { get; }
		public LocalizedString KitItemName { get; }
		public decimal KitItemPriceDelta { get; }
		public int Quantity { get; }
		public decimal KitItemWeightDelta { get; }
		public string TextOption { get; }
		public string ExtensionData { get; }
		public int KitGroupTypeId { get; }
		public int InventoryVariantId { get; }
		public string InventoryVariantColor { get; }
		public string InventoryVariantSize { get; }
		public DateTime CreatedOn { get; }
		public int CartType { get; }
		public bool KitGroupIsReadonly { get; }
		public int KitItemInventoryQuantityDelta { get; }
		public DateTime UpdatedOn { get; }

		public OrderItemKitDetail(int orderNumber,
			int kitCartRecId,
			int customerId,
			int shoppingCartRecId,
			int productId,
			int variantId,
			LocalizedString productName,
			LocalizedString variantName,
			int kitGroupId,
			LocalizedString kitGroupName,
			bool kitGroupIsRequired,
			int kitItemId,
			LocalizedString kitItemName,
			decimal kitItemPriceDelta,
			int quantity,
			decimal kitItemWeightDelta,
			string textOption,
			string extensionData,
			int kitGroupTypeId,
			int inventoryVariantId,
			string inventoryVariantColor,
			string inventoryVariantSize,
			DateTime createdOn,
			int cartType,
			bool kitGroupIsReadonly,
			int kitItemInventoryQuantityDelta,
			DateTime updatedOn)
		{
			OrderNumber = orderNumber;
			KitCartRecId = kitCartRecId;
			CustomerId = customerId;
			ShoppingCartRecId = shoppingCartRecId;
			ProductId = productId;
			VariantId = variantId;
			ProductName = productName;
			VariantName = variantName;
			KitGroupId = kitGroupId;
			KitGroupName = kitGroupName;
			KitGroupIsRequired = kitGroupIsRequired;
			KitItemId = kitItemId;
			KitItemName = kitItemName;
			KitItemPriceDelta = kitItemPriceDelta;
			Quantity = quantity;
			KitItemWeightDelta = kitItemWeightDelta;
			TextOption = textOption;
			ExtensionData = extensionData;
			KitGroupTypeId = kitGroupTypeId;
			InventoryVariantId = inventoryVariantId;
			InventoryVariantColor = inventoryVariantColor;
			InventoryVariantSize = inventoryVariantSize;
			CreatedOn = createdOn;
			CartType = cartType;
			KitGroupIsReadonly = kitGroupIsReadonly;
			KitItemInventoryQuantityDelta = kitItemInventoryQuantityDelta;
			UpdatedOn = updatedOn;
		}
	}
}
