// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
namespace AspDotNetStorefront.Checkout
{
	public class PersistedCheckoutContextBuilder
	{
		PersistedCheckoutContext Source;

		CreditCardDetails CreditCard;
		bool CreditCardSet;

		ECheckDetails ECheck;
		bool ECheckSet;

		PayPalExpressDetails PayPalExpress;
		bool PayPalExpressSet;

		PurchaseOrderDetails PurchaseOrder;
		bool PurchaseOrderSet;

		AcceptJsDetailsCreditCard AcceptJsCreditCard;
		bool AcceptJsCreditCardSet;

		AcceptJsDetailsECheck AcceptJsECheck;
		bool AcceptJsECheckSet;

		BraintreeDetails Braintree;
		bool BraintreeSet;

		SagePayPiDetails SagePayPi;
		bool SagePayPiSet;

		AmazonPaymentsDetails AmazonPayments;
		bool AmazonPaymentsSet;

		bool TermsAndConditionsAccepted;
		bool TermsAndConditionsAcceptedSet;

		bool Over13Checked;
		bool Over13CheckedSet;

		ShippingEstimateDetails ShippingEstimate;
		bool ShippingEstimateSet;

		int? OffsiteRequiredBillingAddressId;
		bool OffsiteRequiredBillingAddressIdSet;

		int? OffsiteRequiredShippingAddressId;
		bool OffsiteRequiredShippingAddressIdSet;

		string Email;
		bool EmailSet;

		int? SelectedShippingMethodId;
		bool SelectedShippingMethodIdSet;

		public PersistedCheckoutContextBuilder From(PersistedCheckoutContext source)
		{
			Source = source;
			return this;
		}

