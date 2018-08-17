// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web;
using System.Web.Mvc;
using AspDotNetStorefront.Auth;
using AspDotNetStorefront.Checkout;
using AspDotNetStorefront.Classes;
using AspDotNetStorefront.Controllers.Classes;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers
{
	[SecureAccessFilter(forceHttps: true)]
	public class CheckoutAccountController : Controller
	{
		readonly AccountControllerHelper AccountControllerHelper;
		readonly AppConfigProvider AppConfigProvider;
		readonly ICheckoutAccountStatusProvider CheckoutAccountStatusProvider;
		readonly IClaimsIdentityProvider ClaimsIdentityProvider;
		readonly NoticeProvider NoticeProvider;
		readonly IPersistedCheckoutContextProvider PersistedCheckoutContextProvider;
		readonly SendWelcomeEmailProvider SendWelcomeEmailProvider;
		readonly CartActionProvider CartActionProvider;
		readonly CaptchaVerificationProvider CaptchaVerificationProvider;
		readonly CaptchaSettings CaptchaSettings;

		public CheckoutAccountController(
			AccountControllerHelper accountControllerHelper,
			AppConfigProvider appConfigProvider,
			ICheckoutAccountStatusProvider checkoutAccountStatusProvider,
			IClaimsIdentityProvider claimsIdentityProvider,
			NoticeProvider noticeProvider,
			IPersistedCheckoutContextProvider persistedCheckoutContextProvider,
			SendWelcomeEmailProvider sendWelcomeEmailProvider,
			CartActionProvider cartActionProvider,
			CaptchaVerificationProvider captchaVerificationProvider)
		{
			AccountControllerHelper = accountControllerHelper;
			AppConfigProvider = appConfigProvider;
			CheckoutAccountStatusProvider = checkoutAccountStatusProvider;
			ClaimsIdentityProvider = claimsIdentityProvider;
			NoticeProvider = noticeProvider;
			PersistedCheckoutContextProvider = persistedCheckoutContextProvider;
			SendWelcomeEmailProvider = sendWelcomeEmailProvider;
			CartActionProvider = cartActionProvider;
			CaptchaVerificationProvider = captchaVerificationProvider;
			CaptchaSettings = new CaptchaSettings();
		}

		[HttpGet, ImportModelStateFromTempData]
		public ActionResult Account()
		{
			var customer = HttpContext.GetCustomer();
			var checkoutContext = PersistedCheckoutContextProvider.LoadCheckoutContext(customer);

			var email = customer.IsRegistered
				? customer.EMail
				: checkoutContext.Email ?? string.Empty;

			var checkoutAccountStatus = CheckoutAccountStatusProvider.GetCheckoutAccountStatus(customer, email);

			var model = new CheckoutAccountViewModel(
				passwordRequired: checkoutAccountStatus.RequireRegisteredCustomer,
				showCaptcha: CaptchaSettings.CaptchaIsConfigured() && CaptchaSettings.RequireCaptchaOnCheckout,
				passwordResetAvailable: !string.IsNullOrEmpty(AppLogic.MailServer()) && AppLogic.MailServer() != AppLogic.ro_TBD)
			{
				Email = checkoutAccountStatus.Email
			};

			if(checkoutAccountStatus.State == CheckoutAccountState.Registered)
				return PartialView(ViewNames.AccountRegisteredPartial, model);

			switch(checkoutAccountStatus.NextAction)
			{
				case CheckoutAccountAction.CanLogin:
					return PartialView(ViewNames.AccountLoginPartial, model);

				case CheckoutAccountAction.CanCreateAccount:
					return PartialView(ViewNames.AccountCreateAccountPartial, model);

				default:
					// In all other cases, the user is allowed to change their email at-will with no password prompt
					return PartialView(ViewNames.AccountCollectEmailPartial, model);
			}
		}

		[HttpPost, ExportModelStateToTempData]
		public ActionResult SetEmail(CheckoutAccountPostModel model)
		{
			var customer = HttpContext.GetCustomer();

			// Don't set the email if they are logged in.
			if(customer.IsRegistered)
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);

			var result = ValidateEmail(model.Email);
			if(result.State == ResultState.Error)
				ModelState.AddModelError("Email", AppLogic.GetString("account.emailaddress.invalid"));

			SaveEmail(model.Email, customer);

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}

		[HttpPost, ExportModelStateToTempData, ValidateAntiForgeryToken]
		public ActionResult SignIn(CheckoutAccountPostModel model)
		{
			var signedInCustomer = HttpContext.GetCustomer();

			// Don't let them sign in if they are logged in.
			if(signedInCustomer.IsRegistered)
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);

			if(string.IsNullOrWhiteSpace(model.Password))
			{
				ModelState.AddModelError("Password", AppLogic.GetString("checkout.passwordrequired"));
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			if(CaptchaSettings.CaptchaIsConfigured()
				&& CaptchaSettings.RequireCaptchaOnCheckout)
			{
				var captchaResult = CaptchaVerificationProvider.ValidateCaptchaResponse(Request.Form[CaptchaVerificationProvider.RecaptchaFormKey], signedInCustomer.LastIPAddress);

				if(!captchaResult.Success)
				{
					NoticeProvider.PushNotice(captchaResult.Error.Message, NoticeType.Failure);

					return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
				}
			}

			// Validate the email
			var emailResult = ValidateEmail(model.Email);
			if(emailResult.State == ResultState.Error)
			{
				ModelState.AddModelError("Email", emailResult.Message);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			// Login
			var result = AccountControllerHelper.Login(
				signedInCustomer: signedInCustomer,
				profile: HttpContext.Profile,
				username: model.Email,
				password: model.Password,
				skinId: signedInCustomer.SkinID);

			if(result.State == AccountControllerHelper.ResultState.Error)
			{
				ModelState.AddModelError("Password", result.Message);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}
			else if(result.State == AccountControllerHelper.ResultState.PasswordChangeRequired)
			{
				NoticeProvider.PushNotice(result.Message, NoticeType.Info);
				return RedirectToAction(
					actionName: ActionNames.ChangePassword,
					controllerName: ControllerNames.Account,
					routeValues: new
					{
						email = model.Email,
						returnUrl = Url.Action(ActionNames.Index, ControllerNames.Checkout),
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
						IsPersistent = false
					},
					identities: identity);

			if(!string.IsNullOrEmpty(result.Message))
				NoticeProvider.PushNotice(result.Message, NoticeType.Info);

			// Consolidate any shopping cart items
			CartActionProvider.ConsolidateCartItems(targetCustomer, CartTypeEnum.ShoppingCart);
			CartActionProvider.ConsolidateCartItems(targetCustomer, CartTypeEnum.WishCart);

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}

		[HttpPost, ExportModelStateToTempData, ValidateAntiForgeryToken]
		public ActionResult CreateAccount(CheckoutAccountPostModel model)
		{
			var customer = HttpContext.GetCustomer();

			// Don't create an account if they are logged in.
			if(customer.IsRegistered)
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);

			if(CaptchaSettings.CaptchaIsConfigured()
				&& CaptchaSettings.RequireCaptchaOnCheckout)
			{
				var captchaResult = CaptchaVerificationProvider.ValidateCaptchaResponse(Request.Form[CaptchaVerificationProvider.RecaptchaFormKey], customer.LastIPAddress);

				if(!captchaResult.Success)
				{
					NoticeProvider.PushNotice(captchaResult.Error.Message, NoticeType.Failure);

					return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
				}
			}

			// Validate the email
			var emailResult = ValidateEmail(model.Email);
			if(emailResult.State == ResultState.Error)
			{
				ModelState.AddModelError("Email", emailResult.Message);
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			if(!Customer.NewEmailPassesDuplicationRules(model.Email, customer.CustomerID))
			{
				ModelState.AddModelError(
					key: "Email",
					errorMessage: AppLogic.GetString("createaccount_process.aspx.1"));
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			// Validate the password
			if(string.IsNullOrEmpty(model.Password))
			{
				ModelState.AddModelError("Password", AppLogic.GetString("checkout.enterpassword"));
				return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			switch(AccountControllerHelper.ValidateAccountPassword(customer, model.Password, model.Password)) //Intentionally passing in matching passwords
			{
				case AccountControllerHelper.PasswordValidationResult.NotStrong:
					ModelState.AddModelError("Password", AppLogic.GetString("account.aspx.69"));
					return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);

				case AccountControllerHelper.PasswordValidationResult.DoesNotMeetMinimum:
					ModelState.AddModelError("Password", AppLogic.GetString("signin.newpassword.normalRegexFailure"));
					return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);

				case AccountControllerHelper.PasswordValidationResult.Ok:
					break;

				default:
					ModelState.AddModelError("Password", AppLogic.GetString("signin.newpassword.unknownError"));
					return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			// Create the account
			var password = new Password(model.Password);
			customer.UpdateCustomer(
				isRegistered: true,
				email: model.Email.ToLowerInvariant().Trim(),
				saltedAndHashedPassword: password.SaltedPassword,
				saltKey: password.Salt,
				storeCreditCardInDb: false
			);

			// Login
			var result = AccountControllerHelper.Login(
				signedInCustomer: customer,
				profile: HttpContext.Profile,
				username: model.Email,
				password: model.Password,
				skinId: customer.SkinID);

			switch(result.State)
			{
				case AccountControllerHelper.ResultState.Error:
					NoticeProvider.PushNotice(result.Message, NoticeType.Failure);
					return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);

				case AccountControllerHelper.ResultState.PasswordChangeRequired:
					NoticeProvider.PushNotice(result.Message, NoticeType.Info);
					return RedirectToAction(
						actionName: ActionNames.ChangePassword,
						controllerName: ControllerNames.Account,
						routeValues: new
						{
							email = model.Email,
							returnUrl = Url.Action(ActionNames.Index, ControllerNames.Checkout),
						});

				case AccountControllerHelper.ResultState.Success:
					break;

				default:
					ModelState.AddModelError("Password", AppLogic.GetString("signin.newpassword.unknownError"));
					return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
			}

			var targetCustomer = new Customer(model.Email);
			var identity = ClaimsIdentityProvider.Create(targetCustomer);

			Request
				.GetOwinContext()
				.Authentication
				.SignIn(
					properties: new Microsoft.Owin.Security.AuthenticationProperties
					{
						IsPersistent = false
					},
					identities: identity);

			if(!string.IsNullOrEmpty(result.Message))
				NoticeProvider.PushNotice(result.Message, NoticeType.Info);

			if(AppConfigProvider.GetAppConfigValue<bool>("SendWelcomeEmail"))
				SendWelcomeEmailProvider.SendWelcomeEmail(targetCustomer);

			return RedirectToAction(ActionNames.Index, ControllerNames.Checkout);
		}

		Result ValidateEmail(string email)
		{
			if(!string.IsNullOrEmpty(email))
				email = email
					.ToLowerInvariant()
					.Trim();

			var emailValidator = new AspDotNetStorefrontCore.Validation.EmailAddressValidator();

			if(string.IsNullOrEmpty(email) || !emailValidator.IsValidEmailAddress(email))
			{
				return new Result(
					state: ResultState.Error,
					message: AppLogic.GetString("account.emailaddress.invalid"));
			}

			return new Result(
				state: ResultState.Success);
		}

		void SaveEmail(string email, Customer customer)
		{
			PersistedCheckoutContextProvider.SaveCheckoutContext(customer, new PersistedCheckoutContextBuilder()
				.From(PersistedCheckoutContextProvider.LoadCheckoutContext(customer))
				.WithEmail(email)
				.Build());

			var allowGuestCheckoutForEmail = Customer.NewEmailPassesDuplicationRules(
				email: email,
				customerId: customer.CustomerID);

			if(!allowGuestCheckoutForEmail)
				return;

			//At this point we have a valid guest email address so lets update the guest account with the email.
			customer.UpdateCustomer(
				email: email
			);

			return;
		}

		public class Result
		{
			public readonly ResultState State;
			public readonly string Message;

			public Result(ResultState state, string message = null)
			{
				State = state;
				Message = message;
			}
		}

		public enum ResultState
		{
			Success,
			Error,
		}
	}
}
