// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;

namespace AspDotNetStorefront.ClientResource
{
	public static class ScriptPaths
	{
		public static readonly string AddToCart = "~/scripts/addtocart.js";
		public static readonly string AmazonPayments = "~/scripts/amazonpayments.js";
		public static readonly string BoostrappedWcagTabs = "~/scripts/bootstrappedWcagTabs.js";
		public static readonly string Card = "~/scripts/card.js";
		public static readonly string Checkout = "~/scripts/checkout.js";
		public static readonly string CountryAndState = "~/scripts/countryandstate.js";
		public static readonly JQueryScriptPaths JQuery = new JQueryScriptPaths();
		public static readonly JQueryValidateScriptPaths JQueryValidate = new JQueryValidateScriptPaths();
		public static readonly string KitProduct = "~/scripts/kitproduct.js";
		public static readonly string Mask = "~/scripts/jquery.mask.js";
		public static readonly string Minicart = "~/scripts/minicart.js";
		public static readonly string ModalLogic = "~/scripts/modalLogic.js";
		[Obsolete("ModalEvent replaced by ModalLogic (10.0.14) - Retained for compatibility with prior skin versions.")]
		public static readonly string ModalEvent = ModalLogic;
		public static readonly string PaypalExpress = "~/scripts/paypalexpress.js";
		public static readonly string SessionTimer = "~/scripts/sessiontimer.js";
		public static readonly string Tabbit = "~/scripts/tabbit.js";
		public static readonly string ToolTip = "~/scripts/tooltip.js";
		public static readonly ValidateScriptPaths Validate = new ValidateScriptPaths();
		public static readonly string ValidationOptions = "~/scripts/validationoptions.js";
		public static readonly string WhatsThis = "~/scripts/whatsthis.js";

		public class JQueryScriptPaths
		{
			public readonly string AdnsfAlias = "~/scripts/jquery.adnsfalias.js";
			public readonly string Validate = "~/scripts/jquery.validate.js";

			public static implicit operator string(JQueryScriptPaths paths)
			{
				return "~/scripts/jquery.js";
			}
		}

		public class JQueryValidateScriptPaths
		{
			public readonly string Unobtrusive = "~/scripts/jquery.validate.unobtrusive.js";
			public readonly string Bootstrap = "~/scripts/jquery.validate.bootstrap.js";
		}

		public class ValidateScriptPaths
		{
			public readonly string PostalCodeRegexLookup = "~/scripts/validate.postalcoderegexlookup.js";
			public readonly string CreditCardFormat = "~/scripts/validate.creditcardformat.js";
			public readonly string CreditCardFutureExpirationDate = "~/scripts/validate.creditcardfutureexpirationdate.js";
			public readonly string RequireChecked = "~/scripts/validate.requirechecked.js";
		}
	}
}
