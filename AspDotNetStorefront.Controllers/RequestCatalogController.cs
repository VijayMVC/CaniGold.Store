// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Text;
using System.Web.Mvc;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Classes;
using AspDotNetStorefront.Classes.Checkout;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontCore.Validation;

namespace AspDotNetStorefront.Controllers
{
	[SecureAccessFilter(forceHttps: true)]
	public class RequestCatalogController : Controller
	{
		readonly AddressHeaderProvider AddressHeaderProvider;
		readonly AddressSelectListBuilder AddressSelectListBuilder;
		readonly NoticeProvider NoticeProvider;
		readonly IPostalCodeLookupProvider PostalCodeLookupProvider;
		readonly AddressSettings AddressSettings;

		public RequestCatalogController(
			AddressHeaderProvider addressHeaderProvider,
			AddressSelectListBuilder addressSelectListBuilder,
			NoticeProvider noticeProvider,
			IPostalCodeLookupProvider postalCodeLookupProvider)
		{
			AddressHeaderProvider = addressHeaderProvider;
			AddressSelectListBuilder = addressSelectListBuilder;
			NoticeProvider = noticeProvider;
			PostalCodeLookupProvider = postalCodeLookupProvider;
			AddressSettings = new AddressSettings();
		}

		public ActionResult Index()
		{
			var customer = HttpContext.GetCustomer();

			var shippingAddress = TypeConversions.ConvertToAddressViewModel(
				customer.PrimaryShippingAddress,
				customer);

			var countries = AddressSelectListBuilder.BuildCountrySelectList(shippingAddress.Country);
			var states = AddressSelectListBuilder.BuildStateSelectList(countries.SelectedValue.ToString(), shippingAddress.State);

			return View(new AddressDetailViewModel(
				address: shippingAddress,
				residenceTypeOptions: AddressSelectListBuilder.BuildResidenceTypeSelectList(shippingAddress.ResidenceType.ToString()),
				stateOptions: states,
				countryOptions: countries,
				showCompanyField: AddressSettings.ShowCompanyField,
				showNickName: AddressSettings.ShowNickName,
				showSuite: AddressSettings.ShowSuite,
				showResidenceTypeField: true,
				showPostalCodeLookup: PostalCodeLookupProvider.IsEnabled(shippingAddress.Country),
				returnUrl: string.Empty,
				header: AddressHeaderProvider.GetHeaderText(shippingAddress.Id, AddressTypes.Shipping)));
		}

		[HttpPost]
		public ActionResult Index(AddressPostViewModel model)
		{
			var customer = HttpContext.GetCustomer();

			if(ModelState.IsValid)
			{
				SendCatalogEmailTo(model.Address, customer);

				return RedirectToAction(ActionNames.Detail, ControllerNames.Topic, new { name = "RequestCatalogSuccessful" });
			}

			return View(new AddressDetailViewModel(
				address: model.Address,
				residenceTypeOptions: AddressSelectListBuilder.BuildResidenceTypeSelectList(model.Address.ResidenceType.ToString()),
				stateOptions: AddressSelectListBuilder.BuildStateSelectList(model.Address.Country, model.Address.State),
				countryOptions: AddressSelectListBuilder.BuildCountrySelectList(model.Address.Country),
				showCompanyField: AddressSettings.ShowCompanyField,
				showNickName: AddressSettings.ShowNickName,
				showSuite: AddressSettings.ShowSuite,
				showResidenceTypeField: true,
				showPostalCodeLookup: PostalCodeLookupProvider.IsEnabled(model.Address.Country),
				returnUrl: string.Empty,
				header: AddressHeaderProvider.GetHeaderText(model.Address.Id, AddressTypes.Shipping)));
		}

		public ActionResult ChangeCountry(AddressPostViewModel model)
		{
			var customer = HttpContext.GetCustomer();

			var countries = AddressSelectListBuilder.BuildCountrySelectList(model.Address.Country);
			var states = AddressSelectListBuilder.BuildStateSelectList(countries.SelectedValue.ToString(), model.Address.State);

			return View(ActionNames.Index, new AddressDetailViewModel(
				address: model.Address,
				residenceTypeOptions: AddressSelectListBuilder.BuildResidenceTypeSelectList(model.Address.ResidenceType.ToString()),
				stateOptions: states,
				countryOptions: countries,
				showCompanyField: AddressSettings.ShowCompanyField,
				showNickName: AddressSettings.ShowNickName,
				showSuite: AddressSettings.ShowSuite,
				showResidenceTypeField: true,
				showPostalCodeLookup: PostalCodeLookupProvider.IsEnabled(model.Address.Country),
				returnUrl: string.Empty,
				header: AddressHeaderProvider.GetHeaderText(model.Address.Id, AddressTypes.Shipping)));
		}

		void SendCatalogEmailTo(AddressViewModel address, Customer customer)
		{
			var body = new StringBuilder();
			body.AppendLine(AppLogic.GetString("requestcatalog.aspx.2", customer.LocaleSetting));
			body.AppendLine(string.Format("Customer Name: {0}", address.Name));
			body.AppendLine(string.Format("Company: {0}", address.Company));
			body.AppendLine(string.Format("Residence Type: {0}", address.ResidenceType));
			body.AppendLine(string.Format("Address1: {0}", address.Address1));
			body.AppendLine(string.Format("Address2: {0}", address.Address2));
			body.AppendLine(string.Format("Suite: {0}", address.Suite));
			body.AppendLine(string.Format("City: {0}", address.City));
			body.AppendLine(string.Format("State: {0}", address.State));
			body.AppendLine(string.Format("Country: {0}", address.Country));
			body.AppendLine(string.Format("ZIP: {0}", address.Zip));

			AppLogic.SendMail(
				subject: AppLogic.GetString("requestcatalog.aspx.3", customer.LocaleSetting),
				body: body.ToString(),
				useHtml: false,
				fromAddress: AppLogic.AppConfig("MailMe_FromAddress"),
				fromName: AppLogic.AppConfig("MailMe_FromName"),
				toAddress: AppLogic.AppConfig("MailMe_ToAddress"),
				toName: AppLogic.AppConfig("MailMe_ToName"),
				bccAddresses: string.Empty,
				server: AppLogic.MailServer());
		}
	}
}
