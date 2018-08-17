// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Threading.Tasks;
using AspDotNetStorefrontCore;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;

namespace AspDotNetStorefront.Auth
{
	public class CustomerSessionStore : IAuthenticationSessionStore
	{
		readonly IClaimsIdentityProvider ClaimsIdentityProvider;

		public CustomerSessionStore(IClaimsIdentityProvider claimsIdentityProvider)
		{
			ClaimsIdentityProvider = claimsIdentityProvider;
		}

		public Task RemoveAsync(string key)
		{
			return Task.FromResult(0);
		}

		public Task RenewAsync(string key, AuthenticationTicket ticket)
		{
			return Task.FromResult(0);
		}

		public Task<AuthenticationTicket> RetrieveAsync(string key)
		{
			// Attempt to load a customer from the provided key.
			if(string.IsNullOrWhiteSpace(key))
				return Task.FromResult<AuthenticationTicket>(null);

			Guid customerGuid;
			if(!Guid.TryParse(key, out customerGuid))
				return Task.FromResult<AuthenticationTicket>(null);

			if(customerGuid == Guid.Empty)
				return Task.FromResult<AuthenticationTicket>(null);

			var customer = new Customer(customerGuid);
			if(!StringComparer.OrdinalIgnoreCase.Equals(customer.CustomerGUID, key))
				return Task.FromResult<AuthenticationTicket>(null);

			// Create an identity and authorization ticket with the details from the customer.
			var identity = ClaimsIdentityProvider.Create(customer);

			var ticket = new AuthenticationTicket(identity, new AuthenticationProperties());

			return Task.FromResult(ticket);
		}

		public Task<string> StoreAsync(AuthenticationTicket ticket)
		{
			if(ticket == null || string.IsNullOrEmpty(ticket.Identity.Name))
				return Task.FromResult(Guid.Empty.ToString());

			// The identity's name is the customer GUID.
			return Task.FromResult(ticket.Identity.Name);
		}
	}
}
