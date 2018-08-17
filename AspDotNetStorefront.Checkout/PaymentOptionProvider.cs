// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using AspDotNetStorefront.ClientResource;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways;

namespace AspDotNetStorefront.Checkout
{
	public class PaymentOptionProvider : IPaymentOptionProvider
	{
		readonly UrlHelper UrlHelper;
		readonly IPaymentMethodInfoProvider PaymentMethodInfoProvider;
		readonly IClientScriptRegistry ClientScriptRegistry;
		readonly IReadOnlyDictionary<string, int> PaymentOptionOrdering;
		readonly IEnumerable<string> OffsitePaymentOptionsForDisplay;
		readonly IEnumerable<string> EditablePaymentOptions;

		public PaymentOptionProvider(UrlHelper urlHelper, IPaymentMethodInfoProvider paymentMethodInfoProvider, IClientScriptRegistry clientScriptRegistry)
		{
			UrlHelper = urlHelper;
			PaymentMethodInfoProvider = paymentMethodInfoProvider;
			ClientScriptRegistry = clientScriptRegistry;

			PaymentOptionOrdering = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
			{
				{ AppLogic.ro_PMPayPalExpress, 0 },
				{ AppLogic.ro_PMPayPalCredit, 1 },
				{ AppLogic.ro_PMCreditCard, 2 },
				{ AppLogic.ro_PMPayPalEmbeddedCheckout, 3 },
				{ AppLogic.ro_PMRequestQuote, 4 },
				{ AppLogic.ro_PMPurchaseOrder, 5 },
				{ AppLogic.ro_PMCheckByMail, 6 },
				{ AppLogic.ro_PMCOD, 7 },
				{ AppLogic.ro_PMMicropay, 8 },
			};

			OffsitePaymentOptionsForDisplay = new[]
			{
				AppLogic.ro_PMPayPalExpress,
				AppLogic.ro_PMAmazonPayments,
				AppLogic.ro_PMPayPalCredit
			};

			EditablePaymentOptions = new[]
			{
				AppLogic.ro_PMCreditCard,
				AppLogic.ro_PMPurchaseOrder
			};
		}

		public IEnumerable<PaymentOption> GetPaymentOptions(HttpContextBase httpContext, Customer customer, ShoppingCart cart)
		{
			var configuration = GetPaymentOptionConfiguration();

			var defaultOrderIndex = PaymentOptionOrdering.Values.Max() + 1;
			var activeGateway = AppLogic.ActivePaymentGatewayCleaned();

			var paymentMethods = (configuration.PaymentMethods ?? string.Empty)
				.ParseAsDelimitedList()
				.Select(paymentMethod => AppLogic.CleanPaymentMethod(paymentMethod));

			// Add PayPal Credit if PayPalExpress is enabled
			if(paymentMethods.Contains(AppLogic.ro_PMPayPalExpress)
				&& AppLogic.AppConfigBool("PayPal.Express.ShowPayPalCreditButton"))
				paymentMethods = paymentMethods.Concat(new[] { AppLogic.ro_PMPayPalCredit });

			// Remove the onsite CC option if PayPal Payments Advanced is enabled
			if(paymentMethods.Contains(AppLogic.ro_PMCreditCard) && paymentMethods.Contains(AppLogic.ro_PMPayPalEmbeddedCheckout))
				paymentMethods = paymentMethods.Except(new string[] { AppLogic.ro_PMCreditCard });

			return paymentMethods
				.Select(paymentMethod => PaymentMethodInfoProvider.GetPaymentMethodInfo(
					paymentMethod: paymentMethod,
					gateway: activeGateway))
				.Select(paymentMethod => GetPaymentOption(httpContext, configuration, paymentMethod, customer, cart, defaultOrderIndex))
				.ToArray();
		}

		public bool CheckoutIsOffsiteOnly(HttpContextBase httpContext, Customer customer, ShoppingCart cart)
		{
			var paymentOptions = GetPaymentOptions(httpContext, customer, cart)
				.Where(option => option.Available);

			return paymentOptions.Any()
				? paymentOptions
					.All(option => option.IsOffsiteForDisplay)
				: false;
		}

		PaymentOption GetPaymentOption(HttpContextBase httpContext, PaymentOptionConfiguration configuration, PaymentMethodInfo paymentMethodInfo, Customer customer, ShoppingCart cart, int defaultOrderIndex)
		{
			return new PaymentOption(
				info: paymentMethodInfo,
				available: IsAvailable(paymentMethodInfo, customer, cart),
				displayOrder: PaymentOptionOrdering.ContainsKey(paymentMethodInfo.Name)
					? PaymentOptionOrdering[paymentMethodInfo.Name]
					: defaultOrderIndex,
				selectionImage: GetImage(configuration, paymentMethodInfo.Name),
				editUrl: GetEditUrl(paymentMethodInfo),
				isOffsiteForDisplay: OffsitePaymentOptionsForDisplay.Contains(paymentMethodInfo.Name, StringComparer.OrdinalIgnoreCase),
				isEditable: GetEditableState(paymentMethodInfo),
				paymentScripts: GetPaymentScripts(httpContext, configuration, paymentMethodInfo.Name),
				scriptTarget: GetPaymentScriptTarget(paymentMethodInfo.Name));
		}

