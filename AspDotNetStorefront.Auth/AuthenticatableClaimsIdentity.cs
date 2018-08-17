// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Security.Claims;

namespace AspDotNetStorefront.Auth
{
	public class AuthenticatableClaimsIdentity : ClaimsIdentity
	{
		public override bool IsAuthenticated { get; }

		public AuthenticatableClaimsIdentity(bool isAuthenticated, IEnumerable<Claim> claims, string authenticationType)
			: base(claims, authenticationType)
		{
			IsAuthenticated = isAuthenticated;
		}
	}
}
