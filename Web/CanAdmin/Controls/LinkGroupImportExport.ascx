﻿<%@ Control Language="C#" AutoEventWireup="true" Inherits="AspDotNetStorefrontControls.LinkGroupImportExport" CodeBehind="LinkGroupImportExport.ascx.cs" %>
<%@ Register Src="LinkGroupLinks.ascx" TagPrefix="aspdnsf" TagName="LinkGroupLinks" %>

<aspdnsf:LinkGroupLinks runat="server" ID="LinkGroupLinks" SelectedLink="<%# SelectedLink %>" />
