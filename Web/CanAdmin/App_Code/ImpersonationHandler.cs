// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.Auth;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront
{
	public class ImpersonationHandler : IHttpHandler
	{
		readonly Func<IClaimsIdentityProvider> ClaimsIdentityProviderFactory;

		public bool IsReusable
		{
			get { return true; }
		}

		public ImpersonationHandler()
		{
			ClaimsIdentityProviderFactory = DependencyResolver.Current.GetService<Func<IClaimsIdentityProvider>>();
		}

		public void ProcessRequest(HttpContext context)
		{
			var customerId = CommonLogic.QueryStringNativeInt("customerId");

			if(customerId == 0)
			{
				context.Response.Redirect("customers.aspx", false);
				context.ApplicationInstance.CompleteRequest();
				return;
			}

			var targetCustomer = new Customer(customerId);

			var urlHelper = DependencyResolver.Current.GetService<UrlHelper>();

			var adminUser = context.GetCustomer();
			var claimsIdentityProvider = ClaimsIdentityProviderFactory();
			var identity = claimsIdentityProvider.Create(targetCustomer);

			// Record the impersonation in the log
			Security.LogEvent("Impersonation Begin Success",
				string.Format("{0} began impersonating {1}", adminUser.EMail, targetCustomer.EMail),
				targetCustomer.CustomerID,
				adminUser.CustomerID,
				adminUser.CurrentSessionID);

			// Log out the current admin
			adminUser.Logout();
			context.Request
				.GetOwinContext()
				.Authentication
				.SignOut(AuthValues.CookiesAuthenticationType);

			// Log in the customer
			context.Request
				.GetOwinContext()
				.Authentication
				.SignIn(
					properties: new Microsoft.Owin.Security.AuthenticationProperties
					{
						IsPersistent = false
					},
					identities: identity);

			AppLogic.ExecuteSigninLogic(null, targetCustomer);
			targetCustomer.ThisCustomerSession.UpdateCustomerSession(null, null);

			// Flag this as an impersonation session
			targetCustomer.ThisCustomerSession.SetVal(AppLogic.ImpersonationSessionKey, adminUser.CustomerID.ToString());

			// Send them to the front end
			var redirectUrl = urlHelper.ActionForStore(
				actionName: ActionNames.Index,
				controllerName: ControllerNames.Home,
				storeId: targetCustomer.StoreID);
			context.Response.Redirect(redirectUrl, false);
			context.ApplicationInstance.CompleteRequest();
		}
	}
}
