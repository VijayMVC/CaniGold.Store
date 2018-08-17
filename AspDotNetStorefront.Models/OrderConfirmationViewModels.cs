// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
namespace AspDotNetStorefront.Models
{
	public class OrderConfirmationViewModel
	{
		public readonly int OrderNumber;
		public readonly string Body;
		public readonly string GoogleTrackingCode;
		public readonly string GeneralTrackingCode;
		public readonly bool ShowGeneralTrackingCode;
		public readonly bool ShowGoogleTrackingCode;
		public readonly bool ShowGoogleCustomerReviews;
		public readonly bool AddPayPalExpressCheckoutScript;
		public readonly bool AddBuySafeScript;

		public OrderConfirmationViewModel(
			int orderNumber,
			string body,
			string googleTrackingCode,
			string generalTrackingCode,
			bool showGoogleTrackingCode,
			bool showGeneralTrackingCode,
			bool showGoogleCustomerReviews,
			bool addPayPalExpressCheckoutScript,
			bool addBuySafeScript)
		{
			OrderNumber = orderNumber;
			Body = body;
			GoogleTrackingCode = googleTrackingCode;
			GeneralTrackingCode = generalTrackingCode;
			ShowGoogleTrackingCode = showGoogleTrackingCode;
			ShowGeneralTrackingCode = showGeneralTrackingCode;
			ShowGoogleCustomerReviews = showGoogleCustomerReviews;
			AddPayPalExpressCheckoutScript = addPayPalExpressCheckoutScript;
			AddBuySafeScript = addBuySafeScript;
		}
	}

	public class BuySafeGuaranteeViewModel
	{
		public readonly int OrderNumber;
		public readonly string JSLocation;
		public readonly string Hash;
		public readonly string Email;
		public readonly decimal Total;

		public BuySafeGuaranteeViewModel(
			int orderNumber,
			string jsLocation,
			string hash,
			string email,
			decimal total)
		{
			OrderNumber = orderNumber;
			JSLocation = jsLocation;
			Hash = hash;
			Email = email;
			Total = total;
		}
	}

	public class ReceiptViewModel
	{
		public readonly string Body;

		public ReceiptViewModel(string body)
		{
			Body = body;
		}
	}
}
