// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.Auth;
using AspDotNetStorefront.Classes;
using AspDotNetStorefront.Controllers.Classes;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefront.StringResource;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontCore.DataRetention;

namespace AspDotNetStorefront.Controllers
{
	[SecureAccessFilter(forceHttps: true)]
	public class AccountController : Controller
	{
		readonly AccountSettings Settings;
		readonly AccountControllerHelper ControllerHelper;
		readonly NoticeProvider NoticeProvider;
		readonly IClaimsIdentityProvider ClaimsIdentityProvider;
		readonly SendWelcomeEmailProvider SendWelcomeEmailProvider;
		readonly AppConfigProvider AppConfigProvider;
		readonly CartActionProvider CartActionProvider;
		readonly IDataRetentionService DataRetentionService;
		readonly IStringResourceProvider StringResourceProvider;
		readonly CaptchaVerificationProvider CaptchaVerificationProvider;
		readonly CaptchaSettings CaptchaSettings;

		public AccountController(NoticeProvider noticeProvider,
			IClaimsIdentityProvider claimsIdentityProvider,
			SendWelcomeEmailProvider sendWelcomeEmailProvider,
			AppConfigProvider appConfigProvider,
			CartActionProvider cartActionProvider,
			IDataRetentionService dataRetentionService,
			IStringResourceProvider stringResourceProvider,
			CaptchaVerificationProvider captchaVerificationProvider)
		{
			Settings = new AccountSettings();
			CaptchaSettings = new CaptchaSettings();
			ControllerHelper = new AccountControllerHelper(Settings);
			NoticeProvider = noticeProvider;
			ClaimsIdentityProvider = claimsIdentityProvider;
			SendWelcomeEmailProvider = sendWelcomeEmailProvider;
			AppConfigProvider = appConfigProvider;
			CartActionProvider = cartActionProvider;
			DataRetentionService = dataRetentionService;
			StringResourceProvider = stringResourceProvider;
			CaptchaVerificationProvider = captchaVerificationProvider;
		}

