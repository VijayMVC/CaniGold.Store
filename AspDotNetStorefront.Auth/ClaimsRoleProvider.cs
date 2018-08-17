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
	public class ClaimsRoleProvider : IClaimsRoleProvider
	{
		public IEnumerable<string> GetRoles(Customer customer)
		{
			var roles = new List<string>();

			if(customer.IsAdminUser)
				roles.Add("Admin");

			if(customer.IsAdminSuperUser)
				roles.Add("SuperAdmin");

			return roles;
		}
	}
}
