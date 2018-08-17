// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Validation.AddressValidator
{
	public class NullAddressValidationProvider : IAddressValidationProvider
	{
		public AddressValidationResult Validate(Address address, AddressTypes addressType = AddressTypes.Unknown)
		{
			return new AddressValidationResult();
		}
	}
}
