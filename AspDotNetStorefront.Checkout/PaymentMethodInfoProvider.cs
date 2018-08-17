// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways;

namespace AspDotNetStorefront.Checkout
{
	public class PaymentMethodInfoProvider : IPaymentMethodInfoProvider
	{
		readonly Dictionary<string, string> PaymentMethodDisplayNames;

		public PaymentMethodInfoProvider()
		{
			PaymentMethodDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ AppLogic.ro_PMPayPalExpress, AppLogic.GetString("pm.paypalexpress.display") },
				{ AppLogic.ro_PMPayPalCredit, AppLogic.GetString("pm.paypalcredit.display") },
				{ AppLogic.ro_PMPayPalEmbeddedCheckout, AppLogic.GetString("pm.paypalpaymentsadvanced.display") },
				{ AppLogic.ro_PMCreditCard, AppLogic.GetString("pm.creditcard.display") },
				{ AppLogic.ro_PMRequestQuote, AppLogic.GetString("pm.requestquote.display") },
				{ AppLogic.ro_PMPurchaseOrder, AppLogic.GetString("pm.purchaseorder.display") },
				{ AppLogic.ro_PMCheckByMail, AppLogic.GetString("pm.checkbymail.display") },
				{ AppLogic.ro_PMCOD, AppLogic.GetString("pm.cod.display") },
				{ AppLogic.ro_PMECheck, AppLogic.GetString("pm.echeck.display") },
				{ AppLogic.ro_PMMicropay, AppLogic.GetString("pm.micropay.display") },
			};
		}

		public PaymentMethodInfo GetPaymentMethodInfo(string paymentMethod, string gateway)
		{
			if(string.IsNullOrEmpty(paymentMethod))
				return null;

			return new PaymentMethodInfo(
				name: paymentMethod,
				displayName: GetDisplayName(paymentMethod, gateway),
				location: GetLocation(paymentMethod, gateway),
				requiresBillingSelection: GetRequiresBillingSelection(paymentMethod, gateway));
		}

		string GetDisplayName(string paymentMethod, string gateway)
		{
			return PaymentMethodDisplayNames.ContainsKey(paymentMethod)
				? PaymentMethodDisplayNames[paymentMethod]
				: null;
		}

		PaymentMethodLocation GetLocation(string paymentMethod, string gateway)
		{
			if(paymentMethod == AppLogic.ro_PMPayPalEmbeddedCheckout)
				return PaymentMethodLocation.Offsite;

			if(paymentMethod == AppLogic.ro_PMCreditCard
				&& gateway == Gateway.ro_GWTWOCHECKOUT)
				return PaymentMethodLocation.Offsite;

			return PaymentMethodLocation.Onsite;
		}

		bool GetRequiresBillingSelection(string paymentMethod, string gateway)
		{
			if(paymentMethod == AppLogic.ro_PMAmazonPayments
				|| paymentMethod == AppLogic.ro_PMPayPalExpress)
				return false;

			return true;
		}
	}
}
