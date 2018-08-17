// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
namespace AspDotNetStorefrontCore.ShippingCalculation
{
	public class AvaTaxRate
	{
		public AvaTaxRate(
			Customer customer,
			int storeId,
			CartTypeEnum cartType,
			decimal shippingTaxRate)
		{
			Customer = customer;
			StoreId = storeId;
			CartType = cartType;
			ShippingTaxRate = shippingTaxRate;
		}

		public Customer Customer
		{ get; set; }

		public int StoreId
		{ get; set; }

		public CartTypeEnum CartType
		{ get; set; }

		public decimal ShippingTaxRate
		{ get; set; }
	}
}
