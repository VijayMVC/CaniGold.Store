// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using AspDotNetStorefront.Caching.ObjectCaching;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Checkout.PreCheckoutRule
{
	public class OffsiteAddressRestrictionPreCheckoutRule : IPreCheckoutRule
	{
		readonly NoticeProvider NoticeProvider;
		readonly ICachedShoppingCartProvider CachedShoppingCartProvider;

		public OffsiteAddressRestrictionPreCheckoutRule(NoticeProvider noticeProvider, ICachedShoppingCartProvider cachedShoppingCartProvider)
		{
			NoticeProvider = noticeProvider;
			CachedShoppingCartProvider = cachedShoppingCartProvider;
		}

		public CartContext Apply(PreCheckoutRuleContext preCheckoutRuleContext)
		{
			var persistedCheckout = preCheckoutRuleContext
				.PersistedCheckoutContext;

			var customer = preCheckoutRuleContext
				.Customer;

			// If an offsite payment method has flagged billing or shipping as required, any changes by the customer will be reverted.
			if(persistedCheckout.OffsiteRequiredBillingAddressId.HasValue
				&& customer.PrimaryBillingAddressID != persistedCheckout.OffsiteRequiredBillingAddressId)
			{
				customer.UpdateCustomer(
					billingAddressId: persistedCheckout.OffsiteRequiredBillingAddressId.Value);
				NoticeProvider.PushNotice(AppLogic.GetString("checkout.offsites.billing.reverted"), NoticeType.Warning);
			}

			if(persistedCheckout.OffsiteRequiredShippingAddressId.HasValue
				&& customer.PrimaryShippingAddressID != persistedCheckout.OffsiteRequiredShippingAddressId)
			{
				customer.UpdateCustomer(
					shippingAddressId: persistedCheckout.OffsiteRequiredShippingAddressId.Value);
				NoticeProvider.PushNotice(AppLogic.GetString("checkout.offsites.shipping.reverted"), NoticeType.Warning);
			}

			return new CartContext(
				cartContext: preCheckoutRuleContext.CartContext,
				cart: CachedShoppingCartProvider.Get(customer, CartTypeEnum.ShoppingCart, AppLogic.StoreID()));
		}
	}
}
