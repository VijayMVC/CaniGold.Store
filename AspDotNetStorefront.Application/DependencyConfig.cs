// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using AspDotNetStorefront.Auth;
using AspDotNetStorefront.Caching.ObjectCaching;
using AspDotNetStorefront.Caching.ObjectCaching.Dependency;
using AspDotNetStorefront.Caching.ObjectCaching.ObjectProvider;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Checkout.AppliedPaymentMethodCleanup;
using AspDotNetStorefront.Checkout.Engine;
using AspDotNetStorefront.Checkout.PreCheckoutRule;
using AspDotNetStorefront.Classes;
using AspDotNetStorefront.Classes.Checkout;
using AspDotNetStorefront.ClientResource;
using AspDotNetStorefront.ClientResource.Dependencies;
using AspDotNetStorefront.Controllers.Classes;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Payment.Wallet;
using AspDotNetStorefront.Routing;
using AspDotNetStorefront.Shipping;
using AspDotNetStorefront.Signifyd;
using AspDotNetStorefront.StringResource;
using AspDotNetStorefront.Validation;
using AspDotNetStorefront.Validation.AddressValidator;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontCore.ShippingCalculation;
using AspDotNetStorefrontCore.Tokens;
using AspDotNetStorefrontCore.Validation;
using AspDotNetStorefrontGateways.Processors;
using Autofac;
using Autofac.Builder;
using Autofac.Configuration;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using Microsoft.Extensions.Configuration;

