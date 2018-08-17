// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web.Mvc;
using AspDotNetStorefront.Caching.ObjectCaching;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways;

namespace AspDotNetStorefront.Controllers
{
	[SecureAccessFilter(forceHttps: true)]
	public class CheckoutECheckController : Controller
	{
		readonly ICachedShoppingCartProvider CachedShoppingCartProvider;
		readonly ECheckAccountTypeProvider ECheckAccountTypeProvider;
		readonly NoticeProvider NoticeProvider;
		readonly IPaymentOptionProvider PaymentOptionProvider;
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;

		public CheckoutECheckController(
			ICachedShoppingCartProvider cachedShoppingCartProvider,
			ECheckAccountTypeProvider eCheckAccountTypeProvider,
			NoticeProvider noticeProvider,
			IPaymentOptionProvider paymentOptionProvider,
			IPersistedCheckoutContextProvider persistedCheckoutContextProvider)
		{
			CachedShoppingCartProvider = cachedShoppingCartProvider;
			ECheckAccountTypeProvider = eCheckAccountTypeProvider;
			NoticeProvider = noticeProvider;
			PaymentOptionProvider = paymentOptionProvider;
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
		}

		[PageTypeFilter(PageTypes.Checkout)]
		[HttpGet]
		[ImportModelStateFromTempData]
		public ActionResult ECheck()
		{
			var customer = HttpContext.GetCustomer();

			if(!PaymentOptionProvider.PaymentMethodSelectionIsValid(AppLogic.ro_PMECheck, customer))
			{
				NoticeProvider.PushNotice(
					message: AppLogic.GetString("checkout.paymentmethodnotallowed"),
					type: NoticeType.Failure);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			// AcceptJS is currently only way to use EChecks
			if(AppLogic.ActivePaymentGatewayCleaned() != Gateway.ro_GWACCEPTJS)
			{
				NoticeProvider.PushNotice(
					message: AppLogic.GetString("acceptjs.echeck.notconfigured"),
					type: NoticeType.Failure);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			var liveMode = AppLogic.AppConfigBool("UseLiveTransactions");

			var acceptJsModel = new AcceptJsViewModel(
				clientKey: liveMode
					? AppLogic.AppConfig("AcceptJs.Live.ClientKey")
					: AppLogic.AppConfig("AcceptJs.Test.ClientKey"),
				apiLoginId: liveMode
					? AppLogic.AppConfig("AcceptJs.Live.ApiLoginId")
					: AppLogic.AppConfig("AcceptJs.Test.ApiLoginId"),
				scriptUrlHostedForm: liveMode
					? AppLogic.AppConfig("AcceptJs.Form.Hosted.Live.Url")
					: AppLogic.AppConfig("AcceptJs.Form.Hosted.Test.Url"),
				scriptUrlOwnForm: liveMode
					? AppLogic.AppConfig("AcceptJs.Form.Own.Live.Url")
					: AppLogic.AppConfig("AcceptJs.Form.Own.Test.Url"));

			var acceptJsECheckModel = new AcceptJsEcheckViewModel(
				acceptJsViewModel: acceptJsModel,
				eCheckViewModel: new ECheckViewModel(),
				checkoutECheckViewModel: new CheckoutECheckViewModel(string.Empty, string.Empty),
				accountTypes: ECheckAccountTypeProvider.GetECheckAccountTypesSelectList());

			return View(ViewNames.AcceptJsECheck, acceptJsECheckModel);
		}

		public ActionResult ECheckDetail()
		{
			var customer = HttpContext.GetCustomer();

			var acceptJsECheckDetails = PersistedCheckoutContextProvider
				.LoadCheckoutContext(customer)
				.AcceptJsDetailsECheck;

			var model = acceptJsECheckDetails == null
				? new CheckoutECheckViewModel(string.Empty, string.Empty)
				: new CheckoutECheckViewModel(
					acceptJsECheckDetails.ECheckDisplayAccountNumberLastFour,
					acceptJsECheckDetails.ECheckDisplayAccountType);

			return PartialView(ViewNames.ECheckDetailPartial, model);
		}
	}
}
