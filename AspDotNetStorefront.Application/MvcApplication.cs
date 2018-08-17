// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Configuration;
using System.Net;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using AspDotNetStorefront.Auth;
using AspDotNetStorefront.Routing;
using AspDotNetStorefront.ViewEngine;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

namespace AspDotNetStorefront.Application
{
	public class MvcApplication : Global
	{
		public MvcApplication()
		{
			ApplicationStartCompleted += OnApplicationStartCompleted;
		}

		void OnApplicationStartCompleted(object sender, EventArgs e)
		{
			// ADNSF Supports its own content security policy filter and requires built in support to be shutoff.
			// If a merchant shuts off csp, we need mvc anti-forgery mechanisms to be disabled.
			AntiForgeryConfig.SuppressXFrameOptionsHeader = true;

			GlobalConfiguration.Configure(WebApiConfig.Register);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleTable.EnableOptimizations = StringComparer.OrdinalIgnoreCase.Equals(
				ConfigurationManager.AppSettings["EnableBundlingAndMinification"],
				bool.TrueString);

			DependencyConfig.RegisterDependencies();
			StringResourceConfig.Configure();

			ViewEngines.Engines.Clear();
			ViewEngines.Engines.Add(new SkinAwareViewEngine());

			ServicePointManager.SecurityProtocol =
				SecurityProtocolType.Ssl3
				| SecurityProtocolType.Tls
				| SecurityProtocolType.Tls11
				| SecurityProtocolType.Tls12;
			
			//This prevents the data-val-required attribute from being added to non-nullable form elements that aren't actually flagged as required.
			//Having that attribute on fields incorrectly throws off screen readers.
			DataAnnotationsModelValidatorProvider.AddImplicitRequiredAttributeForValueTypes = false;
		}

		public void Owin_Start(IAppBuilder app)
		{
			var urlHelper = DependencyResolver.Current.GetService<UrlHelper>();

			app.UseCookieAuthentication(new CookieAuthenticationOptions
			{
				AuthenticationType = AuthValues.CookiesAuthenticationType,
				LoginPath = new PathString(urlHelper.Action(ActionNames.SignIn, ControllerNames.Account)),
				LogoutPath = new PathString(urlHelper.Action(ActionNames.SignOut, ControllerNames.Account)),
				CookieHttpOnly = true,
				CookieSecure = CookieSecureOption.Never,
				SessionStore = new CustomerSessionStore(DependencyResolver.Current.GetService<IClaimsIdentityProvider>()),
				Provider = new AppRelativeCookieAuthenticationProvider(),
			});
		}
	}
}
