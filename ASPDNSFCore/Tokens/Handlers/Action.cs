// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc.Html;
using System.Web.Routing;

namespace AspDotNetStorefrontCore.Tokens.Handlers
{
	public class Action : IParameterizedTokenHandler
	{
		const string ActionName = "actionName";
		const string ControllerName = "controllerName";

		readonly string[] Tokens = { "action" };
		readonly string[] ParameterNames = { ActionName, ControllerName };

		readonly TokenParameterConverter TokenParameterConverter;
		readonly TokenHtmlHelper TokenHtmlHelper;

		public Action(TokenParameterConverter tokenParameterConverter, TokenHtmlHelper tokenHtmlHelper)
		{
			TokenParameterConverter = tokenParameterConverter;
			TokenHtmlHelper = tokenHtmlHelper;
		}

		public string RenderToken(TokenHandlerContext context)
		{
			if(!Tokens.Contains(context.Token, StringComparer.OrdinalIgnoreCase))
				return null;

			var actionName = context.Parameters[ActionName];
			if(string.IsNullOrEmpty(actionName))
				return string.Empty;

			var controllerName = context.Parameters[ControllerName];
			if(string.IsNullOrEmpty(controllerName))
				return string.Empty;

			var routeValues = new RouteValueDictionary(context.Parameters
				.Keys
				.Except(ParameterNames, StringComparer.OrdinalIgnoreCase)
				.ToDictionary(
					key => key,
					key => (object)context.Parameters[key]));

			return TokenHtmlHelper
				.Action(
					actionName: actionName,
					controllerName: controllerName,
					routeValues: routeValues)
				.ToString();
		}

		public IDictionary<string, string> ConvertPositionalParametersToNamedParameters(IEnumerable<string> positionalParameters)
		{
			return TokenParameterConverter.ConvertPositionalToNamedParameters(positionalParameters, ParameterNames);
		}
	}
}
