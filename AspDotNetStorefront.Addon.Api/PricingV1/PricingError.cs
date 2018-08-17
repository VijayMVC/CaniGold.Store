// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
namespace AspDotNetStorefront.Addon.Api.PricingV1
{
	public class PricingError : Error
	{
		public PricingError(string message, Error innerError = null)
			: base(message, innerError)
		{ }

		public class VariantNotFound : PricingError
		{
			public int VariantId { get; }

			public VariantNotFound(int variantId)
				: base($"No variant with the ID {variantId} could be found")
			{
				VariantId = variantId;
			}
		}
	}
}
