<%@ Control Language="C#" AutoEventWireup="true" Inherits="Admin_Controls_ImpersonateToStore" Codebehind="ImpersonateToStore.ascx.cs" %>

<asp:HyperLink CssClass="btn btn-default" runat="server" id="SingleStoreLink" Text="<%$ Tokens:StringResource, admin.customer.ImpersonateCustomer %>" />

<asp:Panel runat="server" class="dropdown impersonation-dropdown"  ID="MultiStorePanel">
	<a class="dropdown-toggle btn btn-default" type="button" data-toggle="dropdown" href="#">
		<asp:Literal runat="server" Text="<%$ Tokens:StringResource, admin.customer.ImpersonateCustomer %>"/>
 		<span class="caret"></span>
	</a>
	<ul class="dropdown-menu" role="menu" >
		<asp:Repeater runat="server" ID="StoreList">
			<ItemTemplate>
				<li role="presentation">
					<a role="menuitem" tabindex="-1" href="<%# Eval("Url") %>" data-confirm="<%# HttpUtility.HtmlAttributeEncode(Eval("Confirm").ToString()) %>";>
						<%# Eval("Name") %>
					</a>
				</li>
			</ItemTemplate>
		</asp:Repeater>
	</ul>
</asp:Panel>