		public PaymentOption GetCustomerSelectedPaymentOption(IEnumerable<PaymentOption> paymentOptions, Customer customer)
		{
			return paymentOptions
				.Where(paymentOption => paymentOption.Info.Name == customer.RequestedPaymentMethod)
				.FirstOrDefault();
		}

		public bool PaymentMethodSelectionIsValid(string selectedPaymentMethod, Customer customer)
		{
			var allowedPaymentMethods = AppLogic.AppConfig("PaymentMethods");
			var cleanSelectedPaymentMethod = AppLogic.CleanPaymentMethod(selectedPaymentMethod);

			switch(cleanSelectedPaymentMethod)
			{
				case AppLogic.ro_PMPurchaseOrder:
					return AppLogic.CustomerLevelAllowsPO(customer.CustomerLevelID);
				default:
					return allowedPaymentMethods.Split(',')
						.Where(pm => AppLogic.CleanPaymentMethod(pm) == cleanSelectedPaymentMethod)
						.Any();
			}
		}

		PaymentOptionConfiguration GetPaymentOptionConfiguration()
		{
			return new PaymentOptionConfiguration(
				paymentMethods: AppLogic.AppConfig("PaymentMethods"),
				payPalApiUsername: AppLogic.AppConfig("PayPal.API.Username"),
				payPalExpressButtonImageUrl: AppLogic.AppConfig("PayPal.Express.ButtonImageURL"),
				payPalCreditButtonImageUrl: AppLogic.AppConfig("PayPal.Express.PayPalCreditButtonURL"),
				amazonPaymentsClientId: AppLogic.AppConfig("AmazonPayments.ClientId"),
				amazonPaymentsMerchantId: AppLogic.AppConfig("AmazonPayments.MerchantId"),
				amazonPaymentsCallbackEndpoint: UrlHelper.Action(ActionNames.AmazonPaymentsCallback, ControllerNames.CheckoutAmazonPayments),
				useLiveTransactions: AppLogic.AppConfigBool("UseLiveTransactions"));
		}

		bool IsAvailable(PaymentMethodInfo paymentMethod, Customer customer, ShoppingCart cart)
		{
			//Don't display micropay unless a user is logged in and has a sufficient balance.
			if(paymentMethod.Name == AppLogic.ro_PMMicropay)
			{
				if(!customer.IsRegistered)
					return false;

				if(customer.MicroPayBalance < cart.Total(true))
					return false;
			}

			if(paymentMethod.Name == AppLogic.ro_PMPurchaseOrder)
				return AppLogic.CustomerLevelAllowsPO(customer.CustomerLevelID);

			return true;
		}

		string GetImage(PaymentOptionConfiguration configuration, string paymentMethod)
		{
			if(paymentMethod == AppLogic.ro_PMPayPalExpress)
				return configuration.PayPalExpressButtonImageUrl;
			else if(paymentMethod == AppLogic.ro_PMPayPalCredit)
				return configuration.PayPalCreditButtonImageUrl;
			else
				return null;
		}

		string GetEditUrl(PaymentMethodInfo paymentMethod)
		{
			if(paymentMethod == null)
				return null;

			if(paymentMethod.Location == PaymentMethodLocation.Onsite
				&& paymentMethod.Name == AppLogic.ro_PMCreditCard)
				return UrlHelper.Action(ActionNames.CreditCard, ControllerNames.CheckoutCreditCard);

			if(paymentMethod.Location == PaymentMethodLocation.Onsite
				&& paymentMethod.Name == AppLogic.ro_PMECheck)
				return UrlHelper.Action(ActionNames.ECheck, ControllerNames.CheckoutECheck);

			if(paymentMethod.Name == AppLogic.ro_PMPurchaseOrder)
				return UrlHelper.Action(ActionNames.PurchaseOrder, ControllerNames.CheckoutPurchaseOrder);

			if(paymentMethod.Name == AppLogic.ro_PMPayPalCredit)
				return UrlHelper.Action(
					ActionNames.StartPayPalExpress,
					ControllerNames.PayPalExpress,
					new RouteValueDictionary
					{
						{ RouteDataKeys.IsPayPalCredit, true }
					});

			return UrlHelper.Action(
				ActionNames.SetPaymentMethod,
				ControllerNames.CheckoutPaymentMethod,
				new RouteValueDictionary
				{
					{ RouteDataKeys.SelectedPaymentMethod, paymentMethod.Name }
				});
		}

		bool GetEditableState(PaymentMethodInfo paymentMethod)
		{
			return (EditablePaymentOptions.Contains(paymentMethod.Name, StringComparer.OrdinalIgnoreCase)
				&& !(paymentMethod.Name == AppLogic.ro_PMCreditCard && AppLogic.ActivePaymentGatewayCleaned() == Gateway.ro_GWTWOCHECKOUT));
		}

