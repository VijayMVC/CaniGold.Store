// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace AspDotNetStorefront.Addon.Api.OrderInfoV1
{
	public class OrderItem
	{
		public int OrderNumber { get; }
		public int ShoppingCartRecId { get; }
		public int CustomerId { get; }
		public int ProductId { get; }
		public int VariantId { get; }
		public int Quantity { get; }
		public string Color { get; }
		public string ColorSkuModifier { get; }
		public string Size { get; }
		public string SizeSkuModifier { get; }
		public LocalizedString ProductName { get; }
		public LocalizedString VariantName { get; }
		public string ProductSku { get; }
		public string ManufacturerPartNumber { get; }
		public decimal Weight { get; }
		public decimal Price { get; }
		public decimal RegularPrice { get; }
		public decimal SalePrice { get; }
		public decimal ExtendedPrice { get; }
		public string QuantityDiscountName { get; }
		public int QuantityDiscountId { get; }
		public decimal QuantityDiscountPercent { get; }
		public bool Taxable { get; }
		public bool IsShipSeparately { get; }
		public bool IsDownload { get; }
		public string DownloadLocation { get; }
		public bool FreeShipping { get; }
		public bool IsSecureAttachment { get; }
		public string Textoption { get; }
		public int CartType { get; }
		public int ShippingAddressID { get; }
		public string ShippingDetail { get; }
		public int ShippingMethodId { get; }
		public string ShippingMethod { get; }
		public int DistributorId { get; }
		public string Notes { get; }
		public DateTime? DistributorEmailSentOn { get; }
		public string ExtensionData { get; }
		public LocalizedString SizeOptionPrompt { get; }
		public LocalizedString ColorOptionPrompt { get; }
		public LocalizedString TextOptionPrompt { get; }
		public DateTime CreatedOn { get; }
		public bool CustomerEntersPrice { get; }
		public string CustomerEntersPricePrompt { get; }
		public bool IsAKit { get; }
		public bool IsSystem { get; }
		public int TaxClassId { get; }
		public decimal TaxRate { get; }
		public bool IsGift { get; }
		public int DownloadStatus { get; }
		public int DownloadValidDays { get; }
		public string DownloadCategory { get; }
		public DateTime? DownloadReleasedOn { get; }
		public string GTIN { get; }
		public DateTime UpdatedOn { get; }
		public IEnumerable<OrderItemKitDetail> KitDetails { get; }

		public OrderItem(
			int orderNumber,
			int shoppingCartRecId,
			int customerId,
			int productId,
			int variantId,
			int quantity,
			string color,
			string colorSkuModifier,
			string size,
			string sizeSkuModifier,
			LocalizedString productName,
			LocalizedString variantName,
			string productSku,
			string manufacturerPartNumber,
			decimal weight,
			decimal price,
			decimal regularPrice,
			decimal salePrice,
			decimal extendedPrice,
			string quantityDiscountName,
			int quantityDiscountId,
			decimal quantityDiscountPercent,
			bool taxable,
			bool isShipSeparately,
			bool isDownload,
			string downloadLocation,
			bool freeShipping,
			bool isSecureAttachment,
			string textoption,
			int cartType,
			int shippingAddressID,
			string shippingDetail,
			int shippingMethodId,
			string shippingMethod,
			int distributorId,
			string notes,
			DateTime? distributorEmailSentOn,
			string extensionData,
			LocalizedString sizeOptionPrompt,
			LocalizedString colorOptionPrompt,
			LocalizedString textOptionPrompt,
			DateTime createdOn,
			bool customerEntersPrice,
			string customerEntersPricePrompt,
			bool isAKit,
			bool isSystem,
			int taxClassId,
			decimal taxRate,
			bool isGift,
			int downloadStatus,
			int downloadValidDays,
			string downloadCategory,
			DateTime? downloadReleasedOn,
			string gtin,
			DateTime updatedOn,
			IEnumerable<OrderItemKitDetail> kitDetails)
		{
			OrderNumber = orderNumber;
			ShoppingCartRecId = shoppingCartRecId;
			CustomerId = customerId;
			ProductId = productId;
			VariantId = variantId;
			Quantity = quantity;
			Color = color;
			ColorSkuModifier = colorSkuModifier;
			Size = size;
			SizeSkuModifier = sizeSkuModifier;
			ProductName = productName;
			VariantName = variantName;
			ProductSku = productSku;
			ManufacturerPartNumber = manufacturerPartNumber;
			Weight = weight;
			Price = price;
			RegularPrice = regularPrice;
			SalePrice = salePrice;
			ExtendedPrice = extendedPrice;
			QuantityDiscountName = quantityDiscountName;
			QuantityDiscountId = quantityDiscountId;
			QuantityDiscountPercent = quantityDiscountPercent;
			Taxable = taxable;
			IsShipSeparately = isShipSeparately;
			IsDownload = isDownload;
			DownloadLocation = downloadLocation;
			FreeShipping = freeShipping;
			IsSecureAttachment = isSecureAttachment;
			Textoption = textoption;
			CartType = cartType;
			ShippingAddressID = shippingAddressID;
			ShippingDetail = shippingDetail;
			ShippingMethodId = shippingMethodId;
			ShippingMethod = shippingMethod;
			DistributorId = distributorId;
			Notes = notes;
			DistributorEmailSentOn = distributorEmailSentOn;
			ExtensionData = extensionData;
			SizeOptionPrompt = sizeOptionPrompt;
			ColorOptionPrompt = colorOptionPrompt;
			TextOptionPrompt = textOptionPrompt;
			CreatedOn = createdOn;
			CustomerEntersPrice = customerEntersPrice;
			CustomerEntersPricePrompt = customerEntersPricePrompt;
			IsAKit = isAKit;
			IsSystem = isSystem;
			TaxClassId = taxClassId;
			TaxRate = taxRate;
			IsGift = isGift;
			DownloadStatus = downloadStatus;
			DownloadValidDays = downloadValidDays;
			DownloadCategory = downloadCategory;
			DownloadReleasedOn = downloadReleasedOn;
			GTIN = gtin;
			UpdatedOn = updatedOn;
			KitDetails = kitDetails;
		}
	}
}