namespace AspDotNetStorefront.Application
{
	public static class DependencyConfig
	{
		public static void RegisterDependencies()
		{
			var builder = new ContainerBuilder();

			#region MVC

			builder.RegisterFilterProvider();
			builder.RegisterControllers(typeof(Controllers.HomeController).Assembly);
			builder.RegisterModule<AutofacWebTypesModule>();

			#endregion

			#region Addon API Modules

			builder.RegisterModule<Addon.Api.AddonApiServiceModule>();
			builder.RegisterModule<Addon.Api.OrderInfoV1.OrderInfoModule>();
			builder.RegisterModule<Addon.Api.ShippingV1.ShippingModule>();
			builder.RegisterModule<Addon.Api.InventoryV1.InventoryModule>();
			builder.RegisterModule<Addon.Api.PricingV1.PricingModule>();

			#endregion

			#region AppConfigs

			builder
				.Register(c => new AppConfigLoader())
				.AsSelf();

			builder
				.Register(c => new AppConfigCache(
					appConfigLoader: c.Resolve<AppConfigLoader>()))
				.AsSelf()
				.SingleInstance()
				.OnActivating(c => c.Instance.ResetCache());

			builder
				.Register(c => new AppConfigProvider(
					appConfigCache: c.Resolve<AppConfigCache>()))
				.AsSelf();

			builder
				.Register(c => new AppConfigValueConverter())
				.AsSelf();

			#endregion

			#region GlobalConfigs

			builder
				.Register(c => new GlobalConfigLoader())
				.AsSelf()
				.SingleInstance();

			builder
				.Register(c => new GlobalConfigCache(
					globalConfigLoader: c.Resolve<GlobalConfigLoader>()))
				.AsSelf()
				.SingleInstance();

			#endregion


			#region Caching

			builder
				.Register(c => new CachedShoppingCartProvider(
					cachedObjectProvider: c.Resolve<CachedObjectProvider<ShoppingCart, ShoppingCartCacheContext>>()))
				.SingleInstance()
				.As<ICachedShoppingCartProvider>();

			builder
				.Register(c => new ShoppingCartCachedObjectContextBuilder(
					appConfigValueConverter: c.Resolve<AppConfigValueConverter>()))
				.SingleInstance()
				.As<ICachedObjectContextBuilder<ShoppingCart, ShoppingCartCacheContext>>();

			builder
				.Register(c => new CachedAvaTaxRateProvider(
					cachedObjectProvider: c.Resolve<CachedObjectProvider<AvaTaxRate, AvaTaxRateCacheContext>>()))
				.SingleInstance()
				.As<ICachedAvaTaxRateProvider>();

			builder
				.Register(c => new ShippingRateCachedObjectContextBuilder(
					appConfigValueConverter: c.Resolve<AppConfigValueConverter>(),
					cachedShoppingCartProvider: c.Resolve<ICachedShoppingCartProvider>()))
				.SingleInstance()
				.As<ICachedObjectContextBuilder<AvaTaxRate, AvaTaxRateCacheContext>>();

			builder
				.Register(c => new CachedShippingMethodCollectionProvider(
					cachedObjectProvider: c.Resolve<CachedObjectProvider<ShippingMethodCollection, ShippingMethodCollectionCacheContext>>()))
				.SingleInstance()
				.As<ICachedShippingMethodCollectionProvider>();

			builder
				.Register(c => new ShippingMethodCollectionCachedObjectContextBuilder(
					shippingMethodCollectionProvider: c.Resolve<IShippingMethodCollectionProvider>(),
					appConfigValueConverter: c.Resolve<AppConfigValueConverter>()))
				.SingleInstance()
				.As<ICachedObjectContextBuilder<ShippingMethodCollection, ShippingMethodCollectionCacheContext>>();

			builder
				.RegisterGeneric(typeof(CachedObjectProvider<,>))
				.SingleInstance()
				.AsSelf();

			builder
				.Register(c => new CacheProvider())
				.SingleInstance()
				.AsSelf();

			builder
				.Register(c => new HashProvider())
				.SingleInstance()
				.AsSelf();

			builder
				.Register(c => new CacheDependencyEvaluator(
					cacheProvider: c.Resolve<CacheProvider>(),
					dependencyStateProvider: c.Resolve<DependencyStateProvider>()))
				.SingleInstance()
				.AsSelf();

			builder
				.Register(c => new DependencyStateProvider(
					managers: c.Resolve<IEnumerable<IDependencyStateManager>>()))
				.SingleInstance()
				.AsSelf();

			builder
				.Register(c => new AppConfigDependencyStateManager(
					appConfigProvider: c.Resolve<AppConfigProvider>(),
					hashProvider: c.Resolve<HashProvider>()))
				.As<IDependencyStateManager>()
				.SingleInstance()
				.AsSelf();

			builder
				.Register(c => new AppConfigValueDependencyStateManager(
					appConfigProvider: c.Resolve<AppConfigProvider>(),
					hashProvider: c.Resolve<HashProvider>()))
				.As<IDependencyStateManager>()
				.SingleInstance()
				.AsSelf();

			builder
				.Register(c => new QueryDependencyStateManager(
					appConfigProvider: c.Resolve<AppConfigProvider>(),
					hashProvider: c.Resolve<HashProvider>()))
				.As<IDependencyStateManager>()
				.SingleInstance()
				.AsSelf();

			builder
				.Register(c => new CacheEntryDependencyStateManager(
					appConfigProvider: c.Resolve<AppConfigProvider>(),
					cacheProvider: c.Resolve<CacheProvider>()))
				.As<IDependencyStateManager>()
				.SingleInstance()
				.AsSelf();

			builder
				.Register(c => new CheckoutShippingSelectionDependencyStateManager(
					appConfigProvider: c.Resolve<AppConfigProvider>(),
					checkoutSelectionProvider: c.Resolve<ICheckoutSelectionProvider>(),
					persistedCheckoutContextProvider: c.Resolve<IPersistedCheckoutContextProvider>(),
					paymentMethodInfoProvider: c.Resolve<IPaymentMethodInfoProvider>(),
					hashProvider: c.Resolve<HashProvider>()))
				.As<IDependencyStateManager>()
				.SingleInstance()
				.AsSelf();

			#endregion

			#region Checkout

			#region Checkout - Pre-Checkout Rules

			builder
				.Register(c => new OffsiteAddressRestrictionPreCheckoutRule(
					noticeProvider: c.Resolve<NoticeProvider>(),
					cachedShoppingCartProvider: c.Resolve<ICachedShoppingCartProvider>()))
				.As<IPreCheckoutRule>();

			builder
				.Register(c => new OffsiteAddressButInvalidPaymentMethodPreCheckoutRule())
				.As<IPreCheckoutRule>();

			builder
				.Register(c => new InventoryTrimmedPreCheckoutRule(
					cachedShoppingCartProvider: c.Resolve<ICachedShoppingCartProvider>(),
					noticeProvider: c.Resolve<NoticeProvider>(),
					cartActionProvider: c.Resolve<CartActionProvider>()))
				.As<IPreCheckoutRule>();

			builder
				.Register(c => new CartAgePreCheckoutRule(
					cachedShoppingCartProvider: c.Resolve<ICachedShoppingCartProvider>(),
					cartActionProvider: c.Resolve<CartActionProvider>()))
				.As<IPreCheckoutRule>();

			builder
				.Register(c => new PaymentMethodPreCheckoutRule(
					cachedShoppingCartProvider: c.Resolve<ICachedShoppingCartProvider>()))
				.As<IPreCheckoutRule>();

			builder
				.Register(c => new ShippingSelectionPreCheckoutRule(
					cachedShippingMethodCollectionProvider: c.Resolve<ICachedShippingMethodCollectionProvider>(),
					cachedShoppingCartProvider: c.Resolve<ICachedShoppingCartProvider>(),
					shippingMethodCartItemApplicator: c.Resolve<IShippingMethodCartItemApplicator>(),
					effectiveShippingAddressProvider: c.Resolve<IEffectiveShippingAddressProvider>()))
				.As<IPreCheckoutRule>();

			builder
				.Register(c => new CartItemAddressPreCheckoutRule(
					cachedShoppingCartProvider: c.Resolve<ICachedShoppingCartProvider>()))
				.As<IPreCheckoutRule>();

			#endregion

			#region Checkout - Payment Method Cleanups

			builder
				.Register(c => new AmazonAppliedPaymentMethodCleanup(
					persistedCheckoutContextProvider: c.Resolve<IPersistedCheckoutContextProvider>()))
				.As<IAppliedPaymentMethodCleanup>();

			builder
				.Register(c => new PayPalExpressAppliedPaymentMethodCleanup(
					persistedCheckoutContextProvider: c.Resolve<IPersistedCheckoutContextProvider>()))
				.As<IAppliedPaymentMethodCleanup>();

			#endregion

			builder
				.Register(c => new AddressHeaderProvider())
				.AsSelf();

			builder
				.Register(c => new CartContextProvider(
					checkoutRules: c.Resolve<IEnumerable<IPreCheckoutRule>>(),
					cachedShoppingCartProvider: c.Resolve<ICachedShoppingCartProvider>()))
				.As<ICartContextProvider>();

			builder
				.Register(c => new CheckoutSelectionProvider(
					persistedCheckoutContextProvider: c.Resolve<IPersistedCheckoutContextProvider>()))
				.As<ICheckoutSelectionProvider>();

			builder
				.Register(c => new CheckoutEngine(
					paymentMethodInfoProvider: c.Resolve<IPaymentMethodInfoProvider>(),
					guards: c.Resolve<Guards>(),
					transitionBuilder: c.Resolve<TransitionBuilder>()))
				.AsSelf();

			builder
				.Register(c => new TransitionBuilder(
					guards: c.Resolve<Guards>()))
				.AsSelf();

			builder
				.Register(c => new Guards(
					cachedShippingMethodCollectionProvider: c.Resolve<ICachedShippingMethodCollectionProvider>(),
					giftCardManager: c.Resolve<GiftCardManager>(),
					appConfigProvider: c.Resolve<AppConfigProvider>(),
					checkoutAccountStatusProvider: c.Resolve<ICheckoutAccountStatusProvider>()))
				.AsSelf();

			builder
				.Register(c => new PersistedCheckoutContextProvider())
				.As<IPersistedCheckoutContextProvider>();

			builder
				.Register(c => new CreditCardValidationProvider(
					creditCardTypeProvider: c.Resolve<CreditCardTypeProvider>()))
				.As<ICreditCardValidationProvider>();

			builder
				.Register(c => new PaymentOptionProvider(
					urlHelper: c.Resolve<UrlHelper>(),
					paymentMethodInfoProvider: c.Resolve<IPaymentMethodInfoProvider>(),
					clientScriptRegistry: c.Resolve<IClientScriptRegistry>()))
				.As<IPaymentOptionProvider>();

			builder
				.Register(c => new PaymentMethodInfoProvider())
				.As<IPaymentMethodInfoProvider>();

			builder
				.Register(c => new AuthorizeNetWalletProvider())
				.As<IWalletProvider>();

			builder
				.Register(c => new ShippingMethodCartItemApplicator())
				.As<IShippingMethodCartItemApplicator>();

			builder
				.Register(c => new EffectiveShippingAddressProvider(
					persistedCheckoutContextProvider: c.Resolve<IPersistedCheckoutContextProvider>()))
				.As<IEffectiveShippingAddressProvider>();

			builder
				.Register(c => new CheckoutConfigurationProvider())
				.As<ICheckoutConfigurationProvider>();

			builder
				.Register(c => new CheckoutAccountStatusProvider(
					appConfigProvider: c.Resolve<AppConfigProvider>(),
					cachedShoppingCartProvider: c.Resolve<ICachedShoppingCartProvider>()))
				.As<ICheckoutAccountStatusProvider>();

			#endregion

			builder
				.Register(c => new Parser())
				.AsSelf();

			builder
				.Register(c => new ShippingMethodCollectionProvider())
				.As<IShippingMethodCollectionProvider>();

			builder
				.Register(c => new CaptchaVerificationProvider(
					appConfigProvider: c.Resolve<AppConfigProvider>()))
				.AsSelf();

			builder
				.Register(c => new AddressControllerHelper(
					addressValidationProviderFactory: c.Resolve<IAddressValidationProviderFactory>()))
				.AsSelf();

			builder
				.Register(c => new AddressSelectListBuilder())
				.AsSelf();

			builder
				.Register(c => new CreditCardSelectListBuilder())
				.AsSelf();

			builder
				.Register(c => new AddressValidationProviderFactory())
				.As<IAddressValidationProviderFactory>();

			builder
				.Register(c => new NoticeProvider(
					noticeStorageProvider: c.Resolve<INoticeStorageProvider>()))
				.AsSelf();

			builder
				.Register(c => new AccountSettings())
				.AsSelf();

			builder
				.Register(c => new AddressSettings())
				.AsSelf();

			builder
				.Register(c => new AccountControllerHelper(c.Resolve<AccountSettings>()))
				.AsSelf();

			builder
				.Register(c =>
					AppLogic.AppConfigBool("UseLegacySENameProvider")
						? (ISearchEngineNameProvider)new LegacySearchEngineNameProvider(new HttpContextWrapper(HttpContext.Current))
						: (ISearchEngineNameProvider)new Utf8SearchEngineNameProvider(new HttpContextWrapper(HttpContext.Current)))
				.As<ISearchEngineNameProvider>()
				.InstancePerRequest();

			builder
				.Register(c => new CartActionProvider(
					urlHelper: c.Resolve<UrlHelper>(),
					cachedShoppingCartProvider: c.Resolve<ICachedShoppingCartProvider>()))
				.AsSelf();

			builder
				 .Register(c => new RestrictedQuantityProvider())
				 .AsSelf();

			builder
				.Register(c => new CookieNoticeStorageProvider(new HttpContextWrapper(HttpContext.Current)))
				.As<INoticeStorageProvider>()
				.InstancePerRequest();

			builder
				.Register(c => new ClaimsRoleProvider())
				.As<IClaimsRoleProvider>();

			builder
				.Register(c => new ClaimsIdentityProvider(
					claimsRoleProvider: c.Resolve<IClaimsRoleProvider>(),
					defaultAuthenticationType: AuthValues.CookiesAuthenticationType,
					defaultIdentityProvider: AuthValues.CookiesAuthenticationIdentityProvider))
				.As<IClaimsIdentityProvider>();

			builder
				.RegisterGeneratedFactory<Func<IClaimsIdentityProvider>>()
				.SingleInstance();

			builder
				.Register(c => new GiftCardManager())
				.AsSelf();

			builder
				.Register(c => new CreditCardTypeProvider())
				.AsSelf();

			builder
				.Register(c => new ECheckAccountTypeProvider())
				.AsSelf();

			builder
				.Register(c => new SkinProvider())
				.As<ISkinProvider>()
				.AsSelf();

			#region Filters

			builder.RegisterFilterProvider();

			builder
				.Register(c => new CustomerRoleVerificationFilterAttribute(c.Resolve<IClaimsIdentityProvider>()))
				.AsAuthenticationFilterFor<Controller>();

			builder
				.Register(c => new SiteDisclaimerFilterAttribute())
				.AsAuthorizationFilterFor<Controller>();

			builder
				.Register(c => new IpBlacklistFilterAttribute())
				.AsAuthorizationFilterFor<Controller>();

			builder
				.Register(c => new MaintenanceFilterAttribute())
				.AsAuthorizationFilterFor<Controller>();

			builder
				.Register(c => new SkinIdRouteDataFilterAttribute())
				.AsActionFilterFor<Controller>();

			builder
				.Register(c => new ReferrerCookieFilterAttribute())
				.AsActionFilterFor<Controller>();

			builder
				.Register(c => new CustomerQueryStringFilterAttribute())
				.AsActionFilterFor<Controller>();

			builder
				.Register(c => new UpdateCustomerActivityFilterAttribute(
					c.Resolve<AspDotNetStorefrontCore.DataRetention.IDataRetentionService>()))
				.AsActionFilterFor<Controller>();

			builder
				.Register(c => new SecureAccessFilterAttribute())
				.AsActionFilterFor<Controller>();

			builder
				.Register(c => new ContentSecurityPolicyFilterAttribute())
				.AsActionFilterFor<Controller>();

			#endregion

			builder
				.Register(c => new UspsPostalCodeLookupProvider(
					appConfigProvider: c.Resolve<AppConfigProvider>()))
				.As<IPostalCodeLookupProvider>();

			builder
				.Register(c => AmazonPaymentsConfiguration.CreateDefaultConfiguration())
				.AsSelf();

			builder
				.Register(c => new HttpClient())
				.AsSelf();

			builder
				.Register(c => new AmazonPaymentsApiProvider(
					configuration: c.Resolve<AmazonPaymentsConfiguration>(),
					httpClient: c.Resolve<HttpClient>()))
				.AsSelf();

			builder
				.Register(c => new ModelBuilder(
					cartActionProvider: c.Resolve<CartActionProvider>(),
					urlHelper: c.Resolve<UrlHelper>(),
					restrictedQuantityProvider: c.Resolve<RestrictedQuantityProvider>()))
				.AsSelf();

			builder
				.Register(c => new SendWelcomeEmailProvider())
				.AsSelf();

			builder
				.Register(c => new SiteMapSettingsProvider(
				   appConfigProvider: c.Resolve<AppConfigProvider>()))
				.AsSelf();

			builder
				.Register(c => new AspDotNetStorefrontCore.DataRetention.DataRetentionService(
					appConfigProvider: c.Resolve<AppConfigProvider>(),
					stringResourceProvider: c.Resolve<IStringResourceProvider>()))
				.As<AspDotNetStorefrontCore.DataRetention.IDataRetentionService>();

			builder
				.Register(c => new PopupImageBuilder())
				.As<IPopupImageBuilder>();

			#region Model Metadata

			// ASP.NET will automatically use any registered ModelMetadataProvider.
			builder
				.Register(c => new AdnsfModelMetadataProvider(
					stringResourceProviderFactory: c.Resolve<IStringResourceProviderFactory>()))
				.As<ModelMetadataProvider>();

			#endregion

			#region String Resources

			builder
				.Register(c => new StringResourceProviderFactory(
					appConfigProvider: c.Resolve<AppConfigProvider>()))
  				.As<IStringResourceProviderFactory>();

			// Use the factory to generate instances
			builder
				.Register(c => c.Resolve<IStringResourceProviderFactory>().Create())
				.As<IStringResourceProvider>();

			#endregion

			#region Tokens

			// Scan for ITokenHandlers in the same assembly as TokenExecutor and register them
			builder
				.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(TokenExecutor)))
				.InNamespace(string.Join(
					".",
					nameof(AspDotNetStorefrontCore),
					nameof(AspDotNetStorefrontCore.Tokens),
					nameof(AspDotNetStorefrontCore.Tokens.Handlers)))
				.AssignableTo<ITokenHandler>()
				.As<ITokenHandler>();

