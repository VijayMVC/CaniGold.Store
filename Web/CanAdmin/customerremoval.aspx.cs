// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Text;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using AspDotNetStorefront.StringResource;
using AspDotNetStorefrontControls;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontCore.DataRetention;

namespace AspDotNetStorefrontAdmin
{
	public partial class CustomerRemoval : AspDotNetStorefront.Admin.AdminPageBase
	{
		readonly IDataRetentionService DataRetentionService;
		readonly IStringResourceProvider StringResourceProvider;

		public CustomerRemoval()
		{
			DataRetentionService = DependencyResolver.Current
				.GetService<IDataRetentionService>();
			StringResourceProvider = DependencyResolver.Current
				.GetService<IStringResourceProvider>();
		}
		
		protected override void OnLoad(EventArgs e)
		{
			if(!AppLogic.AppConfigBool("DataRetentionPolicies.Enabled"))
			{
				AlertMessageDisplay.PushAlertMessage(StringResourceProvider.GetString("admin.customerremoval.dataretention.notenabled.message"),
					AlertMessage.AlertType.Error);

				Response.Redirect("default.aspx");
			}
			base.OnLoad(e);
		}

		protected void GridLoad(object sender, EventArgs e)
		{
			// This is to work around the SqlDataSource not returning rows in the order
			// returned by the database.
			if(sender is GridView)
				((GridView)sender).Sort("Paging.RowIndex", SortDirection.Ascending);
		}

		protected void Save_Click(object sender, EventArgs e)
		{
			var customerAnonymizeSuccesses = new StringBuilder();
			var cannotBeAnonymized = new StringBuilder();
			var customerAnonymizeErrors = new StringBuilder();

			var customerDeleteSuccesses = new StringBuilder();
			var cannotBeDeleted = new StringBuilder();
			var customerDeleteErrors = new StringBuilder();

			var orderAnonymizeSuccesses = new StringBuilder();
			var orderAnonymizeErrors = new StringBuilder();

			foreach(GridViewRow row in Grid.Rows)
			{
				if(row.RowType != DataControlRowType.DataRow)
					continue;

				// We're getting the customer id through a hidden field because the grid data keys are cleared in the GridLoad() sorting above.
				var customerId = Convert.ToInt32(((HiddenField)row.FindControl("hidCustomerId")).Value);
				var chkAnonymize = (CheckBox)row.FindControl("chkAnonymizeCustomer");
				var chkRemove = (CheckBox)row.FindControl("chkRemoveCustomer");
				var customerInfo = ((HiddenField)row.FindControl("hidCustomerInfo")).Value;

				if(chkAnonymize.Checked)
				{
					if(!DataRetentionService.CustomerCanBeAnonymized(customerId))
					{
						cannotBeAnonymized
							.Append(customerInfo)
							.Append("<br />");

						continue;
					}

					var result = DataRetentionService.AnonymizeCustomer(customerId);

					if(!result.Success)
					{
						customerAnonymizeErrors
							.Append(customerInfo)
							.Append("<br />");

						SysLog.LogException(result.Error, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);

						continue;
					}

					if(!result.Value)
					{
						customerAnonymizeErrors
							.Append(customerInfo)
							.Append("<br />");

						continue;
					}

					customerAnonymizeSuccesses
						.Append(customerInfo)
						.Append("<br />");

					foreach(var orderResult in DataRetentionService.AnonymizeOrders(customerId))
					{
						if(!orderResult.Success)
						{
							orderAnonymizeErrors
								.Append(customerInfo)
								.Append("<br />");

							SysLog.LogException(result.Error, MessageTypeEnum.GeneralException, MessageSeverityEnum.Error);

							continue;
						}

						if(!orderResult.Value)
						{
							orderAnonymizeErrors
								.Append(customerInfo)
								.Append("<br />");

							continue;
						}

						orderAnonymizeSuccesses
							.Append(customerInfo)
							.Append("<br />");
					}
				}
			}

			if(customerAnonymizeSuccesses.Length > 0)
				AlertMessageDisplay.PushAlertMessage(
					string.Concat("The following customer(s) have been anonymized:<br />", customerAnonymizeSuccesses.ToString()),
					AlertMessage.AlertType.Success);

			if(cannotBeAnonymized.Length > 0)
				AlertMessageDisplay.PushAlertMessage(
					string.Concat("The following customer(s) and their orders could not be anonymized:<br />", cannotBeAnonymized.ToString()),
					AlertMessage.AlertType.Error);

			if(customerAnonymizeErrors.Length > 0)
				AlertMessageDisplay.PushAlertMessage(
					string.Concat("The following customer(s) could not be anonymized:<br />", customerAnonymizeErrors.ToString()),
					AlertMessage.AlertType.Error);

			if(customerDeleteSuccesses.Length > 0)
				AlertMessageDisplay.PushAlertMessage(
					string.Concat("The following customer(s) have been removed:<br />", customerDeleteSuccesses.ToString()),
					AlertMessage.AlertType.Success);

			if(cannotBeDeleted.Length > 0)
				AlertMessageDisplay.PushAlertMessage(
					string.Concat("The following customer(s) and their orders could not be removed:<br />", cannotBeDeleted.ToString()),
					AlertMessage.AlertType.Error);

			if(customerDeleteErrors.Length > 0)
				AlertMessageDisplay.PushAlertMessage(
					string.Concat("The following customer(s) could not be removed:<br />", customerDeleteErrors.ToString()),
					AlertMessage.AlertType.Error);

			if(orderAnonymizeSuccesses.Length > 0)
				AlertMessageDisplay.PushAlertMessage(
					string.Concat("The following customer(s) orders have been anonymized:<br />", orderAnonymizeSuccesses.ToString()),
					AlertMessage.AlertType.Success);

			if(orderAnonymizeErrors.Length > 0)
				AlertMessageDisplay.PushAlertMessage(
					string.Concat("The following customer's orders could not be anonymized:<br />", orderAnonymizeErrors.ToString()),
					AlertMessage.AlertType.Error);

			FilteredListing.Rebind();
		}
	}
}
