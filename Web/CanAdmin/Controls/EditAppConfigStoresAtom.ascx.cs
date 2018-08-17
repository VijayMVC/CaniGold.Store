// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Web.UI;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefrontAdmin.Controls
{
	public partial class EditAppConfigStoresAtom : UserControl
	{
		private AppConfig _DataSource;
		private bool _DataSourceExists = false;
		private AppConfig _PassedConfig;

		public AppConfig DataSource
		{
			get
			{
				return _DataSource;
			}
			set
			{
				_PassedConfig = value;
				_DataSourceExists = AppConfigManager.AppConfigExists(value.Name);
				if(_DataSourceExists)
					_DataSource = AppConfigManager.GetAppConfig(value.Name);
				else
					_DataSource = value;
			}
		}
		public Boolean HideTableNode { get; set; }
		public String CssClass { get; set; }
	}
}
