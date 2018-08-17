// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Checkout
{
	public interface IPersistedCheckoutContextProvider
	{
		void ClearCheckoutContext(Customer customer);

		PersistedCheckoutContext LoadCheckoutContext(Customer customer);

		void SaveCheckoutContext(Customer customer, PersistedCheckoutContext checkoutContext);
	}

	public class PersistedCheckoutContext
	{
		public CreditCardDetails CreditCard { get; }
		public ECheckDetails ECheck { get; }
		public PayPalExpressDetails PayPalExpress { get; }
		public PurchaseOrderDetails PurchaseOrder { get; }
		public AcceptJsDetailsCreditCard AcceptJsDetailsCreditCard { get; }
		public AcceptJsDetailsECheck AcceptJsDetailsECheck { get; }
		public BraintreeDetails Braintree { get; }
		public SagePayPiDetails SagePayPi { get; }
		public AmazonPaymentsDetails AmazonPayments { get; }
		public bool TermsAndConditionsAccepted { get; }
		public bool Over13Checked { get; }
		public ShippingEstimateDetails ShippingEstimate { get; }
		public int? OffsiteRequiredBillingAddressId { get; }
		public int? OffsiteRequiredShippingAddressId { get; }
		public string Email { get; }
		public int? SelectedShippingMethodId { get; }

		public PersistedCheckoutContext(
			CreditCardDetails creditCard,
			ECheckDetails eCheck,
			PayPalExpressDetails payPalExpress,
			PurchaseOrderDetails purchaseOrder,
			AcceptJsDetailsCreditCard acceptJsDetailsCreditCard,
			AcceptJsDetailsECheck acceptJsDetailsECheck,
			BraintreeDetails braintree,
			SagePayPiDetails sagePayPi,
			AmazonPaymentsDetails amazonPayments,
			bool termsAndConditionsAccepted,
			bool over13Checked,
			ShippingEstimateDetails shippingEstimate,
			int? offsiteRequiredBillingAddressId,
			int? offsiteRequiredShippingAddressId,
			string email,
			int? selectedShippingMethodId)
		{
			CreditCard = creditCard;
			ECheck = eCheck;
			PayPalExpress = payPalExpress;
			PurchaseOrder = purchaseOrder;
			AcceptJsDetailsCreditCard = acceptJsDetailsCreditCard;
			AcceptJsDetailsECheck = acceptJsDetailsECheck;
			Braintree = braintree;
			SagePayPi = sagePayPi;
			AmazonPayments = amazonPayments;
			TermsAndConditionsAccepted = termsAndConditionsAccepted;
			Over13Checked = over13Checked;
			ShippingEstimate = shippingEstimate;
			OffsiteRequiredBillingAddressId = offsiteRequiredBillingAddressId;
			OffsiteRequiredShippingAddressId = offsiteRequiredShippingAddressId;
			Email = email;
			SelectedShippingMethodId = selectedShippingMethodId;
		}
	}

	public class CreditCardDetails
	{
		public string Name { get; }
		public string Number { get; }
		public string IssueNumber { get; }
		public string CardType { get; }
		public DateTime? ExpirationDate { get; }
		public DateTime? StartDate { get; }
		public string Cvv { get; }

		public CreditCardDetails(
			string name,
			string number,
			string issueNumber,
			string cardType,
			DateTime? expirationDate,
			DateTime? startDate,
			string cvv)
		{
			Name = name;
			Number = number;
			IssueNumber = issueNumber;
			CardType = cardType;
			ExpirationDate = expirationDate;
			StartDate = startDate;
			Cvv = cvv;
		}
	}

	public class ECheckDetails
	{
		public string AccountNumber { get; }
		public string RoutingNumber { get; }
		public string NameOnAccount { get; }
		public string AccountType { get; }

		public ECheckDetails(
			string accountNumber,
			string routingNumber,
			string nameOnAccount,
			string accountType)
		{
			AccountNumber = accountNumber;
			RoutingNumber = routingNumber;
			NameOnAccount = nameOnAccount;
			AccountType = accountType;
		}
	}

	public class PayPalExpressDetails
	{
		public string Token { get; }
		public string PayerId { get; }

		public PayPalExpressDetails(string token, string payerId)
		{
			Token = token;
			PayerId = payerId;
		}
	}

	public class PurchaseOrderDetails
	{
		public string Number { get; }

		public PurchaseOrderDetails(string number)
		{
			Number = number;
		}
	}

	public class BraintreeDetails
	{
		public string Nonce { get; }
		public string Token { get; }
		public string PaymentMethod { get; }
		public bool ThreeDSecureApproved { get; }

		public BraintreeDetails(
			string nonce,
			string token,
			string paymentMethod,
			bool threeDSecureApproved)
		{
			Nonce = nonce;
			Token = token;
			PaymentMethod = paymentMethod;
			ThreeDSecureApproved = threeDSecureApproved;
		}
	}

	public class SagePayPiDetails
	{
		public string CardIdentifier { get; }
		public string MerchantSessionId { get; }
		public string PaymentMethod { get; }
		public bool ThreeDSecureApproved { get; }

		public SagePayPiDetails(
			string cardIdentifier,
			string merchantSessionId,
			string paymentMethod,
			bool threeDSecureApproved)
		{
			CardIdentifier = cardIdentifier;
			MerchantSessionId = merchantSessionId;
			PaymentMethod = paymentMethod;
			ThreeDSecureApproved = threeDSecureApproved;
		}
	}

	public class AcceptJsDetailsCreditCard
	{
		public string DataValue { get; }
		public string DataDescriptor { get; }
		public string LastFour { get; }
		public string ExpirationMonth { get; }
		public string ExpirationYear { get; }

		public AcceptJsDetailsCreditCard(
			string dataValue,
			string dataDescriptor,
			string lastFour,
			string expirationMonth,
			string expirationYear)
		{
			DataValue = dataValue;
			DataDescriptor = dataDescriptor;
			LastFour = lastFour;
			ExpirationMonth = expirationMonth;
			ExpirationYear = expirationYear;
		}
	}

	public class AcceptJsDetailsECheck
	{
		public string DataValue { get; }
		public string DataDescriptor { get; }
		public string ECheckDisplayAccountNumberLastFour { get; }
		public string ECheckDisplayAccountType { get; }

		public AcceptJsDetailsECheck(
			string dataValue,
			string dataDescriptor,
			string eCheckDisplayAccountNumberLastFour,
			string eCheckDisplayAccountType)
		{
			DataValue = dataValue;
			DataDescriptor = dataDescriptor;
			ECheckDisplayAccountNumberLastFour = eCheckDisplayAccountNumberLastFour;
			ECheckDisplayAccountType = eCheckDisplayAccountType;
		}
	}

	public class AmazonPaymentsDetails
	{
		public readonly string AmazonOrderReferenceId;

		public AmazonPaymentsDetails(string amazonOrderReferenceId)
		{
			AmazonOrderReferenceId = amazonOrderReferenceId;
		}
	}

	public class ShippingEstimateDetails
	{
		public string Country { get; }
		public string City { get; }
		public string State { get; }
		public string PostalCode { get; }

		public ShippingEstimateDetails(
			string country,
			string city,
			string state,
			string postalCode)
		{
			Country = country;
			City = city;
			State = state;
			PostalCode = postalCode;
		}
	}
}
