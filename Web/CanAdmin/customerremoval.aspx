<%@ Page Language="C#" AutoEventWireup="true" Inherits="AspDotNetStorefrontAdmin.CustomerRemoval" MaintainScrollPositionOnPostback="true" MasterPageFile="~/App_Templates/Admin_Default/AdminMaster.master" CodeBehind="customerremoval.aspx.cs" %>

<%@ Import Namespace="AspDotNetStorefrontCore" %>

<%@ Register TagPrefix="listing" Assembly="AspDotNetStorefrontControls" Namespace="AspDotNetStorefrontControls.Listing" %>
<%@ Register TagPrefix="filter" TagName="IntegerFilter" Src="Controls/Listing/IntegerFilter.ascx" %>
<%@ Register TagPrefix="filter" TagName="StringFilter" Src="Controls/Listing/StringFilter.ascx" %>
<%@ Register TagPrefix="filter" TagName="MultiFieldStringFilter" Src="Controls/Listing/MultiFieldStringFilter.ascx" %>

<asp:Content ContentPlaceHolderID="bodyContentPlaceholder" runat="Server">
	<h1>
		<i class="fa fa-users"></i>
		<asp:Literal runat="server" Text="<%$ Tokens:StringResource, admin.title.customerremoval %>" />
	</h1>

	<aspdnsf:AlertMessage runat="server" ID="AlertMessageDisplay" />

	<listing:FilteredListing runat="server"
		ID="FilteredListing"
		SqlQuery="
			select {0} 
				Customer.CustomerID CustomerID, 
				Customer.CreatedOn,
				customer.firstname,
				customer.lastname,
				customerdataretention.removalrequestdate,
				ltrim(customer.firstname + ' ' + customer.lastname) [Name], 
				Customer.Email
			from 
				dbo.Customer with(nolock)
				join dbo.customerdataretention on customer.customerid = customerdataretention.customerid
			where customer.deleted = 0 and customer.isadmin = 0
			and customerdataretention.removalrequestdate is not null and customerdataretention.removaldate is null
			and {1}"
		SortExpression="customerdataretention.removalrequestdate"
		LocaleSelectionEnabled="true"
		PageSizes="20,50,100"
		DefaultPageSize="20">
			<ActionBarTemplate>
				<asp:Button ID="btnSave"
					runat="server"
					Text="<%$ Tokens:StringResource, admin.common.Update %>"
					CssClass="bulk-save-anon-remove btn btn-primary"
					OnClick="Save_Click" />
			</ActionBarTemplate>
		<Filters>
			<filter:StringFilter runat="server"
				Label="<%$ Tokens:StringResource, admin.common.Email %>"
				FieldName="Customer.Email" />

			<filter:MultiFieldStringFilter runat="server"
				Label="<%$ Tokens:StringResource, admin.common.Name %>"
				Fields="Customer.FirstName,Customer.LastName" />

			<filter:IntegerFilter runat="server"
				Label="<%$ Tokens:StringResource, admin.common.CustomerId %>"
				FieldName="Customer.CustomerID" />
		</Filters>
		<ListingTemplate>
			<div class="white-ui-box">
				<asp:GridView runat="server"
					ID="Grid"
					CssClass="table js-sortable-gridview"
					GridLines="None"
					DataSourceID="FilteredListingDataSource"
					AllowSorting="true"
					AutoGenerateColumns="false"
					DataKeyNames="CustomerID"
					OnLoad="GridLoad">
					<EmptyDataTemplate>
						<div class="alert alert-info">
							<asp:Literal runat="server" Text="<%$ Tokens:StringResource, admin.common.EmptyDataTemplate.NoResults %>" />
						</div>
					</EmptyDataTemplate>
					<Columns>
						<asp:BoundField
							HeaderText="<%$Tokens:StringResource, admin.common.CustomerID %>"
							SortExpression="Customer.CustomerID"
							DataField="CustomerID" />

						<asp:HyperLinkField
							HeaderText="Name"
							SortExpression="ltrim(Customer.FirstName + ' ' + Customer.LastName)"
							DataNavigateUrlFields="CustomerID"
							DataNavigateUrlFormatString="customer.aspx?customerid={0}"
							DataTextField="Name"
							Text="<%$Tokens:StringResource, admin.nolinktext %>" />

						<asp:BoundField
							HeaderText="<%$Tokens:StringResource, admin.editaddress.Email %>"
							SortExpression="Customer.Email"
							DataField="Email" />

						<asp:BoundField
							HeaderText="<%$ Tokens: StringResource, admin.customerremoval.createdate %>"
							HeaderStyle-Width="15%"
							SortExpression="Customer.CreatedOn"
							DataField="CreatedOn" />

						<asp:BoundField
							HeaderText="<%$ Tokens: StringResource, admin.customerremoval.requestdate %>"
							HeaderStyle-Width="15%"
							SortExpression="customerdataretention.removalrequestdate"
							DataField="removalrequestdate" />

						<asp:TemplateField
							HeaderText="<%$ Tokens: StringResource, admin.customerremoval.anonymize %>"
							HeaderStyle-Width="8%">
							<ItemTemplate>
								<asp:CheckBox ID="chkAnonymizeCustomer" runat="server" />
							</ItemTemplate>
						</asp:TemplateField>

						<asp:TemplateField>
							<ItemTemplate>
								<asp:HiddenField ID="hidCustomerId" Value='<%# Eval("customerid") %>' runat="server" />
								<asp:HiddenField ID="hidCustomerInfo" Value='<%# String.Concat(Eval("customerid"), " (", Eval("email"), ")") %>' runat="server" />
							</ItemTemplate>
						</asp:TemplateField>
					</Columns>
				</asp:GridView>
			</div>
		</ListingTemplate>
	</listing:FilteredListing>
</asp:Content>
