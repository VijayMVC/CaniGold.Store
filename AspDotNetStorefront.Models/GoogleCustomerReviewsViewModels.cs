// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
namespace AspDotNetStorefront.Models
{
	public class GoogleCustomerReviewsBadgeViewModel
	{
		public readonly long MerchantId;
		public readonly string BadgePosition;
		public readonly string Language;

		public GoogleCustomerReviewsBadgeViewModel(
			long merchantId,
			string badgePosition,
			string language)
		{
			MerchantId = merchantId;
			BadgePosition = badgePosition;
			Language = language;
		}
	}

	public class GoogleCustomerReviewsOptInSurveyViewModel
	{
		public readonly long MerchantId;
		public readonly int OrderNumber;
		public readonly string Email;
		public readonly string CountryCode;
		public readonly string DeliveryDate;
		public readonly string SurveyPosition;
		public readonly bool BadgeEnabled;
		public readonly string Language;

		public GoogleCustomerReviewsOptInSurveyViewModel(
			long merchantId,
			int orderNumber,
			string email,
			string countryCode,
			string deliveryDate,
			string surveyPosition,
			bool badgeEnabled,
			string language)
		{
			MerchantId = merchantId;
			OrderNumber = orderNumber;
			Email = email;
			CountryCode = countryCode;
			DeliveryDate = deliveryDate;
			SurveyPosition = surveyPosition;
			BadgeEnabled = badgeEnabled;
			Language = language;
		}
	}
}
