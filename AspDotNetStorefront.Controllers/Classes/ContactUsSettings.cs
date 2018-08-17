// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Classes
{
	public class ContactUsSettings
	{
		public readonly string PageTitle;
		public readonly string FromAddress;
		public readonly string FromName;
		public readonly string ToAddress;
		public readonly string ToName;
		public readonly string MailServer;

		public ContactUsSettings()
		{

			PageTitle = $"{AppLogic.AppConfig("StoreName")} - {AppLogic.GetString("ContactUs.Page.Title")}";

			FromAddress = AppLogic.AppConfig("ContactUsFromEmail");
			FromName = AppLogic.AppConfig("ContactUsFromName");
			ToAddress = AppLogic.AppConfig("ContactUsToEmail");
			ToName = AppLogic.AppConfig("ContactUsToName");
			MailServer = AppLogic.AppConfig("MailMe_Server");
		}
	}
}
