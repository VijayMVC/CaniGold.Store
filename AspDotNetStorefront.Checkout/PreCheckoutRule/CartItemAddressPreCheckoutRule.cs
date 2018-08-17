// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Data.SqlClient;
using AspDotNetStorefront.Caching.ObjectCaching;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Checkout.PreCheckoutRule
{
	public class CartItemAddressPreCheckoutRule : IPreCheckoutRule
	{
		readonly ICachedShoppingCartProvider CachedShoppingCartProvider;

		public CartItemAddressPreCheckoutRule(ICachedShoppingCartProvider cachedShoppingCartProvider)
		{
			CachedShoppingCartProvider = cachedShoppingCartProvider;
		}

		public CartContext Apply(PreCheckoutRuleContext preCheckoutRuleContext)
		{

			var customerId = preCheckoutRuleContext
				.Customer
				.CustomerID;

			var shippingAddressId = preCheckoutRuleContext.Customer.PrimaryShippingAddressID;

			DB.ExecuteSQL(@"
				update
					dbo.ShoppingCart
				set
					ShippingAddressId = @shippingAddressId
				where
					CustomerId = @CustomerId
					and CartType in (0,1)
					and StoreId = @StoreId
					AND ShippingAddressid <> @shippingAddressId",

				new SqlParameter("@StoreId", AppLogic.StoreID()),
				new SqlParameter("@CustomerId", customerId),
				new SqlParameter("@ShippingAddressId", shippingAddressId));

			return new CartContext(
				cartContext: preCheckoutRuleContext.CartContext,
				cart: CachedShoppingCartProvider.Get(preCheckoutRuleContext.Customer, CartTypeEnum.ShoppingCart, AppLogic.StoreID()));
		}
	}
}
