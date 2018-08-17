// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Linq;
using System.Web.Mvc;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Controllers.Classes;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefront.Validation.AddressValidator;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers
{
	[SecureAccessFilter(forceHttps: true)]
	public class CheckoutAddressController : Controller
	{
		readonly NoticeProvider NoticeProvider;
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;
		readonly IAddressValidationProviderFactory AddressValidationProviderFactory;
		readonly AddressControllerHelper AddressControllerHelper;

		public CheckoutAddressController(
			NoticeProvider noticeProvider,
			IPersistedCheckoutContextProvider persistedCheckoutContextProvider,
			IAddressValidationProviderFactory addressValidationProviderFactory,
			AddressControllerHelper addressControllerHelper)
		{
			NoticeProvider = noticeProvider;
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
			AddressValidationProviderFactory = addressValidationProviderFactory;
			AddressControllerHelper = addressControllerHelper;
		}

		public ActionResult SelectAddress(AddressTypes addressType)
		{
			var customer = HttpContext.GetCustomer();
			var primaryAddressId = addressType == AddressTypes.Shipping
				? customer.PrimaryShippingAddressID
				: customer.PrimaryBillingAddressID;

			var checkoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);

			var pageTitle = string.Empty;

			if(!AppLogic.AppConfigBool("AllowShipToDifferentThanBillTo") || addressType == AddressTypes.Account)
				pageTitle = AppLogic.GetString("checkoutaddress.chooseabillingandshippingaddress", customer.LocaleSetting);
			else if(addressType == AddressTypes.Shipping)
				pageTitle = AppLogic.GetString("checkoutaddress.chooseashippingaddress", customer.LocaleSetting);
			else
				pageTitle = AppLogic.GetString("checkoutaddress.chooseabillingaddress", customer.LocaleSetting);

			var addresses = AddressControllerHelper.GetCustomerAddresses(customer);

			var model = new SelectAddressViewModel
			{
				SelectedAddressId = primaryAddressId,
				SelectedAddress = addresses
					.Where(address => address.Id == primaryAddressId)
					.FirstOrDefault(),
				AddressOptions = addresses,
				AddressType = addressType,
				PageTitle = pageTitle,
				AddressSelectionLocked = (addressType == AddressTypes.Billing && checkoutContext.OffsiteRequiredBillingAddressId.HasValue)
					|| (addressType == AddressTypes.Shipping && checkoutContext.OffsiteRequiredShippingAddressId.HasValue)
			};

			return PartialView(ViewNames.SelectAddressPartial, model);
		}

		[HttpPost]
		public ActionResult SelectAddress(SelectAddressViewModel model)
		{
			if(!(model.SelectedAddressId > 0))
			{
				NoticeProvider.PushNotice(AppLogic.GetString("validate.selectedaddress", HttpContext.GetCustomer().LocaleSetting), NoticeType.Failure);
				return RedirectToAction(
					ActionNames.SelectAddress,
					ControllerNames.CheckoutAddress);
			}

			return RedirectToAction(
				ActionNames.MakePrimaryAddress,
				ControllerNames.Address,
				new
				{
					addressId = model.SelectedAddressId,
					addressType = model.AddressType,
					returnUrl = Url.Action(ActionNames.Index, ControllerNames.Checkout)
				});
		}
	}
}
