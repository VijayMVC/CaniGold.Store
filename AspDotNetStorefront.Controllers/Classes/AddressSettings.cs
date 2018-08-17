// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Classes
{
	public class AddressSettings
	{
		public readonly bool AllowDifferentShipTo;
		public readonly bool ShowCompanyField;
		public readonly bool ShowNickName;
		public readonly bool ShowSuite;
		public readonly bool UsePhoneNumberMask;

		public AddressSettings()
		{
			AllowDifferentShipTo = AppLogic.AppConfigBool("AllowShipToDifferentThanBillTo");
			ShowCompanyField = AppLogic.AppConfigBool("Address.CollectCompany");
			ShowNickName = AppLogic.AppConfigBool("Address.CollectNickName");
			ShowSuite = AppLogic.AppConfigBool("Address.CollectSuite");
			UsePhoneNumberMask = AppLogic.AppConfigBool("PhoneNumberMask.Enabled");
		}
	}
}
