// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using AspDotNetStorefront;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefrontAdmin
{
	public partial class About : AspDotNetStorefront.Admin.AdminPageBase
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			ltDateTime.Text = Localization.ToNativeDateTimeString(System.DateTime.Now);
			ltExecutionMode.Text = CommonLogic.IIF(IntPtr.Size == 8, "64 Bit", "32 Bit");

			if(!IsPostBack)
			{
				LoadSystemInformation();
				LoadLicensing();
				LoadLocalizationInformation();
				LoadGatewayInformation();
				LoadShippingInformation();
			}

			// perform security audit on this website, and present the merchant with a list of issues needing attention before the site goes live
			if(!AppLogic.AppConfigBool("SkipSecurityAudit") && ThisCustomer.IsAdminSuperUser)
			{
				try
				{
					DisplaySecurityAudit();
				}
				catch { }
			}
			else
			{
				pnlSecurity.Visible = false;
			}
		}

		protected void LoadSystemInformation()
		{
			ltStoreVersion.Text = CommonLogic.GetVersion(maxVersionDigitsToTake: 3);
			ltOnLiveServer.Text = AppLogic.OnLiveServer().ToString();
			ltUseSSL.Text = AppLogic.UseSSL().ToString();
			ltServerPortSecure.Text = CommonLogic.IsSecureConnection().ToString();
			ltAdminDirChanged.Text = CommonLogic.IIF(AppLogic.AppConfig("AdminDir").ToLowerInvariant() == "admin", AppLogic.GetString("admin.common.No", SkinID, LocaleSetting), AppLogic.GetString("admin.common.Yes", SkinID, LocaleSetting));
			ltCaching.Text = (AppLogic.AppConfigBool("CacheMenus") ? AppLogic.GetString("admin.common.OnUC", SkinID, LocaleSetting) : AppLogic.GetString("admin.common.OffUC", SkinID, LocaleSetting));
			ltTrustLevel.Text = AppLogic.TrustLevel.ToString();
		}

		protected void LoadLicensing()
		{
			rptDomains.DataSource = LicenseController.Current.GetLicensedDomains();
			rptDomains.DataBind();
		}

		protected void LoadLocalizationInformation()
		{
			ltWebConfigLocaleSetting.Text = Localization.GetDefaultLocale();
			ltSQLLocaleSetting.Text = Localization.GetSqlServerLocale();
			ltCustomerLocaleSetting.Text = LocaleSetting;
			ltLocalizationCurrencyCode.Text = AppLogic.AppConfig("Localization.StoreCurrency");
			ltLocalizationCurrencyNumericCode.Text = AppLogic.AppConfig("Localization.StoreCurrencyNumericCode");
		}

		protected void LoadGatewayInformation()
		{
			ltPaymentGateway.Text = AppLogic.ActivePaymentGatewayRAW();
			ltUseLiveTransactions.Text = (AppLogic.AppConfigBool("UseLiveTransactions") ? AppLogic.GetString("admin.splash.aspx.20", SkinID, LocaleSetting) : AppLogic.GetString("admin.splash.aspx.21", SkinID, LocaleSetting));
			ltTransactionMode.Text = AppLogic.AppConfig("TransactionMode").ToString();
			ltPaymentMethods.Text = AppLogic.AppConfig("PaymentMethods").ToString();
			ltPrimaryCurrency.Text = Localization.GetPrimaryCurrency();
		}

		protected void LoadShippingInformation()
		{
			ltShippingCalculation.Text = DB.GetSqlS("select Name as S from dbo.ShippingCalculation where Selected=1");
			ltOriginState.Text = AppLogic.AppConfig("RTShipping.OriginState");
			ltOriginZip.Text = AppLogic.AppConfig("RTShipping.OriginZip");
			ltOriginCountry.Text = AppLogic.AppConfig("RTShipping.OriginCountry");
			ltFreeShippingThreshold.Text = AppLogic.AppConfigNativeDecimal("FreeShippingThreshold").ToString();
			ltFreeShippingMethods.Text = AppLogic.AppConfig("ShippingMethodIDIfFreeShippingIsOn");
			ltFreeShippingRateSelection.Text = CommonLogic.IIF(AppLogic.AppConfigBool("FreeShippingAllowsRateSelection"), "On", "Off");
		}

		/// <summary>
		/// Displays Security Audit Messages
		/// </summary>
		void DisplaySecurityAudit()
		{
			var list = Security.GetAuditIssues(new HttpRequestWrapper(Request));
			foreach(var item in list)
			{
				var row = new TableRow();
				var textCell = new TableCell();
				var cellText = new Literal();
				cellText.Text = string.Format(
					"<div class=\"row admin-row\"><div class=\"col-sm-1\"><i class=\"fa {0}\"></i></div><div class=\"col-sm-11\">{1}</div></div>",
					item.ItemType == SecurityAuditItemType.Security
						? "fa-lock"
						: "fa-cogs",
					item.Message);
				textCell.Controls.Add(cellText);
				textCell.CssClass = "splash_SecurityAudit";
				row.Controls.Add(textCell);
				tblSecurityAudit.Controls.Add(row);
			}

			if(list.Any())
			{
				pnlSecurity.Visible = true;
			}
			else
			{
				pnlSecurity.Visible = false;
			}
		}
	}
}