		[HttpGet]
		[RequireCustomerRegistrationFilter]
		[ImportModelStateFromTempData]
		public ActionResult Index()
		{
			var customer = HttpContext.GetCustomer();

			var account = new AccountViewModel
			{
				FirstName = customer.FirstName,
				LastName = customer.LastName,
				Email = customer.EMail,
				EmailConfirmation = customer.EMail,
				Phone = customer.Phone,
				IsOkToEmail = customer.OKToEMail,
				IsOver13 = customer.IsOver13,
				VatRegistrationId = customer.VATRegistrationID,
				SaveCreditCardNumber = customer.StoreCCInDB
			};

			var model = ControllerHelper.BuildAccountIndexViewModel(account, customer, Url);

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[RequireCustomerRegistrationFilter]
		[ExportModelStateToTempData]
		public ActionResult Index(AccountPostViewModel model)
		{
			var customer = HttpContext.GetCustomer();

			if(!ModelState.IsValid)
				return RedirectToAction(ActionNames.Index);

			if(!Customer.NewEmailPassesDuplicationRules(model.Account.Email, customer.CustomerID))
			{
				ModelState.AddModelError("Account.Email", StringResourceProvider.GetString("createaccount_process.aspx.1"));
				return RedirectToAction(ActionNames.Index);
			}

			// The account editor only updates the password if one was specified or if the customer has not yet registered.
			if(!customer.IsRegistered || !string.IsNullOrEmpty(model.Account.Password))
			{
				switch(ControllerHelper.ValidateAccountPassword(customer, model.Account.Password, model.Account.PasswordConfirmation))
				{
					case AccountControllerHelper.PasswordValidationResult.DoesNotMatch:
						ModelState.AddModelError("Account.PasswordConfirmation", StringResourceProvider.GetString("account.aspx.68"));
						return RedirectToAction(ActionNames.Index);

					case AccountControllerHelper.PasswordValidationResult.NotStrong:
						ModelState.AddModelError("Account.Password", StringResourceProvider.GetString("account.aspx.69"));
						return RedirectToAction(ActionNames.Index);

					case AccountControllerHelper.PasswordValidationResult.SameAsCurrent:
						ModelState.AddModelError("Account.Password", StringResourceProvider.GetString("signin.aspx.30"));
						return RedirectToAction(ActionNames.Index);

					case AccountControllerHelper.PasswordValidationResult.SameAsPrevious:
						ModelState.AddModelError("Account.Password", string.Format(StringResourceProvider.GetString("signin.aspx.31"), Settings.NumberOfPreviouslyUsedPasswords));
						return RedirectToAction(ActionNames.Index);

					default:
					case AccountControllerHelper.PasswordValidationResult.Ok:
						break;
				}
			}

			var vatRegistationValidationResult = ControllerHelper.ValidateVatRegistrationId(model.Account, customer);
			if(!vatRegistationValidationResult.Ok)
			{
				NoticeProvider.PushNotice(
					StringResourceProvider.GetString(
						vatRegistationValidationResult.Message
						?? "account.aspx.91"),
					NoticeType.Failure);

				return RedirectToAction(ActionNames.Index);
			}

			ControllerHelper.UpdateAccount(model.Account, customer);
			NoticeProvider.PushNotice(StringResourceProvider.GetString("account.aspx.2"), NoticeType.Success);
			return RedirectToAction(ActionNames.Index);
		}

		[HttpGet]
		[ImportModelStateFromTempData]
		public ActionResult Create()
		{
			var customer = HttpContext.GetCustomer();

			// We will allow registered customers to create new accounts if they end up on the page but we won't
			// prepopulate and fields so its clear they're creating a new account. Otherwise, we'll try and fill in
			// whatever fields we might have fromt he current customer record.
			var account = !customer.IsRegistered
				? new AccountCreateViewModel
				{
					FirstName = customer.FirstName,
					LastName = customer.LastName,
					Email = customer.EMail,
					Phone = customer.Phone,
					IsOkToEmail = customer.OKToEMail,
					IsOver13 = customer.IsOver13,
					VatRegistrationId = customer.VATRegistrationID,
					SaveCreditCardNumber = customer.StoreCCInDB
				}
				: new AccountCreateViewModel();

			return View(new AccountCreateIndexViewModel(
				displayCaptcha: CaptchaSettings.CaptchaIsConfigured() && CaptchaSettings.RequireCaptchaOnCreateAccount,
				requireEmailConfirmation: AppConfigProvider.GetAppConfigValue<bool>("RequireEmailConfirmation"),
				displayOver13Selector: AppConfigProvider.GetAppConfigValue<bool>("RequireOver13Checked"))
			{
				Account = account,
				PrimaryBillingAddress = new AccountAddressViewModel(),
				PrimaryShippingAddress = new AccountAddressViewModel()
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[ExportModelStateToTempData]
		public ActionResult Create(AccountCreatePostModel model)
		{
			var customer = HttpContext.GetCustomer();

			if(!ModelState.IsValid)
				return RedirectToAction(ActionNames.Create);

			if(!Customer.NewEmailPassesDuplicationRules(model.Account.Email, customer.CustomerID))
			{
				ModelState.AddModelError(
					key: "Account.Email",
					errorMessage: StringResourceProvider.GetString("createaccount_process.aspx.1"));
				return RedirectToAction(ActionNames.Create);
			}

			switch(ControllerHelper.ValidateAccountPassword(customer, model.Account.Password, model.Account.PasswordConfirmation))
			{
				case AccountControllerHelper.PasswordValidationResult.DoesNotMatch:
					ModelState.AddModelError(
						key: "Account.PasswordConfirmation",
						errorMessage: StringResourceProvider.GetString("account.aspx.68"));
					return RedirectToAction(ActionNames.Create);

				case AccountControllerHelper.PasswordValidationResult.DoesNotMeetMinimum:
					ModelState.AddModelError(
						key: "Account.Password",
						errorMessage: StringResourceProvider.GetString("signin.newpassword.normalRegexFailure"));
					return RedirectToAction(ActionNames.Create);

				case AccountControllerHelper.PasswordValidationResult.NotStrong:
					ModelState.AddModelError(
						key: "Account.Password",
						errorMessage: StringResourceProvider.GetString("account.aspx.69"));
					return RedirectToAction(ActionNames.Create);
			}

			if(CaptchaSettings.CaptchaIsConfigured()
				&& CaptchaSettings.RequireCaptchaOnCreateAccount)
			{
				var captchaResult = CaptchaVerificationProvider.ValidateCaptchaResponse(Request.Form[CaptchaVerificationProvider.RecaptchaFormKey], customer.LastIPAddress);

				if(!captchaResult.Success)
				{
					NoticeProvider.PushNotice(captchaResult.Error.Message, NoticeType.Failure);

					// This error isn't actually displayed; it is just used to trigger the persisting of form data for the next page load
					ModelState.AddModelError(
						key: CaptchaVerificationProvider.RecaptchaFormKey,
						errorMessage: "Captcha Failed");

					return RedirectToAction(ActionNames.Create);
				}
			}

			var registeredCustomer = ControllerHelper.CreateAccount(model.Account, customer);

			ControllerHelper.Login(
				signedInCustomer: registeredCustomer,
				profile: HttpContext.Profile,
				username: model.Account.Email,
				password: model.Account.Password,
				skinId: registeredCustomer.SkinID,
				registering: true);

			Request
				.GetOwinContext()
				.Authentication
				.SignOut();

			Request
				.GetOwinContext()
				.Authentication
				.SignIn(
					properties: new Microsoft.Owin.Security.AuthenticationProperties
					{
						IsPersistent = true
					},
					identities: ClaimsIdentityProvider.Create(registeredCustomer));

			if(AppConfigProvider.GetAppConfigValue<bool>("SendWelcomeEmail"))
				SendWelcomeEmailProvider.SendWelcomeEmail(registeredCustomer);

			NoticeProvider.PushNotice(StringResourceProvider.GetString("createaccount.aspx.86"), NoticeType.Success);
			return RedirectToAction(ActionNames.Index);
		}

		[AllowInMaintenanceMode]
		[PageTypeFilter(PageTypes.Signin)]
		public ActionResult SignIn(int? errorMessage = null, string returnUrl = null)
		{
			var queryStringErrorMessage = ControllerHelper.GetQueryStringErrorMessage(errorMessage);
			if(!string.IsNullOrEmpty(queryStringErrorMessage))
				NoticeProvider.PushNotice(queryStringErrorMessage, NoticeType.Failure);

			return View(new AccountSignInViewModel(
				returnUrl: returnUrl,
				displayCaptcha: CaptchaSettings.CaptchaIsConfigured() && CaptchaSettings.RequireCaptchaOnLogin,
				passwordResetAvailable: ControllerHelper.IsPasswordResetAvailable()));
		}

		[HttpPost]
		[AllowInMaintenanceMode]
		[PageTypeFilter(PageTypes.Signin)]
		public ActionResult SignIn(AccountSignInViewModel model, string returnUrl = null, int? errorMessage = null)
		{
			if(!ModelState.IsValid)
			{
				return View(new AccountSignInViewModel(
					source: model,
					returnUrl: returnUrl,
					displayCaptcha: CaptchaSettings.CaptchaIsConfigured() && CaptchaSettings.RequireCaptchaOnLogin,
					passwordResetAvailable: ControllerHelper.IsPasswordResetAvailable()));
			}

			var signedInCustomer = HttpContext.GetCustomer();

			if(CaptchaSettings.CaptchaIsConfigured()
				&& CaptchaSettings.RequireCaptchaOnLogin)
			{
				var captchaResult = CaptchaVerificationProvider.ValidateCaptchaResponse(Request.Form[CaptchaVerificationProvider.RecaptchaFormKey], signedInCustomer.LastIPAddress);

				if(!captchaResult.Success)
				{
					NoticeProvider.PushNotice(captchaResult.Error.Message, NoticeType.Failure);

					return View(new AccountSignInViewModel(
						source: model,
						returnUrl: returnUrl,
						displayCaptcha: CaptchaSettings.CaptchaIsConfigured() && CaptchaSettings.RequireCaptchaOnLogin,
						passwordResetAvailable: ControllerHelper.IsPasswordResetAvailable()));
				}
			}

			// Login
			var result = ControllerHelper.Login(
				signedInCustomer: signedInCustomer,
				profile: HttpContext.Profile,
				username: model.Email,
				password: model.Password,
				skinId: signedInCustomer.SkinID);

			if(result.State == AccountControllerHelper.ResultState.Error)
			{
				NoticeProvider.PushNotice(result.Message, NoticeType.Failure);

				return View(new AccountSignInViewModel(
					source: model,
					returnUrl: returnUrl,
					displayCaptcha: CaptchaSettings.CaptchaIsConfigured() && CaptchaSettings.RequireCaptchaOnLogin,
					passwordResetAvailable: ControllerHelper.IsPasswordResetAvailable()));
			}
			else if(result.State == AccountControllerHelper.ResultState.PasswordChangeRequired)
			{
				NoticeProvider.PushNotice(result.Message, NoticeType.Info);
				return RedirectToAction(
					actionName: ActionNames.ChangePassword,
					routeValues: new
					{
						email = model.Email,
						returnUrl = returnUrl
					});
			}

			var targetCustomer = new Customer(model.Email);
			var identity = ClaimsIdentityProvider.Create(targetCustomer);

			Request
				.GetOwinContext()
				.Authentication
				.SignIn(
					properties: new Microsoft.Owin.Security.AuthenticationProperties
					{
						IsPersistent = model.PersistLogin
					},
					identities: identity);

			if(!string.IsNullOrEmpty(result.Message))
				NoticeProvider.PushNotice(result.Message, NoticeType.Info);

			// Consolidate any shopping cart items
			CartActionProvider.ConsolidateCartItems(targetCustomer, CartTypeEnum.ShoppingCart);
			CartActionProvider.ConsolidateCartItems(targetCustomer, CartTypeEnum.WishCart);

			var safeReturnUrl = Url.MakeSafeReturnUrl(returnUrl);

			return Redirect(safeReturnUrl);
		}

		public ActionResult ChangePassword(string email = null, string returnUrl = null, int? errorMessage = null)
		{
			var queryStringErrorMessage = ControllerHelper.GetQueryStringErrorMessage(errorMessage);
			if(!string.IsNullOrEmpty(queryStringErrorMessage))
				NoticeProvider.PushNotice(queryStringErrorMessage, NoticeType.Failure);

			return View(new AccountChangePasswordViewModel(
				returnUrl: returnUrl,
				passwordResetAvailable: ControllerHelper.IsPasswordResetAvailable())
			{
				Email = email
			});
		}

		[HttpPost, ValidateAntiForgeryToken]
		public ActionResult ChangePassword(AccountChangePasswordViewModel model, string returnUrl = null)
		{
			if(!ModelState.IsValid)
				return View(new AccountChangePasswordViewModel(
					source: model,
					returnUrl: returnUrl,
					passwordResetAvailable: ControllerHelper.IsPasswordResetAvailable()));

			var signedInCustomer = HttpContext.GetCustomer();

			var result = ControllerHelper.ChangePassword(
				signedInCustomer: signedInCustomer,
				username: model.Email,
				oldPassword: model.OldPassword,
				newPassword: model.NewPassword,
				newPasswordConfirmation: model.NewPassword,
				skinId: signedInCustomer.SkinID);

			if(result.State == AccountControllerHelper.ResultState.Error)
			{
				NoticeProvider.PushNotice(result.Message, NoticeType.Failure);
				return View(new AccountChangePasswordViewModel(
					source: model,
					returnUrl: returnUrl,
					passwordResetAvailable: ControllerHelper.IsPasswordResetAvailable()));
			}

			var targetCustomer = new Customer(model.Email);
			var identity = ClaimsIdentityProvider.Create(targetCustomer);

			Request
				.GetOwinContext()
				.Authentication
				.SignIn(identity);

			if(!string.IsNullOrEmpty(result.Message))
				NoticeProvider.PushNotice(result.Message, NoticeType.Info);

			var safeReturnUrl = Url.MakeSafeReturnUrl(returnUrl);

			return Redirect(safeReturnUrl);
		}

		public ActionResult SignOut()
		{
			var signedInCustomer = HttpContext.GetCustomer();
			if(signedInCustomer.IsAdminUser)
				Security.LogEvent("Store Logout Success", "", signedInCustomer.CustomerID, signedInCustomer.CustomerID, signedInCustomer.CurrentSessionID);

			signedInCustomer.Logout();

			Request
				.GetOwinContext()
				.Authentication
				.SignOut(AuthValues.CookiesAuthenticationType);

			return RedirectToAction(ActionNames.Index, ControllerNames.Home);
		}

		[HttpPost, ValidateAntiForgeryToken]
		public ActionResult ResetPassword(AccountResetPasswordViewModel model, string returnUrl = null)
		{
			if(!ModelState.IsValid)
				return View(
					viewName: "signIn",
					model: new AccountSignInViewModel(
						returnUrl: returnUrl,
						displayCaptcha: CaptchaSettings.CaptchaIsConfigured() && CaptchaSettings.RequireCaptchaOnLogin,
						passwordResetAvailable: ControllerHelper.IsPasswordResetAvailable()));

			var signedInCustomer = HttpContext.GetCustomer();

			var result = ControllerHelper.RequestNewPassword(
				signedInCustomer: signedInCustomer,
				email: model.Email,
				skinId: signedInCustomer.SkinID);

			if(result.State == AccountControllerHelper.ResultState.Error)
				NoticeProvider.PushNotice(result.Message, NoticeType.Failure);
			else
				NoticeProvider.PushNotice(result.Message, NoticeType.Success);

			return View(
				viewName: "signIn",
				model: new AccountSignInViewModel(
					returnUrl: returnUrl,
						displayCaptcha: CaptchaSettings.CaptchaIsConfigured() && CaptchaSettings.RequireCaptchaOnLogin,
						passwordResetAvailable: ControllerHelper.IsPasswordResetAvailable()));
		}

		[HttpGet]
		[RequireCustomerRegistrationFilter]
		public ActionResult Remove()
		{
			if (!AppConfigProvider.GetAppConfigValue<bool>("DataRetentionPolicies.Enabled"))
				return RedirectToAction(ActionNames.Index, ControllerNames.Account);

			var customer = HttpContext.GetCustomer();

			if(DataRetentionService.PendingRemoveAccountRequest(customer))
				return RedirectToAction(
					ActionNames.Detail,
					ControllerNames.Topic,
					new { @name = "removeaccountconfirmation" });

			var explanationText = new Topic("removeaccount").Contents;

			return View(new AccountRemovalViewModel(explanationText));
		}

		[HttpPost, ValidateAntiForgeryToken]
		public ActionResult Remove(bool removeConfirmation)
		{
			if(!AppConfigProvider.GetAppConfigValue<bool>("DataRetentionPolicies.Enabled"))
				return RedirectToAction(ActionNames.Index, ControllerNames.Account);

			if(!removeConfirmation)
			{
				NoticeProvider.PushNotice(
					StringResourceProvider.GetString("remove.account.confirmationneeded"),
					NoticeType.Failure);

				return RedirectToAction(ActionNames.Remove, ControllerNames.Account);
			}

			var customer = HttpContext.GetCustomer();
			if(customer.IsAdminUser || customer.IsAdminSuperUser) 
			{
				NoticeProvider.PushNotice(
					StringResourceProvider.GetString("remove.account.cannotremoveadmin"),
					NoticeType.Failure);

				return RedirectToAction(ActionNames.Index, ControllerNames.Account);
			}

			DataRetentionService.CreateRemoveAccountRequest(customer);
			DataRetentionService.SendRemoveAccountAcknowledgement(
				customer,
				Settings.StoreName,
				Settings.MailFromAddress);

			return RedirectToAction(
				ActionNames.Detail,
				ControllerNames.Topic,
				new { @name = "removeaccountconfirmation" });
		}

		[Authorize]
		public ActionResult Reorder(int orderId)
		{
			var customer = HttpContext.GetCustomer();

			var order = new Order(orderId);
			if(order == null)
				return HttpNotFound();

			if(!customer.IsAdminUser && customer.CustomerID != order.CustomerID)
				return HttpNotFound();

			string result;
			if(!Order.BuildReOrder(null, customer, order.OrderNumber, out result))
			{
				NoticeProvider.PushNotice(result, NoticeType.Failure);
				return RedirectToAction(ActionNames.Index);
			}

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}
	}
}
