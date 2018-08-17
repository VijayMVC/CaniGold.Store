// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Auth
{
	public interface IClaimsRoleProvider
	{
		IEnumerable<string> GetRoles(Customer customer);
	}
}
