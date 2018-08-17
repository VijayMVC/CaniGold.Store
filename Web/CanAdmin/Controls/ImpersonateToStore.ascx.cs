// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Linq;
using System.Web.Mvc;
using AspDotNetStorefrontCore;

public partial class Admin_Controls_ImpersonateToStore : System.Web.UI.UserControl
{
	public int CustomerId { get; set; }
	public int CustomerStoreId { get; set; }

	readonly UrlHelper UrlHelper;

	public Admin_Controls_ImpersonateToStore()
	{
		UrlHelper = DependencyResolver.Current.GetService<UrlHelper>();
	}

	protected override void DataBind(bool raiseOnDataBinding)
	{
		base.DataBind(raiseOnDataBinding);

		if(CustomerId == 0 || CustomerStoreId == 0)
		{
			this.Visible = false;
			return;
		}

		var stores = Store.GetStoreList();

		if(AppLogic.GlobalConfigBool("AllowCustomerFiltering") || stores.Count() < 2)
		{
			MultiStorePanel.Visible = false;
			SingleStoreLink.Visible = true;

			if(CustomerStoreId != AppLogic.StoreID())
				SingleStoreLink.Attributes.Add("data-confirm", "admin.customer.ConfirmStoreImpersonateSwitch".StringResource());

			SingleStoreLink.NavigateUrl = UrlHelper.AdminLinkForStore(
				adminPage: string.Format("impersonationhandler.axd?customerId={0}", CustomerId),
				storeId: CustomerStoreId);
		}
		else
		{
			MultiStorePanel.Visible = true;
			SingleStoreLink.Visible = false;
			StoreList.DataSource = stores
				.Select(s => new
				{
					Name = string.Concat(
						s.Name,
						AppLogic.StoreID() == s.StoreID
							? " (Current)"
							: ""),
					Url = UrlHelper.AdminLinkForStore(
						adminPage: string.Format("impersonationhandler.axd?customerId={0}", CustomerId),
						storeId: s.StoreID),
					CustomerStoreID = s.StoreID,
					Confirm = AppLogic.StoreID() == s.StoreID
						? ""
						: "admin.customer.ConfirmStoreImpersonateSwitch".StringResource()
				});

			StoreList.DataBind();
		}
	}
}
