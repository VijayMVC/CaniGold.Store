// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Checkout
{
	public interface ICheckoutSelectionProvider
	{
		CheckoutSelectionContext GetCheckoutSelection(Customer customer, PaymentMethodInfo selectedPaymentMethod, PersistedCheckoutContext persistedCheckoutContext);

		/// <summary>
		/// Persists the checkout selections and returns a collection of the updated objects. Note that this method has side effects!
		/// </summary>
		CheckoutSelectionApplicationResult ApplyCheckoutSelections(Customer customer, CheckoutSelectionContext context);
	}

	public class CheckoutSelectionContext
	{
		public readonly PaymentMethodInfo SelectedPaymentMethod;
		public readonly Address SelectedBillingAddress;
		public readonly Address SelectedShippingAddress;
		public readonly int? SelectedShippingMethodId;
		public readonly CreditCardDetails CreditCard;
		public readonly PayPalExpressDetails PayPalExpress;
		public readonly AmazonPaymentsDetails AmazonPayments;
		public readonly PurchaseOrderDetails PurchaseOrder;
		public readonly AcceptJsDetailsCreditCard AcceptJsDetailsCreditCard;
		public readonly BraintreeDetails Braintree;
		public readonly SagePayPiDetails SagePayPi;
		public readonly ECheckDetails ECheck;
		public readonly bool TermsAndConditionsAccepted;
		public readonly bool Over13Checked;
		public readonly string Email;

		public CheckoutSelectionContext(
			PaymentMethodInfo selectedPaymentMethod,
			Address selectedBillingAddress,
			Address selectedShippingAddress,
			int? selectedShippingMethodId,
			CreditCardDetails creditCard,
			ECheckDetails eCheck,
			PayPalExpressDetails payPalExpress,
			AmazonPaymentsDetails amazonPayments,
			PurchaseOrderDetails purchaseOrder,
			AcceptJsDetailsCreditCard acceptJsDetailsCreditCard,
			BraintreeDetails braintree,
			SagePayPiDetails sagePayPi,
			bool termsAndConditionsAccepted,
			bool over13Checked,
			string email)
		{
			SelectedPaymentMethod = selectedPaymentMethod;
			SelectedBillingAddress = selectedBillingAddress;
			SelectedShippingAddress = selectedShippingAddress;
			SelectedShippingMethodId = selectedShippingMethodId;
			CreditCard = creditCard;
			ECheck = eCheck;
			PayPalExpress = payPalExpress;
			AmazonPayments = amazonPayments;
			PurchaseOrder = purchaseOrder;
			AcceptJsDetailsCreditCard = acceptJsDetailsCreditCard;
			Braintree = braintree;
			SagePayPi = sagePayPi;
			TermsAndConditionsAccepted = termsAndConditionsAccepted;
			Over13Checked = over13Checked;
			Email = email;
		}
	}

	public class CheckoutSelectionApplicationResult
	{
		public readonly Customer Customer;
		public readonly PaymentMethodInfo SelectedPaymentMethod;
		public readonly PersistedCheckoutContext PersistedCheckoutContext;

		public CheckoutSelectionApplicationResult(
			Customer customer,
			PaymentMethodInfo selectedPaymentMethod,
			PersistedCheckoutContext persistedCheckoutContext)
		{
			Customer = customer;
			SelectedPaymentMethod = selectedPaymentMethod;
			PersistedCheckoutContext = persistedCheckoutContext;
		}
	}
}
