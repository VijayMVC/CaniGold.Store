// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Web;
using AspDotNetStorefront.ClientResource;

namespace AspDotNetStorefrontCore.Tokens.Handlers
{
	public class BuySafeSeal : ITokenHandler
	{
		readonly IClientScriptRegistry ClientScriptRegistry;
		readonly Func<HttpContextBase> HttpContextFactory;

		readonly string[] Tokens = { "buysafeseal" };

		public BuySafeSeal(IClientScriptRegistry clientScriptRegistry, Func<HttpContextBase> httpContextFactory)
		{
			ClientScriptRegistry = clientScriptRegistry;
			HttpContextFactory = httpContextFactory;
		}

		public string RenderToken(TokenHandlerContext context)
		{
			if(!Tokens.Contains(context.Token, StringComparer.OrdinalIgnoreCase))
				return null;

			if(!AppLogic.GlobalConfigBool("BuySafe.Enabled")
				|| string.IsNullOrEmpty(AppLogic.GlobalConfig("BuySafe.Hash")))
				return string.Empty;

			var httpContext = HttpContextFactory();
			var rolloverJsLocation = AppLogic.GlobalConfig("BuySafe.RollOverJSLocation");

			var scriptReference = ClientScriptRegistry.RegisterScriptReference(httpContext, rolloverJsLocation);

			var inlineScript = ClientScriptRegistry.RegisterInlineScript(
				httpContext,
				string.Format(
					@"<script type=""text/javascript"">
						buySAFE.Hash = {0};
						WriteBuySafeSeal('BuySafeSealSpan', 'GuaranteedSeal');
					</script>",
					HttpUtility.JavaScriptStringEncode(AppLogic.GlobalConfig("BuySafe.Hash"), true)),
				dependencies: new[]
				{
					rolloverJsLocation,
				});

			return string.Format(@"
				<!-- BEGIN: buySAFE Guarantee Seal -->
				{0}
				<span id=""BuySafeSealSpan""></span>
				{1}
				<!-- END: buySAFE Guarantee Seal -->",
				scriptReference,
				inlineScript);
		}
	}
}
