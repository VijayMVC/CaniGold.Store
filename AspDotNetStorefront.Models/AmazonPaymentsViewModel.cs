// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Models
{
	public class AmazonPaymentsViewModel
	{
		public readonly string ClientId;
		public readonly string MerchantId;
		public readonly string ScriptUrl;
		public readonly SelectList ResidenceTypeOptions;

		public AmazonPaymentsViewModel()
		{
			CheckoutStep = AmazonPaymentsCheckoutStep.Login;
		}

		public AmazonPaymentsViewModel(SelectList residenceTypeOptions, string clientId = null, string merchantId = null, string scriptUrl = null)
		{
			ResidenceTypeOptions = residenceTypeOptions;
			ClientId = clientId;
			MerchantId = merchantId;
			ScriptUrl = scriptUrl;
		}

		public string AmazonOrderReferenceId
		{ get; set; }

		[Required(ErrorMessage = "address.residenceType.required")]
		public ResidenceTypes ResidenceType
		{ get; set; }

		public AmazonPaymentsCheckoutStep CheckoutStep
		{ get; set; }
	}

	public enum AmazonPaymentsCheckoutStep
	{
		Login,
		SelectAddress,
	}
}
