// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web.Mvc;
using System.Web.Routing;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Filters
{
	public class RequireCustomerRegistrationFilterAttribute : FilterAttribute, IActionFilter
	{
		public void OnActionExecuting(ActionExecutingContext filterContext)
		{
			var customer = filterContext.HttpContext.GetCustomer();

			if(customer.IsRegistered)
				return;

			var returnUrl = filterContext.HttpContext.Request.Url.PathAndQuery;

			filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
			{
				{ RouteDataKeys.Controller, ControllerNames.Account },
				{ RouteDataKeys.Action, ActionNames.SignIn  },
				{ RouteDataKeys.ReturnUrl, returnUrl }
			});
		}

		public void OnActionExecuted(ActionExecutedContext filterContext)
		{ }
	}

	public enum RequiredRegistrationStatus
	{
		Guest,
		Registered,
	}
}
