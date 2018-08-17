// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace AspDotNetStorefront.Addon.Api.OrderInfoV1
{
	public class Order
	{
		public int OrderNumber { get; }
		public int StoreId { get; }
		public int ParentOrderNumber { get; }
		public string StoreVersion { get; }
		public bool QuoteCheckout { get; }
		public bool IsNew { get; }
		public DateTime? ShippedOn { get; }
		public int CustomerId { get; }
		public Guid CustomerGuid { get; }
		public string Referrer { get; }
		public int SkinId { get; }
		public string LastName { get; }
		public string FirstName { get; }
		public string Email { get; }
		public string Notes { get; }
		public Address BillingAddress { get; }
		public Address ShippingAddress { get; }
		public bool BillingEqualsShipping { get; }
		public int ShippingResidenceType { get; }
		public int ShippingMethodId { get; }
		public string ShippingMethod { get; }
		public int ShippingCalculationId { get; }
		public string Phone { get; }
		public DateTime? RegisterDate { get; }
		public int AffiliateId { get; }
		public string CouponCode { get; }
		public int CouponType { get; }
		public string CouponDescription { get; }
		public decimal CouponDiscountAmount { get; }
		public decimal CouponDiscountPercent { get; }
		public bool CouponIncludesFreeShipping { get; }
		public bool OktoEmail { get; }
		public bool Deleted { get; }
		public string CardType { get; }
		public string CardName { get; }
		public string CardExpirationMonth { get; }
		public string CardExpirationYear { get; }
		public string CardStartDate { get; }
		public string CardIssueNumber { get; }
		public decimal Subtotal { get; }
		public decimal Tax { get; }
		public decimal ShippingCost { get; }
		public decimal Total { get; }
		public string PaymentGateway { get; }
		public string AuthorizationCode { get; }
		public string AuthorizationResult { get; }
		public string AuthorizationPNRef { get; }
		public string TransactionCommand { get; }
		public DateTime OrderDate { get; }
		public int CustomerLevelId { get; }
		public string CustomerLevelName { get; }
		public decimal CustomerLevelDiscountPercent { get; }
		public decimal CustomerLevelDiscountAmount { get; }
		public bool CustomerLevelHasFreeShipping { get; }
		public bool CustomerLevelAllowsQuantityDiscounts { get; }
		public bool CustomerLevelHasNoTax { get; }
		public bool CustomerLevelAllowsCoupons { get; }
		public bool CustomerLevelDiscountsApplyToExtendedPrices { get; }
		public string LastIPAddress { get; }
		public string PaymentMethod { get; }
		public string OrderNotes { get; }
		public string PONumber { get; }
		public DateTime? DownloadEmailSentOn { get; }
		public DateTime? ReceiptEmailSentOn { get; }
		public DateTime? DistributorEmailSentOn { get; }
		public string ShippingTrackingNumber { get; }
		public string ShippedVia { get; }
		public string CustomerServiceNotes { get; }
		public string RealtimeRatesRequest { get; }
		public string RealtimeRatesResponse { get; }
		public string TransactionState { get; }
		public string AVSResult { get; }
		public string CaptureCommand { get; }
		public string CaptureResult { get; }
		public string VoidCommand { get; }
		public string VoidResult { get; }
		public string RefundCommand { get; }
		public string RefundResult { get; }
		public string RefundReason { get; }
		public string CardinalLookupResult { get; }
		public string CardinalAuthenticatResult { get; }
		public string CardinalGatewayParams { get; }
		public bool AffiliateCommissionRecorded { get; }
		public string OrderOptions { get; }
		public decimal Weight { get; }
		public string CarrierReportedRate { get; }
		public string CarrierReportedWeight { get; }
		public string LocaleSetting { get; }
		public string FinalizationData { get; }
		public string ExtensionData { get; }
		public bool AlreadyConfirmed { get; }
		public int CartType { get; }
		public string Last4 { get; }
		public bool ReadyToShip { get; }
		public bool IsPrinted { get; }
		public DateTime? AuthorizedOn { get; }
		public DateTime? CapturedOn { get; }
		public DateTime? RefundedOn { get; }
		public DateTime? VoidedOn { get; }
		public DateTime? FraudedOn { get; }
		public DateTime? EditedOn { get; }
		public string TrackingUrl { get; }
		public DateTime? ShippedEmailSentOn { get; }
		public bool InventoryWasReduced { get; }
		public decimal MaxMindFraudScore { get; }
		public string MaxMindDetails { get; }
		public string VATRegistrationId { get; }
		public int Crypt { get; }
		public int TransactionType { get; }
		public string RecurringSubscriptionId { get; }
		public string RecurringSubscriptionCommand { get; }
		public string RecurringSubscriptionResult { get; }
		public int RelatedOrderNumber { get; }
		public string BuySafeCommand { get; }
		public string BuySafeResult { get; }
		public string ReceiptHtml { get; }
		public DateTime UpdatedOn { get; }
		public DateTime Createdon { get; }
		public IEnumerable<OrderItem> OrderItems { get; }

		public Order(int orderNumber,
			int storeId,
			int parentOrderNumber,
			string storeVersion,
			bool quoteCheckout,
			bool isNew,
			DateTime? shippedOn,
			int customerId,
			Guid customerGuid,
			string referrer,
			int skinId,
			string lastName,
			string firstName,
			string email,
			string notes,
			bool billingEqualsShipping,
			Address billingAddress,
			Address shippingAddress,
			int shippingResidenceType,
			int shippingMethodId,
			string shippingMethod,
			int shippingCalculationId,
			string phone,
			DateTime? registerDate,
			int affiliateId,
			string couponCode,
			int couponType,
			string couponDescription,
			decimal couponDiscountAmount,
			decimal couponDiscountPercent,
			bool couponIncludesFreeShipping,
			bool oktoEmail,
			bool deleted,
			string cardType,
			string cardName,
			string cardExpirationMonth,
			string cardExpirationYear,
			string cardStartDate,
			string cardIssueNumber,
			decimal subtotal,
			decimal tax,
			decimal shippingCost,
			decimal total,
			string paymentGateway,
			string authorizationCode,
			string authorizationResult,
			string authorizationPNRef,
			string transactionCommand,
			DateTime orderDate,
			int customerLevelId,
			string customerLevelName,
			decimal customerLevelDiscountPercent,
			decimal customerLevelDiscountAmount,
			bool customerLevelHasFreeShipping,
			bool customerLevelAllowsQuantityDiscounts,
			bool customerLevelHasNoTax,
			bool customerLevelAllowsCoupons,
			bool customerLevelDiscountsApplyToExtendedPrices,
			string lastIPAddress,
			string paymentMethod,
			string orderNotes,
			string poNumber,
			DateTime? downloadEmailSentOn,
			DateTime? receiptEmailSentOn,
			DateTime? distributorEmailSentOn,
			string shippingTrackingNumber,
			string shippedVia,
			string customerServiceNotes,
			string realtimeRatesRequest,
			string realtimeRatesResponse,
			string transactionState,
			string avsResult,
			string captureCommand,
			string captureResult,
			string voidCommand,
			string voidResult,
			string refundCommand,
			string refundResult,
			string refundReason,
			string cardinalLookupResult,
			string cardinalAuthenticatResult,
			string cardinalGatewayParams,
			bool affiliateCommissionRecorded,
			string orderOptions,
			decimal weight,
			string carrierReportedRate,
			string carrierReportedWeight,
			string localeSetting,
			string finalizationData,
			string extensionData,
			bool alreadyConfirmed,
			int cartType,
			string last4,
			bool readyToShip,
			bool isPrinted,
			DateTime? authorizedOn,
			DateTime? capturedOn,
			DateTime? refundedOn,
			DateTime? voidedOn,
			DateTime? fraudedOn,
			DateTime? editedOn,
			string trackingUrl,
			DateTime? shippedEmailSentOn,
			bool inventoryWasReduced,
			decimal maxMindFraudScore,
			string maxMindDetails,
			string vatRegistrationId,
			int crypt,
			int transactionType,
			string recurringSubscriptionId,
			string recurringSubscriptionCommand,
			string recurringSubscriptionResult,
			int relatedOrderNumber,
			string buySafeCommand,
			string buySafeResult,
			string receiptHtml,
			DateTime updatedOn,
			DateTime createdon,
			IEnumerable<OrderItem> orderItems)
		{
			OrderNumber = orderNumber;
			StoreId = storeId;
			ParentOrderNumber = parentOrderNumber;
			StoreVersion = storeVersion;
			QuoteCheckout = quoteCheckout;
			IsNew = isNew;
			ShippedOn = shippedOn;
			CustomerId = customerId;
			CustomerGuid = customerGuid;
			Referrer = referrer;
			SkinId = skinId;
			LastName = lastName;
			FirstName = firstName;
			Email = email;
			Notes = notes;
			BillingEqualsShipping = billingEqualsShipping;
			BillingAddress = billingAddress;
			ShippingAddress = shippingAddress;
			ShippingResidenceType = shippingResidenceType;
			ShippingMethodId = shippingMethodId;
			ShippingMethod = shippingMethod;
			ShippingCalculationId = shippingCalculationId;
			Phone = phone;
			RegisterDate = registerDate;
			AffiliateId = affiliateId;
			CouponCode = couponCode;
			CouponType = couponType;
			CouponDescription = couponDescription;
			CouponDiscountAmount = couponDiscountAmount;
			CouponDiscountPercent = couponDiscountPercent;
			CouponIncludesFreeShipping = couponIncludesFreeShipping;
			OktoEmail = oktoEmail;
			Deleted = deleted;
			CardType = cardType;
			CardName = cardName;
			CardExpirationMonth = cardExpirationMonth;
			CardExpirationYear = cardExpirationYear;
			CardStartDate = cardStartDate;
			CardIssueNumber = cardIssueNumber;
			Subtotal = subtotal;
			Tax = tax;
			ShippingCost = shippingCost;
			Total = total;
			PaymentGateway = paymentGateway;
			AuthorizationCode = authorizationCode;
			AuthorizationResult = authorizationResult;
			AuthorizationPNRef = authorizationPNRef;
			TransactionCommand = transactionCommand;
			OrderDate = orderDate;
			CustomerLevelId = customerLevelId;
			CustomerLevelName = customerLevelName;
			CustomerLevelDiscountPercent = customerLevelDiscountPercent;
			CustomerLevelDiscountAmount = customerLevelDiscountAmount;
			CustomerLevelHasFreeShipping = customerLevelHasFreeShipping;
			CustomerLevelAllowsQuantityDiscounts = customerLevelAllowsQuantityDiscounts;
			CustomerLevelHasNoTax = customerLevelHasNoTax;
			CustomerLevelAllowsCoupons = customerLevelAllowsCoupons;
			CustomerLevelDiscountsApplyToExtendedPrices = customerLevelDiscountsApplyToExtendedPrices;
			LastIPAddress = lastIPAddress;
			PaymentMethod = paymentMethod;
			OrderNotes = orderNotes;
			PONumber = poNumber;
			DownloadEmailSentOn = downloadEmailSentOn;
			ReceiptEmailSentOn = receiptEmailSentOn;
			DistributorEmailSentOn = distributorEmailSentOn;
			ShippingTrackingNumber = shippingTrackingNumber;
			ShippedVia = shippedVia;
			CustomerServiceNotes = customerServiceNotes;
			RealtimeRatesRequest = realtimeRatesRequest;
			RealtimeRatesResponse = realtimeRatesResponse;
			TransactionState = transactionState;
			AVSResult = avsResult;
			CaptureCommand = captureCommand;
			CaptureResult = captureResult;
			VoidCommand = voidCommand;
			VoidResult = voidResult;
			RefundCommand = refundCommand;
			RefundResult = refundResult;
			RefundReason = refundReason;
			CardinalLookupResult = cardinalLookupResult;
			CardinalAuthenticatResult = cardinalAuthenticatResult;
			CardinalGatewayParams = cardinalGatewayParams;
			AffiliateCommissionRecorded = affiliateCommissionRecorded;
			OrderOptions = orderOptions;
			Weight = weight;
			CarrierReportedRate = carrierReportedRate;
			CarrierReportedWeight = carrierReportedWeight;
			LocaleSetting = localeSetting;
			FinalizationData = finalizationData;
			ExtensionData = extensionData;
			AlreadyConfirmed = alreadyConfirmed;
			CartType = cartType;
			Last4 = last4;
			ReadyToShip = readyToShip;
			IsPrinted = isPrinted;
			AuthorizedOn = authorizedOn;
			CapturedOn = capturedOn;
			RefundedOn = refundedOn;
			VoidedOn = voidedOn;
			FraudedOn = fraudedOn;
			EditedOn = editedOn;
			TrackingUrl = trackingUrl;
			ShippedEmailSentOn = shippedEmailSentOn;
			InventoryWasReduced = inventoryWasReduced;
			MaxMindFraudScore = maxMindFraudScore;
			MaxMindDetails = maxMindDetails;
			VATRegistrationId = vatRegistrationId;
			Crypt = crypt;
			TransactionType = transactionType;
			RecurringSubscriptionId = recurringSubscriptionId;
			RecurringSubscriptionCommand = recurringSubscriptionCommand;
			RecurringSubscriptionResult = recurringSubscriptionResult;
			RelatedOrderNumber = relatedOrderNumber;
			BuySafeCommand = buySafeCommand;
			BuySafeResult = buySafeResult;
			ReceiptHtml = receiptHtml;
			UpdatedOn = updatedOn;
			Createdon = createdon;
			OrderItems = orderItems;
		}
	}
}
