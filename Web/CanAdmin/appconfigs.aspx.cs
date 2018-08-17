// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web.UI.WebControls;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefrontAdmin
{
	public partial class AppConfigs : AspDotNetStorefront.Admin.AdminPageBase
	{
		protected override void OnInit(System.EventArgs e)
		{
			base.OnInit(e);
			var isSuperParam = new Parameter("IsSuperAdmin", System.Data.DbType.Boolean, AppLogic.GetCurrentCustomer().IsAdminSuperUser.ToString());
			FilteredListing.SqlParameters.Add(isSuperParam);
		}
	}
}
