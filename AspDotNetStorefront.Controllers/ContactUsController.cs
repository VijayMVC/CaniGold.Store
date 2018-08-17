// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System.Web.Mvc;
using AspDotNetStorefront.Classes;
using AspDotNetStorefront.Controllers.Classes;
using AspDotNetStorefront.Filters;
using AspDotNetStorefront.Models;
using AspDotNetStorefront.Routing;
using AspDotNetStorefront.StringResource;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront.Controllers
{
	[SecureAccessFilter(forceHttps: true)]
	public class ContactUsController : Controller
	{
		readonly NoticeProvider NoticeProvider;
		readonly IStringResourceProvider StringResourceProvider;
		readonly CaptchaVerificationProvider CaptchaVerificationProvider;
		readonly ContactUsSettings Settings;
		readonly CaptchaSettings CaptchaSettings;

		public ContactUsController(NoticeProvider noticeProvider,
			IStringResourceProvider stringResourceProvider,
			CaptchaVerificationProvider captchaVerificationProvider)
		{
			NoticeProvider = noticeProvider;
			StringResourceProvider = stringResourceProvider;
			CaptchaVerificationProvider = captchaVerificationProvider;
			Settings = new ContactUsSettings();
			CaptchaSettings = new CaptchaSettings();
		}

		[HttpGet]
		[ImportModelStateFromTempData]
		public ActionResult Index()
		{
			ViewBag.MetaTitle = Settings.PageTitle;

			var model = new ContactUsRenderModel(
				pageHeader: StringResourceProvider.GetString("ContactUs.Page.Header"),
				useCaptcha: CaptchaSettings.CaptchaIsConfigured() && CaptchaSettings.RequireCaptchaOnContactForm);

			return View(model);
		}

		[HttpPost]
		[ExportModelStateToTempData]
		public ActionResult Index(ContactUsViewModel model)
		{
			var customer = HttpContext.GetCustomer();

			if(CaptchaSettings.CaptchaIsConfigured()
				&& CaptchaSettings.RequireCaptchaOnContactForm)
			{
				var captchaResult = CaptchaVerificationProvider.ValidateCaptchaResponse(Request.Form[CaptchaVerificationProvider.RecaptchaFormKey], customer.LastIPAddress);

				if(!captchaResult.Success)
				{
					NoticeProvider.PushNotice(captchaResult.Error.Message, NoticeType.Failure);

					// This error isn't actually displayed; it is just used to trigger the persisting of form data for the next page load
					ModelState.AddModelError(
						key: CaptchaVerificationProvider.RecaptchaFormKey,
						errorMessage: "Captcha Failed");

					return RedirectToAction(ActionNames.Index);
				}
			}

			AppLogic.SendMail(subject: model.Subject,
				body: GetContactTopic(model),
				useHtml: true,
				fromAddress: Settings.FromAddress,
				fromName: Settings.FromName,
				toAddress: Settings.ToAddress,
				toName: Settings.ToName,
				bccAddresses: string.Empty,
				server: Settings.MailServer);

			return RedirectToAction(ActionNames.Detail, ControllerNames.Topic, new { name = "ContactUsSuccessful" });
		}

		string GetContactTopic(ContactUsViewModel model)
		{
			return new Topic("ContactEmail")
				.ContentsRAW
				.Replace("%NAME%", model.From)
				.Replace("%EMAIL%", model.Email)
				.Replace("%PHONE%", model.Phone)
				.Replace("%SUBJECT%", model.Subject)
				.Replace("%MESSAGE%", model.Message);
		}
	}
}