		IEnumerable<string> GetPaymentScripts(HttpContextBase httpContext, PaymentOptionConfiguration configuration, string paymentMethod)
		{
			// All script ordering below is very intentional. Be careful when changing.
			if(paymentMethod == AppLogic.ro_PMPayPalExpress)
				return new[]
				{
					ClientScriptRegistry.RegisterScriptBundle(
						httpContext,
						bundleUrl: ScriptBundlePaths.CheckoutBundle,
						urls: new[]
						{
							ScriptPaths.PaypalExpress,
						},
						sharedDependencies: new[]
						{
							ScriptPaths.JQuery.AdnsfAlias,
						}),

					ClientScriptRegistry.RegisterInlineScript(
						httpContext,
						name: InlineScripts.OnPayPalReady,
						content: string.Format(
							"PayPalExpress.init('{0}','{1}','{2}','{3}');",
							configuration.PayPalApiUsername,
							configuration.UseLiveTransactions
								? "production"
								: "sandbox",
							GetPaymentScriptTarget(paymentMethod),
							UrlHelper.Action(
								ActionNames.StartPayPalExpress,
								ControllerNames.PayPalExpress)),
						addScriptTag: true,
						dependencies: new[]
						{
							ScriptPaths.PaypalExpress,
						}),

					ClientScriptRegistry.RegisterScriptReference(
						httpContext,
						url: "//www.paypalobjects.com/api/checkout.js",
						async: true,
						dependencies: new[]
						{
							InlineScripts.OnPayPalReady,
						})};

			if(paymentMethod == AppLogic.ro_PMAmazonPayments)
				return new[]
				{
					ClientScriptRegistry.RegisterScriptBundle(
						httpContext,
						bundleUrl: ScriptBundlePaths.AmazonBundle,
						urls: new[] {
							ScriptPaths.AmazonPayments
						},
						sharedDependencies: new[] {
							ScriptPaths.JQuery.AdnsfAlias
						}),

					ClientScriptRegistry.RegisterInlineScript(
						httpContext,
						name: InlineScripts.OnAmazonLoginReady,
						content: string.Format(
							"AdnsfAmazonPayments.initClientId('{0}');",
							configuration.AmazonPaymentsClientId),
						addScriptTag: true,
						dependencies: new[] {
							ScriptPaths.AmazonPayments
						}),

					ClientScriptRegistry.RegisterScriptReference(
						httpContext,
						url: AppLogic.AppConfigBool("UseLiveTransactions")
							? AppLogic.AppConfig("AmazonPayments.LiveScriptUrl")
							: AppLogic.AppConfig("AmazonPayments.SandboxScriptUrl"),
						dependencies: new[] {
							ScriptPaths.AmazonPayments,
							InlineScripts.OnAmazonLoginReady,
						}),

					ClientScriptRegistry.RegisterInlineScript(
						httpContext,
						content: string.Format(
							"AdnsfAmazonPayments.checkoutButton('{0}','{1}','{2}');",
							GetPaymentScriptTarget(paymentMethod),
							configuration.AmazonPaymentsMerchantId,
							configuration.AmazonPaymentsCallbackEndpoint),
						addScriptTag: true,
						dependencies: new[]
						{
							ScriptPaths.AmazonPayments,
							AppLogic.AppConfigBool("UseLiveTransactions")
								? AppLogic.AppConfig("AmazonPayments.LiveScriptUrl")
								: AppLogic.AppConfig("AmazonPayments.SandboxScriptUrl")
						})};

			return null;
		}

		string GetPaymentScriptTarget(string paymentMethod)
		{
			if(paymentMethod == AppLogic.ro_PMPayPalExpress)
				return "PayPalExpressScriptContainerIdentifier";

			if(paymentMethod == AppLogic.ro_PMAmazonPayments)
				return "AmazonPayButton";

			return null;
		}

		public class PaymentOptionConfiguration
		{
			public readonly string PaymentMethods;
			public readonly string PayPalApiUsername;
			public readonly string PayPalExpressButtonImageUrl;
			public readonly string PayPalCreditButtonImageUrl;
			public readonly string AmazonPaymentsClientId;
			public readonly string AmazonPaymentsMerchantId;
			public readonly string AmazonPaymentsCallbackEndpoint;
			public readonly bool UseLiveTransactions;

			public PaymentOptionConfiguration(
				string paymentMethods,
				string payPalApiUsername,
				string payPalExpressButtonImageUrl,
				string payPalCreditButtonImageUrl,
				string amazonPaymentsClientId,
				string amazonPaymentsMerchantId,
				string amazonPaymentsCallbackEndpoint,
				bool useLiveTransactions)
			{
				PaymentMethods = paymentMethods;
				PayPalApiUsername = payPalApiUsername;
				PayPalExpressButtonImageUrl = payPalExpressButtonImageUrl;
				PayPalCreditButtonImageUrl = payPalCreditButtonImageUrl;
				AmazonPaymentsClientId = amazonPaymentsClientId;
				AmazonPaymentsMerchantId = amazonPaymentsMerchantId;
				AmazonPaymentsCallbackEndpoint = amazonPaymentsCallbackEndpoint;
				UseLiveTransactions = useLiveTransactions;
			}
		}
	}
}
