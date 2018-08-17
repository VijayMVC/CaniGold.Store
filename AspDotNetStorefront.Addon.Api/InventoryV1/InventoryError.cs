// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
namespace AspDotNetStorefront.Addon.Api.InventoryV1
{
	public class InventoryError : Error
	{
		public InventoryError(string message, Error innerError = null)
			: base(message, innerError)
		{ }

		public class VariantNotFound : InventoryError
		{
			public int VariantId { get; }

			public VariantNotFound(int variantId)
				: base($"No variant with the ID {variantId} could be found")
			{
				VariantId = variantId;
			}
		}

		public class SizeNotDefinedOnVariant : InventoryError
		{
			public int VariantId { get; }
			public string Size { get; }

			public SizeNotDefinedOnVariant(int variantId, string size)
				: base($"The variant with the ID {variantId} does not have the size \"{size}\" defined")
			{
				VariantId = variantId;
				Size = size;
			}
		}

		public class ColorNotDefinedOnVariant : InventoryError
		{
			public int VariantId { get; }
			public string Color { get; }

			public ColorNotDefinedOnVariant(int variantId, string color)
				: base($"The variant with the ID {variantId} does not have the color \"{color}\" defined")
			{
				VariantId = variantId;
				Color = color;
			}
		}

		public class InventoryRecordNotFound : InventoryError
		{
			public int VariantId { get; }
			public string Size { get; }
			public string Color { get; }

			public InventoryRecordNotFound(int variantId, string size, string color)
				: base($"The inventory record for variant ID {variantId}, size \"{size}\" and color \"{color}\" could not be found")
			{
				VariantId = variantId;
				Size = size;
				Color = color;
			}
		}
	}
}