			builder
				.Register((c, p) => new TokenExecutor(
					tokenHandlers: p.Any()
						? p.Named<IEnumerable<ITokenHandler>>("handlers")
						: c.Resolve<IEnumerable<ITokenHandler>>()))
				.AsSelf();

			builder
				.Register(c => new TokenParameterConverter())
				.AsSelf();

			builder
				.Register(c => new TokenEntityLinkBuilder(
					urlHelper: c.Resolve<UrlHelper>()))
				.AsSelf();

			builder
				.Register(c => new TokenHtmlHelper(
					requestContext: c.Resolve<RequestContext>()))
				.AsSelf();

			#endregion

			#region Routing

			builder
				.Register(c => new RoutingConfigurationProvider())
				.As<IRoutingConfigurationProvider>();

			#endregion

			#region Web API

			builder
				.RegisterApiControllers(typeof(Controllers.SiteInfoController).Assembly);

			builder
				.RegisterWebApiFilterProvider(GlobalConfiguration.Configuration);

			#endregion

			#region Client resource handling

			builder
				.Register(c => new DependencyGraphProvider())
				.AsSelf();

			builder
				.Register(c => new ClientScriptEqualityComparer(
					clientScriptResourceHandler: c.Resolve<ClientScriptResourceHandler>()))
				.AsSelf();

