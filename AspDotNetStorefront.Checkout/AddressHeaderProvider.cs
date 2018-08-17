// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Checkout
{
	public class AddressHeaderProvider
	{
		public string GetHeaderText(int? addressId, AddressTypes addressType)
		{
			//Does this address already exist?
			var editing = addressId != null;

			//This way the entire final string can be properly translated
			switch(addressType)
			{
				case AddressTypes.Billing:
					return editing
						? AppLogic.GetString("address.editbilling")
						: AppLogic.GetString("address.addbilling");
				case AddressTypes.Shipping:
					return editing
						? AppLogic.GetString("address.editshipping")
						: AppLogic.GetString("address.addshipping");
				default:
					return AppLogic.GetString("address.editaddress");
			}
		}
	}
}
