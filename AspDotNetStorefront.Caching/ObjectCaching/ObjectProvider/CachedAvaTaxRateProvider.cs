// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Collections.Generic;
using AspDotNetStorefront.Caching.ObjectCaching.Dependency;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontCore.ShippingCalculation;

namespace AspDotNetStorefront.Caching.ObjectCaching.ObjectProvider
{
	/// <summary>
	/// Convenience class that wraps <see cref="CachedObjectProvider"/> to provide a cached <see cref="AvaTaxRate"/>
	/// </summary>
	public class CachedAvaTaxRateProvider : ICachedAvaTaxRateProvider
	{
		readonly CachedObjectProvider<AvaTaxRate, AvaTaxRateCacheContext> CachedObjectProvider;

		public CachedAvaTaxRateProvider(CachedObjectProvider<AvaTaxRate, AvaTaxRateCacheContext> cachedObjectProvider)
		{
			CachedObjectProvider = cachedObjectProvider;
		}

		public AvaTaxRate Get(Customer customer, CartTypeEnum cartType, int storeId)
		{
			return CachedObjectProvider.Get(new AvaTaxRateCacheContext(
				customer: customer,
				cartType: cartType,
				storeId: storeId));
		}
	}

	/// <summary>
	/// The context needed to generate a <see cref="AvaTaxRate"/> when its dependencies are
	/// invalidated.
	/// </summary>
	public class AvaTaxRateCacheContext
	{
		public readonly Customer Customer;
		public readonly CartTypeEnum CartType;
		public readonly int StoreId;

		public AvaTaxRateCacheContext(Customer customer, CartTypeEnum cartType, int storeId)
		{
			Customer = customer;
			CartType = cartType;
			StoreId = storeId;
		}
	}

	public class ShippingRateCachedObjectContextBuilder : ICachedObjectContextBuilder<AvaTaxRate, AvaTaxRateCacheContext>
	{
		public const string KeyTemplate = "{0}:{1}:{2}:AvaTaxRate";

		readonly AppConfigValueConverter AppConfigValueConverter;
		readonly ICachedShoppingCartProvider CachedShoppingCartProvider;

		public ShippingRateCachedObjectContextBuilder(AppConfigValueConverter appConfigValueConverter, ICachedShoppingCartProvider cachedShoppingCartProvider)
		{
			AppConfigValueConverter = appConfigValueConverter;
			CachedShoppingCartProvider = cachedShoppingCartProvider;
		}

		public CachedObjectContext<AvaTaxRate, AvaTaxRateCacheContext> CreateContext()
		{
			return new CachedObjectContext<AvaTaxRate, AvaTaxRateCacheContext>(
				keyBuilder: KeyBuilder,
				objectBuilder: ObjectBuilder,
				dependencyStateBuilder: DependencyStateBuilder);
		}

		string KeyBuilder(AvaTaxRateCacheContext objectContext)
		{
			return string.Format(KeyTemplate, objectContext.StoreId, objectContext.Customer.ThisCustomerSession.SessionID, objectContext.CartType);
		}

		AvaTaxRate ObjectBuilder(AvaTaxRateCacheContext objectContext)
		{
			var cart = CachedShoppingCartProvider.Get(objectContext.Customer, CartTypeEnum.ShoppingCart, objectContext.StoreId);

			var taxRate = new AvaTax().GetTaxRate(objectContext.Customer, cart.CartItems, cart.OrderOptions);
			return new AvaTaxRate(objectContext.Customer, objectContext.StoreId, objectContext.CartType, taxRate);
		}

		IEnumerable<DependencyStateContext> DependencyStateBuilder(AvaTaxRateCacheContext objectContext)
		{
			return new DependencyStateContext[]
				{
					new QueryDependencyStateContext("select UpdatedOn from TaxClass"),

					new CacheEntryDependencyStateContext(
						string.Format(
							ShoppingCartCachedObjectContextBuilder.KeyTemplate,
							objectContext.StoreId,
							objectContext.Customer.ThisCustomerSession.SessionID,
							CartTypeEnum.ShoppingCart.ToString())),

					new CacheEntryDependencyStateContext(
						string.Format(
							ShippingMethodCollectionCachedObjectContextBuilder.KeyTemplate,
							objectContext.Customer.ThisCustomerSession.SessionID))
				};
		}

	}
}