			builder
				.Register(c => new ClientScriptRegistry(
					clientScriptResourceHandler: c.Resolve<ClientScriptResourceHandler>(),
					appConfigProvider: c.Resolve<AppConfigProvider>(),
					resourceEqualityComparer: c.Resolve<ClientScriptEqualityComparer>(),
					dependencyGraphProvider: c.Resolve<DependencyGraphProvider>()))
				.As<IClientScriptRegistry>();

			builder
				.Register(c => new ClientScriptResourceHandler(
					clientScriptResourceHandlers: c.Resolve<IEnumerable<IClientScriptResourceHandler>>()))
				.AsSelf();

			builder
				.Register(c => new InlineScriptClientResourceHandler())
				.As<IClientScriptResourceHandler>();

			builder
				.Register(c => new ScriptReferenceClientResourceHandler())
				.As<IClientScriptResourceHandler>();

			builder
				.Register(c => new BundledScriptReferenceClientResourceHandler(
					bundledResourceProvider: c.Resolve<IBundledResourceProvider>()))
				.As<IClientScriptResourceHandler>();

			builder
				.Register(c => new BundledResourceProvider())
				.As<IBundledResourceProvider>();

			#endregion

			#region SignifydApi

			builder
				.Register(c => new SignifydConfigurationProvider(
				appConfigProvider: c.Resolve<AppConfigProvider>()))
				.AsSelf();

			builder
				.Register(c => new SignifydCaseApi())
				.AsSelf();

			#endregion

			var config = new ConfigurationBuilder();
			config.AddJsonFile("dependency.json", optional: true);

			var module = new ConfigurationModule(config.Build());
			builder.RegisterModule(module);

			var container = builder.Build();

			// Set up the MVC resolver
			DependencyResolver.SetResolver(new AutofacDependencyResolver(container));

			// Set up the Web API resolver
			GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
		}
	}
}
