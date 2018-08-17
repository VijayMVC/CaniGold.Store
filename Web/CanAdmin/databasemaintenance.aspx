<%@ Page Language="c#" AutoEventWireup="true" Inherits="AspDotNetStorefrontAdmin.DatabaseMaintenance" MaintainScrollPositionOnPostback="true"
	MasterPageFile="~/App_Templates/Admin_Default/AdminMaster.master" CodeBehind="databasemaintenance.aspx.cs" %>

<asp:Content runat="server" ContentPlaceHolderID="bodyContentPlaceholder">
	<asp:Literal ID="ltScript" runat="server"></asp:Literal>
	<asp:Literal ID="ltValid" runat="server"></asp:Literal>
	<asp:Literal ID="ltError" runat="server"></asp:Literal>

	<asp:Panel runat="server" ID="container">
		<h1>
			<i class="fa fa-wrench"></i>
			<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.menu.Maintenance %>" />
		</h1>
		<div class="row form-group">
			<div class="col-sm-12">
				<label>
					<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.PageInfo %>" /></label>
			</div>
		</div>
		<div class="row form-group">
			<div class="col-sm-12">
				<asp:Label ID="lblNotice" runat="server" CssClass="text-danger" Text="<%$Tokens:StringResource, admin.databasemaintenance.Info %>"></asp:Label>
			</div>
		</div>
		<div class="list-action-bar">
			<asp:Button ID="btnRunMaintenance" runat="server" CssClass="btn btn-primary" Text="<%$Tokens:StringResource, admin.common.Go %>" OnClick="RunMaintenance_Click" />
		</div>
		<div class="white-ui-box">
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.ClearAllShoppingCarts %>" />:</label>
				</div>
				<div class="col-sm-8">
					<asp:DropDownList ID="ClearAllShoppingCarts" CssClass="default" runat="server">
						<asp:ListItem Value="0" Text="<%$Tokens:StringResource, admin.databasemaintenance.LeaveUnchanged %>" />
						<asp:ListItem Value="-1" Text="<%$Tokens:StringResource, admin.databasemaintenance.ClearAll %>" />
						<asp:ListItem Selected="True" Value="30" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan30 %>" />
						<asp:ListItem Value="60" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan60 %>" />
						<asp:ListItem Value="90" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan90 %>" />
						<asp:ListItem Value="120" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan120 %>" />
						<asp:ListItem Value="150" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan150 %>" />
						<asp:ListItem Value="180" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan180 %>" />
					</asp:DropDownList>
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgClearAllShoppingCarts %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.ClearAllWishlists %>" />:</label>
				</div>
				<div class="col-sm-8">
					<asp:DropDownList ID="ClearAllWishLists" CssClass="default" runat="server">
						<asp:ListItem Value="0" Text="<%$Tokens:StringResource, admin.databasemaintenance.LeaveUnchanged %>" />
						<asp:ListItem Value="-1" Text="<%$Tokens:StringResource, admin.databasemaintenance.ClearAll %>" />
						<asp:ListItem Value="30" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan30 %>" />
						<asp:ListItem Value="60" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan60 %>" />
						<asp:ListItem Value="90" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan90 %>" />
						<asp:ListItem Selected="True" Value="120" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan120 %>" />
						<asp:ListItem Value="150" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan150 %>" />
						<asp:ListItem Value="180" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan180 %>" />
					</asp:DropDownList>
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgClearAllWishLists %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.EraseCCInfo %>" />:</label>
				</div>
				<div class="col-sm-8">
					<asp:DropDownList ID="EraseOrderCreditCards" CssClass="default" runat="server">
						<asp:ListItem Value="0" Text="<%$Tokens:StringResource, admin.databasemaintenance.LeaveUnchanged %>" />
						<asp:ListItem Value="-1" Text="<%$Tokens:StringResource, admin.databasemaintenance.ClearAll %>" />
						<asp:ListItem Selected="True" Value="30" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan30 %>" />
						<asp:ListItem Value="60" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan60 %>" />
						<asp:ListItem Value="90" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan90 %>" />
						<asp:ListItem Value="120" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan120 %>" />
						<asp:ListItem Value="150" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan150 %>" />
						<asp:ListItem Value="180" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan180 %>" />
					</asp:DropDownList>
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgEraseOrderCreditCards %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.ClearProfiles %>" /></label>
				</div>
				<div class="col-sm-8">
					<asp:DropDownList ID="EraseProfileLog" CssClass="default" runat="server">
						<asp:ListItem Value="-1" Text="<%$Tokens:StringResource, admin.databasemaintenance.LeaveUnchanged %>" />
						<asp:ListItem Value="0" Text="<%$Tokens:StringResource, admin.databasemaintenance.ClearAll %>" />
						<asp:ListItem Selected="True" Value="30" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan30 %>" />
						<asp:ListItem Value="60" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan60 %>" />
						<asp:ListItem Value="90" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan90 %>" />
						<asp:ListItem Value="120" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan120 %>" />
						<asp:ListItem Value="150" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan150 %>" />
						<asp:ListItem Value="180" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan180 %>" />
					</asp:DropDownList>
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgProfile %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.ClearViews %>" /></label>
				</div>
				<div class="col-sm-8">
					<asp:DropDownList ID="ClearProductViewsOlderThan" CssClass="default" runat="server">
						<asp:ListItem Value="0" Text="<%$Tokens:StringResource, admin.databasemaintenance.LeaveUnchanged %>" />
						<asp:ListItem Value="-1" Text="<%$Tokens:StringResource, admin.databasemaintenance.ClearAll %>" />
						<asp:ListItem Value="30" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan30 %>" />
						<asp:ListItem Value="60" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan60 %>" />
						<asp:ListItem Value="90" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan90 %>" />
						<asp:ListItem Value="120" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan120 %>" />
						<asp:ListItem Value="150" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan150 %>" />
						<asp:ListItem Selected="True" Value="180" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan180 %>" />
					</asp:DropDownList>
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgClearProductViewsOlderThan %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal ID="Literal1" runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.ClearRTShippingData %>" /></label>
				</div>
				<div class="col-sm-8">
					<asp:DropDownList ID="dlClearRTShippingData" CssClass="default" runat="server">
						<asp:ListItem Value="0" Text="<%$Tokens:StringResource, admin.databasemaintenance.LeaveUnchanged %>" />
						<asp:ListItem Value="-1" Text="<%$Tokens:StringResource, admin.databasemaintenance.ClearAll %>" />
						<asp:ListItem Selected="True" Value="30" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan30 %>" />
						<asp:ListItem Value="60" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan60 %>" />
						<asp:ListItem Value="90" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan90 %>" />
						<asp:ListItem Value="120" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan120 %>" />
						<asp:ListItem Value="150" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan150 %>" />
						<asp:ListItem Value="180" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan180 %>" />
					</asp:DropDownList>
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgClearRTShippingDataOlderThan %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal ID="litsearchText" runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.ClearSearchLog %>" /></label>
				</div>
				<div class="col-sm-8">
					<asp:DropDownList ID="dlClearSearchData" CssClass="default" runat="server">
						<asp:ListItem Value="0" Text="<%$Tokens:StringResource, admin.databasemaintenance.LeaveUnchanged %>" />
						<asp:ListItem Value="-1" Text="<%$Tokens:StringResource, admin.databasemaintenance.ClearAll %>" />
						<asp:ListItem Selected="True" Value="30" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan30 %>" />
						<asp:ListItem Value="60" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan60 %>" />
						<asp:ListItem Value="90" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan90 %>" />
						<asp:ListItem Value="120" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan120 %>" />
						<asp:ListItem Value="150" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan150 %>" />
						<asp:ListItem Value="180" Text="<%$Tokens:StringResource, admin.databasemaintenance.MoreThan180 %>" />
					</asp:DropDownList>
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgClearSearchLogOlderThan %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.PurgeAnon %>" />:</label>
				</div>
				<div class="col-sm-8">
					<asp:CheckBox ID="PurgeAnonUsers" runat="server" Checked="True" />
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgPurgeAnonUsers %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.EraseCCInfoFromAddress %>" />:</label>
				</div>
				<div class="col-sm-8">
					<asp:CheckBox ID="EraseAddressCreditCards" runat="server" Checked="True" />
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgEraseAddressCreditCards %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.PurgeAll %>" />:</label>
				</div>
				<div class="col-sm-8">
					<asp:CheckBox ID="PurgeDeletedRecords" runat="server" Checked="False" />
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgPurgeDeletedRecords %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<asp:Panel runat="server">
				<div class="row form-group">
					<div class="col-sm-4">
						<label class="form-control-label">
							<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.anonymizeinactivecustomers %>" />:
						</label>
					</div>
					<div class="col-sm-8">
						<asp:CheckBox ID="AnonymizeInactiveCustomers" runat="server" Checked="False" />
						<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.anonymizeinactivecustomers %>">
							<i class="fa fa-question-circle fa-lg"></i>
						</asp:Label>
					</div>
				</div>
			</asp:Panel>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.TuneIndexes %>" />:</label>
				</div>
				<div class="col-sm-8">
					<asp:CheckBox ID="TuneIndexes" runat="server" Checked="True" />
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgTuneIndexes %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.UpdateStatistics %>" />:</label>
				</div>
				<div class="col-sm-8">
					<asp:CheckBox ID="UpdateStatistics" runat="server" Checked="True" />
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgUpdateStats %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.CleanupLocalizationData %>" />:</label>
				</div>
				<div class="col-sm-8">
					<asp:CheckBox ID="CleanupLocalizationData" runat="server" Checked="True" />
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgLocalizationData %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.CleanupSecurityLog %>" />:</label>
				</div>
				<div class="col-sm-8">
					<asp:CheckBox ID="CleanupSecurityLog" runat="server" Checked="True" />
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgSecurityLog %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
			<div class="row form-group">
				<div class="col-sm-4">
					<label class="form-control-label">
						<asp:Literal runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.SaveSettings %>" />:</label>
				</div>
				<div class="col-sm-8">
					<asp:CheckBox ID="SaveSettings" runat="server" Checked="True" />
					<asp:Label runat="server" CssClass="hover-help" data-toggle="tooltip" ToolTip="<%$Tokens:StringResource, admin.databasemaintenance.tooltip.imgSaveSettings %>">
						<i class="fa fa-question-circle fa-lg"></i>
					</asp:Label>
				</div>
			</div>
		</div>
		<div class="list-action-bar">
			<asp:Button ID="btnRunMaintenanceBottom" runat="server" CssClass="btn btn-primary" Text="<%$Tokens:StringResource, admin.common.Go %>" OnClick="RunMaintenance_Click" />
		</div>
	</asp:Panel>

	<asp:UpdatePanel ID="pnlTimer" runat="server">
		<Triggers>
			<asp:AsyncPostBackTrigger ControlID="tmrMain" />
		</Triggers>
		<ContentTemplate>
			<asp:Timer ID="tmrMain" runat="server" OnTick="ShowTime_Tick" Interval='1000' Enabled="false" />

			<div runat="server" id="divRunning" class="dlgBox">
				<div style="background: transparent url(images/kit/variantListHeader_background.jpg) repeat scroll 0 0; padding: 10px;">
					<asp:Label ID="lblRunning" runat="server" Text="<%$Tokens:StringResource, admin.databasemaintenance.MaintenanceRunning %>" Font-Size="X-Large" ForeColor="LightGray" />
				</div>
				<div style="padding: 20px;">
					<asp:Label ID="lblStatus" runat="server" Visible="false" Font-Size="XX-Large" Text="<%$Tokens:StringResource, admin.databasemaintenance.Processing %>" /><br />
					<asp:Label ID="lblMessage" runat="server" Font-Size="Larger" Text="<%$Tokens:StringResource, admin.databasemaintenance.Finished %>" />
				</div>
			</div>
		</ContentTemplate>
	</asp:UpdatePanel>

</asp:Content>