		public PersistedCheckoutContextBuilder WithCreditCard(CreditCardDetails creditCard)
		{
			CreditCard = creditCard;
			CreditCardSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithECheck(ECheckDetails eCheck)
		{
			ECheck = eCheck;
			ECheckSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithoutCreditCard()
			=> WithCreditCard(null);

		public PersistedCheckoutContextBuilder WithPayPalExpress(PayPalExpressDetails payPalExpress)
		{
			PayPalExpress = payPalExpress;
			PayPalExpressSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithoutPayPalExpress()
			=> WithPayPalExpress(null);

		public PersistedCheckoutContextBuilder WithPurchaseOrder(PurchaseOrderDetails purchaseOrder)
		{
			PurchaseOrder = purchaseOrder;
			PurchaseOrderSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithoutPurchaseOrder()
			=> WithPurchaseOrder(null);

		public PersistedCheckoutContextBuilder WithoutAcceptJsCreditCard()
			=> WithAcceptJsCreditCard(null);

		public PersistedCheckoutContextBuilder WithoutAcceptJsECheck()
			=> WithAcceptJsECheck(null);

		public PersistedCheckoutContextBuilder WithAcceptJsCreditCard(AcceptJsDetailsCreditCard acceptJsCreditCard)
		{
			AcceptJsCreditCard = acceptJsCreditCard;
			AcceptJsCreditCardSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithAcceptJsECheck(AcceptJsDetailsECheck acceptJsECheck)
		{
			AcceptJsECheck = acceptJsECheck;
			AcceptJsECheckSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithBraintree(BraintreeDetails braintree)
		{
			Braintree = braintree;
			BraintreeSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithoutBraintree()
			=> WithBraintree(null);

		public PersistedCheckoutContextBuilder WithSagePayPi(SagePayPiDetails sagePayPi)
		{
			SagePayPi = sagePayPi;
			SagePayPiSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithoutSagePayPi()
			=> WithSagePayPi(null);

		public PersistedCheckoutContextBuilder WithAmazonPayments(AmazonPaymentsDetails amazonPayments)
		{
			AmazonPayments = amazonPayments;
			AmazonPaymentsSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithoutAmazonPayments()
			=> WithAmazonPayments(null);

		public PersistedCheckoutContextBuilder WithTermsAndConditionsAccepted(bool termsAndConditionsAccepted)
		{
			TermsAndConditionsAccepted = termsAndConditionsAccepted;
			TermsAndConditionsAcceptedSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithOver13Checked(bool over13Checked)
		{
			Over13Checked = over13Checked;
			Over13CheckedSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithShippingEstimate(ShippingEstimateDetails shippingEstimate)
		{
			ShippingEstimate = shippingEstimate;
			ShippingEstimateSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithoutShippingEstimate()
			=> WithShippingEstimate(null);

		public PersistedCheckoutContextBuilder WithOffsiteRequiredBillingAddressId(int offsiteRequiredBillingAddressId)
		{
			OffsiteRequiredBillingAddressId = offsiteRequiredBillingAddressId;
			OffsiteRequiredBillingAddressIdSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithoutOffsiteRequiredBillingAddressId()
		{
			OffsiteRequiredBillingAddressId = null;
			OffsiteRequiredBillingAddressIdSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithOffsiteRequiredShippingAddressId(int offsiteRequiredShippingAddressId)
		{
			OffsiteRequiredShippingAddressId = offsiteRequiredShippingAddressId;
			OffsiteRequiredShippingAddressIdSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithoutOffsiteRequiredShippingAddressId()
		{
			OffsiteRequiredShippingAddressId = null;
			OffsiteRequiredShippingAddressIdSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithEmail(string email)
		{
			Email = email;
			EmailSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithoutEmail()
			=> WithEmail(null);

		public PersistedCheckoutContextBuilder WithSelectedShippingMethodId(int? selectedShippingMethodId)
		{
			SelectedShippingMethodId = selectedShippingMethodId;
			SelectedShippingMethodIdSet = true;
			return this;
		}

		public PersistedCheckoutContextBuilder WithoutSelectedShippingMethodId()
		{
			SelectedShippingMethodId = null;
			SelectedShippingMethodIdSet = true;
			return this;
		}

		public PersistedCheckoutContext Build()
			=> new PersistedCheckoutContext(
				creditCard: CreditCardSet
					? CreditCard
					: Source?.CreditCard,
				eCheck: ECheckSet
					? ECheck
					: Source?.ECheck,
				payPalExpress: PayPalExpressSet
					? PayPalExpress
					: Source?.PayPalExpress,
				purchaseOrder: PurchaseOrderSet
					? PurchaseOrder
					: Source?.PurchaseOrder,
				acceptJsDetailsCreditCard: AcceptJsCreditCardSet
					? AcceptJsCreditCard
					: Source?.AcceptJsDetailsCreditCard,
				acceptJsDetailsECheck: AcceptJsECheckSet
					? AcceptJsECheck
					: Source?.AcceptJsDetailsECheck,
				braintree: BraintreeSet
					? Braintree
					: Source?.Braintree,
				sagePayPi: SagePayPiSet
					? SagePayPi
					: Source?.SagePayPi,
				amazonPayments: AmazonPaymentsSet
					? AmazonPayments
					: Source?.AmazonPayments,
				termsAndConditionsAccepted: TermsAndConditionsAcceptedSet
					? TermsAndConditionsAccepted
					: Source?.TermsAndConditionsAccepted ?? false,
				over13Checked: Over13CheckedSet
					? Over13Checked
					: Source?.Over13Checked ?? false,
				shippingEstimate: ShippingEstimateSet
					? ShippingEstimate
					: Source?.ShippingEstimate,
				offsiteRequiredBillingAddressId: OffsiteRequiredBillingAddressIdSet
					? OffsiteRequiredBillingAddressId
					: Source?.OffsiteRequiredBillingAddressId,
				offsiteRequiredShippingAddressId: OffsiteRequiredShippingAddressIdSet
					? OffsiteRequiredShippingAddressId
					: Source?.OffsiteRequiredShippingAddressId,
				email: EmailSet
					? Email
					: Source?.Email,
				selectedShippingMethodId: SelectedShippingMethodIdSet
					? SelectedShippingMethodId
					: Source?.SelectedShippingMethodId);
	}
}
