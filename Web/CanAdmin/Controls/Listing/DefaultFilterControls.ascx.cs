// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web.UI;

namespace AspDotNetStorefrontControls.Listing
{
	public partial class DefaultFilterControls : UserControl
	{
		protected override void CreateChildControls()
		{
			base.CreateChildControls();
			DataBind();
		}
	}
}
