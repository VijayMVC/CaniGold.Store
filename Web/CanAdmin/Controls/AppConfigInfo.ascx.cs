// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using AspDotNetStorefrontControls;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefrontAdmin.Controls
{
	public partial class AppConfigInfo : BaseUserControl<AppConfig>
	{
		public String AppConfigName;
		private AppConfig _AppConfig
		{
			get
			{
				return AppConfigManager.GetAppConfig(AppConfigName);
			}
		}

		protected void Page_Load(object sender, EventArgs e)
		{
			imgAppConfigInfo.Attributes.Add("title", Server.HtmlEncode(_AppConfig.Description));
		}
	}
}
