// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Security.Claims;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Auth
{
	public class ClaimsIdentityProvider : IClaimsIdentityProvider
	{
		const string IdentityProviderClaimType = "http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider";

		readonly IClaimsRoleProvider ClaimsRoleProvider;
		readonly string DefaultAuthenticationType;
		readonly string DefaultIdentityProvider;

		public ClaimsIdentityProvider(
			IClaimsRoleProvider claimsRoleProvider,
			string defaultAuthenticationType,
			string defaultIdentityProvider)
		{
			ClaimsRoleProvider = claimsRoleProvider;
			DefaultAuthenticationType = defaultAuthenticationType;
			DefaultIdentityProvider = defaultIdentityProvider;
		}

		public ClaimsIdentity CreateAnonymous(string authenticationType = null, string identityProvider = null)
		{
			return new AuthenticatableClaimsIdentity(
				isAuthenticated: false,
				claims: new[]
				{
					new Claim(IdentityProviderClaimType, identityProvider ?? DefaultIdentityProvider),
				},
				authenticationType: authenticationType ?? DefaultAuthenticationType);
		}

		public ClaimsIdentity Create(Customer customer, string authenticationType = null, string identityProvider = null)
		{
			if(customer == null)
				return CreateAnonymous(authenticationType, identityProvider);

			var identity = new AuthenticatableClaimsIdentity(
				isAuthenticated: customer.IsRegistered,
				claims: new[]
				{
					new Claim(ClaimTypes.Name, customer.CustomerGUID),
					new Claim(ClaimTypes.NameIdentifier, customer.CustomerGUID),
					new Claim(IdentityProviderClaimType, identityProvider ?? DefaultIdentityProvider),
				},
				authenticationType: authenticationType ?? DefaultAuthenticationType);

			foreach(var roleName in ClaimsRoleProvider.GetRoles(customer))
			{
				var roleClaim = identity.FindFirst(c =>
					c.Type == ClaimTypes.Role &&
					c.Value == roleName);

				if(roleClaim != null)
					continue;

				identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
			}

			return identity;
		}
	}
}
