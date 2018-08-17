// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Checkout.AppliedPaymentMethodCleanup
{
	public class AmazonAppliedPaymentMethodCleanup : IAppliedPaymentMethodCleanup
	{
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;

		public AmazonAppliedPaymentMethodCleanup(IPersistedCheckoutContextProvider persistedCheckoutContextProvider)
		{
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
		}

		public void Cleanup(AppliedPaymentMethodCleanupContext context)
		{
			// Only finalize the process if this is an Amazon Payments order.
			if(context.PaymentMethod != AppLogic.ro_PMAmazonPayments)
				return;

			// Currently, Amazon only needs to clean up the checkout context if there's an error.
			//  Successful orders will trigger the checkout process to clear the context.
			if(context.Status == AppLogic.ro_OK)
				return;

			var updatedPersistedCheckoutContext = new PersistedCheckoutContextBuilder()
				.From(PersistedCheckoutContextProvider.LoadCheckoutContext(context.Customer))
				.WithoutAmazonPayments()
				.WithoutAcceptJsCreditCard()
				.WithoutAcceptJsECheck()
				.WithoutOffsiteRequiredBillingAddressId()
				.WithoutOffsiteRequiredShippingAddressId()
				.Build();

			PersistedCheckoutContextProvider.SaveCheckoutContext(context.Customer, updatedPersistedCheckoutContext);
			context.Customer.UpdateCustomer(requestedPaymentMethod: string.Empty);
		}
	}
}
